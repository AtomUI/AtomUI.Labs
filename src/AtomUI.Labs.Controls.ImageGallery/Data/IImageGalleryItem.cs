namespace AtomUI.Labs.Controls.ImageGallery;

public interface IImageGalleryItem
{
    object Key { get; }

    string? Title { get; }

    IImageGallerySource MainImageSource { get; }

    IImageGallerySource? ThumbnailImageSource { get; }
}
