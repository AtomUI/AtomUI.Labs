using AtomUI.Labs.Controls.ImageGallery.Loading;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Loading;

public sealed class SharedImageResourceTests
{
    [Fact]
    public void OwnerDisposeIsIdempotentAndAcquiredLeasePinsResource()
    {
        var releaseCount = 0;
        var sourceLease = CreateLease(() => Interlocked.Increment(ref releaseCount));
        var resource = new SharedImageResource(sourceLease);
        var first = resource.TryAcquire();
        first.ShouldNotBeNull();

        resource.Dispose();
        resource.Dispose();
        releaseCount.ShouldBe(0);

        var second = resource.TryAcquire();
        second.ShouldNotBeNull();
        first!.Image.ShouldNotBeNull();
        first.Dispose();
        releaseCount.ShouldBe(0);
        second!.Dispose();

        releaseCount.ShouldBe(1);
        resource.TryAcquire().ShouldBeNull();
    }

    [Fact]
    public void ConcurrentAcquireAndReleaseNeverDoubleReleasesSource()
    {
        var releaseCount = 0;
        var resource = new SharedImageResource(
            CreateLease(() => Interlocked.Increment(ref releaseCount)));

        Parallel.For(0, 10_000, _ =>
        {
            using var lease = resource.TryAcquire();
            lease.ShouldNotBeNull();
            lease!.DecodedPixelSize.ShouldBe(new PixelSize(64, 32));
        });

        resource.Dispose();
        resource.Dispose();
        releaseCount.ShouldBe(1);
    }

    private static ImageGalleryImageLease CreateLease(Action release) =>
        ImageGalleryImageLease.Create(
            new TestImage(),
            new PixelSize(640, 320),
            new PixelSize(64, 32),
            64L * 32 * 4,
            release);

    private sealed class TestImage : IImage
    {
        public Size Size => new(64, 32);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }
}
