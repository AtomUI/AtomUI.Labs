using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AtomUI.Labs.Controls.ImageGallery.Filmstrip;

internal sealed class ImageGalleryVirtualizingPanel : VirtualizingStackPanel
{
    public ImageGalleryVirtualizingPanel()
    {
        CacheLength = 0.5;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (this.FindAncestorOfType<ImageGallery>() is { } owner)
        {
            Orientation = owner.ThumbnailFilmstripPlacement is
                ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom
                ? Orientation.Horizontal
                : Orientation.Vertical;
            foreach (var child in Children.OfType<ImageGalleryThumbnailItem>())
            {
                child.ApplySlotMetrics();
            }
        }

        return base.MeasureOverride(availableSize);
    }
}
