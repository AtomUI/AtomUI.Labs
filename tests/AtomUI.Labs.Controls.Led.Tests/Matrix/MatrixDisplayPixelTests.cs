using System.Runtime.InteropServices;
using AtomUI.Labs.Controls.Led.Matrix;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixDisplayPixelTests
{
    static MatrixDisplayPixelTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    public static IEnumerable<object[]> DotShapeScalingCases()
    {
        var shapes = new[]
        {
            MatrixDotShape.Circle,
            MatrixDotShape.Square,
            MatrixDotShape.RoundedSquare
        };
        var scales = new[] { 1.0, 1.25, 1.5, 2.0 };
        return shapes.SelectMany(shape => scales.Select(scale => new object[] { shape, scale }));
    }

    [Theory]
    [InlineData("A", 18)]
    [InlineData("M", 18)]
    [InlineData("0", 19)]
    [InlineData("8", 17)]
    [InlineData("?", 9)]
    [InlineData(":", 8)]
    [InlineData(" ", 0)]
    public void RepresentativeGlyphs_ShouldRenderOneDisconnectedRegionPerActiveDot(string text, int expectedRegions)
    {
        var analysis = Capture(CreateDisplay(text), 1);

        analysis.ConnectedRegions.ShouldBe(expectedRegions);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void GlyphPhysicalBounds_ShouldTrackRenderScaling(double renderScaling)
    {
        var analysis = Capture(CreateDisplay("8"), renderScaling);

        analysis.HasContent.ShouldBeTrue();
        (analysis.ContentWidth / renderScaling).ShouldBe(32, 1.5);
        (analysis.ContentHeight / renderScaling).ShouldBe(46, 1.5);
    }

    [Fact]
    public void Clip_ShouldKeepContentPixelsInsideControlBounds()
    {
        var display = CreateDisplay("8888");
        display.Width = 40;
        display.Height = 60;
        display.OverflowMode = MatrixOverflowMode.Clip;

        var analysis = Capture(display, 1.5);

        analysis.HasContent.ShouldBeTrue();
        analysis.MaxX.ShouldBeLessThan(60);
        analysis.MaxY.ShouldBeLessThan(90);
    }

    [Fact]
    public void ScaleDown_ShouldKeepCompleteLongTextInsideControlBounds()
    {
        var display = CreateDisplay("8888");
        display.Width = 80;
        display.Height = 30;
        display.OverflowMode = MatrixOverflowMode.ScaleDown;

        var analysis = Capture(display, 1);

        analysis.HasContent.ShouldBeTrue();
        analysis.MinX.ShouldBeGreaterThanOrEqualTo(0);
        analysis.MinY.ShouldBeGreaterThanOrEqualTo(0);
        analysis.MaxX.ShouldBeLessThan(80);
        analysis.MaxY.ShouldBeLessThan(30);
    }

    [Fact]
    public void ContentAlignment_ShouldMovePixelsWithoutChangingTheirSize()
    {
        var left = CreateDisplay("8");
        left.Width = 200;
        left.Height = 80;

        var center = CreateDisplay("8");
        center.Width = 200;
        center.Height = 80;
        center.HorizontalContentAlignment = HorizontalAlignment.Center;
        center.VerticalContentAlignment = VerticalAlignment.Center;

        var right = CreateDisplay("8");
        right.Width = 200;
        right.Height = 80;
        right.HorizontalContentAlignment = HorizontalAlignment.Right;
        right.VerticalContentAlignment = VerticalAlignment.Bottom;

        var leftAnalysis = Capture(left, 1);
        var centerAnalysis = Capture(center, 1);
        var rightAnalysis = Capture(right, 1);

        leftAnalysis.ContentWidth.ShouldBe(centerAnalysis.ContentWidth);
        leftAnalysis.ContentWidth.ShouldBe(rightAnalysis.ContentWidth);
        leftAnalysis.ContentHeight.ShouldBe(centerAnalysis.ContentHeight);
        leftAnalysis.ContentHeight.ShouldBe(rightAnalysis.ContentHeight);
        leftAnalysis.MinX.ShouldBeLessThan(centerAnalysis.MinX);
        centerAnalysis.MinX.ShouldBeLessThan(rightAnalysis.MinX);
        leftAnalysis.MinY.ShouldBeLessThan(centerAnalysis.MinY);
        centerAnalysis.MinY.ShouldBeLessThan(rightAnalysis.MinY);
    }

    [Fact]
    public void InactiveDots_ShouldAddVisiblePixelsWithoutChangingActiveDots()
    {
        var activeOnly = CreateDisplay("A");
        var withInactive = CreateDisplay("A");
        withInactive.ShowInactiveDots = true;
        withInactive.InactiveBrush = new SolidColorBrush(Color.FromRgb(72, 72, 72));

        var activeOnlyAnalysis = Capture(activeOnly, 1);
        var withInactiveAnalysis = Capture(withInactive, 1);

        withInactiveAnalysis.VisiblePixelCount.ShouldBeGreaterThan(activeOnlyAnalysis.VisiblePixelCount);
        activeOnlyAnalysis.ConnectedRegions.ShouldBe(18);
        withInactiveAnalysis.ConnectedRegions.ShouldBe(35);
    }

    [Theory]
    [MemberData(nameof(DotShapeScalingCases))]
    public void EveryDotShape_ShouldKeepGlyphDotsDisconnectedAcrossRenderScaling(
        MatrixDotShape dotShape,
        double renderScaling)
    {
        var display = CreateDisplay("8");
        display.DotShape = dotShape;

        var analysis = Capture(display, renderScaling);

        analysis.ConnectedRegions.ShouldBe(17);
    }

    [Fact]
    public void DotShape_ShouldIncreaseVisualDensityFromCircleToRoundedSquareToSquare()
    {
        var circle = CreateDisplay("8");
        var roundedSquare = CreateDisplay("8");
        roundedSquare.DotShape = MatrixDotShape.RoundedSquare;
        roundedSquare.DotCornerRadiusRatio = 0.25;
        var square = CreateDisplay("8");
        square.DotShape = MatrixDotShape.Square;
        circle.DotSize = roundedSquare.DotSize = square.DotSize = 10;
        circle.DotSpacing = roundedSquare.DotSpacing = square.DotSpacing = 4;

        var circleAnalysis = Capture(circle, 2);
        var roundedSquareAnalysis = Capture(roundedSquare, 2);
        var squareAnalysis = Capture(square, 2);

        roundedSquareAnalysis.VisiblePixelCount.ShouldBeGreaterThan(circleAnalysis.VisiblePixelCount);
        squareAnalysis.VisiblePixelCount.ShouldBeGreaterThan(roundedSquareAnalysis.VisiblePixelCount);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void AsymmetricRoundedBorder_ShouldRemainOneConnectedRegionAcrossRenderScaling(double renderScaling)
    {
        var display = CreateDisplay("");
        display.Width = 100;
        display.Height = 60;
        display.BorderBrush = Brushes.White;
        display.BorderThickness = new Thickness(2, 4, 6, 8);
        display.CornerRadius = new CornerRadius(12, 8, 16, 4);

        var analysis = Capture(display, renderScaling);

        analysis.ConnectedRegions.ShouldBe(1);
        analysis.MinX.ShouldBe(0);
        analysis.MinY.ShouldBe(0);
        analysis.MaxX.ShouldBeLessThanOrEqualTo((int)Math.Ceiling(100 * renderScaling) - 1);
        analysis.MaxY.ShouldBeLessThanOrEqualTo((int)Math.Ceiling(60 * renderScaling) - 1);
    }

    [Fact]
    public void SquareAsymmetricBorder_ShouldUseConfiguredSideThicknesses()
    {
        var display = CreateDisplay("");
        display.Width = 100;
        display.Height = 60;
        display.BorderBrush = Brushes.White;
        display.BorderThickness = new Thickness(2, 4, 6, 8);

        var pixels = CapturePixels(display, 1);

        pixels.IsVisible(1, 30).ShouldBeTrue();
        pixels.IsVisible(3, 30).ShouldBeFalse();
        pixels.IsVisible(50, 2).ShouldBeTrue();
        pixels.IsVisible(50, 6).ShouldBeFalse();
        pixels.IsVisible(96, 30).ShouldBeTrue();
        pixels.IsVisible(92, 30).ShouldBeFalse();
        pixels.IsVisible(50, 56).ShouldBeTrue();
        pixels.IsVisible(50, 50).ShouldBeFalse();
    }

    private static MatrixDisplay CreateDisplay(string text)
    {
        return new MatrixDisplay
        {
            Text             = text,
            Width            = 220,
            Height           = 100,
            DotSize          = 4,
            DotSpacing       = 3,
            CharacterSpacing = 8,
            Padding          = default,
            Background       = Brushes.Black,
            ActiveBrush      = Brushes.White,
            InactiveBrush    = null,
            ShowInactiveDots = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
    }

    private static PixelAnalysis Capture(MatrixDisplay display, double renderScaling)
    {
        var pixels = CapturePixels(display, renderScaling);
        return Analyze(pixels.Bytes, pixels.Width, pixels.Height, pixels.RowBytes);
    }

    private static PixelFrame CapturePixels(MatrixDisplay display, double renderScaling)
    {
        var window = new Window
        {
            Width      = 240,
            Height     = 120,
            Background = Brushes.Black,
            Content    = display
        };

        try
        {
            window.Show();
            window.SetRenderScaling(renderScaling);
            Dispatcher.UIThread.RunJobs();
            using var frame = window.CaptureRenderedFrame();
            frame.ShouldNotBeNull();
            using var framebuffer = frame!.Lock();
            framebuffer.Format.BitsPerPixel.ShouldBe(32);

            var bytes = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
            Marshal.Copy(framebuffer.Address, bytes, 0, bytes.Length);
            return new PixelFrame(bytes, framebuffer.Size.Width, framebuffer.Size.Height, framebuffer.RowBytes);
        }
        finally
        {
            window.Close();
        }
    }

    private static PixelAnalysis Analyze(byte[] bytes, int width, int height, int rowBytes)
    {
        var visible = new bool[width * height];
        var visibleCount = 0;
        var minX = width;
        var minY = height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = y * rowBytes + x * 4;
                var isVisible = bytes[offset] > 8 || bytes[offset + 1] > 8 || bytes[offset + 2] > 8;
                if (!isVisible)
                {
                    continue;
                }

                visible[y * width + x] = true;
                visibleCount++;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return new PixelAnalysis(
            visibleCount,
            CountConnectedRegions(visible, width, height),
            minX,
            minY,
            maxX,
            maxY);
    }

    private static int CountConnectedRegions(bool[] visible, int width, int height)
    {
        var visited = new bool[visible.Length];
        var queue = new Queue<int>();
        var regionCount = 0;
        var directions = new[] { -1, 0, 1 };

        for (var index = 0; index < visible.Length; index++)
        {
            if (!visible[index] || visited[index])
            {
                continue;
            }

            regionCount++;
            visited[index] = true;
            queue.Enqueue(index);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var x = current % width;
                var y = current / width;
                foreach (var dy in directions)
                {
                    foreach (var dx in directions)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        var nextX = x + dx;
                        var nextY = y + dy;
                        if ((uint)nextX >= width || (uint)nextY >= height)
                        {
                            continue;
                        }

                        var next = nextY * width + nextX;
                        if (visible[next] && !visited[next])
                        {
                            visited[next] = true;
                            queue.Enqueue(next);
                        }
                    }
                }
            }
        }

        return regionCount;
    }

    private readonly record struct PixelAnalysis(
        int VisiblePixelCount,
        int ConnectedRegions,
        int MinX,
        int MinY,
        int MaxX,
        int MaxY)
    {
        public bool HasContent => VisiblePixelCount > 0;

        public int ContentWidth => HasContent ? MaxX - MinX + 1 : 0;

        public int ContentHeight => HasContent ? MaxY - MinY + 1 : 0;
    }

    private readonly record struct PixelFrame(byte[] Bytes, int Width, int Height, int RowBytes)
    {
        public bool IsVisible(int x, int y)
        {
            var offset = y * RowBytes + x * 4;
            return Bytes[offset] > 8 || Bytes[offset + 1] > 8 || Bytes[offset + 2] > 8;
        }
    }
}
