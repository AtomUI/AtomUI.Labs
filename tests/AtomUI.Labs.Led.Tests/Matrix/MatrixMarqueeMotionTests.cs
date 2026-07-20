using AtomUI.Labs.Led.Marquee;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixMarqueeMotionTests
{
    private readonly LeftThroughMarqueeMotion _motion = new();

    [Theory]
    [InlineData(0, 200)]
    [InlineData(0.25, 125)]
    [InlineData(0.5, 50)]
    [InlineData(0.75, -25)]
    [InlineData(1, -100)]
    public void LeftThrough_ShouldMoveFromViewportRightToContentLeft(
        double progress,
        double expectedX)
    {
        var plan = _motion.Calculate(new MarqueeMotionContext(200, 100, progress));

        plan.PlacementCount.ShouldBe(1);
        plan.GetX(0).ShouldBe(expectedX);
    }

    [Theory]
    [InlineData(-1, 200)]
    [InlineData(double.NaN, 200)]
    [InlineData(double.NegativeInfinity, 200)]
    [InlineData(2, -100)]
    [InlineData(double.PositiveInfinity, -100)]
    public void LeftThrough_ShouldClampProgress(double progress, double expectedX)
    {
        _motion.Calculate(new MarqueeMotionContext(200, 100, progress)).FirstX.ShouldBe(expectedX);
    }

    [Fact]
    public void RenderPlan_ShouldRejectUnavailablePlacement()
    {
        var plan = new MarqueeRenderPlan(1, 10);

        Should.Throw<ArgumentOutOfRangeException>(() => plan.GetX(1));
    }
}
