namespace AtomUI.Labs.Led.Marquee;

internal static class LeftThroughMarqueeMotion
{
    public static MarqueeRenderPlan Calculate(in MarqueeMotionContext context)
    {
        var progress = double.IsNaN(context.Progress) ? 0 : Math.Clamp(context.Progress, 0, 1);
        var viewportWidth = CoerceDimension(context.ViewportWidth);
        var contentWidth = CoerceDimension(context.ContentWidth);
        var distance = viewportWidth + contentWidth;
        return new MarqueeRenderPlan(1, viewportWidth - distance * progress);
    }

    private static double CoerceDimension(double value)
    {
        return double.IsFinite(value) && value > 0 ? value : 0;
    }
}
