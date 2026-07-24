using System.Diagnostics;
using System.Globalization;
using System.Text;
using AtomUI.Labs.Controls.Led.Matrix;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.Led.Performance;

internal static class Program
{
    private const int DefaultCount = 20;
    private const int DefaultDynamicFrames = 600;
    private const int DefaultSoakFrames = 36_000;
    private const int DynamicWarmupFrames = 20;
    private const int DynamicTextPoolSize = 1_000;

    private static string? _textSink;
    private static DrawingGroup? _drawingSink;

    [STAThread]
    public static int Main(string[] args)
    {
        var options = PerformanceOptions.Parse(args);
        AppBuilder.Configure<PerformanceApplication>()
                  .UseHeadless(new AvaloniaHeadlessPlatformOptions
                  {
                      UseHeadlessDrawing = false
                  })
                  .UseSkia()
                  .SetupWithoutStarting();

        if (options.RunGlowPrototypes)
        {
            return GlowPrototypeRunner.Run(options.Count, options.MarkdownOutputPath);
        }

        if (options.RunFormalGlow)
        {
            return FormalGlowPerformanceRunner.Run(options.DynamicFrames, options.MarkdownOutputPath);
        }

        var results = new[]
        {
            MeasureMatrixRender("Matrix.CachedRender.8.Clip", "12345678", MatrixOverflowMode.Clip, options.Count, false),
            MeasureMatrixRender("Matrix.DynamicText.8.Clip", "00000000", MatrixOverflowMode.Clip, options.Count, true),
            MeasureMatrixRender("Matrix.CachedRender.1000.Clip", new string('8', 1_000), MatrixOverflowMode.Clip, options.Count, false),
            MeasureMatrixRender("Matrix.CachedRender.10000.Clip", new string('8', 10_000), MatrixOverflowMode.Clip, options.Count, false),
            MeasureMatrixRender("Matrix.CachedRender.1000.ScaleDown", new string('8', 1_000), MatrixOverflowMode.ScaleDown, options.Count, false)
        };
        var dynamicResults = new[]
        {
            MeasureDynamicLoad(6, false, options.DynamicFrames),
            MeasureDynamicLoad(6, true, options.DynamicFrames),
            MeasureDynamicLoad(8, false, options.DynamicFrames),
            MeasureDynamicLoad(8, true, options.DynamicFrames),
            MeasureDynamicLoad(16, false, options.DynamicFrames),
            MeasureDynamicLoad(16, true, options.DynamicFrames)
        };
        var allocationResults = MeasureAllocationAttribution(options.DynamicFrames);
        var soakResult = MeasureDynamicSoak(options.SoakFrames);

        Console.WriteLine(RenderTable(results));
        Console.WriteLine(RenderDynamicLoadTable(dynamicResults));
        Console.WriteLine(RenderAllocationTable(allocationResults));
        Console.WriteLine(RenderSoakTable(soakResult));
        if (!string.IsNullOrWhiteSpace(options.MarkdownOutputPath))
        {
            var fullPath = Path.GetFullPath(options.MarkdownOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(
                fullPath,
                RenderMarkdown(results, dynamicResults, allocationResults, soakResult),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine($"Wrote markdown result: {fullPath}");
        }

        return 0;
    }

    private static MatrixDynamicLoadResult MeasureDynamicLoad(
        int characterCount,
        bool showInactiveDots,
        int frameCount)
    {
        var display = new MatrixDisplay
        {
            DotSize          = 6,
            DotSpacing       = 2,
            CharacterSpacing = 8,
            Padding          = default,
            ActiveBrush      = Brushes.White,
            InactiveBrush    = showInactiveDots ? Brushes.Gray : null,
            ShowInactiveDots = showInactiveDots
        };
        var viewport = new Size(800, 80);
        var bounds   = new Rect(viewport);
        for (var frame = 0; frame < DynamicWarmupFrames; frame++)
        {
            UpdateDynamicDisplay(display, viewport, bounds, characterCount, frame);
        }

        var geometryBuildCount = display.GeometryBuildCount;
        var layoutVersion       = display.LayoutCacheVersion;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch       = Stopwatch.StartNew();
        for (var frame = DynamicWarmupFrames; frame < frameCount + DynamicWarmupFrames; frame++)
        {
            UpdateDynamicDisplay(display, viewport, bounds, characterCount, frame);
        }

        stopwatch.Stop();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var drawing = Render(display);
        return new MatrixDynamicLoadResult(
            characterCount,
            showInactiveDots,
            frameCount,
            stopwatch.Elapsed,
            allocatedBytes,
            display.LayoutCacheVersion - layoutVersion,
            display.GeometryBuildCount - geometryBuildCount,
            display.GeometryCacheCount,
            CountGeometryDrawings(drawing));
    }

    private static void UpdateDynamicDisplay(
        MatrixDisplay display,
        Size viewport,
        Rect bounds,
        int characterCount,
        int frame)
    {
        UpdateDynamicDisplay(display, viewport, bounds, CreateDynamicText(characterCount, frame));
    }

    private static void UpdateDynamicDisplay(
        MatrixDisplay display,
        Size viewport,
        Rect bounds,
        string text)
    {
        display.Text = text;
        display.Measure(viewport);
        display.Arrange(bounds);
        _drawingSink = Render(display);
    }

    private static string CreateDynamicText(int characterCount, int value)
    {
        return characterCount switch
        {
            6 => (value % 1_000_000).ToString("D6", CultureInfo.InvariantCulture),
            8 => (value % 100_000_000).ToString("D8", CultureInfo.InvariantCulture),
            16 => "TEMP"
                  + (value % 10_000).ToString("D4", CultureInfo.InvariantCulture)
                  + "RPM"
                  + (value % 100_000).ToString("D5", CultureInfo.InvariantCulture),
            _ => throw new ArgumentOutOfRangeException(nameof(characterCount))
        };
    }

    private static string[] CreateDynamicTextPool(int characterCount, int count)
    {
        var texts = new string[count];
        for (var i = 0; i < texts.Length; i++)
        {
            texts[i] = CreateDynamicText(characterCount, i);
        }

        return texts;
    }

    private static IReadOnlyList<MatrixAllocationResult> MeasureAllocationAttribution(int frameCount)
    {
        var viewport = new Size(800, 80);
        var bounds = new Rect(viewport);
        var texts = CreateDynamicTextPool(16, Math.Max(frameCount, DynamicWarmupFrames));

        var layoutDisplay = CreateDynamicDisplay(true);
        WarmDynamicDisplay(layoutDisplay, viewport, bounds, texts);

        var emptyRenderDisplay = CreateDynamicDisplay(false);
        emptyRenderDisplay.Text = string.Empty;
        emptyRenderDisplay.Measure(viewport);
        emptyRenderDisplay.Arrange(bounds);
        _drawingSink = Render(emptyRenderDisplay);

        var activeRenderDisplay = CreateDynamicDisplay(false);
        WarmDynamicDisplay(activeRenderDisplay, viewport, bounds, texts);

        var dualLayerRenderDisplay = CreateDynamicDisplay(true);
        WarmDynamicDisplay(dualLayerRenderDisplay, viewport, bounds, texts);

        var dynamicDisplay = CreateDynamicDisplay(true);
        WarmDynamicDisplay(dynamicDisplay, viewport, bounds, texts);

        var endToEndDisplay = CreateDynamicDisplay(true);
        WarmDynamicDisplay(endToEndDisplay, viewport, bounds, texts);

        return new[]
        {
            MeasureAllocationStage(
                "Caller.TextFormatting.16",
                frameCount,
                frame => _textSink = CreateDynamicText(16, frame)),
            MeasureAllocationStage(
                "Harness.EmptyDrawingGroup",
                frameCount,
                _ => _drawingSink = RenderEmpty()),
            MeasureAllocationStage(
                "Matrix.LayoutOnly.Precomputed.16",
                frameCount,
                frame =>
                {
                    layoutDisplay.Text = texts[frame % texts.Length];
                    layoutDisplay.Measure(viewport);
                    layoutDisplay.Arrange(bounds);
                }),
            MeasureAllocationStage(
                "Matrix.CachedRenderOnly.EmptyText",
                frameCount,
                _ => _drawingSink = Render(emptyRenderDisplay)),
            MeasureAllocationStage(
                "Matrix.CachedRenderOnly.16.InactiveOff",
                frameCount,
                _ => _drawingSink = Render(activeRenderDisplay)),
            MeasureAllocationStage(
                "Matrix.CachedRenderOnly.16.InactiveOn",
                frameCount,
                _ => _drawingSink = Render(dualLayerRenderDisplay)),
            MeasureAllocationStage(
                "Matrix.DynamicPrecomputed.16.InactiveOn",
                frameCount,
                frame => UpdateDynamicDisplay(
                    dynamicDisplay,
                    viewport,
                    bounds,
                    texts[frame % texts.Length])),
            MeasureAllocationStage(
                "EndToEnd.DynamicFormatted.16.InactiveOn",
                frameCount,
                frame => UpdateDynamicDisplay(
                    endToEndDisplay,
                    viewport,
                    bounds,
                    CreateDynamicText(16, frame)))
        };
    }

    private static MatrixAllocationResult MeasureAllocationStage(
        string name,
        int operationCount,
        Action<int> operation)
    {
        _textSink = null;
        _drawingSink = null;
        ForceFullCollection();

        var stopwatch = new Stopwatch();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Start();
        for (var operationIndex = 0; operationIndex < operationCount; operationIndex++)
        {
            operation(operationIndex);
        }

        stopwatch.Stop();
        return new MatrixAllocationResult(
            name,
            operationCount,
            stopwatch.Elapsed,
            GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
    }

    private static MatrixSoakResult MeasureDynamicSoak(int frameCount)
    {
        var viewport = new Size(800, 80);
        var bounds = new Rect(viewport);
        var textPool = CreateDynamicTextPool(16, Math.Min(DynamicTextPoolSize, frameCount));
        var display = CreateDynamicDisplay(true);
        WarmDynamicDisplay(display, viewport, bounds, textPool);

        var layoutVersion = display.LayoutCacheVersion;
        var geometryBuildCount = display.GeometryBuildCount;
        var initialGeometryCacheCount = display.GeometryCacheCount;
        _textSink = null;
        _drawingSink = null;
        ForceFullCollection();

        var retainedBytesBefore = GC.GetTotalMemory(forceFullCollection: false);
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var peakManagedBytes = retainedBytesBefore;
        var sampleInterval = Math.Max(1, frameCount / 20);
        var stopwatch = Stopwatch.StartNew();
        for (var frame = 0; frame < frameCount; frame++)
        {
            UpdateDynamicDisplay(
                display,
                viewport,
                bounds,
                textPool[frame % textPool.Length]);
            if ((frame + 1) % sampleInterval == 0)
            {
                peakManagedBytes = Math.Max(peakManagedBytes, GC.GetTotalMemory(forceFullCollection: false));
            }
        }

        stopwatch.Stop();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var gen0Collections = GC.CollectionCount(0) - gen0Before;
        var gen1Collections = GC.CollectionCount(1) - gen1Before;
        var gen2Collections = GC.CollectionCount(2) - gen2Before;
        _drawingSink = null;
        ForceFullCollection();
        var retainedBytesAfter = GC.GetTotalMemory(forceFullCollection: false);
        var drawing = Render(display);

        return new MatrixSoakResult(
            frameCount,
            stopwatch.Elapsed,
            allocatedBytes,
            gen0Collections,
            gen1Collections,
            gen2Collections,
            retainedBytesBefore,
            retainedBytesAfter,
            peakManagedBytes,
            display.LayoutCacheVersion - layoutVersion,
            display.GeometryBuildCount - geometryBuildCount,
            initialGeometryCacheCount,
            display.GeometryCacheCount,
            CountGeometryDrawings(drawing));
    }

    private static MatrixDisplay CreateDynamicDisplay(bool showInactiveDots)
    {
        return new MatrixDisplay
        {
            DotSize          = 6,
            DotSpacing       = 2,
            CharacterSpacing = 8,
            Padding          = default,
            ActiveBrush      = Brushes.White,
            InactiveBrush    = showInactiveDots ? Brushes.Gray : null,
            ShowInactiveDots = showInactiveDots
        };
    }

    private static void WarmDynamicDisplay(
        MatrixDisplay display,
        Size viewport,
        Rect bounds,
        IReadOnlyList<string> texts)
    {
        for (var frame = 0; frame < DynamicWarmupFrames; frame++)
        {
            UpdateDynamicDisplay(display, viewport, bounds, texts[frame % texts.Count]);
        }
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static MatrixPerformanceResult MeasureMatrixRender(
        string name,
        string text,
        MatrixOverflowMode overflowMode,
        int updateCount,
        bool updateText)
    {
        var display = new MatrixDisplay
        {
            Text             = text,
            DotSize          = 6,
            DotSpacing       = 2,
            CharacterSpacing = 8,
            Padding          = default,
            Width            = 320,
            Height           = 80,
            OverflowMode     = overflowMode,
            ActiveBrush      = Brushes.White,
            InactiveBrush    = null,
            ShowInactiveDots = false
        };
        var viewport = new Size(320, 80);
        var bounds   = new Rect(viewport);
        display.Measure(viewport);
        display.Arrange(bounds);

        for (var i = 0; i < 10; i++)
        {
            UpdateAndRender(display, viewport, bounds, updateText, i);
        }

        var submittedGeometryCommands = CountGeometryDrawings(Render(display));
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch       = Stopwatch.StartNew();
        for (var i = 0; i < updateCount; i++)
        {
            UpdateAndRender(display, viewport, bounds, updateText, i);
        }

        stopwatch.Stop();
        return new MatrixPerformanceResult(
            name,
            text.Length,
            updateCount,
            stopwatch.Elapsed,
            GC.GetAllocatedBytesForCurrentThread() - allocatedBefore,
            submittedGeometryCommands);
    }

    private static void UpdateAndRender(
        MatrixDisplay display,
        Size viewport,
        Rect bounds,
        bool updateText,
        int iteration)
    {
        if (updateText)
        {
            display.Text = iteration.ToString("D8", CultureInfo.InvariantCulture);
            display.Measure(viewport);
            display.Arrange(bounds);
        }

        Render(display);
    }

    private static DrawingGroup Render(MatrixDisplay display)
    {
        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        display.Render(context);
        return drawing;
    }

    private static DrawingGroup RenderEmpty()
    {
        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        return drawing;
    }

    private static int CountGeometryDrawings(Drawing drawing)
    {
        if (drawing is GeometryDrawing)
        {
            return 1;
        }

        return drawing is DrawingGroup group
            ? group.Children.Sum(CountGeometryDrawings)
            : 0;
    }

    private static string RenderTable(IReadOnlyList<MatrixPerformanceResult> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Scenario                                  Chars  Updates  Total ms  us/update  KB total  bytes/update  geometry commands");
        builder.AppendLine("-------------------------------------------------------------------------------------------------------------------");
        foreach (var result in results)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{result.Name,-42}{result.TextLength,7}{result.UpdateCount,9}{result.Elapsed.TotalMilliseconds,10:0.00}{result.MicrosecondsPerUpdate,11:0.00}{result.AllocatedBytes / 1024.0,10:0.0}{result.BytesPerUpdate,14:0.0}{result.SubmittedGeometryCommands,18}");
        }

        return builder.ToString();
    }

    private static string RenderDynamicLoadTable(IReadOnlyList<MatrixDynamicLoadResult> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Dynamic load  Inactive  Frames  Total ms  us/frame  bytes/frame  Layout builds  Geometry builds  Cache  Commands/frame");
        builder.AppendLine("---------------------------------------------------------------------------------------------------------------------");
        foreach (var result in results)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{result.CharacterCount + " chars",-14}{(result.ShowInactiveDots ? "On" : "Off"),-10}{result.FrameCount,7}{result.Elapsed.TotalMilliseconds,10:0.00}{result.MicrosecondsPerFrame,10:0.00}{result.BytesPerFrame,13:0.0}{result.LayoutBuilds,15}{result.GeometryBuilds,17}{result.GeometryCacheCount,7}{result.GeometryCommandsPerFrame,16}");
        }

        return builder.ToString();
    }

    private static string RenderAllocationTable(IReadOnlyList<MatrixAllocationResult> results)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Allocation attribution                            Operations  Total ms  us/op  bytes/op");
        builder.AppendLine("--------------------------------------------------------------------------------------");
        foreach (var result in results)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{result.Name,-50}{result.OperationCount,11}{result.Elapsed.TotalMilliseconds,10:0.00}{result.MicrosecondsPerOperation,8:0.00}{result.BytesPerOperation,11:0.0}");
        }

        return builder.ToString();
    }

    private static string RenderSoakTable(MatrixSoakResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Dynamic soak (16 chars, inactive dots on, precomputed text)");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Frames={result.FrameCount}, Total={result.Elapsed.TotalMilliseconds:0.00} ms, us/frame={result.MicrosecondsPerFrame:0.00}, bytes/frame={result.BytesPerFrame:0.0}");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Natural GC: Gen0={result.Gen0Collections}, Gen1={result.Gen1Collections}, Gen2={result.Gen2Collections}");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Managed live after full GC: before={result.RetainedBytesBefore}, after={result.RetainedBytesAfter}, delta={result.RetainedBytesDelta}");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Sampled managed peak={result.PeakManagedBytes}, Layout builds={result.LayoutBuilds}, Geometry builds={result.GeometryBuilds}, Cache={result.InitialGeometryCacheCount}->{result.FinalGeometryCacheCount}, Commands/frame={result.GeometryCommandsPerFrame}");
        return builder.ToString();
    }

    private static string RenderMarkdown(
        IReadOnlyList<MatrixPerformanceResult> results,
        IReadOnlyList<MatrixDynamicLoadResult> dynamicResults,
        IReadOnlyList<MatrixAllocationResult> allocationResults,
        MatrixSoakResult soakResult)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Matrix Interaction Performance Baseline");
        builder.AppendLine();
        builder.AppendLine($"- Date: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine("- Configuration: Release, .NET 10");
        builder.AppendLine("- Runner: `tools/performances/AtomUI.Labs.Controls.Led.Performance --count <N> --frames <N> --soak-frames <N>`");
        builder.AppendLine("- Scope: CPU-side layout and DrawingGroup command submission; excludes GPU/platform presentation cost");
        builder.AppendLine();
        builder.AppendLine("| Scenario | Characters | Updates | Total ms | us/update | KB total | bytes/update | Geometry commands |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var result in results)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {result.Name} | {result.TextLength} | {result.UpdateCount} | {result.Elapsed.TotalMilliseconds:0.00} | {result.MicrosecondsPerUpdate:0.00} | {result.AllocatedBytes / 1024.0:0.0} | {result.BytesPerUpdate:0.0} | {result.SubmittedGeometryCommands} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Fixed-Length Dynamic Load");
        builder.AppendLine();
        builder.AppendLine("| Characters | Inactive dots | Frames | Total ms | us/frame | bytes/frame | Layout builds | Geometry builds | Final cache | Geometry commands/frame |");
        builder.AppendLine("| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var result in dynamicResults)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {result.CharacterCount} | {(result.ShowInactiveDots ? "On" : "Off")} | {result.FrameCount} | {result.Elapsed.TotalMilliseconds:0.00} | {result.MicrosecondsPerFrame:0.00} | {result.BytesPerFrame:0.0} | {result.LayoutBuilds} | {result.GeometryBuilds} | {result.GeometryCacheCount} | {result.GeometryCommandsPerFrame} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Allocation Attribution");
        builder.AppendLine();
        builder.AppendLine("All dynamic Matrix attribution scenarios use 16 characters with inactive dots enabled. `Precomputed` scenarios exclude caller-side string construction. The rows are independently measured and are not mathematically additive.");
        builder.AppendLine();
        builder.AppendLine("| Stage | Operations | Total ms | us/operation | bytes/operation |");
        builder.AppendLine("| --- | ---: | ---: | ---: | ---: |");
        foreach (var result in allocationResults)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {result.Name} | {result.OperationCount} | {result.Elapsed.TotalMilliseconds:0.00} | {result.MicrosecondsPerOperation:0.00} | {result.BytesPerOperation:0.0} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Long-Running Soak");
        builder.AppendLine();
        builder.AppendLine("The soak uses a precomputed 1,000-value text ring after warmup. Natural GC counts are captured without forced collections during the measured loop. Retained bytes are compared only after full collections before and after the loop.");
        builder.AppendLine();
        builder.AppendLine("| Frames | Total ms | us/frame | bytes/frame | Gen0 | Gen1 | Gen2 | Live before | Live after | Live delta | Sampled peak | Layout builds | Geometry builds | Cache | Commands/frame |");
        builder.AppendLine("| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |");
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"| {soakResult.FrameCount} | {soakResult.Elapsed.TotalMilliseconds:0.00} | {soakResult.MicrosecondsPerFrame:0.00} | {soakResult.BytesPerFrame:0.0} | {soakResult.Gen0Collections} | {soakResult.Gen1Collections} | {soakResult.Gen2Collections} | {soakResult.RetainedBytesBefore} | {soakResult.RetainedBytesAfter} | {soakResult.RetainedBytesDelta} | {soakResult.PeakManagedBytes} | {soakResult.LayoutBuilds} | {soakResult.GeometryBuilds} | {soakResult.InitialGeometryCacheCount}->{soakResult.FinalGeometryCacheCount} | {soakResult.GeometryCommandsPerFrame} |");

        return builder.ToString();
    }

    private sealed record MatrixPerformanceResult(
        string Name,
        int TextLength,
        int UpdateCount,
        TimeSpan Elapsed,
        long AllocatedBytes,
        int SubmittedGeometryCommands)
    {
        public double MicrosecondsPerUpdate => Elapsed.TotalMilliseconds * 1000 / UpdateCount;

        public double BytesPerUpdate => AllocatedBytes / (double)UpdateCount;
    }

    private sealed record MatrixDynamicLoadResult(
        int CharacterCount,
        bool ShowInactiveDots,
        int FrameCount,
        TimeSpan Elapsed,
        long AllocatedBytes,
        int LayoutBuilds,
        int GeometryBuilds,
        int GeometryCacheCount,
        int GeometryCommandsPerFrame)
    {
        public double MicrosecondsPerFrame => Elapsed.TotalMilliseconds * 1000 / FrameCount;

        public double BytesPerFrame => AllocatedBytes / (double)FrameCount;
    }

    private sealed record MatrixAllocationResult(
        string Name,
        int OperationCount,
        TimeSpan Elapsed,
        long AllocatedBytes)
    {
        public double MicrosecondsPerOperation => Elapsed.TotalMilliseconds * 1000 / OperationCount;

        public double BytesPerOperation => AllocatedBytes / (double)OperationCount;
    }

    private sealed record MatrixSoakResult(
        int FrameCount,
        TimeSpan Elapsed,
        long AllocatedBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        long RetainedBytesBefore,
        long RetainedBytesAfter,
        long PeakManagedBytes,
        int LayoutBuilds,
        int GeometryBuilds,
        int InitialGeometryCacheCount,
        int FinalGeometryCacheCount,
        int GeometryCommandsPerFrame)
    {
        public double MicrosecondsPerFrame => Elapsed.TotalMilliseconds * 1000 / FrameCount;

        public double BytesPerFrame => AllocatedBytes / (double)FrameCount;

        public long RetainedBytesDelta => RetainedBytesAfter - RetainedBytesBefore;
    }

    private sealed record PerformanceOptions(
        int Count,
        int DynamicFrames,
        int SoakFrames,
        string? MarkdownOutputPath,
        bool RunGlowPrototypes,
        bool RunFormalGlow)
    {
        public static PerformanceOptions Parse(string[] args)
        {
            var count = DefaultCount;
            var dynamicFrames = DefaultDynamicFrames;
            var soakFrames = DefaultSoakFrames;
            string? markdownOutputPath = null;
            var runGlowPrototypes = false;
            var runFormalGlow = false;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--count" && i + 1 < args.Length && int.TryParse(args[++i], out var parsedCount))
                {
                    count = Math.Max(1, parsedCount);
                }
                else if (args[i] == "--frames" && i + 1 < args.Length && int.TryParse(args[++i], out var parsedFrames))
                {
                    dynamicFrames = Math.Max(1, parsedFrames);
                }
                else if (args[i] == "--soak-frames" && i + 1 < args.Length && int.TryParse(args[++i], out var parsedSoakFrames))
                {
                    soakFrames = Math.Max(1, parsedSoakFrames);
                }
                else if (args[i] == "--markdown" && i + 1 < args.Length)
                {
                    markdownOutputPath = args[++i];
                }
                else if (args[i] == "--glow-prototypes")
                {
                    runGlowPrototypes = true;
                }
                else if (args[i] == "--formal-glow")
                {
                    runFormalGlow = true;
                }
            }

            return new PerformanceOptions(
                count,
                dynamicFrames,
                soakFrames,
                markdownOutputPath,
                runGlowPrototypes,
                runFormalGlow);
        }
    }
}

internal sealed class PerformanceApplication : Application;
