using System.Numerics;
using AtomUI.Labs.Led.Matrix;
using AtomUI.Labs.Led.Matrix.Character;
using AtomUI.Labs.Led.Matrix.Rendering;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixGlyphGeometryFactoryTests
{
    static MatrixGlyphGeometryFactoryTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData(MatrixDotShape.Circle)]
    [InlineData(MatrixDotShape.Square)]
    [InlineData(MatrixDotShape.RoundedSquare)]
    public void Create_ShouldPartitionEveryGlyphIntoExactlyThirtyFiveDots(MatrixDotShape dotShape)
    {
        foreach (var character in MatrixFiveBySevenGlyphMap.SupportedCharacters)
        {
            MatrixFiveBySevenGlyphMap.TryGetGlyph(character, out var glyph).ShouldBeTrue();

            var geometry = MatrixGlyphGeometryFactory.Create(glyph, 6, 2, dotShape, 0.25);
            var expectedActiveCount = BitOperations.PopCount(glyph.Bits);

            geometry.ActiveDotCount.ShouldBe(expectedActiveCount);
            geometry.InactiveDotCount.ShouldBe(MatrixGlyph.Width * MatrixGlyph.Height - expectedActiveCount);
            (geometry.ActiveDotCount + geometry.InactiveDotCount).ShouldBe(35);
        }
    }

    [Fact]
    public void Create_ShouldKeepGeometryBoundsFiniteForSanitizedValues()
    {
        MatrixFiveBySevenGlyphMap.TryGetGlyph('8', out var glyph).ShouldBeTrue();

        var geometry = MatrixGlyphGeometryFactory.Create(
            glyph,
            double.MaxValue,
            double.PositiveInfinity,
            MatrixDotShape.RoundedSquare,
            double.NaN);

        MatrixValueSanitizer.IsFinite(geometry.ActiveGeometry.Bounds.X).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.ActiveGeometry.Bounds.Y).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.ActiveGeometry.Bounds.Width).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.ActiveGeometry.Bounds.Height).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.InactiveGeometry.Bounds.X).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.InactiveGeometry.Bounds.Y).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.InactiveGeometry.Bounds.Width).ShouldBeTrue();
        MatrixValueSanitizer.IsFinite(geometry.InactiveGeometry.Bounds.Height).ShouldBeTrue();
    }

    [Theory]
    [InlineData(MatrixDotShape.Circle)]
    [InlineData(MatrixDotShape.Square)]
    [InlineData(MatrixDotShape.RoundedSquare)]
    public void Create_ShouldKeepTheSameOuterBoundsForEveryShape(MatrixDotShape dotShape)
    {
        var allDots = new MatrixGlyph((1UL << (MatrixGlyph.Width * MatrixGlyph.Height)) - 1);

        var geometry = MatrixGlyphGeometryFactory.Create(allDots, 6, 2, dotShape, 0.25);

        geometry.ActiveGeometry.Bounds.ShouldBe(new Avalonia.Rect(0, 0, 38, 54));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(0.25, 0.25)]
    [InlineData(0.5, 0.5)]
    [InlineData(1, 0.5)]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    public void RoundedCornerRatio_ShouldClampToSupportedRange(double value, double expected)
    {
        MatrixDotShapeResolver
            .GetEffectiveCornerRadiusRatio(MatrixDotShape.RoundedSquare, value)
            .ShouldBe(expected);
    }

    [Fact]
    public void UnsupportedShape_ShouldFallbackToCircleAndIgnoreCornerRatio()
    {
        var unsupported = (MatrixDotShape)999;

        MatrixDotShapeResolver.CoerceShape(unsupported).ShouldBe(MatrixDotShape.Circle);
        MatrixDotShapeResolver.GetEffectiveCornerRadiusRatio(unsupported, 0.3).ShouldBe(0);
    }
}
