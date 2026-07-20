namespace AtomUI.Labs.Led.Marquee;

internal readonly record struct MarqueeMotionContext(
    double ViewportWidth,
    double ContentWidth,
    double Progress);
