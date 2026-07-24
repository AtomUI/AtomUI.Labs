namespace AtomUI.Labs.Controls.Led.Marquee;

internal readonly record struct MarqueeRenderPlan(
    int PlacementCount,
    double FirstX,
    double SecondX = 0)
{
    public double GetX(int index)
    {
        return index switch
        {
            0 when PlacementCount > 0 => FirstX,
            1 when PlacementCount > 1 => SecondX,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }
}
