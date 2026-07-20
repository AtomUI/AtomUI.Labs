using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace AtomUI.Labs.Led.Performance;

internal static class GlowPrototypeRunner
{
    private const double Width = 180;
    private const double Height = 140;
    private static DrawingGroup? _drawingSink;

    public static int Run(int frameCount, string? markdownOutputPath)
    {
        var corpus = CreateCorpus();
        var brushes = CreateBrushes();
        var pixelResults = new List<GlowPrototypePixelResult>();
        foreach (var glowCase in corpus)
        {
            foreach (var brushCase in brushes)
            {
                foreach (var radius in new[] { 6d, 12d, 24d })
                {
                    foreach (var renderer in CreateRenderers(brushCase.Brush, radius))
                    {
                        foreach (var renderScaling in new[] { 1d, 1.25, 1.5, 2d })
                        {
                            var result = Capture(renderer, glowCase.Geometry, renderScaling);
                            pixelResults.Add(new GlowPrototypePixelResult(
                                renderer.Name,
                                glowCase.Name,
                                brushCase.Name,
                                radius,
                                renderScaling,
                                result.VisibleOutsideSourcePixels,
                                result.VisibleInsideSourcePixels));
                        }
                    }
                }
            }
        }

        var validationResults = RunBehaviorValidations();

        var representative = corpus.First(item => item.Name == "Matrix.Circle").Geometry;
        var performanceResults = new List<GlowPrototypePerformanceResult>();
        foreach (var renderer in new IGlowPrototypeRenderer[]
                 {
                     new NoGlowPrototype(),
                     new VectorExpansionGlowPrototype(Brushes.Cyan, 0.65, 12),
                     new ScopedBlurEffectGlowPrototype(Brushes.Cyan, 0.65, 12)
                 })
        {
            var result = Measure(renderer, representative, frameCount);
            performanceResults.Add(new GlowPrototypePerformanceResult(
                renderer.Name,
                frameCount,
                result.Elapsed,
                result.AllocatedBytes));
        }

        Console.WriteLine(RenderSummary(pixelResults, performanceResults, validationResults));
        if (!string.IsNullOrWhiteSpace(markdownOutputPath))
        {
            var fullPath = Path.GetFullPath(markdownOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(
                fullPath,
                RenderMarkdown(pixelResults, performanceResults, validationResults),
                new UTF8Encoding(false));
            Console.WriteLine($"Wrote Glow prototype result: {fullPath}");
        }

        return pixelResults.All(result => result.VisibleOutsideSourcePixels > 0)
               && validationResults.All(result => result.Passed)
            ? 0
            : 1;
    }

    private static IReadOnlyList<GlowPrototypeCase> CreateCorpus()
    {
        return new[]
        {
            new GlowPrototypeCase("Matrix.Circle", new EllipseGeometry(new Rect(70, 50, 40, 40))),
            new GlowPrototypeCase("Matrix.Square", new RectangleGeometry(new Rect(70, 50, 40, 40))),
            new GlowPrototypeCase(
                "Matrix.RoundedSquare",
                new RectangleGeometry(new Rect(70, 50, 40, 40), 10, 10)),
            new GlowPrototypeCase(
                "Segment.Horizontal",
                CreatePolygon(
                    new Point(55, 58),
                    new Point(65, 50),
                    new Point(115, 50),
                    new Point(125, 58),
                    new Point(115, 66),
                    new Point(65, 66))),
            new GlowPrototypeCase(
                "Segment.Vertical",
                CreatePolygon(
                    new Point(74, 35),
                    new Point(82, 45),
                    new Point(82, 95),
                    new Point(74, 105),
                    new Point(66, 95),
                    new Point(66, 45))),
            new GlowPrototypeCase(
                "Segment.Diagonal",
                CreatePolygon(
                    new Point(58, 38),
                    new Point(70, 38),
                    new Point(122, 102),
                    new Point(110, 102))),
            new GlowPrototypeCase(
                "Segment.ColonDots",
                new GeometryGroup
                {
                    Children =
                    {
                        new EllipseGeometry(new Rect(84, 46, 12, 12)),
                        new EllipseGeometry(new Rect(84, 82, 12, 12))
                    }
                })
        };
    }

    private static IReadOnlyList<GlowBrushCase> CreateBrushes()
    {
        return new[]
        {
            new GlowBrushCase("Solid", Brushes.Cyan),
            new GlowBrushCase("AlphaSolid", new SolidColorBrush(Color.FromArgb(128, 0, 255, 255))),
            new GlowBrushCase(
                "LinearGradient",
                new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Colors.Cyan, 0),
                        new GradientStop(Colors.Blue, 1)
                    }
                }),
            new GlowBrushCase(
                "RadialGradient",
                new RadialGradientBrush
                {
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Colors.White, 0),
                        new GradientStop(Colors.Cyan, 1)
                    }
                })
        };
    }

    private static StreamGeometry CreatePolygon(params Point[] points)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(points[0], true);
        for (var i = 1; i < points.Length; i++)
        {
            context.LineTo(points[i], true);
        }

        context.EndFigure(true);
        return geometry;
    }

    private static IReadOnlyList<IGlowPrototypeRenderer> CreateRenderers(IBrush brush, double radius)
    {
        return new IGlowPrototypeRenderer[]
        {
            new VectorExpansionGlowPrototype(brush, 0.65, radius),
            new ScopedBlurEffectGlowPrototype(brush, 0.65, radius)
        };
    }

    private static IReadOnlyList<GlowBehaviorValidationResult> RunBehaviorValidations()
    {
        var renderer = new ScopedBlurEffectGlowPrototype(Brushes.Cyan, 0.65, 12);
        var initialBuildCount = renderer.EffectBuildCount;
        renderer.UpdateBrush(Brushes.Magenta);
        renderer.UpdateOpacity(0.4);
        var brushOpacityReuse = renderer.EffectBuildCount == initialBuildCount;
        renderer.UpdateRadius(18);
        var radiusReuse = renderer.EffectBuildCount == initialBuildCount
                          && renderer.RadiusUpdateCount == 1;

        var layerRenderer = new ScopedBlurEffectGlowPrototype(Brushes.Cyan, 0.65, 12);
        var layerFrame = CaptureFrame(
            layerRenderer,
            new EllipseGeometry(new Rect(70, 50, 40, 40)),
            1,
            new GlowCaptureOptions(Decorations: true));
        var inactiveColor = layerFrame.GetColor(22, 70);
        var borderColor = layerFrame.GetColor(2, 70);
        var backgroundColor = layerFrame.GetColor(36, 20);
        var layersIsolated = layerFrame.IsColor(22, 70, Colors.Gray)
                             && layerFrame.IsColor(2, 70, Colors.Yellow)
                             && layerFrame.IsColor(36, 20, Colors.Black);

        var clipBounds = new Rect(60, 40, 60, 60);
        var clipFrame = CaptureFrame(
            new ScopedBlurEffectGlowPrototype(Brushes.Cyan, 0.65, 24),
            new EllipseGeometry(new Rect(70, 50, 40, 40)),
            1,
            new GlowCaptureOptions(ClipBounds: clipBounds));
        var clipIsStrict = clipFrame.CountVisibleOutside(clipBounds) == 0;

        var localGeometry = new EllipseGeometry(new Rect(0, 0, 40, 40));
        var fullScaleFrame = CaptureFrame(
            new ScopedBlurEffectGlowPrototype(Brushes.Cyan, 0.65, 12),
            localGeometry,
            1,
            new GlowCaptureOptions(ContentScale: 1, ContentOffset: new Vector(70, 50)));
        var halfScaleFrame = CaptureFrame(
            new ScopedBlurEffectGlowPrototype(Brushes.Cyan, 0.65, 12),
            localGeometry,
            1,
            new GlowCaptureOptions(ContentScale: 0.5, ContentOffset: new Vector(70, 50)));
        var fullHalo = CalculateMaximumHalo(
            fullScaleFrame.GetVisibleBounds(),
            new Rect(70, 50, 40, 40));
        var halfHalo = CalculateMaximumHalo(
            halfScaleFrame.GetVisibleBounds(),
            new Rect(70, 50, 20, 20));
        var scaleRatio = halfHalo / fullHalo;
        var scaleFollowsContent = scaleRatio is >= 0.35 and <= 0.65;

        var results = new List<GlowBehaviorValidationResult>
        {
            new GlowBehaviorValidationResult(
                "ScopedEffect.BrushOpacityReuse",
                brushOpacityReuse,
                $"EffectBuildCount={renderer.EffectBuildCount}"),
            new GlowBehaviorValidationResult(
                "ScopedEffect.RadiusUpdatesCurrentEffect",
                radiusReuse,
                $"EffectBuildCount={renderer.EffectBuildCount}, RadiusUpdateCount={renderer.RadiusUpdateCount}"),
            new GlowBehaviorValidationResult(
                "ScopedEffect.LayerIsolation",
                layersIsolated,
                $"Background={backgroundColor}, Inactive={inactiveColor}, Border={borderColor}"),
            new GlowBehaviorValidationResult(
                "ScopedEffect.StrictClip",
                clipIsStrict,
                $"VisibleOutsideClip={clipFrame.CountVisibleOutside(clipBounds)}"),
            new GlowBehaviorValidationResult(
                "ScopedEffect.ScaleFollowsContent",
                scaleFollowsContent,
                $"FullHalo={fullHalo:0.00}, HalfHalo={halfHalo:0.00}, Ratio={scaleRatio:0.000}")
        };

        foreach (var renderScaling in new[] { 1d, 1.25, 1.5, 2d })
        {
            foreach (var radius in new[] { 6d, 12d, 24d })
            {
                var groupedFrame = CaptureGroupedFrame(renderScaling, radius);
                var sourceBounds = new Rect(52, 48, 84, 24);
                var outsidePixels = AnalyzePixels(groupedFrame, renderScaling, sourceBounds)
                    .VisibleOutsideSourcePixels;
                results.Add(new GlowBehaviorValidationResult(
                    $"ScopedEffect.Grouped.Dpi{renderScaling:0.##}.Radius{radius:0}",
                    outsidePixels > 0,
                    $"VisibleOutsideSourcePixels={outsidePixels}"));
            }
        }

        return results;
    }

    private static double CalculateMaximumHalo(Rect visibleBounds, Rect sourceBounds)
    {
        return new[]
        {
            sourceBounds.Left - visibleBounds.Left,
            visibleBounds.Right - sourceBounds.Right,
            sourceBounds.Top - visibleBounds.Top,
            visibleBounds.Bottom - sourceBounds.Bottom
        }.Max();
    }

    private static GlowPixelResult Capture(
        IGlowPrototypeRenderer renderer,
        Geometry sourceGeometry,
        double renderScaling)
    {
        var frame = CaptureFrame(
            renderer,
            sourceGeometry,
            renderScaling,
            new GlowCaptureOptions(ContentScale: 1));
        return AnalyzePixels(frame, renderScaling, sourceGeometry.Bounds);
    }

    private static PixelFrame CaptureFrame(
        IGlowPrototypeRenderer renderer,
        Geometry sourceGeometry,
        double renderScaling,
        GlowCaptureOptions options)
    {
        var control = new GlowPrototypeControl(renderer, sourceGeometry, options)
        {
            Width = Width,
            Height = Height
        };
        var window = new Window
        {
            Width = Width,
            Height = Height,
            Background = Brushes.Black,
            Content = control
        };

        try
        {
            window.Show();
            window.SetRenderScaling(renderScaling);
            Dispatcher.UIThread.RunJobs();
            using var frame = window.CaptureRenderedFrame();
            if (frame is null)
            {
                throw new InvalidOperationException("Headless renderer did not produce a frame.");
            }

            using var framebuffer = frame.Lock();
            var bytes = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
            Marshal.Copy(framebuffer.Address, bytes, 0, bytes.Length);
            return new PixelFrame(
                bytes,
                framebuffer.Size.Width,
                framebuffer.Size.Height,
                framebuffer.RowBytes,
                renderScaling,
                framebuffer.Format);
        }
        finally
        {
            window.Close();
        }
    }

    private static PixelFrame CaptureGroupedFrame(double renderScaling, double radius)
    {
        var control = new GroupedGlowPrototypeControl(radius)
        {
            Width = Width,
            Height = Height
        };
        var window = new Window
        {
            Width = Width,
            Height = Height,
            Background = Brushes.Black,
            Content = control
        };

        try
        {
            window.Show();
            window.SetRenderScaling(renderScaling);
            Dispatcher.UIThread.RunJobs();
            using var frame = window.CaptureRenderedFrame();
            if (frame is null)
            {
                throw new InvalidOperationException("Headless renderer did not produce a grouped frame.");
            }

            using var framebuffer = frame.Lock();
            var bytes = new byte[framebuffer.RowBytes * framebuffer.Size.Height];
            Marshal.Copy(framebuffer.Address, bytes, 0, bytes.Length);
            return new PixelFrame(
                bytes,
                framebuffer.Size.Width,
                framebuffer.Size.Height,
                framebuffer.RowBytes,
                renderScaling,
                framebuffer.Format);
        }
        finally
        {
            window.Close();
        }
    }

    private static GlowPixelResult AnalyzePixels(
        PixelFrame frame,
        double renderScaling,
        Rect sourceBounds)
    {
        var inside = 0;
        var outside = 0;
        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++)
            {
                if (!frame.IsVisible(x, y))
                {
                    continue;
                }

                var point = new Point((x + 0.5) / renderScaling, (y + 0.5) / renderScaling);
                if (sourceBounds.Contains(point))
                {
                    inside++;
                }
                else
                {
                    outside++;
                }
            }
        }

        return new GlowPixelResult(outside, inside);
    }

    private static GlowPerformanceMeasurement Measure(
        IGlowPrototypeRenderer renderer,
        Geometry sourceGeometry,
        int frameCount)
    {
        for (var i = 0; i < 20; i++)
        {
            RenderToDrawingGroup(renderer, sourceGeometry);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < frameCount; i++)
        {
            _drawingSink = RenderToDrawingGroup(renderer, sourceGeometry);
        }

        stopwatch.Stop();
        return new GlowPerformanceMeasurement(
            stopwatch.Elapsed,
            GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
    }

    private static DrawingGroup RenderToDrawingGroup(
        IGlowPrototypeRenderer renderer,
        Geometry sourceGeometry)
    {
        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        renderer.Render(context, sourceGeometry);
        context.DrawGeometry(Brushes.White, null, sourceGeometry);
        return drawing;
    }

    private static string RenderSummary(
        IReadOnlyList<GlowPrototypePixelResult> pixelResults,
        IReadOnlyList<GlowPrototypePerformanceResult> performanceResults,
        IReadOnlyList<GlowBehaviorValidationResult> validationResults)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Glow prototype pixel cases: {pixelResults.Count}");
        foreach (var route in pixelResults.GroupBy(item => item.Route))
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{route.Key}: passed={route.Count(item => item.VisibleOutsideSourcePixels > 0)}/{route.Count()}, minOutsidePixels={route.Min(item => item.VisibleOutsideSourcePixels)}");
        }

        builder.AppendLine("Command-submission smoke (40x40 circle, radius 12, opacity 0.65)");
        builder.AppendLine("Route | Frames | us/frame | bytes/frame");
        foreach (var result in performanceResults)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{result.Route} | {result.FrameCount} | {result.MicrosecondsPerFrame:0.00} | {result.BytesPerFrame:0.0}");
        }

        foreach (var result in validationResults)
        {
            builder.AppendLine($"{result.Name}: {(result.Passed ? "PASS" : "FAIL")} ({result.Detail})");
        }

        return builder.ToString();
    }

    private static string RenderMarkdown(
        IReadOnlyList<GlowPrototypePixelResult> pixelResults,
        IReadOnlyList<GlowPrototypePerformanceResult> performanceResults,
        IReadOnlyList<GlowBehaviorValidationResult> validationResults)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# LED Glow Prototype Smoke Evaluation");
        builder.AppendLine();
        builder.AppendLine($"- Date: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine("- Scope: Headless pixel feasibility plus CPU-side DrawingGroup command submission");
        builder.AppendLine("- Corpus: 3 Matrix shapes and 4 Segment geometry categories");
        builder.AppendLine("- Matrix: 2 routes x 7 geometries x 4 brushes x 3 radii x 4 RenderScaling values = 672 pixel cases");
        builder.AppendLine("- Warning: command-submission timings are single-process smoke data and exclude real GPU presentation cost");
        builder.AppendLine();
        builder.AppendLine("## Pixel Feasibility");
        builder.AppendLine();
        builder.AppendLine("| Route | Passed | Total | Minimum visible pixels outside source bounds |");
        builder.AppendLine("|---|---:|---:|---:|");
        foreach (var route in pixelResults.GroupBy(item => item.Route))
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {route.Key} | {route.Count(item => item.VisibleOutsideSourcePixels > 0)} | {route.Count()} | {route.Min(item => item.VisibleOutsideSourcePixels)} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Command Submission Smoke");
        builder.AppendLine();
        builder.AppendLine("| Route | Frames | us/frame | bytes/frame |");
        builder.AppendLine("|---|---:|---:|---:|");
        foreach (var result in performanceResults)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {result.Route} | {result.FrameCount} | {result.MicrosecondsPerFrame:0.00} | {result.BytesPerFrame:0.0} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Behavior Counters");
        builder.AppendLine();
        builder.AppendLine("| Validation | Result | Detail |");
        builder.AppendLine("|---|---|---|");
        foreach (var result in validationResults)
        {
            builder.AppendLine($"| {result.Name} | {(result.Passed ? "PASS" : "FAIL")} | {result.Detail} |");
        }

        return builder.ToString();
    }

    private sealed class GlowPrototypeControl(
        IGlowPrototypeRenderer renderer,
        Geometry sourceGeometry,
        GlowCaptureOptions options) : Control
    {
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
            if (options.Decorations)
            {
                context.DrawRectangle(Brushes.Gray, null, new Rect(16, 52, 20, 36));
            }

            if (options.ClipBounds is { } clipBounds)
            {
                using (context.PushClip(clipBounds))
                {
                    RenderSource(context);
                }
            }
            else
            {
                RenderSource(context);
            }

            if (options.Decorations)
            {
                context.DrawRectangle(
                    null,
                    new Pen(Brushes.Yellow, 4),
                    new RoundedRect(new Rect(Bounds.Size).Deflate(2)));
            }
        }

        private void RenderSource(DrawingContext context)
        {
            using (context.PushTransform(
                       Avalonia.Matrix.CreateScale(options.ContentScale, options.ContentScale)
                       * Avalonia.Matrix.CreateTranslation(options.ContentOffset.X, options.ContentOffset.Y)))
            {
                renderer.Render(context, sourceGeometry);
                context.DrawGeometry(Brushes.White, null, sourceGeometry);
            }
        }
    }

    private sealed class GroupedGlowPrototypeControl : Control
    {
        private static readonly Geometry Source = new EllipseGeometry(new Rect(0, 0, 24, 24));
        private static readonly IReadOnlyList<Point> Offsets =
        [
            new Point(52, 48),
            new Point(82, 48),
            new Point(112, 48)
        ];
        private readonly ScopedBlurEffectGlowPrototype _renderer;

        public GroupedGlowPrototypeControl(double radius)
        {
            _renderer = new ScopedBlurEffectGlowPrototype(Brushes.Cyan, 0.65, radius);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
            _renderer.RenderMany(context, Source, Offsets, 0, Offsets.Count, new Rect(52, 48, 84, 24));
            foreach (var offset in Offsets)
            {
                using (context.PushTransform(Avalonia.Matrix.CreateTranslation(offset.X, offset.Y)))
                {
                    context.DrawGeometry(Brushes.White, null, Source);
                }
            }
        }
    }

    private readonly record struct GlowCaptureOptions(
        Rect? ClipBounds = null,
        bool Decorations = false,
        double ContentScale = 1,
        Vector ContentOffset = default);

    private sealed record PixelFrame(
        byte[] Bytes,
        int Width,
        int Height,
        int RowBytes,
        double RenderScaling,
        PixelFormat Format)
    {
        public bool IsColor(int logicalX, int logicalY, Color expected, int tolerance = 3)
        {
            var actual = GetColor(logicalX, logicalY);
            return Math.Abs(actual.B - expected.B) <= tolerance
                   && Math.Abs(actual.G - expected.G) <= tolerance
                   && Math.Abs(actual.R - expected.R) <= tolerance;
        }

        public Color GetColor(int logicalX, int logicalY)
        {
            var x = Math.Clamp((int)(logicalX * RenderScaling), 0, Width - 1);
            var y = Math.Clamp((int)(logicalY * RenderScaling), 0, Height - 1);
            var offset = y * RowBytes + x * 4;
            return Format == PixelFormat.Rgba8888
                ? Color.FromArgb(
                    Bytes[offset + 3],
                    Bytes[offset],
                    Bytes[offset + 1],
                    Bytes[offset + 2])
                : Color.FromArgb(
                    Bytes[offset + 3],
                    Bytes[offset + 2],
                    Bytes[offset + 1],
                    Bytes[offset]);
        }

        public int CountVisibleOutside(Rect logicalBounds)
        {
            var count = 0;
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var point = new Point((x + 0.5) / RenderScaling, (y + 0.5) / RenderScaling);
                    if (!logicalBounds.Contains(point) && IsVisible(x, y))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public Rect GetVisibleBounds()
        {
            var minX = Width;
            var minY = Height;
            var maxX = -1;
            var maxY = -1;
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (!IsVisible(x, y))
                    {
                        continue;
                    }

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            return maxX < minX
                ? default
                : new Rect(
                    minX / RenderScaling,
                    minY / RenderScaling,
                    (maxX - minX + 1) / RenderScaling,
                    (maxY - minY + 1) / RenderScaling);
        }

        public bool IsVisible(int x, int y)
        {
            var offset = y * RowBytes + x * 4;
            return Bytes[offset] > 8 || Bytes[offset + 1] > 8 || Bytes[offset + 2] > 8;
        }
    }

    private sealed record GlowPrototypeCase(string Name, Geometry Geometry);

    private sealed record GlowBrushCase(string Name, IBrush Brush);

    private readonly record struct GlowPixelResult(
        int VisibleOutsideSourcePixels,
        int VisibleInsideSourcePixels);

    private readonly record struct GlowPerformanceMeasurement(TimeSpan Elapsed, long AllocatedBytes);

    private sealed record GlowPrototypePixelResult(
        string Route,
        string Geometry,
        string Brush,
        double Radius,
        double RenderScaling,
        int VisibleOutsideSourcePixels,
        int VisibleInsideSourcePixels);

    private sealed record GlowPrototypePerformanceResult(
        string Route,
        int FrameCount,
        TimeSpan Elapsed,
        long AllocatedBytes)
    {
        public double MicrosecondsPerFrame => Elapsed.TotalMilliseconds * 1000 / FrameCount;

        public double BytesPerFrame => AllocatedBytes / (double)FrameCount;
    }

    private sealed record GlowBehaviorValidationResult(string Name, bool Passed, string Detail);
}

internal interface IGlowPrototypeRenderer
{
    string Name { get; }

    void Render(DrawingContext context, Geometry sourceGeometry);
}

internal sealed class NoGlowPrototype : IGlowPrototypeRenderer
{
    public string Name => "Baseline.NoGlow";

    public void Render(DrawingContext context, Geometry sourceGeometry)
    {
    }
}

internal sealed class OpacityOnlyGlowPrototype : IGlowPrototypeRenderer
{
    private readonly IBrush _brush;
    private readonly double _opacity;

    public OpacityOnlyGlowPrototype(IBrush brush, double opacity)
    {
        _brush = brush;
        _opacity = opacity;
    }

    public string Name => "OpacityOnly";

    public void Render(DrawingContext context, Geometry sourceGeometry)
    {
        using (context.PushOpacity(_opacity))
        {
            context.DrawGeometry(_brush, null, sourceGeometry);
        }
    }
}

internal sealed class VectorExpansionGlowPrototype : IGlowPrototypeRenderer
{
    private readonly IBrush _brush;
    private readonly double _opacity;
    private readonly IReadOnlyList<Pen> _pens;

    public VectorExpansionGlowPrototype(IBrush brush, double opacity, double radius)
    {
        _brush = brush;
        _opacity = opacity;
        _pens = new[]
        {
            new Pen(brush, radius * 2),
            new Pen(brush, radius * 1.5),
            new Pen(brush, radius),
            new Pen(brush, radius * 0.5)
        };
    }

    public string Name => "A.VectorExpansion";

    public void Render(DrawingContext context, Geometry sourceGeometry)
    {
        for (var i = 0; i < _pens.Count; i++)
        {
            var layerOpacity = _opacity * (i + 1) / (_pens.Count * 2);
            using (context.PushOpacity(layerOpacity))
            {
                context.DrawGeometry(null, _pens[i], sourceGeometry);
            }
        }

        using (context.PushOpacity(_opacity * 0.25))
        {
            context.DrawGeometry(_brush, null, sourceGeometry);
        }
    }
}

internal sealed class ScopedBlurEffectGlowPrototype : IGlowPrototypeRenderer
{
    private IBrush _brush;
    private double _opacity;
    private readonly BlurEffect _effect;

    public ScopedBlurEffectGlowPrototype(IBrush brush, double opacity, double radius)
    {
        _brush = brush;
        _opacity = opacity;
        _effect = new BlurEffect { Radius = radius };
        EffectBuildCount = 1;
    }

    public string Name => "C.ScopedBlurEffect";

    public int EffectBuildCount { get; }

    public int RadiusUpdateCount { get; private set; }

    public void UpdateBrush(IBrush brush)
    {
        _brush = brush;
    }

    public void UpdateOpacity(double opacity)
    {
        _opacity = opacity;
    }

    public void UpdateRadius(double radius)
    {
        _effect.Radius = radius;
        RadiusUpdateCount++;
    }

    public void Render(DrawingContext context, Geometry sourceGeometry)
    {
        using (context.PushOpacity(_opacity))
        using (context.PushEffect(_effect, sourceGeometry.Bounds))
        {
            context.DrawGeometry(_brush, null, sourceGeometry);
        }
    }

    public void RenderMany(
        DrawingContext context,
        Geometry sourceGeometry,
        IReadOnlyList<Point> offsets,
        int start,
        int count,
        Rect effectBounds)
    {
        using (context.PushOpacity(_opacity))
        using (context.PushEffect(_effect, effectBounds))
        {
            for (var i = start; i < start + count; i++)
            {
                var offset = offsets[i];
                using (context.PushTransform(Avalonia.Matrix.CreateTranslation(offset.X, offset.Y)))
                {
                    context.DrawGeometry(_brush, null, sourceGeometry);
                }
            }
        }
    }
}
