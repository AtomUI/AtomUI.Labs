using AtomUI.Labs.Led.Segment.Character;
using Avalonia.Media;

namespace AtomUI.Labs.Led.Segment.Rendering;

internal readonly record struct SegmentGeometryItem(
    SegmentParts Part,
    Geometry Geometry);
