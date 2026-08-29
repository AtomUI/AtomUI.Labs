using Avalonia;

namespace AtomUI.Labs.Controls.ImageGallery.Viewport;

internal static class ImageGalleryViewportMath
{
    public static double CalculateFitZoom(
        Size viewport,
        Size image,
        int rotationAngle,
        bool allowUpscaling)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0 ||
            image.Width <= 0 || image.Height <= 0)
        {
            return 1;
        }

        var rotated = GetRotatedSize(image, rotationAngle);
        var fit = Math.Min(viewport.Width / rotated.Width, viewport.Height / rotated.Height);
        return allowUpscaling ? fit : Math.Min(1, fit);
    }

    public static Size GetRotatedSize(Size image, int rotationAngle) =>
        NormalizeRotation(rotationAngle) is 90 or 270
            ? new Size(image.Height, image.Width)
            : image;

    public static Vector ClampPan(
        Vector requested,
        Size viewport,
        Size image,
        int rotationAngle,
        double zoom)
    {
        var rotated = GetRotatedSize(image, rotationAngle);
        var horizontal = Math.Max(0, (rotated.Width * zoom - viewport.Width) / 2);
        var vertical = Math.Max(0, (rotated.Height * zoom - viewport.Height) / 2);
        return new Vector(
            Math.Clamp(requested.X, -horizontal, horizontal),
            Math.Clamp(requested.Y, -vertical, vertical));
    }

    public static Vector ZoomAroundPoint(
        Vector pan,
        Point anchor,
        Size viewport,
        double oldZoom,
        double newZoom)
    {
        if (oldZoom <= 0 || !double.IsFinite(oldZoom) || !double.IsFinite(newZoom))
        {
            return pan;
        }

        var center = new Point(viewport.Width / 2, viewport.Height / 2);
        var fromCenter = anchor - center;
        var ratio = newZoom / oldZoom;
        return new Vector(
            fromCenter.X - (fromCenter.X - pan.X) * ratio,
            fromCenter.Y - (fromCenter.Y - pan.Y) * ratio);
    }

    public static int NormalizeRotation(int angle)
    {
        var normalized = angle % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }
}
