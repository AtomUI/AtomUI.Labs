using AtomUI.Labs.Led.Glow;
using AtomUI.Labs.Led.Matrix;
using AtomUI.Labs.Led.Segment;
using Avalonia;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Glow;

public class LedGlowRendererTests
{
    static LedGlowRendererTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(double.NegativeInfinity, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(0.35, 0.35)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public void CoerceOpacity_ShouldFollowSharedContract(double value, double expected)
    {
        LedGlowValueSanitizer.CoerceOpacity(value).ShouldBe(expected);
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(double.NegativeInfinity, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(0.5, 0.5)]
    [InlineData(6, 6)]
    [InlineData(12, 12)]
    [InlineData(24, 24)]
    [InlineData(25, 24)]
    [InlineData(double.MaxValue, 24)]
    public void CoerceRadius_ShouldFollowSharedContract(double value, double expected)
    {
        LedGlowValueSanitizer.CoerceRadius(value).ShouldBe(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GlowDisabled_ShouldNotBuildOrSubmitEffect(bool matrix)
    {
        if (matrix)
        {
            var display = CreateMatrix();
            Render(display);
            display.GlowEffectBuildCount.ShouldBe(0);
            display.GlowEffectScopeCount.ShouldBe(0);
        }
        else
        {
            var display = CreateSegment();
            Render(display);
            display.GlowEffectBuildCount.ShouldBe(0);
            display.GlowEffectScopeCount.ShouldBe(0);
        }
    }

    [Fact]
    public void Matrix_ShouldUseOneEffectScopePerRenderAndReuseEffect()
    {
        var display = CreateMatrix();
        display.GlowBrush = Brushes.Cyan;

        Render(display);
        display.GlowEffectBuildCount.ShouldBe(1);
        display.GlowEffectScopeCount.ShouldBe(1);

        display.GlowBrush = Brushes.Magenta;
        display.GlowOpacity = 0.6;
        display.GlowRadius = 12;
        Render(display);

        display.GlowEffectBuildCount.ShouldBe(1);
        display.GlowEffectScopeCount.ShouldBe(2);
    }

    [Fact]
    public void Segment_ShouldUseOneEffectScopePerRenderAndReuseEffect()
    {
        var display = CreateSegment();
        display.GlowBrush = Brushes.Cyan;

        Render(display);
        display.GlowEffectBuildCount.ShouldBe(1);
        display.GlowEffectScopeCount.ShouldBe(1);

        display.GlowBrush = Brushes.Magenta;
        display.GlowOpacity = 0.6;
        display.GlowRadius = 12;
        Render(display);

        display.GlowEffectBuildCount.ShouldBe(1);
        display.GlowEffectScopeCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ClearingGlowBrush_ShouldReleaseRendererState(bool matrix)
    {
        if (matrix)
        {
            var display = CreateMatrix();
            display.GlowBrush = Brushes.Cyan;
            Render(display);
            display.GlowEffectBuildCount.ShouldBe(1);

            display.GlowBrush = null;

            display.GlowEffectBuildCount.ShouldBe(0);
            display.GlowEffectScopeCount.ShouldBe(0);
        }
        else
        {
            var display = CreateSegment();
            display.GlowBrush = Brushes.Cyan;
            Render(display);
            display.GlowEffectBuildCount.ShouldBe(1);

            display.GlowBrush = null;

            display.GlowEffectBuildCount.ShouldBe(0);
            display.GlowEffectScopeCount.ShouldBe(0);
        }
    }

    [Theory]
    [InlineData(0, 6)]
    [InlineData(0.35, 0)]
    [InlineData(double.NaN, 6)]
    [InlineData(0.35, double.PositiveInfinity)]
    public void InvalidOrZeroEffectiveValues_ShouldSkipEffect(double opacity, double radius)
    {
        var display = CreateMatrix();
        display.GlowBrush = Brushes.Cyan;
        display.GlowOpacity = opacity;
        display.GlowRadius = radius;

        Render(display);

        display.GlowEffectBuildCount.ShouldBe(0);
        display.GlowEffectScopeCount.ShouldBe(0);
    }

    private static MatrixDisplay CreateMatrix()
    {
        var display = new MatrixDisplay
        {
            Text = "MATRIX",
            ActiveBrush = Brushes.Red,
            InactiveBrush = Brushes.DarkGray
        };
        display.Measure(new Size(600, 120));
        display.Arrange(new Rect(0, 0, 600, 120));
        return display;
    }

    private static SegmentDisplay CreateSegment()
    {
        var display = new SegmentDisplay
        {
            Text = "12:45",
            ActiveBrush = Brushes.Red,
            InactiveBrush = Brushes.DarkGray
        };
        display.Measure(new Size(600, 120));
        display.Arrange(new Rect(0, 0, 600, 120));
        return display;
    }

    private static void Render(MatrixDisplay display)
    {
        using var context = new DrawingGroup().Open();
        display.Render(context);
    }

    private static void Render(SegmentDisplay display)
    {
        using var context = new DrawingGroup().Open();
        display.Render(context);
    }
}
