using System.Diagnostics;
using System.Globalization;
using System.Text;
using AtomUI.Labs.Controls.Led.Matrix;
using AtomUI.Labs.Controls.Led.Segment;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.Led.Performance;

internal static class FormalGlowPerformanceRunner
{
    private const int WarmupFrames = 100;
    private const int DisabledTimingTrialCount = 5;
    private const double DisabledMaximumTimingRatio = 1.05;
    private const double DisabledMaximumTimingDeltaMicroseconds = 0.25;
    private const double DisabledMaximumAllocationRatio = 1.025;
    private const double DisabledMaximumAllocationDeltaPerFrame = 4_096;
    private static DrawingGroup? _drawingSink;

    public static int Run(int frameCount, string? markdownOutputPath)
    {
        PrewarmRuntime();
        var results = new List<FormalGlowResult>();
        foreach (var controlKind in new[] { FormalGlowControlKind.Matrix, FormalGlowControlKind.Segment })
        {
            foreach (var mode in Enum.GetValues<FormalGlowMode>())
            {
                results.Add(Measure(controlKind, mode, frameCount));
            }
        }

        var disabledPairs = Enum.GetValues<FormalGlowControlKind>()
                                .Select(kind => MeasureDisabledPair(kind, frameCount))
                                .ToArray();
        var disabledAllocations = Enum.GetValues<FormalGlowControlKind>()
                                      .Select(kind => CreateDisabledAllocationResult(kind, results))
                                      .ToArray();

        var report = RenderReport(results, disabledPairs, disabledAllocations);
        Console.WriteLine(report);
        if (!string.IsNullOrWhiteSpace(markdownOutputPath))
        {
            var fullPath = Path.GetFullPath(markdownOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(
                fullPath,
                RenderMarkdown(results, disabledPairs, disabledAllocations),
                new UTF8Encoding(false));
            Console.WriteLine($"Wrote formal Glow result: {fullPath}");
        }

        return Validate(results, disabledPairs, disabledAllocations) ? 0 : 1;
    }

    private static DisabledPairResult MeasureDisabledPair(FormalGlowControlKind kind, int frameCount)
    {
        var nullBrush = CreateControl(kind, FormalGlowMode.DisabledNullBrush);
        var zeroOpacity = CreateControl(kind, FormalGlowMode.DisabledZeroOpacity);
        var viewport = new Size(800, 140);
        foreach (var control in new[] { nullBrush, zeroOpacity })
        {
            control.Measure(viewport);
            control.Arrange(new Rect(viewport));
        }

        for (var i = 0; i < 1_000; i++)
        {
            _drawingSink = Render(i % 2 == 0 ? nullBrush : zeroOpacity);
            _drawingSink = Render(i % 2 == 0 ? zeroOpacity : nullBrush);
        }

        var trials = new DisabledPairResult[DisabledTimingTrialCount];
        for (var trial = 0; trial < trials.Length; trial++)
        {
            trials[trial] = MeasureDisabledPairTrial(kind, nullBrush, zeroOpacity, frameCount);
        }

        return trials.OrderBy(result => result.ZeroToNullRatio).ElementAt(trials.Length / 2);
    }

    private static DisabledPairResult MeasureDisabledPairTrial(
        FormalGlowControlKind kind,
        Control nullBrush,
        Control zeroOpacity,
        int frameCount)
    {
        long nullTicks = 0;
        long zeroTicks = 0;
        const int batchSize = 16;
        var measuredFrames = 0;
        var batch = 0;
        while (measuredFrames < frameCount)
        {
            var count = Math.Min(batchSize, frameCount - measuredFrames);
            if (batch % 2 == 0)
            {
                nullTicks += MeasureRenderTicks(nullBrush, count);
                zeroTicks += MeasureRenderTicks(zeroOpacity, count);
            }
            else
            {
                zeroTicks += MeasureRenderTicks(zeroOpacity, count);
                nullTicks += MeasureRenderTicks(nullBrush, count);
            }

            measuredFrames += count;
            batch++;
        }

        return new DisabledPairResult(kind, frameCount, nullTicks, zeroTicks);
    }

    private static long MeasureRenderTicks(Control control, int count)
    {
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
        {
            _drawingSink = Render(control);
        }

        return Stopwatch.GetTimestamp() - start;
    }

    private static FormalGlowResult Measure(
        FormalGlowControlKind controlKind,
        FormalGlowMode mode,
        int frameCount)
    {
        var control = CreateControl(controlKind, mode);
        var viewport = new Size(800, 140);
        control.Measure(viewport);
        control.Arrange(new Rect(viewport));
        for (var frame = 0; frame < WarmupFrames; frame++)
        {
            Update(mode, control, frame);
            _drawingSink = Render(control);
        }

        var initialBuilds = GetEffectBuildCount(control);
        var initialScopes = GetEffectScopeCount(control);
        ForceFullCollection();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (var frame = 0; frame < frameCount; frame++)
        {
            Update(mode, control, frame);
            _drawingSink = Render(control);
        }

        stopwatch.Stop();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var drawing = _drawingSink ?? throw new InvalidOperationException("Measured Glow frame was not captured.");
        return new FormalGlowResult(
            controlKind,
            mode,
            frameCount,
            stopwatch.Elapsed,
            allocatedBytes,
            GetEffectBuildCount(control) - initialBuilds,
            GetEffectScopeCount(control) - initialScopes,
            CountGeometryDrawings(drawing));
    }

    private static void PrewarmRuntime()
    {
        foreach (var controlKind in new[] { FormalGlowControlKind.Matrix, FormalGlowControlKind.Segment })
        {
            foreach (var mode in Enum.GetValues<FormalGlowMode>())
            {
                var control = CreateControl(controlKind, mode);
                var viewport = new Size(800, 140);
                control.Measure(viewport);
                control.Arrange(new Rect(viewport));
                Update(mode, control, 1);
                _drawingSink = Render(control);
            }
        }

        _drawingSink = null;
        ForceFullCollection();

        foreach (var controlKind in new[] { FormalGlowControlKind.Matrix, FormalGlowControlKind.Segment })
        {
            foreach (var mode in Enum.GetValues<FormalGlowMode>())
            {
                _ = Measure(controlKind, mode, 1_000);
            }
        }

        _drawingSink = null;
        ForceFullCollection();
    }

    private static Control CreateControl(FormalGlowControlKind kind, FormalGlowMode mode)
    {
        var glowBrush = mode == FormalGlowMode.DisabledNullBrush ? null : Brushes.Cyan;
        var glowOpacity = mode == FormalGlowMode.DisabledZeroOpacity ? 0 : 0.35;
        return kind switch
        {
            FormalGlowControlKind.Matrix => new MatrixDisplay
            {
                Text = "TEMP2026RPM1234",
                DotSize = 6,
                DotSpacing = 2,
                CharacterSpacing = 8,
                ActiveBrush = Brushes.White,
                InactiveBrush = Brushes.Gray,
                GlowBrush = glowBrush,
                GlowOpacity = glowOpacity,
                GlowRadius = 6
            },
            FormalGlowControlKind.Segment => new SegmentDisplay
            {
                Text = "TEMP2026RPM1234",
                CharacterHeight = 72,
                SegmentThickness = 8,
                SegmentGap = 2,
                CharacterSpacing = 8,
                ActiveBrush = Brushes.White,
                InactiveBrush = Brushes.Gray,
                GlowBrush = glowBrush,
                GlowOpacity = glowOpacity,
                GlowRadius = 6
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static void Update(FormalGlowMode mode, Control control, int frame)
    {
        switch (mode)
        {
            case FormalGlowMode.OpacityAnimation:
                SetOpacity(control, frame % 120 / 119d);
                break;
            case FormalGlowMode.RadiusAnimation:
                SetRadius(control, frame % 49 <= 24 ? frame % 49 : 48 - frame % 49);
                break;
        }
    }

    private static void SetOpacity(Control control, double value)
    {
        if (control is MatrixDisplay matrix)
        {
            matrix.GlowOpacity = value;
        }
        else
        {
            ((SegmentDisplay)control).GlowOpacity = value;
        }
    }

    private static void SetRadius(Control control, double value)
    {
        if (control is MatrixDisplay matrix)
        {
            matrix.GlowRadius = value;
        }
        else
        {
            ((SegmentDisplay)control).GlowRadius = value;
        }
    }

    private static int GetEffectBuildCount(Control control)
    {
        return control is MatrixDisplay matrix
            ? matrix.GlowEffectBuildCount
            : ((SegmentDisplay)control).GlowEffectBuildCount;
    }

    private static int GetEffectScopeCount(Control control)
    {
        return control is MatrixDisplay matrix
            ? matrix.GlowEffectScopeCount
            : ((SegmentDisplay)control).GlowEffectScopeCount;
    }

    private static DrawingGroup Render(Control control)
    {
        var drawing = new DrawingGroup();
        using var context = drawing.Open();
        control.Render(context);
        return drawing;
    }

    private static int CountGeometryDrawings(Drawing drawing)
    {
        if (drawing is GeometryDrawing)
        {
            return 1;
        }

        return drawing is DrawingGroup group ? group.Children.Sum(CountGeometryDrawings) : 0;
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static bool Validate(
        IReadOnlyList<FormalGlowResult> results,
        IReadOnlyList<DisabledPairResult> disabledPairs,
        IReadOnlyList<DisabledAllocationResult> disabledAllocations)
    {
        return results.Where(result => result.Mode is FormalGlowMode.DisabledNullBrush or FormalGlowMode.DisabledZeroOpacity)
                      .All(result => result.EffectBuilds == 0 && result.EffectScopes == 0)
               && results.Where(result => result.Mode == FormalGlowMode.Static)
                         .All(result => result.EffectBuilds == 0 && result.EffectScopes == result.FrameCount)
               && disabledPairs.All(result => result.IsWithinBudget)
               && disabledAllocations.All(result => result.IsWithinBudget);
    }

    private static DisabledAllocationResult CreateDisabledAllocationResult(
        FormalGlowControlKind controlKind,
        IReadOnlyList<FormalGlowResult> results)
    {
        var nullBrush = results.Single(result =>
            result.Control == controlKind && result.Mode == FormalGlowMode.DisabledNullBrush);
        var zeroOpacity = results.Single(result =>
            result.Control == controlKind && result.Mode == FormalGlowMode.DisabledZeroOpacity);
        return new DisabledAllocationResult(
            controlKind,
            nullBrush.BytesPerFrame,
            zeroOpacity.BytesPerFrame);
    }

    private static string RenderReport(
        IReadOnlyList<FormalGlowResult> results,
        IReadOnlyList<DisabledPairResult> disabledPairs,
        IReadOnlyList<DisabledAllocationResult> disabledAllocations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Formal LED Glow performance (DrawingGroup command submission)");
        builder.AppendLine("Control  Mode                  Frames  us/frame  bytes/frame  Effect builds  Effect scopes  Commands");
        foreach (var result in results)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{result.Control,-9}{result.Mode,-22}{result.FrameCount,7}{result.MicrosecondsPerFrame,10:0.00}{result.BytesPerFrame,13:0.0}{result.EffectBuilds,15}{result.EffectScopes,15}{result.GeometryCommands,10}");
        }

        builder.AppendLine("Disabled paired timing");
        builder.AppendLine("Control  NullBrush us/frame  ZeroOpacity us/frame  added us  zero/null  Gate");
        foreach (var pair in disabledPairs)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{pair.Control,-9}{pair.NullMicrosecondsPerFrame,19:0.00}{pair.ZeroMicrosecondsPerFrame,22:0.00}{pair.AdditionalMicrosecondsPerFrame,10:0.00}{pair.ZeroToNullRatio,11:0.000}  {(pair.IsWithinBudget ? "PASS" : "FAIL")}");
        }

        builder.AppendLine("Disabled allocation");
        builder.AppendLine("Control  NullBrush bytes/frame  ZeroOpacity bytes/frame  added  zero/null  Gate");
        foreach (var allocation in disabledAllocations)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"{allocation.Control,-9}{allocation.NullBytesPerFrame,22:0.0}{allocation.ZeroBytesPerFrame,25:0.0}{allocation.AdditionalBytesPerFrame,8:0.0}{allocation.ZeroToNullRatio,11:0.000}  {(allocation.IsWithinBudget ? "PASS" : "FAIL")}");
        }

        return builder.ToString();
    }

    private static string RenderMarkdown(
        IReadOnlyList<FormalGlowResult> results,
        IReadOnlyList<DisabledPairResult> disabledPairs,
        IReadOnlyList<DisabledAllocationResult> disabledAllocations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Formal LED Glow Performance");
        builder.AppendLine();
        builder.AppendLine("DrawingGroup command submission only; timings are not isolated GPU presentation cost.");
        builder.AppendLine();
        builder.AppendLine("| Control | Mode | Frames | us/frame | bytes/frame | Effect builds | Effect scopes | Commands | ");
        builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
        foreach (var result in results)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {result.Control} | {result.Mode} | {result.FrameCount} | {result.MicrosecondsPerFrame:0.00} | {result.BytesPerFrame:0.0} | {result.EffectBuilds} | {result.EffectScopes} | {result.GeometryCommands} |");
        }

        builder.AppendLine();
        builder.AppendLine("| Control | NullBrush us/frame | ZeroOpacity us/frame | added us | zero/null | Gate |");
        builder.AppendLine("|---|---:|---:|---:|---:|---|");
        foreach (var pair in disabledPairs)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {pair.Control} | {pair.NullMicrosecondsPerFrame:0.00} | {pair.ZeroMicrosecondsPerFrame:0.00} | {pair.AdditionalMicrosecondsPerFrame:0.00} | {pair.ZeroToNullRatio:0.000} | {(pair.IsWithinBudget ? "PASS" : "FAIL")} |");
        }

        builder.AppendLine();
        builder.AppendLine("| Control | NullBrush bytes/frame | ZeroOpacity bytes/frame | added | zero/null | Gate |");
        builder.AppendLine("|---|---:|---:|---:|---:|---|");
        foreach (var allocation in disabledAllocations)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"| {allocation.Control} | {allocation.NullBytesPerFrame:0.0} | {allocation.ZeroBytesPerFrame:0.0} | {allocation.AdditionalBytesPerFrame:0.0} | {allocation.ZeroToNullRatio:0.000} | {(allocation.IsWithinBudget ? "PASS" : "FAIL")} |");
        }

        return builder.ToString();
    }

    private enum FormalGlowControlKind
    {
        Matrix,
        Segment
    }

    private enum FormalGlowMode
    {
        DisabledNullBrush,
        DisabledZeroOpacity,
        Static,
        OpacityAnimation,
        RadiusAnimation
    }

    private sealed record FormalGlowResult(
        FormalGlowControlKind Control,
        FormalGlowMode Mode,
        int FrameCount,
        TimeSpan Elapsed,
        long AllocatedBytes,
        int EffectBuilds,
        int EffectScopes,
        int GeometryCommands)
    {
        public double MicrosecondsPerFrame => Elapsed.TotalMilliseconds * 1000 / FrameCount;

        public double BytesPerFrame => AllocatedBytes / (double)FrameCount;
    }

    private sealed record DisabledPairResult(
        FormalGlowControlKind Control,
        int FrameCount,
        long NullTicks,
        long ZeroTicks)
    {
        public double NullMicrosecondsPerFrame => NullTicks * 1_000_000d / Stopwatch.Frequency / FrameCount;

        public double ZeroMicrosecondsPerFrame => ZeroTicks * 1_000_000d / Stopwatch.Frequency / FrameCount;

        public double ZeroToNullRatio => ZeroTicks / (double)NullTicks;

        public double AdditionalMicrosecondsPerFrame =>
            Math.Max(0, ZeroMicrosecondsPerFrame - NullMicrosecondsPerFrame);

        public bool IsWithinBudget =>
            ZeroToNullRatio <= DisabledMaximumTimingRatio
            || AdditionalMicrosecondsPerFrame <= DisabledMaximumTimingDeltaMicroseconds;
    }

    private sealed record DisabledAllocationResult(
        FormalGlowControlKind Control,
        double NullBytesPerFrame,
        double ZeroBytesPerFrame)
    {
        public double AdditionalBytesPerFrame => Math.Max(0, ZeroBytesPerFrame - NullBytesPerFrame);

        public double ZeroToNullRatio
        {
            get
            {
                return NullBytesPerFrame <= 0
                    ? (AdditionalBytesPerFrame <= 0 ? 1 : double.PositiveInfinity)
                    : ZeroBytesPerFrame / NullBytesPerFrame;
            }
        }

        public bool IsWithinBudget =>
            AdditionalBytesPerFrame <= DisabledMaximumAllocationDeltaPerFrame
            && ZeroToNullRatio <= DisabledMaximumAllocationRatio;
    }
}
