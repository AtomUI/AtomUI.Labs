namespace AtomUI.Labs.Controls.Led.Glow;

internal static class LedGlowValueSanitizer
{
    public const double DefaultOpacity = 0.35;
    public const double DefaultRadius = 6;
    public const double MaximumRadius = 24;

    public static double CoerceOpacity(double value)
    {
        return double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
    }

    public static double CoerceRadius(double value)
    {
        return double.IsFinite(value) ? Math.Clamp(value, 0, MaximumRadius) : 0;
    }
}
