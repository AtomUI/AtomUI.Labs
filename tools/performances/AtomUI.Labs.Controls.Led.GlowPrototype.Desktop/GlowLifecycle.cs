using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;

namespace AtomUI.Labs.Controls.Led.GlowPrototype.Desktop;

internal sealed record GlowLifecycleOptions(
    int Cycles,
    int TicksPerCycle,
    int Instances,
    string? OutputPath)
{
    public static bool TryParse(string[] args, out GlowLifecycleOptions options)
    {
        options = new GlowLifecycleOptions(100, 10, 64, null);
        if (!args.Contains("--lifecycle", StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        options = new GlowLifecycleOptions(
            ParsePositive(args, "--cycles", 100),
            ParsePositive(args, "--ticks-per-cycle", 10),
            ParsePositive(args, "--instances", 64),
            GetValue(args, "--output"));
        return true;
    }

    private static int ParsePositive(string[] args, string name, int fallback)
    {
        var value = GetValue(args, name);
        if (value is null)
        {
            return fallback;
        }

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : throw new ArgumentOutOfRangeException(name, "Value must be a positive integer.");
    }

    private static string? GetValue(string[] args, string name)
    {
        var index = Array.FindIndex(args, item => string.Equals(item, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

internal sealed class GlowLifecycleCoordinatorWindow : Window
{
    private readonly GlowLifecycleOptions _options;
    private readonly List<WeakReference<Window>> _windowReferences = [];
    private readonly List<WeakReference<GlowDesktopBenchmarkSurface>> _surfaceReferences = [];
    private readonly List<long> _settledMemory = [];
    private GlowLifecycleChildWindow? _current;
    private int _completedCycles;
    private bool _finishScheduled;

    public GlowLifecycleCoordinatorWindow(GlowLifecycleOptions options)
    {
        _options = options;
        Title = "Glow lifecycle coordinator";
        Width = 320;
        Height = 120;
        Background = Brushes.Black;
        Content = new TextBlock
        {
            Text = "Glow lifecycle test is running...",
            Margin = new Thickness(16),
            Foreground = Brushes.White
        };
        Opened += (_, _) => StartNextCycle();
    }

    private void StartNextCycle()
    {
        if (_completedCycles >= _options.Cycles)
        {
            if (!_finishScheduled)
            {
                _finishScheduled = true;
                DispatcherTimer.RunOnce(Finish, TimeSpan.FromMilliseconds(500));
            }
            return;
        }

        var child = new GlowLifecycleChildWindow(_options.Instances, _options.TicksPerCycle);
        _current = child;
        _windowReferences.Add(new WeakReference<Window>(child));
        _surfaceReferences.Add(new WeakReference<GlowDesktopBenchmarkSurface>(child.Surface));
        child.Closed += OnChildClosed;
        child.Show(this);
    }

    private void OnChildClosed(object? sender, EventArgs e)
    {
        if (sender is GlowLifecycleChildWindow child)
        {
            child.Closed -= OnChildClosed;
        }

        _current = null;
        _completedCycles++;
        if (_completedCycles % 20 == 0)
        {
            ForceCollection();
            _settledMemory.Add(GC.GetTotalMemory(true));
        }

        Dispatcher.UIThread.Post(StartNextCycle, DispatcherPriority.Background);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Finish()
    {
        _current = null;
        ForceCollection();
        var retainedWindows = _windowReferences.Count(reference => reference.TryGetTarget(out _));
        var retainedSurfaces = _surfaceReferences.Count(reference => reference.TryGetTarget(out _));
        var memoryGrowth = _settledMemory.Count < 2 ? 0 : _settledMemory[^1] - _settledMemory[0];
        var passed = retainedWindows == 0 && retainedSurfaces == 0;
        var report = new StringBuilder()
            .AppendLine("Glow lifecycle stress")
            .AppendLine(CultureInfo.InvariantCulture, $"Cycles: {_options.Cycles}")
            .AppendLine(CultureInfo.InvariantCulture, $"Ticks/cycle: {_options.TicksPerCycle}")
            .AppendLine(CultureInfo.InvariantCulture, $"Instances/window: {_options.Instances}")
            .AppendLine(CultureInfo.InvariantCulture, $"Retained windows: {retainedWindows}")
            .AppendLine(CultureInfo.InvariantCulture, $"Retained surfaces: {retainedSurfaces}")
            .AppendLine(CultureInfo.InvariantCulture, $"Settled memory samples: {string.Join(",", _settledMemory)}")
            .AppendLine(CultureInfo.InvariantCulture, $"First-to-last settled memory delta: {memoryGrowth}")
            .AppendLine(CultureInfo.InvariantCulture, $"Weak-reference lifecycle contract: {(passed ? "PASS" : "FAIL")}")
            .ToString();
        Console.Write(report);
        if (_options.OutputPath is not null)
        {
            File.WriteAllText(_options.OutputPath, report);
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown(passed ? 0 : 3);
        }
    }

    private static void ForceCollection()
    {
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}

internal sealed class GlowLifecycleChildWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly int _ticksPerCycle;
    private int _ticks;

    public GlowLifecycleChildWindow(int instances, int ticksPerCycle)
    {
        _ticksPerCycle = ticksPerCycle;
        Width = 720;
        Height = 480;
        ShowInTaskbar = false;
        Surface = new GlowDesktopBenchmarkSurface(GlowDesktopBenchmarkRoute.Scoped, 12, instances);
        Content = Surface;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += OnTick;
        Opened += OnOpened;
        Closed += OnClosed;
    }

    public GlowDesktopBenchmarkSurface Surface { get; }

    private void OnOpened(object? sender, EventArgs e)
    {
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        Surface.MutateGlow(_ticks);
        Surface.Advance();
        _ticks++;
        if (_ticks >= _ticksPerCycle)
        {
            Close();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        Opened -= OnOpened;
        Closed -= OnClosed;
        Content = null;
    }
}
