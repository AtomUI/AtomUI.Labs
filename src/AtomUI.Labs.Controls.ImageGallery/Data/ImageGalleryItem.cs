namespace AtomUI.Labs.Controls.ImageGallery;

public sealed class ImageGalleryItem : IImageGalleryItem
{
    public required object Key { get; init; }

    public string? Title { get; init; }

    public required IImageGallerySource MainImageSource { get; init; }

    public IImageGallerySource? ThumbnailImageSource { get; init; }

    public object? Data { get; init; }
}
