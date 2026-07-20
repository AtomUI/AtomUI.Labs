using System.Diagnostics;
using System.Globalization;
using System.Text;
using AtomUI.Labs.Led.Matrix;
using AtomUI.Labs.Led.Segment;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AtomUI.Labs.Led.GlowPrototype.Desktop;

internal enum FormalGlowDesktopControl
{
    Matrix,
    Segment
}

internal enum FormalGlowDesktopMode
{
    NoGlow,
    Static,
    Opacity,
    Radius,
    DynamicText
}

internal sealed record FormalGlowDesktopOptions(
    FormalGlowDesktopControl Control,
    FormalGlowDesktopMode Mode,
    int Instances,
    int FrequencyHz,
    int WarmupTicks,
    int MeasurementTicks,
    string? OutputPath)
{
    public static bool TryParse(string[] args, out FormalGlowDesktopOptions options)
    {
        options = new FormalGlowDesktopOptions(
            FormalGlowDesktopControl.Matrix,
            FormalGlowDesktopMode.NoGlow,
            10,
            60,
            60,
            300,
            null);
        if (!args.Contains("--formal-controls", StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        options = new FormalGlowDesktopOptions(
            ParseControl(GetValue(args, "--control") ?? "matrix"),
            ParseMode(GetValue(args, "--mode") ?? "noglow"),
            ParsePositive(args, "--instances", 10),
            ParseFrequency(args),
            ParseNonNegative(args, "--warmup", 60),
            ParsePositive(args, "--ticks", 300),
            GetValue(args, "--output"));
        return true;
    }

    private static FormalGlowDesktopControl ParseControl(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "matrix" => FormalGlowDesktopControl.Matrix,
            "segment" => FormalGlowDesktopControl.Segment,
            _ => throw new ArgumentException("--control must be matrix or segment.")
        };
    }

    private static FormalGlowDesktopMode ParseMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "noglow" => FormalGlowDesktopMode.NoGlow,
            "static" => FormalGlowDesktopMode.Static,
            "opacity" => FormalGlowDesktopMode.Opacity,
            "radius" => FormalGlowDesktopMode.Radius,
            "dynamictext" => FormalGlowDesktopMode.DynamicText,
            _ => throw new ArgumentException("--mode must be noglow, static, opacity, radius, or dynamictext.")
        };
    }

    private static int ParseFrequency(string[] args)
    {
        var value = ParseNonNegative(args, "--hz", 60);
        return value <= 240 ? value : throw new ArgumentOutOfRangeException("--hz", "Value must not exceed 240.");
    }

    private static int ParsePositive(string[] args, string name, int fallback)
    {
        var value = ParseNonNegative(args, name, fallback);
        return value > 0 ? value : throw new ArgumentOutOfRangeException(name, "Value must be positive.");
    }

    private static int ParseNonNegative(string[] args, string name, int fallback)
    {
        var value = GetValue(args, name);
        if (value is null)
        {
            return fallback;
        }

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0
            ? parsed
            : throw new ArgumentOutOfRangeException(name, "Value must be a non-negative integer.");
    }

    private static string? GetValue(string[] args, string name)
    {
        var index = Array.FindIndex(args, item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

internal sealed class FormalGlowDesktopWindow : Window
{
    private readonly FormalGlowDesktopOptions _options;
    private readonly IReadOnlyList<IFormalGlowDesktopTarget> _targets;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private int _ticks;
    private long _allocatedBefore;

    public FormalGlowDesktopWindow(FormalGlowDesktopOptions options)
    {
        _options = options;
        Title = $"Formal LED Glow - {options.Control} {options.Mode}";
        Width = 1600;
        Height = 1100;
        Background = Brushes.Black;
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        _targets = Enumerable.Range(0, options.Instances)
                             .Select(index => CreateTarget(options, index))
                             .ToArray();
        foreach (var target in _targets)
        {
            panel.Children.Add(target.Control);
        }

        Content = panel;
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
                target.ResetRenderCallbacks();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _allocatedBefore = GC.GetTotalAllocatedBytes(true);
            _stopwatch.Restart();
        }

        if (_options.FrequencyHz > 0)
        {
            foreach (var target in _targets)
            {
                target.Update(_options.Mode, _ticks);
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
        _stopwatch.Stop();
        var callbacks = _targets.Sum(target => target.RenderCallbacks);
        var expected = _options.FrequencyHz == 0 ? 0L : (long)_options.Instances * _options.MeasurementTicks;
        var completion = expected == 0 ? 1 : callbacks / (double)expected;
        var allocated = GC.GetTotalAllocatedBytes(false) - _allocatedBefore;
        var report = new StringBuilder()
            .AppendLine("Formal LED Glow real-window benchmark")
            .AppendLine(CultureInfo.InvariantCulture, $"Control: {_options.Control}")
            .AppendLine(CultureInfo.InvariantCulture, $"Mode: {_options.Mode}")
            .AppendLine(CultureInfo.InvariantCulture, $"Instances: {_options.Instances}")
            .AppendLine(CultureInfo.InvariantCulture, $"Frequency Hz: {_options.FrequencyHz}")
            .AppendLine(CultureInfo.InvariantCulture, $"Warmup ticks: {_options.WarmupTicks}")
            .AppendLine(CultureInfo.InvariantCulture, $"Measurement ticks: {_options.MeasurementTicks}")
            .AppendLine(CultureInfo.InvariantCulture, $"Elapsed ms: {_stopwatch.Elapsed.TotalMilliseconds:0.00}")
            .AppendLine(CultureInfo.InvariantCulture, $"Render callbacks: {callbacks}")
            .AppendLine(CultureInfo.InvariantCulture, $"Expected callbacks: {expected}")
            .AppendLine(CultureInfo.InvariantCulture, $"Callback completion: {completion:P2}")
            .AppendLine(CultureInfo.InvariantCulture, $"Allocated bytes/process: {allocated}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"Allocated bytes/completed callback: {(callbacks == 0 ? 0 : allocated / (double)callbacks):0.0}")
            .AppendLine("Warning: elapsed time includes dispatcher, composition and window backend work; it is not isolated GPU time.")
            .ToString();
        Console.Write(report);
        if (_options.OutputPath is not null)
        {
            File.WriteAllText(_options.OutputPath, report);
        }

        _timer.Tick -= OnTick;
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown(0);
        }
    }

    private static IFormalGlowDesktopTarget CreateTarget(FormalGlowDesktopOptions options, int index)
    {
        return options.Control == FormalGlowDesktopControl.Matrix
            ? new FormalMatrixGlowTarget(options.Mode, index)
            : new FormalSegmentGlowTarget(options.Mode, index);
    }
}

internal interface IFormalGlowDesktopTarget
{
    Control Control { get; }

    long RenderCallbacks { get; }

    void ResetRenderCallbacks();

    void Update(FormalGlowDesktopMode mode, int tick);
}

internal sealed class FormalMatrixGlowTarget : MatrixDisplay, IFormalGlowDesktopTarget
{
    public FormalMatrixGlowTarget(FormalGlowDesktopMode mode, int index)
    {
        Width = 210;
        Height = 92;
        Margin = new Thickness(4);
        Text = (index % 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
        DotSize = 4;
        DotSpacing = 2;
        CharacterSpacing = 5;
        Padding = new Thickness(8);
        Background = new SolidColorBrush(Color.FromRgb(6, 10, 14));
        ActiveBrush = Brushes.White;
        InactiveBrush = new SolidColorBrush(Color.FromArgb(28, 100, 130, 140));
        GlowBrush = mode == FormalGlowDesktopMode.NoGlow ? null : Brushes.Cyan;
        GlowOpacity = 0.35;
        GlowRadius = 6;
    }

    public Control Control => this;

    public long RenderCallbacks { get; private set; }

    public void ResetRenderCallbacks() => RenderCallbacks = 0;

    public void Update(FormalGlowDesktopMode mode, int tick)
    {
        ApplyMode(this, mode, tick);
        if (mode == FormalGlowDesktopMode.DynamicText)
        {
            Text = (tick % 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
        }
        else
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        RenderCallbacks++;
        base.Render(context);
    }

    private static void ApplyMode(MatrixDisplay display, FormalGlowDesktopMode mode, int tick)
    {
        if (mode == FormalGlowDesktopMode.Opacity)
        {
            display.GlowOpacity = tick % 120 / 119d;
        }
        else if (mode == FormalGlowDesktopMode.Radius)
        {
            var phase = tick % 49;
            display.GlowRadius = phase <= 24 ? phase : 48 - phase;
        }
    }
}

internal sealed class FormalSegmentGlowTarget : SegmentDisplay, IFormalGlowDesktopTarget
{
    public FormalSegmentGlowTarget(FormalGlowDesktopMode mode, int index)
    {
        Width = 260;
        Height = 92;
        Margin = new Thickness(4);
        Text = (index % 100_000).ToString("D5", CultureInfo.InvariantCulture);
        CharacterHeight = 54;
        SegmentThickness = 6;
        SegmentGap = 2;
        CharacterSpacing = 5;
        Padding = new Thickness(8);
        Background = new SolidColorBrush(Color.FromRgb(6, 10, 14));
        ActiveBrush = Brushes.White;
        InactiveBrush = new SolidColorBrush(Color.FromArgb(28, 100, 130, 140));
        GlowBrush = mode == FormalGlowDesktopMode.NoGlow ? null : Brushes.Cyan;
        GlowOpacity = 0.35;
        GlowRadius = 6;
    }

    public Control Control => this;

    public long RenderCallbacks { get; private set; }

    public void ResetRenderCallbacks() => RenderCallbacks = 0;

    public void Update(FormalGlowDesktopMode mode, int tick)
    {
        if (mode == FormalGlowDesktopMode.Opacity)
        {
            GlowOpacity = tick % 120 / 119d;
        }
        else if (mode == FormalGlowDesktopMode.Radius)
        {
            var phase = tick % 49;
            GlowRadius = phase <= 24 ? phase : 48 - phase;
        }

        if (mode == FormalGlowDesktopMode.DynamicText)
        {
            Text = (tick % 100_000).ToString("D5", CultureInfo.InvariantCulture);
        }
        else
        {
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        RenderCallbacks++;
        base.Render(context);
    }
}
