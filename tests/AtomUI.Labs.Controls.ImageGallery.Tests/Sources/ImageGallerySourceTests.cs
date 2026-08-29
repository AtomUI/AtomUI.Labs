using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Sources;

public sealed class ImageGallerySourceTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Exif_orientation_maps_raw_bounds_into_logical_bounds(int orientation)
    {
        const double rawWidth = 40;
        const double rawHeight = 20;
        var matrix = ExifOrientedImage.CreateRawToLogicalTransform(
            rawWidth,
            rawHeight,
            orientation);
        var corners = new[]
        {
            matrix.Transform(default),
            matrix.Transform(new Point(rawWidth, 0)),
            matrix.Transform(new Point(0, rawHeight)),
            matrix.Transform(new Point(rawWidth, rawHeight)),
        };
        var minX = corners.Min(point => point.X);
        var minY = corners.Min(point => point.Y);
        var maxX = corners.Max(point => point.X);
        var maxY = corners.Max(point => point.Y);
        var logicalWidth = orientation is >= 5 and <= 8 ? rawHeight : rawWidth;
        var logicalHeight = orientation is >= 5 and <= 8 ? rawWidth : rawHeight;

        Assert.Equal(0, minX);
        Assert.Equal(0, minY);
        Assert.Equal(logicalWidth, maxX - minX);
        Assert.Equal(logicalHeight, maxY - minY);
    }

    public ImageGallerySourceTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public async Task StreamSourceCanBeLoadedRepeatedlyAndClosesEveryStream()
    {
        var bytes = CreatePng(4, 3);
        var streams = new List<TrackingMemoryStream>();
        var source = ImageGallerySources.FromStream("image-v1", _ =>
        {
            var stream = new TrackingMemoryStream(bytes);
            streams.Add(stream);
            return ValueTask.FromResult<Stream>(stream);
        });
        var request = new ImageGalleryImageRequest(
            ImageGalleryImagePurpose.MainImage,
            null,
            ImageGalleryLoadLimits.Default);

        using var first = await source.LoadAsync(request, CancellationToken.None);
        using var second = await source.LoadAsync(request, CancellationToken.None);

        first.SourcePixelSize.ShouldBe(new PixelSize(4, 3));
        second.SourcePixelSize.ShouldBe(new PixelSize(4, 3));
        streams.Count.ShouldBe(2);
        streams.ShouldAllBe(stream => stream.IsDisposed);
    }

    [Fact]
    public async Task EncodedByteLimitStopsReadingBeforeDecode()
    {
        var bytes = CreatePng(4, 3);
        var source = ImageGallerySources.FromStream(
            "limited",
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes)));
        var limits = ImageGalleryLoadLimits.Default with { MaximumEncodedBytes = 8 };

        var exception = await Should.ThrowAsync<InvalidDataException>(async () =>
            await source.LoadAsync(
                new ImageGalleryImageRequest(ImageGalleryImagePurpose.MainImage, null, limits),
                CancellationToken.None));

        exception.Message.ShouldContain("MaximumEncodedBytes");
    }

    [Fact]
    public void HeaderParserRecognizesPngDimensions()
    {
        var bytes = CreatePng(13, 7);

        var header = ImageHeaderParser.Parse(bytes);

        header.LogicalPixelSize.ShouldBe(new PixelSize(13, 7));
        header.IsAnimated.ShouldBeFalse();
    }

    [Fact]
    public void AnimatedWebpHeaderIsMarkedAndRejected()
    {
        var bytes = new byte[30];
        "RIFF"u8.CopyTo(bytes.AsSpan(0, 4));
        "WEBP"u8.CopyTo(bytes.AsSpan(8, 4));
        "VP8X"u8.CopyTo(bytes.AsSpan(12, 4));
        bytes[16] = 10;
        bytes[20] = 0x02;
        bytes[24] = 9;
        bytes[27] = 4;

        var header = ImageHeaderParser.Parse(bytes);

        header.IsAnimated.ShouldBeTrue();
        header.LogicalPixelSize.ShouldBe(new PixelSize(10, 5));
    }

    [Fact]
    public void SourceFactoriesValidateProtocolsAtConstruction()
    {
        Should.Throw<ArgumentException>(() =>
            ImageGallerySources.FromAvaloniaResource(new Uri("https://example.com/image.png")));
        Should.Throw<ArgumentException>(() =>
            ImageGallerySources.FromHttp(new Uri("avares://Test/image.png"), new HttpClient()));
        Should.Throw<ArgumentNullException>(() =>
            ImageGallerySources.FromStream(null!, _ => ValueTask.FromResult<Stream>(Stream.Null)));
    }

    [Fact]
    public async Task AvaloniaResourceSourcesLoadAndDownsampleAllShowcaseJpegs()
    {
        AvaloniaTestApp.EnsureInitialized();
        var root = new Uri(
            "avares://AtomUI.Labs.Controls.ImageGallery.Tests/Assets/ImageGallery/");
        var resources = AssetLoader.GetAssets(root, null).OrderBy(uri => uri.AbsoluteUri).ToArray();
        resources.Length.ShouldBe(20);

        foreach (var resource in resources)
        {
            var source = ImageGallerySources.FromAvaloniaResource(resource);
            using var lease = await source.LoadAsync(
                new ImageGalleryImageRequest(
                    ImageGalleryImagePurpose.MainImage,
                    new PixelSize(320, 240),
                    ImageGalleryLoadLimits.Default),
                TestContext.Current.CancellationToken);

            lease.SourcePixelSize.Width.ShouldBeGreaterThan(0);
            lease.SourcePixelSize.Height.ShouldBeGreaterThan(0);
            lease.DecodedPixelSize.Width.ShouldBeLessThanOrEqualTo(321);
            lease.DecodedPixelSize.Height.ShouldBeLessThanOrEqualTo(241);
        }
    }

    [Fact]
    public async Task DelegateSourceDisposesLeaseThatViolatesRequestLimits()
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(2, 2));
        var releases = 0;
        var source = ImageGallerySources.Create("delegate", (_, _) =>
            ValueTask.FromResult(ImageGalleryImageLease.Create(
                bitmap,
                new PixelSize(100, 100),
                new PixelSize(100, 100),
                40_000,
                () => releases++)));
        var limits = ImageGalleryLoadLimits.Default with
        {
            MaximumDimension = 10,
            MaximumSourcePixelCount = 100
        };

        await Should.ThrowAsync<InvalidDataException>(async () =>
            await source.LoadAsync(
                new ImageGalleryImageRequest(ImageGalleryImagePurpose.MainImage, null, limits),
                CancellationToken.None));
        releases.ShouldBe(1);
    }

    private static byte[] CreatePng(int width, int height)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height));
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        return stream.ToArray();
    }

    private sealed class TrackingMemoryStream(byte[] bytes) : MemoryStream(bytes)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
