using AtomUI.Labs.Led.Marquee;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixMarqueeMotionTests
{
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
        var plan = LeftThroughMarqueeMotion.Calculate(new MarqueeMotionContext(200, 100, progress));

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
        LeftThroughMarqueeMotion.Calculate(new MarqueeMotionContext(200, 100, progress)).FirstX.ShouldBe(expectedX);
    }

    [Fact]
    public void RenderPlan_ShouldRejectUnavailablePlacement()
    {
        var plan = new MarqueeRenderPlan(1, 10);

        Should.Throw<ArgumentOutOfRangeException>(() => plan.GetX(1));
    }

    [Theory]
    [InlineData(double.NaN, 100, -50)]
    [InlineData(double.PositiveInfinity, 100, -50)]
    [InlineData(double.NegativeInfinity, 100, -50)]
    [InlineData(200, double.NaN, 100)]
    [InlineData(200, double.PositiveInfinity, 100)]
    [InlineData(200, double.NegativeInfinity, 100)]
    public void LeftThrough_ShouldCoerceNonFiniteDimensions(
        double viewportWidth,
        double contentWidth,
        double expectedX)
    {
        var plan = LeftThroughMarqueeMotion.Calculate(new MarqueeMotionContext(viewportWidth, contentWidth, 0.5));

        double.IsFinite(plan.FirstX).ShouldBeTrue();
        plan.FirstX.ShouldBe(expectedX);
    }
}
