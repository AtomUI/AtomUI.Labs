using AtomUI.Labs.Led.Matrix.Layout;
using Avalonia;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixLayoutEngineTests
{
    [Fact]
    public void Calculate_ShouldReturnPaddingOnlySizeForEmptyText()
    {
        var layout = MatrixLayoutEngine.Calculate(null, CreateOptions(padding: new Thickness(1, 2, 3, 4)));

        layout.DesiredSize.ShouldBe(new Size(4, 6));
        layout.GlyphSize.ShouldBe(new Size(38, 54));
        layout.Slots.ShouldBeEmpty();
    }

    [Fact]
    public void Calculate_ShouldUseFiveBySevenDotFormula()
    {
        var layout = MatrixLayoutEngine.Calculate("A", CreateOptions());

        layout.DesiredSize.ShouldBe(new Size(38, 54));
        layout.GlyphSize.ShouldBe(new Size(38, 54));
        layout.Slots.Count.ShouldBe(1);
        layout.Slots[0].Origin.ShouldBe(default);
    }

    [Fact]
    public void Calculate_ShouldApplyCharacterSpacingAndPadding()
    {
        var layout = MatrixLayoutEngine.Calculate(
            "Ab",
            CreateOptions(
                characterSpacing: 8,
                padding: new Thickness(2, 3, 4, 5)));

        layout.DesiredSize.ShouldBe(new Size(90, 62));
        layout.Slots[0].Origin.ShouldBe(new Point(2, 3));
        layout.Slots[1].Origin.ShouldBe(new Point(48, 3));
        layout.Slots[0].Pattern.Character.ShouldBe('A');
        layout.Slots[1].Pattern.Character.ShouldBe('B');
    }

    [Fact]
    public void Calculate_ShouldPreserveFullCellForSpaceAndFallback()
    {
        var layout = MatrixLayoutEngine.Calculate("A 中", CreateOptions());

        layout.Slots.Count.ShouldBe(3);
        layout.Slots[1].Pattern.Character.ShouldBe(' ');
        layout.Slots[2].Pattern.Character.ShouldBe('?');
        layout.Slots[1].Origin.X.ShouldBe(46);
        layout.Slots[2].Origin.X.ShouldBe(92);
        layout.DesiredSize.Width.ShouldBe(130);
    }

    [Fact]
    public void Calculate_ShouldCreateOneSlotPerUnicodeScalar()
    {
        const string text = "A😀Z";
        var layout = MatrixLayoutEngine.Calculate(text, CreateOptions());

        layout.Slots.Count.ShouldBe(3);
        layout.Slots.Select(slot => slot.Pattern.Character).ShouldBe(new[] { 'A', '?', 'Z' });
        layout.DesiredSize.ShouldBe(new Size(130, 54));
    }

    [Fact]
    public void Calculate_ShouldCreateOneFallbackSlotForUnpairedSurrogate()
    {
        var text = new string(new[] { 'A', '\uD800', 'Z' });

        var layout = MatrixLayoutEngine.Calculate(text, CreateOptions());

        layout.Slots.Count.ShouldBe(3);
        layout.Slots.Select(slot => slot.Pattern.Character).ShouldBe(new[] { 'A', '?', 'Z' });
        layout.DesiredSize.ShouldBe(new Size(130, 54));
    }

    [Fact]
    public void Calculate_ShouldCoerceInvalidValues()
    {
        var layout = MatrixLayoutEngine.Calculate(
            "AA",
            CreateOptions(
                dotSize: double.NaN,
                dotSpacing: double.PositiveInfinity,
                characterSpacing: -2,
                padding: new Thickness(double.NaN, -1, double.PositiveInfinity, 2)));

        layout.DesiredSize.ShouldBe(new Size(10, 9));
        layout.Slots[0].Origin.ShouldBe(default);
        layout.Slots[1].Origin.ShouldBe(new Point(5, 0));
    }

    private static MatrixLayoutOptions CreateOptions(
        double dotSize = 6,
        double dotSpacing = 2,
        double characterSpacing = 8,
        Thickness? padding = null)
    {
        return new MatrixLayoutOptions(
            dotSize,
            dotSpacing,
            characterSpacing,
            padding ?? default);
    }
}
