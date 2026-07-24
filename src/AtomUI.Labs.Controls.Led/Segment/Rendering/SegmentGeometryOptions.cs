namespace AtomUI.Labs.Controls.Led.Segment.Rendering;

internal readonly record struct SegmentGeometryOptions(
    double Thickness,
    double Gap,
    double BevelRatio = 0.5,
    double DotScale = 0.72);
