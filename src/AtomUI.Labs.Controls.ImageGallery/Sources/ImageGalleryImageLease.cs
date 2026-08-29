using System.Threading;
using Avalonia;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.ImageGallery;

public sealed class ImageGalleryImageLease : IDisposable
{
    private IImage? _image;
    private Action? _release;

    private ImageGalleryImageLease(
        IImage image,
        PixelSize sourcePixelSize,
        PixelSize decodedPixelSize,
        long? estimatedMemorySizeBytes,
        Action release)
    {
        _image = image;
        _release = release;
        SourcePixelSize = sourcePixelSize;
        DecodedPixelSize = decodedPixelSize;
        EstimatedMemorySizeBytes = estimatedMemorySizeBytes;
    }

    public IImage Image =>
        Volatile.Read(ref _image) ??
        throw new ObjectDisposedException(nameof(ImageGalleryImageLease));

    public PixelSize SourcePixelSize { get; }

    public PixelSize DecodedPixelSize { get; }

    public long? EstimatedMemorySizeBytes { get; }

    public static ImageGalleryImageLease Create(
        IImage image,
        PixelSize sourcePixelSize,
        PixelSize decodedPixelSize,
        long? estimatedMemorySizeBytes,
        Action release)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(release);

        ValidatePixelSize(sourcePixelSize, nameof(sourcePixelSize));
        ValidatePixelSize(decodedPixelSize, nameof(decodedPixelSize));
        if (estimatedMemorySizeBytes is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(estimatedMemorySizeBytes),
                estimatedMemorySizeBytes,
                "Estimated memory size must be positive when supplied.");
        }

        return new ImageGalleryImageLease(
            image,
            sourcePixelSize,
            decodedPixelSize,
            estimatedMemorySizeBytes,
            release);
    }

    public void Dispose()
    {
        var release = Interlocked.Exchange(ref _release, null);
        Interlocked.Exchange(ref _image, null);
        release?.Invoke();
    }

    private static void ValidatePixelSize(PixelSize value, string parameterName)
    {
        if (value.Width <= 0 || value.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Pixel dimensions must both be positive.");
        }
    }
}
