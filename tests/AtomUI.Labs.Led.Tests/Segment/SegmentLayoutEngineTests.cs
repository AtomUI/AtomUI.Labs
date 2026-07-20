using AtomUI.Labs.Led.Segment.Character;
using AtomUI.Labs.Led.Segment.Layout;
using Avalonia;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Segment;

public class SegmentLayoutEngineTests
{
    [Fact]
    public void Calculate_ShouldReturnPaddingOnlySizeForEmptyText()
    {
        var layout = SegmentLayoutEngine.Calculate(null, CreateOptions(padding: new Thickness(1, 2, 3, 4)));

        layout.DesiredSize.ShouldBe(new Size(4, 6));
        layout.Slots.ShouldBeEmpty();
    }

    [Fact]
    public void Calculate_ShouldCreateSlotForEveryInputCharacter()
    {
        var layout = SegmentLayoutEngine.Calculate("12:45", CreateOptions());

        layout.Slots.Count.ShouldBe(5);
        layout.Slots[0].Pattern.Character.ShouldBe('1');
        layout.Slots[1].Pattern.Character.ShouldBe('2');
        layout.Slots[2].Pattern.Kind.ShouldBe(SegmentCharacterKind.Colon);
        layout.Slots[3].Pattern.Character.ShouldBe('4');
        layout.Slots[4].Pattern.Character.ShouldBe('5');
    }

    [Fact]
    public void Calculate_ShouldUseNarrowWidthForColonAndDot()
    {
        var layout = SegmentLayoutEngine.Calculate("8:8.8", CreateOptions());

        layout.Slots[0].Bounds.Width.ShouldBe(50);
        layout.Slots[1].Bounds.Width.ShouldBe(16);
        layout.Slots[2].Bounds.Width.ShouldBe(50);
        layout.Slots[3].Bounds.Width.ShouldBe(16);
        layout.Slots[4].Bounds.Width.ShouldBe(50);
    }

    [Fact]
    public void Calculate_ShouldApplyPaddingAndSpacingToDesiredSizeAndSlotPositions()
    {
        var layout = SegmentLayoutEngine.Calculate(
            "88",
            CreateOptions(
                characterHeight: 100,
                characterAspectRatio: 0.5,
                characterSpacing: 6,
                padding: new Thickness(2, 3, 4, 5)));

        layout.DesiredSize.ShouldBe(new Size(112, 108));
        layout.Slots[0].Bounds.ShouldBe(new Rect(2, 3, 50, 100));
        layout.Slots[1].Bounds.ShouldBe(new Rect(58, 3, 50, 100));
    }

    [Fact]
    public void Calculate_ShouldUseFinalHeightWhenAvailable()
    {
        var layout = SegmentLayoutEngine.Calculate(
            "8",
            CreateOptions(
                characterHeight: 40,
                characterAspectRatio: 0.5,
                padding: new Thickness(2, 3, 4, 5)),
            new Size(200, 128));

        layout.Slots[0].Bounds.Height.ShouldBe(120);
        layout.Slots[0].Bounds.Width.ShouldBe(60);
        layout.DesiredSize.ShouldBe(new Size(66, 128));
    }

    [Fact]
    public void Calculate_ShouldKeepConfiguredHeightWhenFinalHeightIsInfinity()
    {
        var layout = SegmentLayoutEngine.Calculate(
            "8",
            CreateOptions(characterHeight: 40, characterAspectRatio: 0.5),
            new Size(200, double.PositiveInfinity));

        layout.Slots[0].Bounds.Height.ShouldBe(40);
        layout.Slots[0].Bounds.Width.ShouldBe(20);
    }

    [Fact]
    public void Calculate_ShouldClampNegativeLayoutInputs()
    {
        var layout = SegmentLayoutEngine.Calculate(
            "88",
            CreateOptions(characterHeight: -10, characterAspectRatio: -1, characterSpacing: -2));

        layout.DesiredSize.ShouldBe(new Size(0, 0));
        layout.Slots[0].Bounds.ShouldBe(new Rect(0, 0, 0, 0));
        layout.Slots[1].Bounds.ShouldBe(new Rect(0, 0, 0, 0));
    }

    [Fact]
    public void Calculate_ShouldCoerceNonFiniteLayoutInputs()
    {
        var layout = SegmentLayoutEngine.Calculate(
            "88",
            CreateOptions(
                characterHeight: double.NaN,
                characterAspectRatio: double.PositiveInfinity,
                characterSpacing: double.NegativeInfinity,
                padding: new Thickness(double.NaN, -1, double.PositiveInfinity, 2)));

        layout.DesiredSize.ShouldBe(new Size(0, 2));
        layout.Slots[0].Bounds.ShouldBe(new Rect(0, 0, 0, 0));
        layout.Slots[1].Bounds.ShouldBe(new Rect(0, 0, 0, 0));
    }

    private static SegmentLayoutOptions CreateOptions(
        double characterHeight = 100,
        double characterAspectRatio = 0.5,
        double characterSpacing = 4,
        Thickness? padding = null)
    {
        return new SegmentLayoutOptions(
            characterHeight,
            characterAspectRatio,
            characterSpacing,
            padding ?? default);
    }
}
