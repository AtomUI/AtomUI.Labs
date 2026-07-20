using Avalonia;
using AtomUI.Labs.Led.Segment.Character;

namespace AtomUI.Labs.Led.Segment.Layout;

internal readonly record struct SegmentCharacterSlot(
    SegmentCharacterPattern Pattern,
    Rect Bounds);
