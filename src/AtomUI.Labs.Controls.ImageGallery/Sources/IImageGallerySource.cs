namespace AtomUI.Labs.Controls.ImageGallery;

public interface IImageGallerySource
{
    object Identity { get; }

    ValueTask<ImageGalleryImageLease> LoadAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken);
}
