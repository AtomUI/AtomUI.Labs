using Avalonia.Media;

namespace AtomUI.Labs.Led.Segment.Rendering;

internal sealed record SegmentVisibleGeometry(
    Geometry? ActiveGeometry,
    Geometry? InactiveGeometry);
