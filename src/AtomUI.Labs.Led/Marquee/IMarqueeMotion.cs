namespace AtomUI.Labs.Led.Marquee;

internal interface IMarqueeMotion
{
    MarqueeRenderPlan Calculate(in MarqueeMotionContext context);
}
