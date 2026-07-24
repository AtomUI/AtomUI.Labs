using AtomUI.Labs.Controls.Led.Segment;
using Avalonia;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Segment;

public class SegmentDisplayMeasureTests
{
    [Theory]
    [InlineData(null, 0, 0)]
    [InlineData("", 0, 0)]
    [InlineData("88.8", 178, 100)]
    [InlineData("12:45", 232, 100)]
    public void Measure_ShouldUseSegmentLayoutEngineSemantics(string? text, double expectedWidth, double expectedHeight)
    {
        var display = CreateDisplay(text);

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(new Size(expectedWidth, expectedHeight));
    }

    [Fact]
    public void Measure_ShouldApplyPadding()
    {
        var display = CreateDisplay("88");
        display.Padding = new Thickness(2, 3, 4, 5);

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(new Size(110, 108));
    }

    [Fact]
    public void Measure_ShouldCoerceInvalidNumericInputs()
    {
        var display = CreateDisplay("88");
        display.CharacterHeight      = double.NaN;
        display.CharacterAspectRatio = double.PositiveInfinity;
        display.CharacterSpacing     = double.NegativeInfinity;
        display.Padding              = new Thickness(double.NaN, -1, double.PositiveInfinity, 2);

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(new Size(0, 2));
    }

    [Fact]
    public void Measure_ShouldKeepDesiredSizeFiniteForExtremeFiniteInputs()
    {
        var display = CreateDisplay("8888");
        display.CharacterHeight = double.MaxValue;
        display.CharacterAspectRatio = double.MaxValue;
        display.CharacterSpacing = double.MaxValue;
        display.Padding = new Thickness(double.MaxValue);

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        double.IsFinite(display.DesiredSize.Width).ShouldBeTrue();
        double.IsFinite(display.DesiredSize.Height).ShouldBeTrue();
    }

    [Fact]
    public void Measure_ShouldNotDependOnSegmentThicknessOrGap()
    {
        var display = CreateDisplay("88.8");
        display.SegmentThickness = 1;
        display.SegmentGap       = 0;
        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var desiredSize = display.DesiredSize;

        display.SegmentThickness = 200;
        display.SegmentGap       = 200;
        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(desiredSize);
    }

    private static SegmentDisplay CreateDisplay(string? text)
    {
        return new SegmentDisplay
        {
            Text                 = text,
            CharacterHeight      = 100,
            CharacterAspectRatio = 0.5,
            CharacterSpacing     = 4
        };
    }
}
