using Avalonia;
using AtomUI.Labs.Controls.Led.Segment.Character;

namespace AtomUI.Labs.Controls.Led.Segment.Layout;

internal readonly record struct SegmentCharacterSlot(
    SegmentCharacterPattern Pattern,
    Rect Bounds);
