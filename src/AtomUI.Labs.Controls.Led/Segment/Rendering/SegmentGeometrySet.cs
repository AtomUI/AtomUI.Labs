namespace AtomUI.Labs.Controls.Led.Segment.Rendering;

internal sealed class SegmentGeometrySet
{
    public SegmentGeometrySet(IReadOnlyList<SegmentGeometryItem> items)
    {
        Items = items;
    }

    public IReadOnlyList<SegmentGeometryItem> Items { get; }
}
