using Avalonia;

namespace AtomUI.Labs.Led.Segment;

internal static class SegmentValueSanitizer
{
    public const double MaximumLayoutValue = 1_000_000;

    public static double CoerceNonNegative(double value)
    {
        return IsFinite(value) ? Math.Clamp(value, 0, MaximumLayoutValue) : 0;
    }

    public static double CoerceAtLeast(double value, double minimum)
    {
        return IsFinite(value) ? Math.Clamp(value, minimum, MaximumLayoutValue) : minimum;
    }

    public static double CoerceRange(double value, double minimum, double maximum)
    {
        if (!IsFinite(value))
        {
            return minimum;
        }

        return Math.Clamp(value, minimum, maximum);
    }

    public static Thickness CoerceThickness(Thickness thickness)
    {
        return new Thickness(
            CoerceNonNegative(thickness.Left),
            CoerceNonNegative(thickness.Top),
            CoerceNonNegative(thickness.Right),
            CoerceNonNegative(thickness.Bottom));
    }

    public static CornerRadius CoerceCornerRadius(CornerRadius cornerRadius)
    {
        return new CornerRadius(
            CoerceNonNegative(cornerRadius.TopLeft),
            CoerceNonNegative(cornerRadius.TopRight),
            CoerceNonNegative(cornerRadius.BottomRight),
            CoerceNonNegative(cornerRadius.BottomLeft));
    }

    public static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
