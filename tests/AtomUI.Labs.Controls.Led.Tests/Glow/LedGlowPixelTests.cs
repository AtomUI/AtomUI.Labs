using System.Runtime.InteropServices;
using AtomUI.Labs.Controls.Led.Matrix;
using AtomUI.Labs.Controls.Led.Segment;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Glow;

public class LedGlowPixelTests
{
    static LedGlowPixelTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(1, 24)]
    [InlineData(2, 6)]
    [InlineData(2, 24)]
    public void MatrixGlow_ShouldAddPixelsWithoutChangingBorder(double renderScaling, double radius)
    {
        var withoutGlow = CreateMatrix(null, radius);
        var baseline = Capture(withoutGlow, renderScaling);
        var withGlow = CreateMatrix(Brushes.Cyan, radius);
        var glowing = Capture(withGlow, renderScaling);

        CountDifferentPixels(baseline, glowing).ShouldBeGreaterThan(0);
        CountWhitePixels(glowing).ShouldBe(CountWhitePixels(baseline));
        glowing.GetPixel(2, 2, renderScaling).ShouldBe(baseline.GetPixel(2, 2, renderScaling));
        withGlow.GlowEffectBuildCount.ShouldBe(1);
        withGlow.GlowEffectScopeCount.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(1, 24)]
    [InlineData(2, 6)]
    [InlineData(2, 24)]
    public void SegmentGlow_ShouldAddPixelsAndKeepActiveCoreSharp(double renderScaling, double radius)
    {
        var baseline = Capture(CreateSegment(null, radius), renderScaling);
        var withGlow = CreateSegment(Brushes.Cyan, radius);
        var glowing = Capture(withGlow, renderScaling);

        CountDifferentPixels(baseline, glowing).ShouldBeGreaterThan(0);
        CountWhitePixels(glowing).ShouldBe(CountWhitePixels(baseline));
        withGlow.GlowEffectBuildCount.ShouldBe(1);
        withGlow.GlowEffectScopeCount.ShouldBeGreaterThan(0);
    }

    private static MatrixDisplay CreateMatrix(IBrush? glowBrush, double radius)
    {
        return new MatrixDisplay
        {
            Width = 240,
            Height = 120,
            Text = "88",
            DotSize = 7,
            DotSpacing = 3,
            Padding = new Thickness(16),
            Background = Brushes.Black,
            BorderBrush = Brushes.Yellow,
            BorderThickness = new Thickness(4),
            ActiveBrush = Brushes.White,
            InactiveBrush = null,
            ShowInactiveDots = false,
            GlowBrush = glowBrush,
            GlowOpacity = 0.65,
            GlowRadius = radius,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
    }

    private static SegmentDisplay CreateSegment(IBrush? glowBrush, double radius)
    {
        return new SegmentDisplay
        {
            Width = 300,
            Height = 120,
            Text = "12:45",
            CharacterHeight = 72,
            Padding = new Thickness(16),
            Background = Brushes.Black,
            ActiveBrush = Brushes.White,
            InactiveBrush = null,
            ShowInactiveSegments = false,
            GlowBrush = glowBrush,
            GlowOpacity = 0.65,
            GlowRadius = radius,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
    }

    private static PixelFrame Capture(Control control, double renderScaling)
    {
        var window = new Window
        {
            Width = 340,
            Height = 160,
            Background = Brushes.Black,
            Content = control
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

    private static int CountDifferentPixels(PixelFrame left, PixelFrame right)
    {
        left.Width.ShouldBe(right.Width);
        left.Height.ShouldBe(right.Height);
        var count = 0;
        for (var y = 0; y < left.Height; y++)
        {
            for (var x = 0; x < left.Width; x++)
            {
                if (left.GetPixel(x, y) != right.GetPixel(x, y))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountWhitePixels(PixelFrame frame)
    {
        var count = 0;
        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++)
            {
                var pixel = frame.GetPixel(x, y);
                if (pixel.B >= 250 && pixel.G >= 250 && pixel.R >= 250)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private readonly record struct PixelFrame(byte[] Bytes, int Width, int Height, int RowBytes)
    {
        public Pixel GetPixel(int x, int y)
        {
            var offset = y * RowBytes + x * 4;
            return new Pixel(Bytes[offset], Bytes[offset + 1], Bytes[offset + 2], Bytes[offset + 3]);
        }

        public Pixel GetPixel(int logicalX, int logicalY, double renderScaling)
        {
            return GetPixel(
                Math.Clamp((int)(logicalX * renderScaling), 0, Width - 1),
                Math.Clamp((int)(logicalY * renderScaling), 0, Height - 1));
        }
    }

    private readonly record struct Pixel(byte B, byte G, byte R, byte A);
}
