using Avalonia;
using Avalonia.Media.Imaging;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.PublicApi;

public sealed class ImageGalleryImageLeaseTests
{
    public ImageGalleryImageLeaseTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void DisposeInvokesReleaseExactlyOnce()
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(2, 2));
        var releaseCount = 0;
        var lease = ImageGalleryImageLease.Create(
            bitmap,
            new PixelSize(20, 10),
            new PixelSize(2, 2),
            16,
            () => releaseCount++);

        lease.Dispose();
        lease.Dispose();

        releaseCount.ShouldBe(1);
        Should.Throw<ObjectDisposedException>(() => _ = lease.Image);
        lease.SourcePixelSize.ShouldBe(new PixelSize(20, 10));
    }

    [Fact]
    public void InvalidMetadataIsRejectedBeforeOwnershipTransfer()
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize(2, 2));

        Should.Throw<ArgumentOutOfRangeException>(() =>
            ImageGalleryImageLease.Create(
                bitmap,
                new PixelSize(0, 10),
                new PixelSize(2, 2),
                16,
                () => { }));

        Should.Throw<ArgumentOutOfRangeException>(() =>
            ImageGalleryImageLease.Create(
                bitmap,
                new PixelSize(20, 10),
                new PixelSize(2, 2),
                0,
                () => { }));
    }
}
