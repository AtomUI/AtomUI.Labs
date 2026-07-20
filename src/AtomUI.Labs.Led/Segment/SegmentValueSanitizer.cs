using Avalonia;

namespace AtomUI.Labs.Led.Segment;

internal static class SegmentValueSanitizer
{
    public static double CoerceNonNegative(double value)
    {
        return IsFinite(value) ? Math.Max(0, value) : 0;
    }

    public static double CoerceAtLeast(double value, double minimum)
    {
        return IsFinite(value) ? Math.Max(minimum, value) : minimum;
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

    public static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
