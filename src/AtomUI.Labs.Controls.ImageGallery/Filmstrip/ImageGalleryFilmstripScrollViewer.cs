using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AtomUI.Labs.Controls.ImageGallery.Filmstrip;

internal sealed class ImageGalleryFilmstripScrollViewer : ScrollViewer
{
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        // The complete PART_Filmstrip region owns wheel routing. Do not let the
        // ScrollViewer base class consume the event before it reaches that host.
    }

    internal bool TryScrollFromWheel(Vector delta)
    {
        if (this.FindAncestorOfType<ImageGallery>() is not { } owner)
        {
            return false;
        }

        const double wheelStep = 48;
        if (owner.ThumbnailFilmstripPlacement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom)
        {
            var mainAxisDelta = delta.X != 0 ? delta.X : delta.Y;
            var maximum = Math.Max(0, Extent.Width - Viewport.Width);
            if (mainAxisDelta == 0 || !double.IsFinite(mainAxisDelta) || maximum <= 0)
            {
                return false;
            }

            Offset = new Vector(Math.Clamp(Offset.X - mainAxisDelta * wheelStep, 0, maximum), 0);
        }
        else
        {
            var mainAxisDelta = delta.Y != 0 ? delta.Y : delta.X;
            var maximum = Math.Max(0, Extent.Height - Viewport.Height);
            if (mainAxisDelta == 0 || !double.IsFinite(mainAxisDelta) || maximum <= 0)
            {
                return false;
            }

            Offset = new Vector(0, Math.Clamp(Offset.Y - mainAxisDelta * wheelStep, 0, maximum));
        }

        return true;
    }

    internal bool TryScrollIndexIntoView(int index)
    {
        if (index < 0 || this.FindAncestorOfType<ImageGallery>() is not { } owner)
        {
            return false;
        }

        var horizontal = owner.ThumbnailFilmstripPlacement is
            ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom;
        var viewport = horizontal ? Viewport.Width : Viewport.Height;
        var extent = horizontal ? Extent.Width : Extent.Height;
        if (!double.IsFinite(viewport) || !double.IsFinite(extent) || viewport <= 0 || extent <= 0)
        {
            return false;
        }

        var stride = owner.ThumbnailItemExtent + owner.ThumbnailItemSpacing;
        var itemStart = index * stride;
        var itemEnd = itemStart + owner.ThumbnailItemExtent;
        var current = horizontal ? Offset.X : Offset.Y;
        var target = current;

        if (itemStart < current)
        {
            target = itemStart;
        }
        else if (itemEnd > current + viewport)
        {
            target = itemEnd - viewport;
        }

        target = Math.Clamp(target, 0, Math.Max(0, extent - viewport));
        Offset = horizontal ? new Vector(target, 0) : new Vector(0, target);
        return true;
    }
}
