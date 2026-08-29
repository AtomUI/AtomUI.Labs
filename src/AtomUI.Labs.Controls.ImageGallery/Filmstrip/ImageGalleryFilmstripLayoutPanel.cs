using Avalonia;
using Avalonia.Controls;

namespace AtomUI.Labs.Controls.ImageGallery.Filmstrip;

internal enum ImageGalleryFilmstripRole
{
    Add,
    Previous,
    Items,
    Next,
}

internal sealed class ImageGalleryFilmstripLayoutPanel : Panel
{
    public static readonly AttachedProperty<ImageGalleryFilmstripRole> RoleProperty =
        AvaloniaProperty.RegisterAttached<ImageGalleryFilmstripLayoutPanel, Control, ImageGalleryFilmstripRole>(
            "Role",
            ImageGalleryFilmstripRole.Items);

    public static void SetRole(Control element, ImageGalleryFilmstripRole value) =>
        element.SetValue(RoleProperty, value);

    public static ImageGalleryFilmstripRole GetRole(Control element) =>
        element.GetValue(RoleProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
        {
            child.Measure(availableSize);
        }

        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var owner = TemplatedParent as ImageGallery;
        var horizontal = owner?.ThumbnailFilmstripPlacement is
            ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom;
        var spacing = owner?.ThumbnailItemSpacing ?? 8;
        var start = 0d;
        var end = horizontal ? finalSize.Width : finalSize.Height;

        ArrangeLeading(ImageGalleryFilmstripRole.Add);
        ArrangeLeading(ImageGalleryFilmstripRole.Previous);
        ArrangeTrailing(ImageGalleryFilmstripRole.Next);

        foreach (var child in Children.Where(child => GetRole(child) == ImageGalleryFilmstripRole.Items))
        {
            child.Arrange(horizontal
                ? new Rect(start, 0, Math.Max(0, end - start), finalSize.Height)
                : new Rect(0, start, finalSize.Width, Math.Max(0, end - start)));
        }

        return finalSize;

        void ArrangeLeading(ImageGalleryFilmstripRole role)
        {
            foreach (var child in Children.Where(child => GetRole(child) == role))
            {
                if (!child.IsVisible)
                {
                    child.Arrange(default);
                    continue;
                }

                var extent = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
                var crossExtent = horizontal
                    ? Math.Min(child.DesiredSize.Height, finalSize.Height)
                    : Math.Min(child.DesiredSize.Width, finalSize.Width);
                var crossStart = horizontal
                    ? Math.Max(0, (finalSize.Height - crossExtent) / 2)
                    : Math.Max(0, (finalSize.Width - crossExtent) / 2);
                child.Arrange(horizontal
                    ? new Rect(start, crossStart, extent, crossExtent)
                    : new Rect(crossStart, start, crossExtent, extent));
                start += extent + spacing;
            }
        }

        void ArrangeTrailing(ImageGalleryFilmstripRole role)
        {
            foreach (var child in Children.Where(child => GetRole(child) == role))
            {
                if (!child.IsVisible)
                {
                    child.Arrange(default);
                    continue;
                }

                var extent = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
                var crossExtent = horizontal
                    ? Math.Min(child.DesiredSize.Height, finalSize.Height)
                    : Math.Min(child.DesiredSize.Width, finalSize.Width);
                var crossStart = horizontal
                    ? Math.Max(0, (finalSize.Height - crossExtent) / 2)
                    : Math.Max(0, (finalSize.Width - crossExtent) / 2);
                end -= extent;
                child.Arrange(horizontal
                    ? new Rect(end, crossStart, extent, crossExtent)
                    : new Rect(crossStart, end, crossExtent, extent));
                end -= spacing;
            }
        }
    }
}
