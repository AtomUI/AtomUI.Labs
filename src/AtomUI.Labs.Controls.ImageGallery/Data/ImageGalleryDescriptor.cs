namespace AtomUI.Labs.Controls.ImageGallery.Data;

internal sealed record ImageGalleryDescriptor(
    IImageGalleryItem Item,
    object Key,
    string? Title,
    IImageGallerySource MainImageSource,
    IImageGallerySource? ThumbnailImageSource)
{
    public object MainSourceIdentity => MainImageSource.Identity;

    public IImageGallerySource EffectiveThumbnailSource =>
        ThumbnailImageSource ?? MainImageSource;
}
