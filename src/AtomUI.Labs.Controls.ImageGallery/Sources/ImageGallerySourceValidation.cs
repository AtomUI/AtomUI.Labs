using System.Buffers.Binary;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AtomUI.Labs.Controls.ImageGallery;

internal static class ImageGallerySourceValidation
{
    private const int ReadBufferSize = 64 * 1024;

    public static void ValidateRequest(ImageGalleryImageRequest request)
    {
        if (!Enum.IsDefined(request.Purpose))
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.Purpose, "Unknown image purpose.");
        }

        if (!ImageGalleryLoadLimits.IsValid(request.Limits))
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.Limits, "Load limits are invalid.");
        }

        if (request.TargetPixelSize is { } target &&
            (target.Width <= 0 || target.Height <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(request), target, "Target pixel dimensions must be positive.");
        }
    }

    public static async ValueTask<MemoryStream> ReadBoundedAsync(
        Stream source,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        if (source.CanSeek)
        {
            var remaining = source.Length - source.Position;
            if (remaining > maximumBytes)
            {
                throw new InvalidDataException(
                    $"Encoded image length {remaining} exceeds MaximumEncodedBytes {maximumBytes}.");
            }
        }

        var capacity = source.CanSeek
            ? checked((int)Math.Min(source.Length - source.Position, 1024 * 1024))
            : ReadBufferSize;
        var destination = new MemoryStream(Math.Max(capacity, 0));
        var buffer = new byte[ReadBufferSize];
        long total = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > maximumBytes)
            {
                destination.Dispose();
                throw new InvalidDataException(
                    $"Encoded image length exceeds MaximumEncodedBytes {maximumBytes}.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        destination.Position = 0;
        return destination;
    }

    public static async ValueTask<ImageGalleryImageLease> DecodeAsync(
        MemoryStream encoded,
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!encoded.TryGetBuffer(out var segment))
            {
                throw new InvalidDataException("Unable to inspect encoded image buffer.");
            }

            var header = ImageHeaderParser.Parse(segment.AsSpan(0, checked((int)encoded.Length)));
            ValidateHeader(header, request.Limits);
            var decodeWidth = CalculateDecodeWidth(header, request.TargetPixelSize);

            encoded.Position = 0;
            var bitmap = await Task.Run(
                () => decodeWidth is { } width
                    ? Bitmap.DecodeToWidth(encoded, width)
                    : new Bitmap(encoded),
                cancellationToken).ConfigureAwait(false);

            IImage image = header.ExifOrientation == 1
                ? bitmap
                : new ExifOrientedImage(bitmap, header.ExifOrientation);
            var decodedSize = header.ExifOrientation is >= 5 and <= 8
                ? new PixelSize(bitmap.PixelSize.Height, bitmap.PixelSize.Width)
                : bitmap.PixelSize;
            var lease = ImageGalleryImageLease.Create(
                image,
                header.LogicalPixelSize,
                decodedSize,
                EstimateDecodedBytes(decodedSize),
                bitmap.Dispose);

            try
            {
                ValidateLease(lease, request.Limits);
                return lease;
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }
        finally
        {
            encoded.Dispose();
        }
    }

    public static void ValidateLease(
        ImageGalleryImageLease lease,
        ImageGalleryLoadLimits limits)
    {
        ValidatePixelSize(lease.SourcePixelSize, limits, "source");
        ValidatePixelSize(lease.DecodedPixelSize, limits, "decoded");
        var decodedBytes = lease.EstimatedMemorySizeBytes ??
                           EstimateDecodedBytes(lease.DecodedPixelSize);
        if (decodedBytes > limits.MaximumDecodedBytes)
        {
            throw new InvalidDataException(
                $"Decoded image estimate {decodedBytes} exceeds MaximumDecodedBytes {limits.MaximumDecodedBytes}.");
        }
    }

    private static void ValidateHeader(ImageHeader header, ImageGalleryLoadLimits limits)
    {
        if (header.IsAnimated)
        {
            throw new NotSupportedException("Animated images are not supported by ImageGallery.");
        }

        ValidatePixelSize(header.LogicalPixelSize, limits, "source");
    }

    private static void ValidatePixelSize(
        PixelSize value,
        ImageGalleryLoadLimits limits,
        string role)
    {
        if (value.Width <= 0 || value.Height <= 0)
        {
            throw new InvalidDataException($"The {role} image dimensions must be positive.");
        }

        if (value.Width > limits.MaximumDimension || value.Height > limits.MaximumDimension)
        {
            throw new InvalidDataException(
                $"The {role} image dimension {value} exceeds MaximumDimension {limits.MaximumDimension}.");
        }

        var pixels = checked((long)value.Width * value.Height);
        if (pixels > limits.MaximumSourcePixelCount)
        {
            throw new InvalidDataException(
                $"The {role} image pixel count {pixels} exceeds MaximumSourcePixelCount {limits.MaximumSourcePixelCount}.");
        }
    }

    private static int? CalculateDecodeWidth(ImageHeader header, PixelSize? target)
    {
        var source = header.LogicalPixelSize;
        if (target is not { } requested ||
            (source.Width <= requested.Width && source.Height <= requested.Height))
        {
            return null;
        }

        var scale = Math.Min(
            (double)requested.Width / source.Width,
            (double)requested.Height / source.Height);
        return Math.Max(1, (int)Math.Ceiling(header.RawPixelSize.Width * scale));
    }

    private static long EstimateDecodedBytes(PixelSize size) =>
        checked((long)size.Width * size.Height * 4);
}

internal readonly record struct ImageHeader(
    PixelSize RawPixelSize,
    PixelSize LogicalPixelSize,
    bool IsAnimated,
    int ExifOrientation);

internal static class ImageHeaderParser
{
    private static ReadOnlySpan<byte> PngSignature =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static ImageHeader Parse(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith(PngSignature))
        {
            return ParsePng(data);
        }

        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
        {
            return ParseJpeg(data);
        }

        if (data.Length >= 26 && data[0] == (byte)'B' && data[1] == (byte)'M')
        {
            return ParseBmp(data);
        }

        if (data.Length >= 20 &&
            data[..4].SequenceEqual("RIFF"u8) &&
            data.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return ParseWebp(data);
        }

        throw new NotSupportedException("The encoded image format is not supported.");
    }

    private static ImageHeader ParsePng(ReadOnlySpan<byte> data)
    {
        if (data.Length < 33 || !data.Slice(12, 4).SequenceEqual("IHDR"u8))
        {
            throw new InvalidDataException("The PNG header is incomplete.");
        }

        var width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(16, 4)));
        var height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(20, 4)));
        var animated = false;
        var offset = 8;
        while (offset + 12 <= data.Length)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4)));
            if (offset + 12L + length > data.Length)
            {
                throw new InvalidDataException("A PNG chunk extends beyond the encoded data.");
            }

            var type = data.Slice(offset + 4, 4);
            if (type.SequenceEqual("acTL"u8))
            {
                animated = true;
            }

            offset += 12 + length;
            if (type.SequenceEqual("IEND"u8))
            {
                break;
            }
        }

        var size = new PixelSize(width, height);
        return new ImageHeader(size, size, animated, 1);
    }

    private static ImageHeader ParseBmp(ReadOnlySpan<byte> data)
    {
        var dibSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(14, 4));
        if (dibSize < 40 || data.Length < 54)
        {
            throw new NotSupportedException("Only Windows BMP information headers are supported.");
        }

        var width = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(18, 4));
        var height = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(22, 4));
        if (width <= 0 || height == 0 || height == int.MinValue)
        {
            throw new InvalidDataException("The BMP dimensions are invalid.");
        }

        var size = new PixelSize(width, Math.Abs(height));
        return new ImageHeader(size, size, false, 1);
    }

    private static ImageHeader ParseWebp(ReadOnlySpan<byte> data)
    {
        var chunk = data.Slice(12, 4);
        var payload = data.Slice(20);
        int width;
        int height;
        var animated = false;

        if (chunk.SequenceEqual("VP8X"u8))
        {
            if (payload.Length < 10)
            {
                throw new InvalidDataException("The WebP VP8X header is incomplete.");
            }

            animated = (payload[0] & 0x02) != 0;
            width = 1 + ReadUInt24LittleEndian(payload.Slice(4, 3));
            height = 1 + ReadUInt24LittleEndian(payload.Slice(7, 3));
        }
        else if (chunk.SequenceEqual("VP8 "u8))
        {
            if (payload.Length < 10 ||
                payload[3] != 0x9D || payload[4] != 0x01 || payload[5] != 0x2A)
            {
                throw new InvalidDataException("The WebP VP8 header is invalid.");
            }

            width = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(6, 2)) & 0x3FFF;
            height = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(8, 2)) & 0x3FFF;
        }
        else if (chunk.SequenceEqual("VP8L"u8))
        {
            if (payload.Length < 5 || payload[0] != 0x2F)
            {
                throw new InvalidDataException("The WebP VP8L header is invalid.");
            }

            width = 1 + (payload[1] | ((payload[2] & 0x3F) << 8));
            height = 1 + ((payload[2] >> 6) | (payload[3] << 2) | ((payload[4] & 0x0F) << 10));
        }
        else
        {
            throw new NotSupportedException("The WebP encoding variant is not supported.");
        }

        var size = new PixelSize(width, height);
        return new ImageHeader(size, size, animated, 1);
    }

    private static ImageHeader ParseJpeg(ReadOnlySpan<byte> data)
    {
        var offset = 2;
        var orientation = 1;
        PixelSize? rawSize = null;

        while (offset + 4 <= data.Length)
        {
            while (offset < data.Length && data[offset] == 0xFF)
            {
                offset++;
            }

            if (offset >= data.Length)
            {
                break;
            }

            var marker = data[offset++];
            if (marker == 0xD9)
            {
                break;
            }

            if (marker == 0xDA)
            {
                // Start Of Scan terminates the metadata/header region. Bytes after
                // this marker are entropy-coded image data and must not be parsed
                // as length-prefixed JPEG segments.
                break;
            }

            if (marker is 0x01 or 0xD8 || marker is >= 0xD0 and <= 0xD7)
            {
                continue;
            }

            if (offset + 2 > data.Length)
            {
                break;
            }

            var length = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            if (length < 2 || offset + length > data.Length)
            {
                throw new InvalidDataException("A JPEG segment extends beyond the encoded data.");
            }

            var payload = data.Slice(offset + 2, length - 2);
            if (marker == 0xE1)
            {
                orientation = TryReadExifOrientation(payload) ?? orientation;
            }
            else if (IsStartOfFrame(marker))
            {
                if (payload.Length < 5)
                {
                    throw new InvalidDataException("The JPEG frame header is incomplete.");
                }

                var height = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(1, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(3, 2));
                rawSize = new PixelSize(width, height);
            }

            offset += length;
            if (rawSize is not null && orientation != 1)
            {
                break;
            }
        }

        if (rawSize is not { } size)
        {
            throw new InvalidDataException("The JPEG dimensions could not be read.");
        }

        var logical = orientation is >= 5 and <= 8
            ? new PixelSize(size.Height, size.Width)
            : size;
        return new ImageHeader(size, logical, false, orientation);
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker is >= 0xC0 and <= 0xCF &&
        marker is not 0xC4 and not 0xC8 and not 0xCC;

    private static int? TryReadExifOrientation(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 14 || !payload[..6].SequenceEqual("Exif\0\0"u8))
        {
            return null;
        }

        var tiff = payload[6..];
        var littleEndian = tiff[..2].SequenceEqual("II"u8);
        if (!littleEndian && !tiff[..2].SequenceEqual("MM"u8))
        {
            return null;
        }

        uint ReadUInt32(ReadOnlySpan<byte> value) =>
            littleEndian
                ? BinaryPrimitives.ReadUInt32LittleEndian(value)
                : BinaryPrimitives.ReadUInt32BigEndian(value);
        ushort ReadUInt16(ReadOnlySpan<byte> value) =>
            littleEndian
                ? BinaryPrimitives.ReadUInt16LittleEndian(value)
                : BinaryPrimitives.ReadUInt16BigEndian(value);

        var ifdOffset = checked((int)ReadUInt32(tiff.Slice(4, 4)));
        if (ifdOffset < 0 || ifdOffset + 2 > tiff.Length)
        {
            return null;
        }

        var count = ReadUInt16(tiff.Slice(ifdOffset, 2));
        var entryOffset = ifdOffset + 2;
        for (var index = 0; index < count; index++)
        {
            var offset = entryOffset + index * 12;
            if (offset + 12 > tiff.Length)
            {
                return null;
            }

            if (ReadUInt16(tiff.Slice(offset, 2)) == 0x0112)
            {
                var orientation = ReadUInt16(tiff.Slice(offset + 8, 2));
                return orientation is >= 1 and <= 8 ? orientation : 1;
            }
        }

        return null;
    }

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> value) =>
        value[0] | (value[1] << 8) | (value[2] << 16);
}
