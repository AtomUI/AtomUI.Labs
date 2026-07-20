namespace AtomUI.Labs.Led.Marquee;

internal static class LedMarqueeValueSanitizer
{
    public const double DefaultSpeed = 48;
    public const double MaximumSpeed = 10_000;
    public static readonly TimeSpan DefaultRepeatDelay = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan MaximumRepeatDelay = TimeSpan.FromMinutes(1);

    public static double CoerceSpeed(double value)
    {
        if (double.IsNaN(value) || value <= 0)
        {
            return 0;
        }

        return double.IsPositiveInfinity(value) ? MaximumSpeed : Math.Min(value, MaximumSpeed);
    }

    public static TimeSpan CoerceRepeatDelay(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return value > MaximumRepeatDelay ? MaximumRepeatDelay : value;
    }
}
