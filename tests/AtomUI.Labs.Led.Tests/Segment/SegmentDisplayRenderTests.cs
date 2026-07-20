using AtomUI.Labs.Led.Segment;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Segment;

public class SegmentDisplayRenderTests
{
    static SegmentDisplayRenderTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData("HELLO")]
    [InlineData("12:45")]
    [InlineData("88.8")]
    [InlineData("")]
    [InlineData("?中")]
    public void Render_ShouldNotThrowForSupportedAndUnsupportedText(string text)
    {
        var display = CreateDisplay(text);

        var drawingGroup = RenderToDrawingGroup(display);

        drawingGroup.Children.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(12, 4)]
    public void Render_ShouldNotThrowForSmallArrangedSizes(double width, double height)
    {
        var display = CreateDisplay("88.8");
        display.Measure(new Size(width, height));
        display.Arrange(new Rect(0, 0, width, height));

        var drawingGroup = RenderToDrawingGroup(display);

        drawingGroup.Children.ShouldNotBeNull();
    }

    [Fact]
    public void Render_ShouldNotThrowWhenActiveBrushIsNull()
    {
        var display = CreateDisplay("HELLO");
        display.ActiveBrush = null;

        var drawingGroup = RenderToDrawingGroup(display);

        drawingGroup.Children.ShouldNotBeNull();
    }

    [Fact]
    public void Render_ShouldDrawInactiveAndActiveSegmentsForSegmentCharacters()
    {
        var display = CreateDisplay("1");

        var drawings = RenderToGeometryDrawings(display).ToList();

        drawings.Count.ShouldBe(3);
        CountBrush(drawings, Colors.Black).ShouldBe(1);
        CountBrush(drawings, Colors.DarkGray).ShouldBe(1);
        CountBrush(drawings, Colors.Red).ShouldBe(1);
    }

    [Fact]
    public void Render_ShouldSkipInactiveSegmentsWhenDisabled()
    {
        var display = CreateDisplay("1");
        display.ShowInactiveSegments = false;

        var drawings = RenderToGeometryDrawings(display).ToList();

        drawings.Count.ShouldBe(2);
        CountBrush(drawings, Colors.Black).ShouldBe(1);
        CountBrush(drawings, Colors.DarkGray).ShouldBe(0);
        CountBrush(drawings, Colors.Red).ShouldBe(1);
    }

    [Theory]
    [InlineData(":", 2, 1)]
    [InlineData(".", 2, 1)]
    public void Render_ShouldDrawColonAndDotWithoutInactiveSegments(string text, int expectedTotal, int expectedActiveDots)
    {
        var display = CreateDisplay(text);

        var drawings = RenderToGeometryDrawings(display).ToList();

        drawings.Count.ShouldBe(expectedTotal);
        CountBrush(drawings, Colors.Black).ShouldBe(1);
        CountBrush(drawings, Colors.DarkGray).ShouldBe(0);
        CountBrush(drawings, Colors.Red).ShouldBe(expectedActiveDots);
    }

    [Fact]
    public void Render_ShouldDrawOnlyBackgroundWhenActiveBrushIsNull()
    {
        var display = CreateDisplay("1");
        display.ActiveBrush = null;

        var drawings = RenderToGeometryDrawings(display).ToList();

        drawings.Count.ShouldBe(1);
        CountBrush(drawings, Colors.Black).ShouldBe(1);
    }

    [Fact]
    public void Render_ShouldDrawGlowLayerForActiveSegmentsWhenGlowBrushIsSet()
    {
        var display = CreateDisplay("1");
        display.GlowBrush = Brushes.Yellow;
        display.GlowOpacity = 0.4;

        var drawings = RenderToGeometryDrawings(display).ToList();

        drawings.Count.ShouldBe(4);
        CountBrush(drawings, Colors.Black).ShouldBe(1);
        CountBrush(drawings, Colors.DarkGray).ShouldBe(1);
        CountBrush(drawings, Colors.Yellow).ShouldBe(1);
        CountBrush(drawings, Colors.Red).ShouldBe(1);
    }

    [Fact]
    public void Render_ShouldSkipGlowLayerWhenGlowOpacityIsZero()
    {
        var display = CreateDisplay("1");
        display.GlowBrush = Brushes.Yellow;
        display.GlowOpacity = 0;

        var drawings = RenderToGeometryDrawings(display).ToList();

        drawings.Count.ShouldBe(3);
        CountBrush(drawings, Colors.Yellow).ShouldBe(0);
    }

    [Fact]
    public void Render_ShouldCullLongTextBeforeSubmittingSingleGlowEffect()
    {
        var display = CreateDisplay(new string('8', 1000));
        display.GlowBrush = Brushes.Cyan;
        display.GlowRadius = 24;
        var thousandCharacterCommands = RenderToGeometryDrawings(display).Count();
        display.GlowEffectScopeCount.ShouldBe(1);

        display.Text = new string('8', 100);
        var hundredCharacterCommands = RenderToGeometryDrawings(display).Count();

        thousandCharacterCommands.ShouldBe(hundredCharacterCommands);
        thousandCharacterCommands.ShouldBeLessThan(250);
        display.GlowEffectScopeCount.ShouldBe(2);
    }

    [Fact]
    public void Render_ShouldCoerceInvalidNumericInputs()
    {
        var display = CreateDisplay("88");
        display.CharacterHeight      = double.NaN;
        display.CharacterAspectRatio = double.PositiveInfinity;
        display.CharacterSpacing     = double.NegativeInfinity;
        display.SegmentThickness     = double.NaN;
        display.SegmentGap           = double.PositiveInfinity;
        display.Measure(new Size(400, 120));
        display.Arrange(new Rect(0, 0, 400, 120));

        var drawings = RenderToGeometryDrawings(display).ToList();

        drawings.ShouldNotBeEmpty();
    }

    [Fact]
    public void Render_ShouldReuseSegmentGeometriesForRepeatedRenderWithSameLayout()
    {
        var display = CreateDisplay("12");

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        RenderToDrawingGroup(display);
        var secondVersion = display.GeometryCacheVersion;

        secondVersion.ShouldBe(firstVersion);
    }

    [Fact]
    public void Render_ShouldReuseVisibleGeometryForRepeatedRenderAndBrushChanges()
    {
        var display = CreateDisplay("12:45");
        display.GlowBrush = Brushes.Cyan;
        RenderToDrawingGroup(display);
        var buildCount = display.VisibleGeometryBuildCount;

        RenderToDrawingGroup(display);
        display.ActiveBrush = Brushes.Blue;
        display.InactiveBrush = Brushes.Gray;
        display.GlowBrush = Brushes.Magenta;
        display.GlowOpacity = 0.6;
        display.GlowRadius = 12;
        RenderToDrawingGroup(display);

        display.VisibleGeometryBuildCount.ShouldBe(buildCount);
    }

    [Fact]
    public void Render_ShouldRebuildVisibleGeometryForPatternChangeButReuseSegmentGeometry()
    {
        var display = CreateDisplay("1111");
        RenderToDrawingGroup(display);
        var geometryVersion = display.GeometryCacheVersion;
        var visibleBuildCount = display.VisibleGeometryBuildCount;

        display.Text = "8888";
        RenderToDrawingGroup(display);

        display.GeometryCacheVersion.ShouldBe(geometryVersion);
        display.VisibleGeometryBuildCount.ShouldBe(visibleBuildCount + 1);
    }

    [Fact]
    public void Render_ShouldReuseSegmentGeometriesWhenOnlyBrushChanges()
    {
        var display = CreateDisplay("12");

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        display.ActiveBrush = Brushes.Blue;
        display.InactiveBrush = Brushes.Gray;
        RenderToDrawingGroup(display);
        var secondVersion = display.GeometryCacheVersion;

        secondVersion.ShouldBe(firstVersion);
    }

    [Fact]
    public void Render_ShouldReuseSegmentGeometriesWhenOnlyGlowSettingsChange()
    {
        var display = CreateDisplay("12");

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        display.GlowBrush = Brushes.Yellow;
        display.GlowOpacity = 0.4;
        RenderToDrawingGroup(display);
        var secondVersion = display.GeometryCacheVersion;

        secondVersion.ShouldBe(firstVersion);
    }

    [Fact]
    public void Render_ShouldReuseSegmentGeometriesWhenOnlyAlignmentOrOverflowChanges()
    {
        var display = CreateDisplay("12");

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        display.HorizontalContentAlignment = HorizontalAlignment.Center;
        display.VerticalContentAlignment = VerticalAlignment.Bottom;
        display.OverflowMode = SegmentOverflowMode.ScaleDown;
        RenderToDrawingGroup(display);
        var secondVersion = display.GeometryCacheVersion;

        secondVersion.ShouldBe(firstVersion);
    }

    [Fact]
    public void Render_ShouldApplyTransformForCenteredScaleDownContent()
    {
        var display = CreateDisplay("888888");
        display.Background = null;
        display.HorizontalContentAlignment = HorizontalAlignment.Center;
        display.VerticalContentAlignment = VerticalAlignment.Center;
        display.OverflowMode = SegmentOverflowMode.ScaleDown;
        display.Measure(new Size(120, 40));
        display.Arrange(new Rect(0, 0, 120, 40));

        var drawingGroup = RenderToDrawingGroup(display);

        var transform = EnumerateDrawingGroups(drawingGroup)
                        .Select(group => group.Transform)
                        .OfType<MatrixTransform>()
                        .FirstOrDefault();
        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBeLessThan(1);
        transform.Value.M22.ShouldBeLessThan(1);
    }

    [Fact]
    public void Render_ShouldKeepIdentityScaleForClipOverflowWhenContentIsConstrained()
    {
        var display = CreateDisplay("888888");
        display.Background = null;
        display.OverflowMode = SegmentOverflowMode.Clip;
        display.Measure(new Size(120, 40));
        display.Arrange(new Rect(0, 0, 120, 40));

        var drawingGroup = RenderToDrawingGroup(display);

        var transform = FindLayoutTransform(drawingGroup);
        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBe(1);
        transform.Value.M22.ShouldBe(1);
    }

    [Fact]
    public void Render_ShouldNotScaleUpWhenScaleDownContentHasExtraSpace()
    {
        var display = CreateDisplay("12");
        display.Background = null;
        display.OverflowMode = SegmentOverflowMode.ScaleDown;
        display.HorizontalContentAlignment = HorizontalAlignment.Center;
        display.VerticalContentAlignment = VerticalAlignment.Center;
        display.Measure(new Size(800, 120));
        display.Arrange(new Rect(0, 0, 800, 120));

        var drawingGroup = RenderToDrawingGroup(display);

        var transform = FindLayoutTransform(drawingGroup);
        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBe(1);
        transform.Value.M22.ShouldBe(1);
        transform.Value.M31.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Render_ShouldScaleDownForExtremelySmallBoundsWithoutThrowing()
    {
        var display = CreateDisplay("888888");
        display.Background = null;
        display.OverflowMode = SegmentOverflowMode.ScaleDown;
        display.Measure(new Size(1, 1));
        display.Arrange(new Rect(0, 0, 1, 1));

        var drawingGroup = RenderToDrawingGroup(display);

        var transform = FindLayoutTransform(drawingGroup);
        transform.ShouldNotBeNull();
        transform.Value.M11.ShouldBeGreaterThanOrEqualTo(0);
        transform.Value.M11.ShouldBeLessThanOrEqualTo(1);
        transform.Value.M22.ShouldBeGreaterThanOrEqualTo(0);
        transform.Value.M22.ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public void Render_ShouldReuseSegmentGeometriesWhenTextChangesWithSameTopology()
    {
        var display = CreateDisplay("12");

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        display.Text = "34";
        RenderToDrawingGroup(display);
        var secondVersion = display.GeometryCacheVersion;

        secondVersion.ShouldBe(firstVersion);
    }

    [Fact]
    public void Render_ShouldUseCurrentPatternWhenTextReusesSegmentGeometries()
    {
        var display = CreateDisplay("1");

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        display.Text = "8";
        var drawings = RenderToGeometryDrawings(display).ToList();

        display.GeometryCacheVersion.ShouldBe(firstVersion);
        CountBrush(drawings, Colors.Red).ShouldBe(1);
    }

    [Theory]
    [InlineData("12:34", "12.34")]
    [InlineData("1234", "12:34")]
    [InlineData("12", "1 ")]
    public void Render_ShouldRefreshSegmentGeometriesWhenTextTopologyChanges(string initialText, string updatedText)
    {
        var display = CreateDisplay(initialText);

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        display.Text = updatedText;
        RenderToDrawingGroup(display);

        display.GeometryCacheVersion.ShouldBe(firstVersion + 1);
    }

    [Fact]
    public void Render_ShouldRebuildLayoutButReuseGeometryDuringHighFrequencyNumericUpdates()
    {
        const int updateCount = 2000;
        var display = CreateDisplay("0000");
        RenderToDrawingGroup(display);
        var initialLayoutVersion   = display.LayoutCacheVersion;
        var initialGeometryVersion = display.GeometryCacheVersion;

        for (var i = 1; i <= updateCount; i++)
        {
            display.Text = (i % 10000).ToString("D4");
            RenderToDrawingGroup(display);
        }

        display.LayoutCacheVersion.ShouldBe(initialLayoutVersion + updateCount);
        display.GeometryCacheVersion.ShouldBe(initialGeometryVersion);

        display.Text = "12:34";
        RenderToDrawingGroup(display);
        display.GeometryCacheVersion.ShouldBe(initialGeometryVersion + 1);

        display.SegmentThickness = 10;
        RenderToDrawingGroup(display);
        display.GeometryCacheVersion.ShouldBe(initialGeometryVersion + 2);
    }

    [Fact]
    public void Render_ShouldRefreshSegmentGeometriesWhenGeometryOptionsChange()
    {
        var display = CreateDisplay("12");

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        var firstLayoutVersion = display.LayoutCacheVersion;
        display.SegmentThickness = 12;
        RenderToDrawingGroup(display);
        var secondVersion = display.GeometryCacheVersion;

        secondVersion.ShouldBe(firstVersion + 1);
        display.LayoutCacheVersion.ShouldBe(firstLayoutVersion);
    }

    [Fact]
    public void Render_ShouldRefreshLayoutAndGeometriesWhenCharacterSpacingChanges()
    {
        var display = CreateDisplay("1234");

        RenderToDrawingGroup(display);
        var firstLayoutVersion   = display.LayoutCacheVersion;
        var firstGeometryVersion = display.GeometryCacheVersion;
        display.CharacterSpacing = 12;
        RenderToDrawingGroup(display);

        display.LayoutCacheVersion.ShouldBe(firstLayoutVersion + 1);
        display.GeometryCacheVersion.ShouldBe(firstGeometryVersion + 1);
    }

    [Fact]
    public void Render_ShouldRefreshSegmentGeometriesWhenShapeOptionsChange()
    {
        var display = CreateDisplay("12:45");

        RenderToDrawingGroup(display);
        var firstVersion = display.GeometryCacheVersion;
        display.SegmentBevelRatio = 0.25;
        RenderToDrawingGroup(display);
        var secondVersion = display.GeometryCacheVersion;
        display.DotScale = 0.5;
        RenderToDrawingGroup(display);
        var thirdVersion = display.GeometryCacheVersion;

        secondVersion.ShouldBe(firstVersion + 1);
        thirdVersion.ShouldBe(secondVersion + 1);
    }

    private static SegmentDisplay CreateDisplay(string text)
    {
        var display = new SegmentDisplay
        {
            Text                 = text,
            CharacterHeight      = 72,
            CharacterAspectRatio = 0.58,
            CharacterSpacing     = 8,
            SegmentThickness     = 8,
            SegmentGap           = 2,
            ActiveBrush          = Brushes.Red,
            InactiveBrush        = Brushes.DarkGray,
            Background           = Brushes.Black
        };
        display.Measure(new Size(400, 120));
        display.Arrange(new Rect(0, 0, 400, 120));
        return display;
    }

    private static DrawingGroup RenderToDrawingGroup(SegmentDisplay display)
    {
        var drawingGroup = new DrawingGroup();
        using (var context = drawingGroup.Open())
        {
            display.Render(context);
        }

        return drawingGroup;
    }

    private static IEnumerable<GeometryDrawing> RenderToGeometryDrawings(SegmentDisplay display)
    {
        return EnumerateGeometryDrawings(RenderToDrawingGroup(display));
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
        return drawings.Count(drawing => drawing.Brush is ISolidColorBrush brush && brush.Color == color);
    }
}
