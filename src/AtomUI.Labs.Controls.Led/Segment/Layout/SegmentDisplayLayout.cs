using Avalonia;

namespace AtomUI.Labs.Controls.Led.Segment.Layout;

internal sealed class SegmentDisplayLayout
{
    public SegmentDisplayLayout(Size desiredSize, IReadOnlyList<SegmentCharacterSlot> slots)
    {
        DesiredSize = desiredSize;
        Slots       = slots;
    }

    public Size DesiredSize { get; }

    public IReadOnlyList<SegmentCharacterSlot> Slots { get; }
}
