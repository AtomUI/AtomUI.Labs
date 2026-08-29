using Avalonia;
using Avalonia.Controls;

namespace AtomUI.Labs.Controls.ImageGallery.Layout;

internal enum ImageGalleryOverlayRole
{
    Viewport,
    PreviousViewportNavigation,
    NextViewportNavigation,
    Toolbar,
    Filmstrip,
}

internal sealed class ImageGalleryOverlayPanel : Panel
{
    public static readonly AttachedProperty<ImageGalleryOverlayRole> RoleProperty =
        AvaloniaProperty.RegisterAttached<ImageGalleryOverlayPanel, Control, ImageGalleryOverlayRole>(
            "Role",
            ImageGalleryOverlayRole.Viewport);

    public static void SetRole(Control element, ImageGalleryOverlayRole value) =>
        element.SetValue(RoleProperty, value);

    public static ImageGalleryOverlayRole GetRole(Control element) =>
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
        if (owner is null)
        {
            foreach (var child in Children)
            {
                child.Arrange(new Rect(finalSize));
            }

            return finalSize;
        }

        var fullBounds = new Rect(finalSize);
        var filmstripBounds = CalculateFilmstripBounds(owner, finalSize);
        var filmstripMinimum = CalculateFilmstripMinimum(owner);
        var filmstripAxis = owner.ThumbnailFilmstripPlacement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom
            ? filmstripBounds.Width
            : filmstripBounds.Height;
        var filmstripAvailable = owner.IsThumbnailFilmstripVisible && filmstripAxis >= filmstripMinimum;
        var responsive = filmstripAvailable
            ? ImageGalleryResponsiveState.Normal
            : ImageGalleryResponsiveState.Minimal;

        var toolbarChild = Children.FirstOrDefault(child =>
            GetRole(child) == ImageGalleryOverlayRole.Toolbar);
        var toolbarBounds = default(Rect);
        if (responsive != ImageGalleryResponsiveState.Minimal && toolbarChild is not null)
        {
            var safe = CalculateToolbarSafeBounds(owner, finalSize, filmstripBounds);
            toolbarBounds = PlaceAlongEdge(
                owner.EffectiveToolbarPlacement,
                owner.ToolbarAlongEdgeAlignment,
                toolbarChild.DesiredSize,
                safe,
                owner.ToolbarEdgeGap);
            if (toolbarBounds.Width + 0.01 < Math.Min(toolbarChild.DesiredSize.Width, safe.Width) ||
                toolbarBounds.Height + 0.01 < Math.Min(toolbarChild.DesiredSize.Height, safe.Height))
            {
                responsive = ImageGalleryResponsiveState.Compact;
            }
        }

        owner.SetResponsiveState(responsive);
        var filmstripOccupiesSide = filmstripAvailable && owner.IsEffectiveFilmstripVisible;
        var toolbarOccupiesSide = responsive == ImageGalleryResponsiveState.Normal &&
                                  owner.IsEffectiveToolbarVisible &&
                                  toolbarBounds.Width > 0 &&
                                  toolbarBounds.Height > 0;
        var leftOccupied =
            (filmstripOccupiesSide && owner.ThumbnailFilmstripPlacement == ImageGalleryEdgePlacement.Left) ||
            (toolbarOccupiesSide && owner.EffectiveToolbarPlacement == ImageGalleryEdgePlacement.Left);
        var rightOccupied =
            (filmstripOccupiesSide && owner.ThumbnailFilmstripPlacement == ImageGalleryEdgePlacement.Right) ||
            (toolbarOccupiesSide && owner.EffectiveToolbarPlacement == ImageGalleryEdgePlacement.Right);
        owner.SetEffectiveViewportNavigationVisibility(!leftOccupied, !rightOccupied);

        foreach (var child in Children)
        {
            var bounds = GetRole(child) switch
            {
                ImageGalleryOverlayRole.Viewport => fullBounds,
                ImageGalleryOverlayRole.PreviousViewportNavigation =>
                    PlaceViewportNavigation(child.DesiredSize, finalSize, previous: true),
                ImageGalleryOverlayRole.NextViewportNavigation =>
                    PlaceViewportNavigation(child.DesiredSize, finalSize, previous: false),
                ImageGalleryOverlayRole.Toolbar when responsive == ImageGalleryResponsiveState.Normal => toolbarBounds,
                ImageGalleryOverlayRole.Filmstrip when filmstripAvailable => filmstripBounds,
                _ => default,
            };
            child.Arrange(bounds);
        }

        return finalSize;
    }

    private static Rect CalculateFilmstripBounds(ImageGallery owner, Size finalSize)
    {
        var gap = owner.ThumbnailFilmstripEdgeGap;
        var inset = owner.ThumbnailFilmstripAlongEdgeInset;
        var extent = owner.ThumbnailFilmstripExtent;
        return owner.ThumbnailFilmstripPlacement switch
        {
            ImageGalleryEdgePlacement.Top => new Rect(
                inset,
                gap,
                Math.Max(0, finalSize.Width - inset * 2),
                Math.Min(extent, Math.Max(0, finalSize.Height - gap))),
            ImageGalleryEdgePlacement.Bottom => new Rect(
                inset,
                Math.Max(0, finalSize.Height - gap - extent),
                Math.Max(0, finalSize.Width - inset * 2),
                Math.Min(extent, Math.Max(0, finalSize.Height - gap))),
            ImageGalleryEdgePlacement.Left => new Rect(
                gap,
                inset,
                Math.Min(extent, Math.Max(0, finalSize.Width - gap)),
                Math.Max(0, finalSize.Height - inset * 2)),
            _ => new Rect(
                Math.Max(0, finalSize.Width - gap - extent),
                inset,
                Math.Min(extent, Math.Max(0, finalSize.Width - gap)),
                Math.Max(0, finalSize.Height - inset * 2)),
        };
    }

    private static double CalculateFilmstripMinimum(ImageGallery owner)
    {
        const double navigationButtons = 72;
        var addButton = owner.IsAddImageButtonVisible ? 36 : 0;
        return owner.ThumbnailItemExtent + owner.ThumbnailItemSpacing * 2 + navigationButtons + addButton;
    }

    private static Rect CalculateToolbarSafeBounds(
        ImageGallery owner,
        Size finalSize,
        Rect filmstripBounds)
    {
        var inset = owner.ToolbarAlongEdgeInset;
        var safe = new Rect(
            inset,
            inset,
            Math.Max(0, finalSize.Width - inset * 2),
            Math.Max(0, finalSize.Height - inset * 2));
        if (!owner.IsThumbnailFilmstripVisible)
        {
            return safe;
        }

        var toolbarHorizontal = owner.EffectiveToolbarPlacement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom;
        var filmstripHorizontal = owner.ThumbnailFilmstripPlacement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom;
        if (toolbarHorizontal == filmstripHorizontal)
        {
            return safe;
        }

        var collisionGap = owner.OverlayElementGap;
        return owner.ThumbnailFilmstripPlacement switch
        {
            ImageGalleryEdgePlacement.Left => new Rect(
                Math.Max(safe.X, filmstripBounds.Right + collisionGap),
                safe.Y,
                Math.Max(0, safe.Right - Math.Max(safe.X, filmstripBounds.Right + collisionGap)),
                safe.Height),
            ImageGalleryEdgePlacement.Right => new Rect(
                safe.X,
                safe.Y,
                Math.Max(0, Math.Min(safe.Right, filmstripBounds.Left - collisionGap) - safe.X),
                safe.Height),
            ImageGalleryEdgePlacement.Top => new Rect(
                safe.X,
                Math.Max(safe.Y, filmstripBounds.Bottom + collisionGap),
                safe.Width,
                Math.Max(0, safe.Bottom - Math.Max(safe.Y, filmstripBounds.Bottom + collisionGap))),
            _ => new Rect(
                safe.X,
                safe.Y,
                safe.Width,
                Math.Max(0, Math.Min(safe.Bottom, filmstripBounds.Top - collisionGap) - safe.Y)),
        };
    }

    private static Rect PlaceAlongEdge(
        ImageGalleryEdgePlacement placement,
        ImageGalleryEdgeAlignment alignment,
        Size desired,
        Rect safe,
        double edgeGap)
    {
        var horizontal = placement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom;
        var width = alignment == ImageGalleryEdgeAlignment.Stretch && horizontal
            ? safe.Width
            : Math.Min(desired.Width, safe.Width);
        var height = alignment == ImageGalleryEdgeAlignment.Stretch && !horizontal
            ? safe.Height
            : Math.Min(desired.Height, safe.Height);
        double x;
        double y;
        if (horizontal)
        {
            x = Align(safe.X, safe.Width, width, alignment);
            y = placement == ImageGalleryEdgePlacement.Top
                ? Math.Max(safe.Y, edgeGap)
                : Math.Min(safe.Bottom - height, safe.Bottom - edgeGap - height);
        }
        else
        {
            x = placement == ImageGalleryEdgePlacement.Left
                ? Math.Max(safe.X, edgeGap)
                : Math.Min(safe.Right - width, safe.Right - edgeGap - width);
            y = Align(safe.Y, safe.Height, height, alignment);
        }

        return new Rect(Math.Max(safe.X, x), Math.Max(safe.Y, y), width, height);
    }

    private static double Align(double start, double available, double extent, ImageGalleryEdgeAlignment alignment) =>
        alignment switch
        {
            ImageGalleryEdgeAlignment.End => start + available - extent,
            ImageGalleryEdgeAlignment.Center => start + (available - extent) / 2,
            _ => start,
        };

    private static Rect PlaceViewportNavigation(Size desired, Size finalSize, bool previous)
    {
        var width = Math.Max(32, desired.Width);
        var height = Math.Max(32, desired.Height);
        const double inset = 16;
        return new Rect(
            previous ? inset : Math.Max(inset, finalSize.Width - inset - width),
            Math.Max(0, (finalSize.Height - height) / 2),
            Math.Min(width, finalSize.Width),
            Math.Min(height, finalSize.Height));
    }
}
