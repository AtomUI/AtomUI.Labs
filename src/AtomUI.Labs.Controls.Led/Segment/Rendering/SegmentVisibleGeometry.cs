using Avalonia.Media;

namespace AtomUI.Labs.Controls.Led.Segment.Rendering;

internal sealed record SegmentVisibleGeometry(
    Geometry? ActiveGeometry,
    Geometry? InactiveGeometry);
