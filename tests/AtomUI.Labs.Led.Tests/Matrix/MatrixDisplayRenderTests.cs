using AtomUI.Labs.Led.Matrix;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixDisplayRenderTests
{
    static MatrixDisplayRenderTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData("MATRIX")]
    [InlineData("labs 2026")]
    [InlineData("?-.:_+=/")]
    [InlineData("A中Z")]
    [InlineData("")]
    public void Render_ShouldNotThrowForSupportedFallbackAndEmptyText(string text)
    {
        var display = CreateDisplay(text);

        var drawing = RenderToDrawingGroup(display);

        drawing.Children.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(12, 4)]
    public void Render_ShouldNotThrowForSmallBounds(double width, double height)
    {
        var display = CreateDisplay("88");
        display.Measure(new Size(width, height));
        display.Arrange(new Rect(0, 0, width, height));

        var drawing = RenderToDrawingGroup(display);

        drawing.Children.ShouldNotBeNull();
    }

    [Fact]
    public void Render_ShouldBatchActiveAndInactiveDotsIntoTwoGeometryCommands()
    {
        var display = CreateDisplay("A");

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(2);
        CountBrush(dots, Colors.Red).ShouldBe(1);
        CountBrush(dots, Colors.DarkGray).ShouldBe(1);
    }

    [Theory]
    [InlineData(MatrixDotShape.Circle)]
    [InlineData(MatrixDotShape.Square)]
    [InlineData(MatrixDotShape.RoundedSquare)]
    public void Render_ShouldKeepTwoGeometryCommandsForEveryDotShape(MatrixDotShape dotShape)
    {
        var display = CreateDisplay("A");
        display.DotShape = dotShape;

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(2);
    }

    [Fact]
    public void Render_ShouldSkipInactiveDotsWhenDisabled()
    {
        var display = CreateDisplay("A");
        display.ShowInactiveDots = false;

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(1);
        CountBrush(dots, Colors.Red).ShouldBe(1);
        CountBrush(dots, Colors.DarkGray).ShouldBe(0);
    }

    [Fact]
    public void Render_ShouldSkipInactiveDotsWhenInactiveBrushIsNull()
    {
        var display = CreateDisplay("A");
        display.InactiveBrush = null;

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(1);
    }

    [Fact]
    public void Render_ShouldDrawOnlyBackgroundWhenActiveBrushIsNull()
    {
        var display = CreateDisplay("A");
        display.ActiveBrush = null;

        var drawings = EnumerateGeometryDrawings(RenderToDrawingGroup(display)).ToList();

        drawings.Count.ShouldBe(1);
        CountBrush(drawings, Colors.Black).ShouldBe(1);
    }

    [Fact]
    public void Render_ShouldDrawUniformBorderOnlyWhenBrushAndThicknessArePresent()
    {
        var display = CreateDisplay("A");
        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = new Thickness(2);

        var drawings = EnumerateGeometryDrawings(RenderToDrawingGroup(display)).ToList();

        drawings.Count(drawing => HasPenBrush(drawing, Colors.Blue)).ShouldBe(1);

        display.BorderBrush = null;
        drawings = EnumerateGeometryDrawings(RenderToDrawingGroup(display)).ToList();
        drawings.Count(drawing => HasPenBrush(drawing, Colors.Blue)).ShouldBe(0);

        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = default;
        drawings = EnumerateGeometryDrawings(RenderToDrawingGroup(display)).ToList();
        drawings.Count(drawing => HasPenBrush(drawing, Colors.Blue)).ShouldBe(0);
    }

    [Fact]
    public void Render_ShouldDrawAsymmetricBorderAsSingleGeometry()
    {
        var display = CreateDisplay("A");
        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = new Thickness(1, 2, 3, 4);
        display.CornerRadius = new CornerRadius(12);

        var drawings = EnumerateGeometryDrawings(RenderToDrawingGroup(display)).ToList();

        CountBrush(drawings, Colors.Blue).ShouldBe(1);
        display.BorderGeometryBuildCount.ShouldBe(1);
    }

    [Fact]
    public void Render_ShouldKeepBorderWhenActiveBrushIsNull()
    {
        var display = CreateDisplay("A");
        display.ActiveBrush = null;
        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = new Thickness(1, 2, 3, 4);

        var drawings = EnumerateGeometryDrawings(RenderToDrawingGroup(display)).ToList();

        CountBrush(drawings, Colors.Black).ShouldBe(1);
        CountBrush(drawings, Colors.Blue).ShouldBe(1);
    }

    [Fact]
    public void Render_ShouldReuseComplexBorderGeometryForBrushChanges()
    {
        var display = CreateDisplay("A");
        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = new Thickness(1, 2, 3, 4);
        RenderToDrawingGroup(display);
        var buildCount = display.BorderGeometryBuildCount;

        display.BorderBrush = Brushes.Green;
        RenderToDrawingGroup(display);

        display.BorderGeometryBuildCount.ShouldBe(buildCount);
    }

    [Fact]
    public void Render_ShouldAlignContentInsideBorderViewport()
    {
        var display = CreateDisplay("A");
        display.Background = null;
        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = new Thickness(10, 12, 14, 16);
        display.HorizontalContentAlignment = HorizontalAlignment.Left;
        display.VerticalContentAlignment = VerticalAlignment.Top;

        var transform = FindLayoutTransform(RenderToDrawingGroup(display));

        transform.ShouldNotBeNull();
        transform.Value.M31.ShouldBe(10);
        transform.Value.M32.ShouldBe(12);
    }

    [Fact]
    public void Render_ShouldScaleContentButKeepBorderPenThickness()
    {
        var display = CreateDisplay("1234567890");
        display.Background = null;
        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = new Thickness(4);
        display.OverflowMode = MatrixOverflowMode.ScaleDown;
        display.Measure(new Size(120, 40));
        display.Arrange(new Rect(0, 0, 120, 40));

        var drawing = RenderToDrawingGroup(display);
        var transform = FindLayoutTransform(drawing);
        var border = EnumerateGeometryDrawings(drawing).Single(item => HasPenBrush(item, Colors.Blue));

        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBeLessThan(1);
        border.Pen!.Thickness.ShouldBe(4);
    }

    [Fact]
    public void Render_ShouldCoerceInvalidBorderValuesWithoutThrowing()
    {
        var display = CreateDisplay("A");
        display.BorderBrush = Brushes.Blue;
        display.BorderThickness = new Thickness(double.NaN, -1, double.PositiveInfinity, 2);
        display.CornerRadius = new CornerRadius(double.PositiveInfinity, -1, double.NaN, 4);

        var drawings = EnumerateGeometryDrawings(RenderToDrawingGroup(display)).ToList();

        drawings.All(drawing => drawing.Geometry is null
            || MatrixValueSanitizer.IsFinite(drawing.Geometry.Bounds.Width)).ShouldBeTrue();
    }

    [Fact]
    public void Render_ShouldReuseLayoutForRepeatedRenderAndVisualOnlyChanges()
    {
        var display = CreateDisplay("AB");

        RenderToDrawingGroup(display);
        var firstVersion = display.LayoutCacheVersion;
        RenderToDrawingGroup(display);
        display.ActiveBrush = Brushes.Blue;
        display.InactiveBrush = Brushes.Gray;
        display.HorizontalContentAlignment = HorizontalAlignment.Center;
        display.VerticalContentAlignment = VerticalAlignment.Bottom;
        display.OverflowMode = MatrixOverflowMode.ScaleDown;
        display.DotShape = MatrixDotShape.RoundedSquare;
        display.DotCornerRadiusRatio = 0.35;
        RenderToDrawingGroup(display);

        display.LayoutCacheVersion.ShouldBe(firstVersion);
    }

    [Fact]
    public void Render_ShouldRefreshLayoutForTextAndSizeChanges()
    {
        var display = CreateDisplay("AB");

        RenderToDrawingGroup(display);
        var firstVersion = display.LayoutCacheVersion;
        display.Text = "CD";
        RenderToDrawingGroup(display);
        var secondVersion = display.LayoutCacheVersion;
        display.DotSize = 9;
        RenderToDrawingGroup(display);

        secondVersion.ShouldBe(firstVersion + 1);
        display.LayoutCacheVersion.ShouldBe(secondVersion + 1);
    }

    [Fact]
    public void Render_ShouldCenterContentInLargerBounds()
    {
        var display = CreateDisplay("A");
        display.Background = null;
        display.HorizontalContentAlignment = HorizontalAlignment.Center;
        display.VerticalContentAlignment = VerticalAlignment.Center;
        display.Measure(new Size(300, 120));
        display.Arrange(new Rect(0, 0, 300, 120));

        var transform = FindLayoutTransform(RenderToDrawingGroup(display));

        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBe(1);
        transform.Value.M22.ShouldBe(1);
        transform.Value.M31.ShouldBeGreaterThan(0);
        transform.Value.M32.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Render_ShouldScaleDownConstrainedContentWithoutScalingUp()
    {
        var display = CreateDisplay("1234567890");
        display.Background = null;
        display.OverflowMode = MatrixOverflowMode.ScaleDown;
        display.HorizontalContentAlignment = HorizontalAlignment.Center;
        display.VerticalContentAlignment = VerticalAlignment.Center;
        display.Measure(new Size(120, 30));
        display.Arrange(new Rect(0, 0, 120, 30));

        var transform = FindLayoutTransform(RenderToDrawingGroup(display));

        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBeGreaterThanOrEqualTo(0);
        transform.Value.M11.ShouldBeLessThan(1);
        transform.Value.M22.ShouldBe(transform.Value.M11);
    }

    [Fact]
    public void Render_ShouldKeepIdentityScaleForClipOverflow()
    {
        var display = CreateDisplay("1234567890");
        display.Background = null;
        display.OverflowMode = MatrixOverflowMode.Clip;
        display.Measure(new Size(120, 30));
        display.Arrange(new Rect(0, 0, 120, 30));

        var transform = FindLayoutTransform(RenderToDrawingGroup(display));

        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBe(1);
        transform.Value.M22.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1)]
    public void Render_ShouldApplyMarqueePositionWithoutScaleDown(double progress)
    {
        var display = CreateDisplay("MATRIX");
        display.IsMarqueeEnabled = true;
        display.OverflowMode = MatrixOverflowMode.ScaleDown;
        display.HorizontalContentAlignment = HorizontalAlignment.Right;
        display.MarqueeProgress = progress;
        var contentWidth = display.DesiredSize.Width;

        var transform = FindLayoutTransform(RenderToDrawingGroup(display));

        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBe(1);
        transform.Value.M22.ShouldBe(1);
        transform.Value.M31.ShouldBe(600 - (600 + contentWidth) * progress, 0.0001);
    }

    [Fact]
    public void Render_ShouldKeepLongMarqueeWorkBoundedByViewport()
    {
        var hundred = CreateDisplay(new string('A', 100));
        hundred.IsMarqueeEnabled = true;
        hundred.MarqueeProgress = 0.5;
        hundred.Measure(new Size(120, 60));
        hundred.Arrange(new Rect(0, 0, 120, 60));

        var tenThousand = CreateDisplay(new string('A', 10_000));
        tenThousand.IsMarqueeEnabled = true;
        tenThousand.MarqueeProgress = 0.5;
        tenThousand.Measure(new Size(120, 60));
        tenThousand.Arrange(new Rect(0, 0, 120, 60));

        var hundredCommands = RenderToGlyphLayerDrawings(hundred).Count();
        var tenThousandCommands = RenderToGlyphLayerDrawings(tenThousand).Count();

        hundredCommands.ShouldBeGreaterThan(0);
        tenThousandCommands.ShouldBeInRange(hundredCommands - 2, hundredCommands + 2);
    }

    [Fact]
    public void MarqueeProgress_ShouldReuseLayoutAndGlyphGeometry()
    {
        var display = CreateDisplay("MATRIX 2026");
        display.IsMarqueeEnabled = true;
        display.MarqueeProgress = 0.5;
        RenderToDrawingGroup(display);
        var layoutVersion = display.LayoutCacheVersion;
        var geometryBuildCount = display.GeometryBuildCount;

        for (var frame = 0; frame < 600; frame++)
        {
            display.MarqueeProgress = frame / 599d;
            RenderToDrawingGroup(display);
        }

        display.LayoutCacheVersion.ShouldBe(layoutVersion);
        display.GeometryBuildCount.ShouldBe(geometryBuildCount);
    }

    [Fact]
    public void Render_ShouldCullGlyphsOutsideClipViewport()
    {
        var display = CreateDisplay(new string('A', 100));
        display.Measure(new Size(46, 60));
        display.Arrange(new Rect(0, 0, 46, 60));

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(2);
    }

    [Fact]
    public void Render_ShouldKeepAllGlyphsWhenScaleDownFitsThemIntoViewport()
    {
        var display = CreateDisplay(new string('A', 10));
        display.OverflowMode = MatrixOverflowMode.ScaleDown;
        display.Measure(new Size(120, 60));
        display.Arrange(new Rect(0, 0, 120, 60));

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(20);
    }

    [Theory]
    [InlineData(0, 60)]
    [InlineData(60, 0)]
    public void Render_ShouldSkipDotsForZeroAreaBounds(double width, double height)
    {
        var display = CreateDisplay("A");
        display.Measure(new Size(width, height));
        display.Arrange(new Rect(0, 0, width, height));

        RenderToGlyphLayerDrawings(display).ShouldBeEmpty();
    }

    [Fact]
    public void Render_ShouldCoerceInvalidNumericInputs()
    {
        var display = CreateDisplay("A");
        display.DotSize = double.NaN;
        display.DotSpacing = double.PositiveInfinity;
        display.CharacterSpacing = double.NegativeInfinity;
        display.Padding = new Thickness(double.NaN, -1, double.PositiveInfinity, 2);
        display.Measure(new Size(40, 20));
        display.Arrange(new Rect(0, 0, 40, 20));

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(2);
        dots.ShouldAllBe(drawing =>
            MatrixValueSanitizer.IsFinite(drawing.Geometry!.Bounds.X)
            && MatrixValueSanitizer.IsFinite(drawing.Geometry.Bounds.Y)
            && MatrixValueSanitizer.IsFinite(drawing.Geometry.Bounds.Width)
            && MatrixValueSanitizer.IsFinite(drawing.Geometry.Bounds.Height));
    }

    [Fact]
    public void Render_ShouldKeepGeometryFiniteForExtremelyLargeFiniteValues()
    {
        var display = CreateDisplay("A");
        display.DotSize = double.MaxValue;
        display.DotSpacing = double.MaxValue;
        display.CharacterSpacing = double.MaxValue;
        display.Padding = default;
        display.OverflowMode = MatrixOverflowMode.ScaleDown;
        display.Measure(new Size(40, 20));
        display.Arrange(new Rect(0, 0, 40, 20));

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(2);
        dots.ShouldAllBe(drawing =>
            MatrixValueSanitizer.IsFinite(drawing.Geometry!.Bounds.X)
            && MatrixValueSanitizer.IsFinite(drawing.Geometry.Bounds.Y)
            && MatrixValueSanitizer.IsFinite(drawing.Geometry.Bounds.Width)
            && MatrixValueSanitizer.IsFinite(drawing.Geometry.Bounds.Height));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Render_ShouldNotThrowForInvalidDotCornerRadiusRatio(double ratio)
    {
        var display = CreateDisplay("A");
        display.DotShape = MatrixDotShape.RoundedSquare;
        display.DotCornerRadiusRatio = ratio;

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(2);
    }

    [Fact]
    public void Render_ShouldFallbackUnsupportedDotShapeToCircle()
    {
        var display = CreateDisplay("A");
        display.DotShape = (MatrixDotShape)999;

        var dots = RenderToGlyphLayerDrawings(display).ToList();

        dots.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Render_ShouldNotThrowForInvalidCornerRadius(double radius)
    {
        var display = CreateDisplay("A");
        display.CornerRadius = new CornerRadius(radius);

        var drawing = RenderToDrawingGroup(display);

        drawing.Children.ShouldNotBeNull();
    }

    private static MatrixDisplay CreateDisplay(string text)
    {
        var display = new MatrixDisplay
        {
            Text             = text,
            DotSize          = 6,
            DotSpacing       = 2,
            CharacterSpacing = 8,
            Padding          = new Thickness(4),
            Background       = Brushes.Black,
            ActiveBrush      = Brushes.Red,
            InactiveBrush    = Brushes.DarkGray
        };
        display.Measure(new Size(600, 160));
        display.Arrange(new Rect(0, 0, 600, 160));
        return display;
    }

    private static DrawingGroup RenderToDrawingGroup(MatrixDisplay display)
    {
        var drawingGroup = new DrawingGroup();
        using (var context = drawingGroup.Open())
        {
            display.Render(context);
        }

        return drawingGroup;
    }

    private static IEnumerable<GeometryDrawing> RenderToGlyphLayerDrawings(MatrixDisplay display)
    {
        return EnumerateGeometryDrawings(RenderToDrawingGroup(display))
               .Where(drawing => HasBrush(drawing, Colors.Red) || HasBrush(drawing, Colors.DarkGray));
    }

    private static IEnumerable<GeometryDrawing> EnumerateGeometryDrawings(Drawing drawing)
    {
        if (drawing is GeometryDrawing geometryDrawing)
        {
            yield return geometryDrawing;
        }
        else if (drawing is DrawingGroup drawingGroup)
        {
            foreach (var child in drawingGroup.Children.SelectMany(EnumerateGeometryDrawings))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<DrawingGroup> EnumerateDrawingGroups(Drawing drawing)
    {
        if (drawing is DrawingGroup drawingGroup)
        {
            yield return drawingGroup;
            foreach (var child in drawingGroup.Children.SelectMany(EnumerateDrawingGroups))
            {
                yield return child;
            }
        }
    }

    private static MatrixTransform? FindLayoutTransform(Drawing drawing)
    {
        return EnumerateDrawingGroups(drawing)
               .Select(group => group.Transform)
               .OfType<MatrixTransform>()
               .FirstOrDefault();
    }

    private static int CountBrush(IEnumerable<GeometryDrawing> drawings, Color color)
    {
        return drawings.Count(drawing => HasBrush(drawing, color));
    }

    private static bool HasBrush(GeometryDrawing drawing, Color color)
    {
        return drawing.Brush is ISolidColorBrush brush && brush.Color == color;
    }

    private static bool HasPenBrush(GeometryDrawing drawing, Color color)
    {
        return drawing.Pen?.Brush is ISolidColorBrush brush && brush.Color == color;
    }
}
