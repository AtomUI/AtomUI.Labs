using Avalonia;
using Avalonia.Layout;

namespace AtomUI.Labs.Led;

internal static class LedDisplayLayoutMath
{
    public static double CalculateScaleDown(Size desiredSize, Size bounds)
    {
        if (!IsPositiveFinite(desiredSize.Width)
            || !IsPositiveFinite(desiredSize.Height)
            || !IsPositiveFinite(bounds.Width)
            || !IsPositiveFinite(bounds.Height))
        {
            return 1;
        }

        var scaleX = bounds.Width / desiredSize.Width;
        var scaleY = bounds.Height / desiredSize.Height;
        return Math.Min(1, Math.Min(scaleX, scaleY));
    }

    public static Vector CalculateAlignmentOffset(
        Size desiredSize,
        Size bounds,
        double scale,
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment)
    {
        var horizontalExtra = Math.Max(0, bounds.Width - desiredSize.Width * scale);
        var verticalExtra = Math.Max(0, bounds.Height - desiredSize.Height * scale);
        var x = horizontalAlignment switch
        {
            HorizontalAlignment.Center  => horizontalExtra / 2,
            HorizontalAlignment.Right   => horizontalExtra,
            HorizontalAlignment.Stretch => horizontalExtra / 2,
            _                           => 0
        };
        var y = verticalAlignment switch
        {
            VerticalAlignment.Center  => verticalExtra / 2,
            VerticalAlignment.Bottom  => verticalExtra,
            VerticalAlignment.Stretch => verticalExtra / 2,
            _                         => 0
        };
        return new Vector(x, y);
    }

    private static bool IsPositiveFinite(double value)
    {
        return double.IsFinite(value) && value > 0;
    }
}
