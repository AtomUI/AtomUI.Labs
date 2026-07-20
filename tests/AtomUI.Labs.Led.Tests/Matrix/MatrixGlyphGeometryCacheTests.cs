using AtomUI.Labs.Led.Matrix;
using AtomUI.Labs.Led.Matrix.Character;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixGlyphGeometryCacheTests
{
    static MatrixGlyphGeometryCacheTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void Render_ShouldBuildOneGeometryPerDistinctGlyphShape()
    {
        var display = CreateDisplay("ABBA");

        Render(display);

        display.GeometryBuildCount.ShouldBe(2);
        display.GeometryCacheCount.ShouldBe(2);
    }

    [Fact]
    public void TextAndVisualChanges_ShouldReuseGeometryCache()
    {
        var display = CreateDisplay("AB");
        Render(display);
        var buildCount = display.GeometryBuildCount;

        display.Text = "BAAB";
        display.ActiveBrush = Brushes.Blue;
        display.InactiveBrush = Brushes.Gray;
        display.CharacterSpacing = 12;
        display.Padding = new Thickness(4);
        Render(display);

        display.GeometryBuildCount.ShouldBe(buildCount);
        display.GeometryCacheCount.ShouldBe(2);
    }

    [Fact]
    public void DotMetrics_ShouldClearAndRebuildGeometryCache()
    {
        var display = CreateDisplay("AB");
        Render(display);
        var buildCount = display.GeometryBuildCount;

        display.DotSize = 8;
        display.GeometryCacheCount.ShouldBe(0);
        Render(display);

        display.GeometryBuildCount.ShouldBe(buildCount + 2);
        display.GeometryCacheCount.ShouldBe(2);

        display.DotSpacing = 3;
        display.GeometryCacheCount.ShouldBe(0);
        Render(display);

        display.GeometryBuildCount.ShouldBe(buildCount + 4);
        display.GeometryCacheCount.ShouldBe(2);
    }

    [Fact]
    public void DotShape_ShouldClearAndRebuildGeometryCache()
    {
        var display = CreateDisplay("AB");
        Render(display);
        var buildCount = display.GeometryBuildCount;

        display.DotShape = MatrixDotShape.Square;
        display.GeometryCacheCount.ShouldBe(0);
        Render(display);

        display.GeometryBuildCount.ShouldBe(buildCount + 2);
        display.GeometryCacheCount.ShouldBe(2);

        display.DotShape = MatrixDotShape.RoundedSquare;
        display.GeometryCacheCount.ShouldBe(0);
        Render(display);

        display.GeometryBuildCount.ShouldBe(buildCount + 4);
        display.GeometryCacheCount.ShouldBe(2);
    }

    [Fact]
    public void CornerRadiusRatio_ShouldOnlyRebuildRoundedSquareGeometry()
    {
        var display = CreateDisplay("A");
        Render(display);
        var buildCount = display.GeometryBuildCount;

        display.DotCornerRadiusRatio = 0.4;
        display.GeometryCacheCount.ShouldBe(1);
        Render(display);
        display.GeometryBuildCount.ShouldBe(buildCount);

        display.DotShape = MatrixDotShape.Square;
        Render(display);
        buildCount = display.GeometryBuildCount;
        display.DotCornerRadiusRatio = 0.1;
        display.GeometryCacheCount.ShouldBe(1);
        Render(display);
        display.GeometryBuildCount.ShouldBe(buildCount);

        display.DotShape = MatrixDotShape.RoundedSquare;
        Render(display);
        buildCount = display.GeometryBuildCount;
        display.DotCornerRadiusRatio = 0.4;
        display.GeometryCacheCount.ShouldBe(0);
        Render(display);
        display.GeometryBuildCount.ShouldBe(buildCount + 1);
    }

    [Fact]
    public void AllSupportedTextAndHistory_ShouldKeepCacheBoundedByDistinctGlyphShapes()
    {
        var allSupportedText = MatrixFiveBySevenGlyphMap.SupportedCharacters;
        var expectedShapeCount = allSupportedText
                                 .Select(character => MatrixCharacterMap.GetPattern(character).Glyph.Bits)
                                 .Distinct()
                                 .Count();
        var display = CreateDisplay(allSupportedText);
        display.Measure(new Size(10_000, 120));
        display.Arrange(new Rect(0, 0, 10_000, 120));
        Render(display);

        display.GeometryCacheCount.ShouldBe(expectedShapeCount);
        display.GeometryCacheCount.ShouldBeLessThanOrEqualTo(45);

        var random = new Random(0x45);
        for (var iteration = 0; iteration < 200; iteration++)
        {
            display.Text = new string(
                Enumerable.Range(0, 32)
                          .Select(_ => allSupportedText[random.Next(allSupportedText.Length)])
                          .ToArray());
            Render(display);
            display.GeometryCacheCount.ShouldBeLessThanOrEqualTo(expectedShapeCount);
        }

        display.GeometryCacheCount.ShouldBe(expectedShapeCount);
    }

    [Fact]
    public void RepeatedDotMetricChanges_ShouldRetainOnlyCurrentGeometrySet()
    {
        var display = CreateDisplay("A");

        for (var iteration = 0; iteration < 200; iteration++)
        {
            display.DotSize = 4 + iteration % 7;
            display.DotSpacing = iteration % 4;
            Render(display);

            display.GeometryCacheCount.ShouldBe(1);
        }

        display.GeometryBuildCount.ShouldBe(200);
    }

    [Fact]
    public void RepeatedShapeChanges_ShouldRetainOnlyCurrentGeometrySet()
    {
        var display = CreateDisplay("A");
        var shapes = new[]
        {
            MatrixDotShape.Circle,
            MatrixDotShape.Square,
            MatrixDotShape.RoundedSquare
        };

        for (var iteration = 0; iteration < 120; iteration++)
        {
            display.DotShape = shapes[iteration % shapes.Length];
            display.DotCornerRadiusRatio = iteration % 6 / 10.0;
            Render(display);

            display.GeometryCacheCount.ShouldBe(1);
        }
    }

    private static MatrixDisplay CreateDisplay(string text)
    {
        var display = new MatrixDisplay
        {
            Text             = text,
            DotSize          = 6,
            DotSpacing       = 2,
            CharacterSpacing = 8,
            ActiveBrush      = Brushes.Red,
            InactiveBrush    = Brushes.DarkGray
        };
        display.Measure(new Size(600, 120));
        display.Arrange(new Rect(0, 0, 600, 120));
        return display;
    }

    private static void Render(MatrixDisplay display)
    {
        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        display.Render(context);
    }
}
