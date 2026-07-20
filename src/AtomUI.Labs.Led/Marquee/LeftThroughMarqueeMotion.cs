namespace AtomUI.Labs.Led.Marquee;

internal sealed class LeftThroughMarqueeMotion : IMarqueeMotion
{
    public MarqueeRenderPlan Calculate(in MarqueeMotionContext context)
    {
        var progress = double.IsNaN(context.Progress) ? 0 : Math.Clamp(context.Progress, 0, 1);
        var distance = Math.Max(0, context.ViewportWidth) + Math.Max(0, context.ContentWidth);
        return new MarqueeRenderPlan(1, Math.Max(0, context.ViewportWidth) - distance * progress);
    }
}
