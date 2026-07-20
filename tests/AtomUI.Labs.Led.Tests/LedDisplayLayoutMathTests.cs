using AtomUI.Labs.Led;
using Avalonia;
using Avalonia.Layout;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests;

public class LedDisplayLayoutMathTests
{
    [Theory]
    [InlineData(200, 100, 100, 80, 0.5)]
    [InlineData(100, 200, 80, 100, 0.5)]
    [InlineData(100, 50, 200, 100, 1)]
    [InlineData(100, 100, 25, 40, 0.25)]
    public void CalculateScaleDown_ShouldFitWithoutScalingUp(
        double desiredWidth,
        double desiredHeight,
        double boundsWidth,
        double boundsHeight,
        double expected)
    {
        var scale = LedDisplayLayoutMath.CalculateScaleDown(
            new Size(desiredWidth, desiredHeight),
            new Size(boundsWidth, boundsHeight));

        scale.ShouldBe(expected);
    }

    [Theory]
    [InlineData(0, 100, 100, 100)]
    [InlineData(100, 0, 100, 100)]
    [InlineData(100, 100, 0, 100)]
    [InlineData(100, 100, 100, 0)]
    [InlineData(double.NaN, 100, 100, 100)]
    [InlineData(100, double.PositiveInfinity, 100, 100)]
    [InlineData(100, 100, double.NegativeInfinity, 100)]
    public void CalculateScaleDown_ShouldUseIdentityForInvalidDimensions(
        double desiredWidth,
        double desiredHeight,
        double boundsWidth,
        double boundsHeight)
    {
        var scale = LedDisplayLayoutMath.CalculateScaleDown(
            new Size(desiredWidth, desiredHeight),
            new Size(boundsWidth, boundsHeight));

        scale.ShouldBe(1);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left, VerticalAlignment.Top, 0, 0)]
    [InlineData(HorizontalAlignment.Center, VerticalAlignment.Center, 75, 40)]
    [InlineData(HorizontalAlignment.Right, VerticalAlignment.Bottom, 150, 80)]
    [InlineData(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 75, 40)]
    public void CalculateAlignmentOffset_ShouldApplyAlignmentToScaledContent(
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        double expectedX,
        double expectedY)
    {
        var offset = LedDisplayLayoutMath.CalculateAlignmentOffset(
            new Size(100, 40),
            new Size(200, 100),
            0.5,
            horizontalAlignment,
            verticalAlignment);

        offset.ShouldBe(new Vector(expectedX, expectedY));
    }

    [Fact]
    public void CalculateAlignmentOffset_ShouldNotProduceNegativeExtraSpace()
    {
        var offset = LedDisplayLayoutMath.CalculateAlignmentOffset(
            new Size(200, 100),
            new Size(100, 50),
            1,
            HorizontalAlignment.Right,
            VerticalAlignment.Bottom);

        offset.ShouldBe(default);
    }
}
