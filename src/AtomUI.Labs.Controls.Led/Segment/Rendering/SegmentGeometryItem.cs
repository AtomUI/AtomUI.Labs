using AtomUI.Labs.Controls.Led.Segment.Character;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.Led.Segment.Rendering;

internal readonly record struct SegmentGeometryItem(
    SegmentParts Part,
    Geometry Geometry);
