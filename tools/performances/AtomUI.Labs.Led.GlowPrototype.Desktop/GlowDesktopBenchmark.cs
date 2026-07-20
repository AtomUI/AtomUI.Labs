using System.Diagnostics;
using System.Globalization;
using System.Text;
using AtomUI.Labs.Led.Performance;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AtomUI.Labs.Led.GlowPrototype.Desktop;

internal enum GlowDesktopBenchmarkRoute
{
    NoGlow,
    Geometry,
    Opacity,
    Vector,
    Scoped
}

internal enum GlowEffectGranularity
{
    PerGeometry,
    PerControl,
    Batch8,
    Batch16
}

internal sealed record GlowDesktopBenchmarkOptions(
    GlowDesktopBenchmarkRoute Route,
    int Instances,
    int WarmupTicks,
    int MeasurementTicks,
    int FrequencyHz,
    double Radius,
    bool BatchTopology,
    GlowEffectGranularity Granularity,
    double Spacing,
    string? OutputPath)
{
    public static bool TryParse(string[] args, out GlowDesktopBenchmarkOptions options)
    {
        options = new GlowDesktopBenchmarkOptions(
            GlowDesktopBenchmarkRoute.NoGlow,
            64,
            120,
            600,
            60,
            12,
            false,
            GlowEffectGranularity.PerGeometry,
            40,
            null);
        if (!args.Contains("--benchmark", StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var route = ParseRoute(GetValue(args, "--route") ?? "noglow");
        var instances = ParsePositiveInt(GetValue(args, "--instances"), 64, "--instances");
        var warmupTicks = ParseNonNegativeInt(GetValue(args, "--warmup"), 120, "--warmup");
        var measurementTicks = ParsePositiveInt(GetValue(args, "--ticks"), 600, "--ticks");
        var frequencyHz = ParseNonNegativeInt(GetValue(args, "--hz"), 60, "--hz");
        if (frequencyHz > 240)
        {
            throw new ArgumentOutOfRangeException("--hz", "Value must not exceed 240 Hz.");
        }

        var radius = ParseRadius(GetValue(args, "--radius"));
        options = new GlowDesktopBenchmarkOptions(
            route,
            instances,
            warmupTicks,
            measurementTicks,
            frequencyHz,
            radius,
            string.Equals(GetValue(args, "--topology"), "batch", StringComparison.OrdinalIgnoreCase),
            ParseGranularity(GetValue(args, "--granularity")),
            ParseSpacing(GetValue(args, "--spacing")),
            GetValue(args, "--output"));
        return true;
    }

    private static string? GetValue(string[] args, string name)
    {
        var index = Array.FindIndex(args, item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static GlowDesktopBenchmarkRoute ParseRoute(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "noglow" => GlowDesktopBenchmarkRoute.NoGlow,
            "geometry" => GlowDesktopBenchmarkRoute.Geometry,
            "opacity" => GlowDesktopBenchmarkRoute.Opacity,
            "vector" => GlowDesktopBenchmarkRoute.Vector,
            "scoped" => GlowDesktopBenchmarkRoute.Scoped,
            _ => throw new ArgumentException(
                $"Unsupported --route value '{value}'. Use noglow, geometry, opacity, vector, or scoped.")
        };
    }

    private static double ParseRadius(string? value)
    {
        if (value is null)
        {
            return 12;
        }

        return double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)
               && parsed is >= 0 and <= 24
            ? parsed
            : throw new ArgumentOutOfRangeException("--radius", "Value must be between 0 and 24 DIP.");
    }

    private static GlowEffectGranularity ParseGranularity(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            null or "pergeometry" => GlowEffectGranularity.PerGeometry,
            "percontrol" => GlowEffectGranularity.PerControl,
            "batch8" => GlowEffectGranularity.Batch8,
            "batch16" => GlowEffectGranularity.Batch16,
            _ => throw new ArgumentException(
                $"Unsupported --granularity value '{value}'. Use pergeometry, percontrol, batch8, or batch16.")
        };
    }

    private static double ParseSpacing(string? value)
    {
        if (value is null)
        {
            return 40;
        }

        return double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)
               && parsed is >= 28 and <= 120
            ? parsed
            : throw new ArgumentOutOfRangeException("--spacing", "Value must be between 28 and 120 DIP.");
    }

    private static int ParsePositiveInt(string? value, int fallback, string name)
    {
        var parsed = ParseNonNegativeInt(value, fallback, name);
        return parsed > 0 ? parsed : throw new ArgumentOutOfRangeException(name, "Value must be greater than zero.");
    }

    private static int ParseNonNegativeInt(string? value, int fallback, string name)
    {
        if (value is null)
        {
            return fallback;
        }

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0
            ? parsed
            : throw new ArgumentOutOfRangeException(name, "Value must be a non-negative integer.");
    }
}

internal sealed class GlowDesktopBenchmarkWindow : Window
{
    private readonly GlowDesktopBenchmarkOptions _options;
    private readonly IReadOnlyList<IGlowDesktopBenchmarkTarget> _targets;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _measurement = new();
    private int _ticks;
    private long _allocatedBefore;

    public GlowDesktopBenchmarkWindow(GlowDesktopBenchmarkOptions options)
    {
        _options = options;
        Title = $"Glow desktop benchmark - {options.Route}";
        Width = 960;
        Height = 720;
        Background = Brushes.Black;

        if (options.BatchTopology)
        {
            var surface = new GlowDesktopBenchmarkSurface(
                options.Route,
                options.Radius,
                options.Instances,
                options.Granularity,
                options.Spacing);
            _targets = [surface];
            Content = surface;
        }
        else
        {
            var panel = new WrapPanel { Orientation = Orientation.Horizontal };
            var cells = Enumerable.Range(0, options.Instances)
                                  .Select(index => new GlowDesktopBenchmarkCell(options.Route, options.Radius, index))
                                  .ToList();
            _targets = cells;
            foreach (var cell in cells)
            {
                panel.Children.Add(cell);
            }

            Content = panel;
        }
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1d / Math.Max(1, options.FrequencyHz))
        };
        _timer.Tick += OnTick;
        Opened += (_, _) => _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _ticks++;
        if (_ticks == _options.WarmupTicks + 1)
        {
            foreach (var target in _targets)
            {
                target.ResetCounters();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _allocatedBefore = GC.GetTotalAllocatedBytes(true);
            _measurement.Restart();
        }

        if (_options.FrequencyHz > 0)
        {
            foreach (var target in _targets)
            {
                target.Advance();
            }
        }

        if (_ticks < _options.WarmupTicks + _options.MeasurementTicks)
        {
            return;
        }

        _timer.Stop();
        Dispatcher.UIThread.Post(Finish, DispatcherPriority.ApplicationIdle);
    }

    private void Finish()
    {
        _measurement.Stop();
        var renderCallbacks = _targets.Sum(target => target.RenderCallbacks);
        var glowSubmissions = _targets.Sum(target => target.GlowSubmissions);
        var effectScopes = _targets.Sum(target => target.EffectScopes);
        var allocatedBytes = GC.GetTotalAllocatedBytes(false) - _allocatedBefore;
        var expectedCallbacks = _options.FrequencyHz == 0
            ? 0
            : (long)(_options.BatchTopology ? 1 : _options.Instances) * _options.MeasurementTicks;
        var callbackCompletion = expectedCallbacks == 0 ? 1 : renderCallbacks / (double)expectedCallbacks;
        var bytesPerCallback = renderCallbacks == 0 ? 0 : allocatedBytes / (double)renderCallbacks;
        var noGlowContractPassed = _options.Route != GlowDesktopBenchmarkRoute.NoGlow || glowSubmissions == 0;
        var report = new StringBuilder()
            .AppendLine("Glow desktop render-callback benchmark")
            .AppendLine(CultureInfo.InvariantCulture, $"Route: {_options.Route}")
            .AppendLine(CultureInfo.InvariantCulture, $"Instances: {_options.Instances}")
            .AppendLine(CultureInfo.InvariantCulture, $"Warmup ticks: {_options.WarmupTicks}")
            .AppendLine(CultureInfo.InvariantCulture, $"Measurement ticks: {_options.MeasurementTicks}")
            .AppendLine(CultureInfo.InvariantCulture, $"Frequency Hz: {_options.FrequencyHz}")
            .AppendLine(CultureInfo.InvariantCulture, $"Radius DIP: {_options.Radius:0.##}")
            .AppendLine(CultureInfo.InvariantCulture, $"Topology: {(_options.BatchTopology ? "Batch" : "Cells")}")
            .AppendLine(CultureInfo.InvariantCulture, $"Effect granularity: {_options.Granularity}")
            .AppendLine(CultureInfo.InvariantCulture, $"Geometry spacing DIP: {_options.Spacing:0.##}")
            .AppendLine(CultureInfo.InvariantCulture, $"Elapsed ms: {_measurement.Elapsed.TotalMilliseconds:0.00}")
            .AppendLine(CultureInfo.InvariantCulture, $"Render callbacks: {renderCallbacks}")
            .AppendLine(CultureInfo.InvariantCulture, $"Expected callbacks: {expectedCallbacks}")
            .AppendLine(CultureInfo.InvariantCulture, $"Callback completion: {callbackCompletion:P2}")
            .AppendLine(CultureInfo.InvariantCulture, $"Glow submissions: {glowSubmissions}")
            .AppendLine(CultureInfo.InvariantCulture, $"Effect scopes: {effectScopes}")
            .AppendLine(CultureInfo.InvariantCulture, $"Allocated bytes/process: {allocatedBytes}")
            .AppendLine(CultureInfo.InvariantCulture, $"Allocated bytes/completed callback: {bytesPerCallback:0.0}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"NoGlow zero-submission contract: {(_options.Route == GlowDesktopBenchmarkRoute.NoGlow ? (noGlowContractPassed ? "PASS" : "FAIL") : "N/A")}")
            .AppendLine("Warning: elapsed time includes dispatcher scheduling and window rendering; it is not isolated GPU time.")
            .ToString();

        Console.Write(report);
        if (_options.OutputPath is not null)
        {
            File.WriteAllText(_options.OutputPath, report);
        }

        _timer.Tick -= OnTick;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown(noGlowContractPassed ? 0 : 2);
        }
        else
        {
            Close();
        }
    }
}

internal interface IGlowDesktopBenchmarkTarget
{
    long RenderCallbacks { get; }

    long GlowSubmissions { get; }

    long EffectScopes { get; }

    void Advance();

    void ResetCounters();
}

internal sealed class GlowDesktopBenchmarkCell : Control, IGlowDesktopBenchmarkTarget
{
    private static readonly Geometry Source = new EllipseGeometry(new Rect(8, 8, 24, 24));
    private readonly IGlowPrototypeRenderer? _renderer;
    private double _offset;

    public GlowDesktopBenchmarkCell(GlowDesktopBenchmarkRoute route, double radius, int index)
    {
        Width = 48;
        Height = 48;
        _offset = index % 2;
        var brush = new SolidColorBrush(Color.FromRgb(64, 222, 255));
        _renderer = route switch
        {
            GlowDesktopBenchmarkRoute.NoGlow => null,
            GlowDesktopBenchmarkRoute.Geometry => new OpacityOnlyGlowPrototype(brush, 1),
            GlowDesktopBenchmarkRoute.Opacity => new OpacityOnlyGlowPrototype(brush, 0.65),
            GlowDesktopBenchmarkRoute.Vector => new VectorExpansionGlowPrototype(brush, 0.65, radius),
            GlowDesktopBenchmarkRoute.Scoped => new ScopedBlurEffectGlowPrototype(brush, 0.65, radius),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };
    }

    public long RenderCallbacks { get; private set; }

    public long GlowSubmissions { get; private set; }

    public long EffectScopes { get; private set; }

    public void Advance()
    {
        _offset = _offset == 0 ? 1 : 0;
        InvalidateVisual();
    }

    public void ResetCounters()
    {
        RenderCallbacks = 0;
        GlowSubmissions = 0;
        EffectScopes = 0;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        RenderCallbacks++;
        context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
        using (context.PushTransform(Avalonia.Matrix.CreateTranslation(_offset, 0)))
        {
            if (_renderer is not null)
            {
                _renderer.Render(context, Source);
                GlowSubmissions++;
                if (_renderer is ScopedBlurEffectGlowPrototype)
                {
                    EffectScopes++;
                }
            }

            context.DrawGeometry(Brushes.White, null, Source);
        }
    }
}

internal sealed class GlowDesktopBenchmarkSurface : Control, IGlowDesktopBenchmarkTarget
{
    private static readonly Geometry Source = new EllipseGeometry(new Rect(0, 0, 24, 24));
    private readonly IGlowPrototypeRenderer?[] _renderers;
    private readonly GlowEffectGranularity _granularity;
    private readonly ScopedBlurEffectGlowPrototype? _groupedScopedRenderer;
    private readonly List<Point> _offsets = [];
    private readonly double _spacing;
    private double _offset;
    private bool _glowEnabled = true;

    public GlowDesktopBenchmarkSurface(
        GlowDesktopBenchmarkRoute route,
        double radius,
        int instances,
        GlowEffectGranularity granularity = GlowEffectGranularity.PerGeometry,
        double spacing = 40)
    {
        _granularity = granularity;
        _spacing = spacing;
        _renderers = Enumerable.Range(0, instances)
                               .Select(_ => CreateRenderer(route, radius))
                               .ToArray();
        if (route == GlowDesktopBenchmarkRoute.Scoped && granularity != GlowEffectGranularity.PerGeometry)
        {
            _groupedScopedRenderer = new ScopedBlurEffectGlowPrototype(
                new SolidColorBrush(Color.FromRgb(64, 222, 255)),
                0.65,
                radius);
        }
    }

    public long RenderCallbacks { get; private set; }

    public long GlowSubmissions { get; private set; }

    public long EffectScopes { get; private set; }

    public void Advance()
    {
        _offset = _offset == 0 ? 1 : 0;
        InvalidateVisual();
    }

    public void ResetCounters()
    {
        RenderCallbacks = 0;
        GlowSubmissions = 0;
        EffectScopes = 0;
    }

    public void MutateGlow(int iteration)
    {
        _glowEnabled = iteration % 7 != 0;
        var radius = iteration % 4 switch
        {
            0 => 0,
            1 => 6,
            2 => 12,
            _ => 24
        };
        var opacity = iteration % 5 / 5d;
        var brush = iteration % 2 == 0 ? Brushes.Cyan : Brushes.Magenta;
        foreach (var renderer in _renderers)
        {
            if (renderer is ScopedBlurEffectGlowPrototype scoped)
            {
                scoped.UpdateBrush(brush);
                scoped.UpdateOpacity(opacity);
                scoped.UpdateRadius(radius);
            }
        }
        if (_groupedScopedRenderer is not null)
        {
            _groupedScopedRenderer.UpdateBrush(brush);
            _groupedScopedRenderer.UpdateOpacity(opacity);
            _groupedScopedRenderer.UpdateRadius(radius);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        RenderCallbacks++;
        context.DrawRectangle(Brushes.Black, null, new Rect(Bounds.Size));
        var columns = Math.Max(1, (int)(Bounds.Width / _spacing));
        BuildOffsets(columns);
        if (_glowEnabled && _groupedScopedRenderer is not null)
        {
            RenderGroupedGlow(context);
        }
        for (var i = 0; i < _renderers.Length; i++)
        {
            var point = _offsets[i];
            var x = point.X;
            var y = point.Y;
            using (context.PushTransform(Avalonia.Matrix.CreateTranslation(x, y)))
            {
                var renderer = _renderers[i];
                if (_glowEnabled && _groupedScopedRenderer is null && renderer is not null)
                {
                    renderer.Render(context, Source);
                    GlowSubmissions++;
                    if (renderer is ScopedBlurEffectGlowPrototype)
                    {
                        EffectScopes++;
                    }
                }

                context.DrawGeometry(Brushes.White, null, Source);
            }
        }
    }

    private void BuildOffsets(int columns)
    {
        _offsets.Clear();
        for (var i = 0; i < _renderers.Length; i++)
        {
            _offsets.Add(new Point(
                8 + i % columns * _spacing + _offset,
                8 + i / columns * _spacing));
        }
    }

    private void RenderGroupedGlow(DrawingContext context)
    {
        var batchSize = _granularity switch
        {
            GlowEffectGranularity.PerControl => _offsets.Count,
            GlowEffectGranularity.Batch8 => 8,
            GlowEffectGranularity.Batch16 => 16,
            _ => throw new InvalidOperationException($"Unsupported grouped granularity {_granularity}.")
        };
        for (var start = 0; start < _offsets.Count; start += batchSize)
        {
            var count = Math.Min(batchSize, _offsets.Count - start);
            var bounds = CalculateBounds(start, count).Intersect(new Rect(Bounds.Size));
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                continue;
            }

            _groupedScopedRenderer!.RenderMany(context, Source, _offsets, start, count, bounds);
            GlowSubmissions += count;
            EffectScopes++;
        }
    }

    private Rect CalculateBounds(int start, int count)
    {
        var bounds = Source.Bounds.Translate((Vector)_offsets[start]);
        for (var i = start + 1; i < start + count; i++)
        {
            bounds = bounds.Union(Source.Bounds.Translate((Vector)_offsets[i]));
        }

        return bounds;
    }

    private static IGlowPrototypeRenderer? CreateRenderer(GlowDesktopBenchmarkRoute route, double radius)
    {
        var brush = new SolidColorBrush(Color.FromRgb(64, 222, 255));
        return route switch
        {
            GlowDesktopBenchmarkRoute.NoGlow => null,
            GlowDesktopBenchmarkRoute.Geometry => new OpacityOnlyGlowPrototype(brush, 1),
            GlowDesktopBenchmarkRoute.Opacity => new OpacityOnlyGlowPrototype(brush, 0.65),
            GlowDesktopBenchmarkRoute.Vector => new VectorExpansionGlowPrototype(brush, 0.65, radius),
            GlowDesktopBenchmarkRoute.Scoped => new ScopedBlurEffectGlowPrototype(brush, 0.65, radius),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };
    }
}
