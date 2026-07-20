using AtomUI.Labs.Led.Matrix;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixDisplayMeasureTests
{
    [Theory]
    [InlineData(null, 0, 0)]
    [InlineData("", 0, 0)]
    [InlineData("A", 38, 54)]
    [InlineData("AB", 84, 54)]
    public void Measure_ShouldUseMatrixLayoutSemantics(string? text, double expectedWidth, double expectedHeight)
    {
        var display = new MatrixDisplay { Text = text };

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(new Size(expectedWidth, expectedHeight));
    }

    [Fact]
    public void Measure_ShouldApplyPaddingAndCoerceInvalidValues()
    {
        var display = new MatrixDisplay
        {
            Text             = "AA",
            DotSize          = double.NaN,
            DotSpacing       = double.PositiveInfinity,
            CharacterSpacing = -1,
            Padding          = new Thickness(double.NaN, -1, double.PositiveInfinity, 2)
        };

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(new Size(10, 9));
    }

    [Fact]
    public void Measure_ShouldKeepFiniteSizeForExtremelyLargeFiniteValues()
    {
        var display = new MatrixDisplay
        {
            Text             = "AA",
            DotSize          = double.MaxValue,
            DotSpacing       = double.MaxValue,
            CharacterSpacing = double.MaxValue,
            Padding          = new Thickness(double.MaxValue)
        };

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(new Size(21_000_000, 15_000_000));
        MatrixValueSanitizer.IsFinite(display.DesiredSize.Width).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(display.DesiredSize.Height).ShouldBeTrue();
        display.DotSize.ShouldBe(double.MaxValue);
    }

    [Fact]
    public void DotShapeChanges_ShouldNotChangeDesiredSize()
    {
        var display = new MatrixDisplay { Text = "MATRIX" };
        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var desiredSize = display.DesiredSize;

        display.DotShape = MatrixDotShape.RoundedSquare;
        display.DotCornerRadiusRatio = 0.4;
        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(desiredSize);
    }

    [Fact]
    public void Measure_ShouldAddEachBorderSideIndependentlyOfBrush()
    {
        var display = new MatrixDisplay
        {
            Text = "A",
            BorderBrush = null,
            BorderThickness = new Thickness(1, 2, 3, 4)
        };

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(new Size(42, 60));
    }

    [Fact]
    public void Measure_ShouldCoerceInvalidBorderThicknessWithoutChangingRawProperty()
    {
        var thickness = new Thickness(double.NaN, -1, double.PositiveInfinity, 2);
        var display = new MatrixDisplay
        {
            Text = "A",
            BorderThickness = thickness
        };

        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(new Size(38, 56));
        double.IsNaN(display.BorderThickness.Left).ShouldBeTrue();
        display.BorderThickness.Top.ShouldBe(-1);
        double.IsPositiveInfinity(display.BorderThickness.Right).ShouldBeTrue();
        display.BorderThickness.Bottom.ShouldBe(2);
    }

    [Fact]
    public void BorderBrushChanges_ShouldNotChangeDesiredSize()
    {
        var display = new MatrixDisplay
        {
            Text = "A",
            BorderThickness = new Thickness(2)
        };
        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var desiredSize = display.DesiredSize;

        display.BorderBrush = Brushes.Blue;
        display.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        display.DesiredSize.ShouldBe(desiredSize);
    }
}
