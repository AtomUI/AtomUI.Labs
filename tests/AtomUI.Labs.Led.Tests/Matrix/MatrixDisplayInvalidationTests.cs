using AtomUI.Labs.Led.Matrix;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixDisplayInvalidationTests
{
    static MatrixDisplayInvalidationTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public void LayoutProperties_ShouldInvalidateMeasure()
    {
        var display = new MatrixDisplay { Text = "A" };
        ShowInWindow(display, window =>
        {
            AssertInvalidatesMeasure(display, () => display.Text = "B");
            AssertInvalidatesMeasure(display, () => display.DotSize = 7);
            AssertInvalidatesMeasure(display, () => display.DotSpacing = 3);
            AssertInvalidatesMeasure(display, () => display.CharacterSpacing = 9);
            AssertInvalidatesMeasure(display, () => display.Padding = new Thickness(2));
            AssertInvalidatesMeasure(display, () => display.BorderThickness = new Thickness(2));
        });
    }

    [Fact]
    public void VisualProperties_ShouldInvalidateRenderWithoutInvalidatingMeasure()
    {
        var display = new CountingMatrixDisplay { Text = "A" };
        ShowInWindow(display, window =>
        {
            AssertInvalidatesRenderOnly(display, window, () => display.HorizontalContentAlignment = HorizontalAlignment.Center);
            AssertInvalidatesRenderOnly(display, window, () => display.VerticalContentAlignment = VerticalAlignment.Center);
            AssertInvalidatesRenderOnly(display, window, () => display.OverflowMode = MatrixOverflowMode.ScaleDown);
            AssertInvalidatesRenderOnly(display, window, () => display.DotShape = MatrixDotShape.RoundedSquare);
            AssertInvalidatesRenderOnly(display, window, () => display.DotCornerRadiusRatio = 0.3);
            AssertInvalidatesRenderOnly(display, window, () => display.Background = Brushes.Black);
            AssertInvalidatesRenderOnly(display, window, () => display.BorderBrush = Brushes.Blue);
            AssertInvalidatesRenderOnly(display, window, () => display.CornerRadius = new CornerRadius(4));
            AssertInvalidatesRenderOnly(display, window, () => display.ActiveBrush = Brushes.Red);
            AssertInvalidatesRenderOnly(display, window, () => display.InactiveBrush = Brushes.Gray);
            AssertInvalidatesRenderOnly(display, window, () => display.ShowInactiveDots = false);
            AssertInvalidatesRenderOnly(display, window, () => display.MarqueeSpeed = 64);
            AssertInvalidatesRenderOnly(display, window, () => display.MarqueeRepeatDelay = TimeSpan.FromSeconds(1));
        });
    }

    private static void AssertInvalidatesMeasure(MatrixDisplay display, Action change)
    {
        display.IsMeasureValid.ShouldBeTrue();

        change();

        display.IsMeasureValid.ShouldBeFalse();
        Dispatcher.UIThread.RunJobs();
        display.IsMeasureValid.ShouldBeTrue();
    }

    private static void AssertInvalidatesRenderOnly(CountingMatrixDisplay display, Window window, Action change)
    {
        using var previousFrame = window.CaptureRenderedFrame();
        var renderCount = display.RenderCount;
        display.IsMeasureValid.ShouldBeTrue();

        change();

        display.IsMeasureValid.ShouldBeTrue();
        using var currentFrame = window.CaptureRenderedFrame();
        display.RenderCount.ShouldBeGreaterThan(renderCount);
    }

    private static void ShowInWindow(Control content, Action<Window> assertion)
    {
        var window = new Window
        {
            Width   = 320,
            Height  = 120,
            Content = content
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            assertion(window);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class CountingMatrixDisplay : MatrixDisplay
    {
        public int RenderCount { get; private set; }

        public override void Render(DrawingContext context)
        {
            RenderCount++;
            base.Render(context);
        }
    }
}
