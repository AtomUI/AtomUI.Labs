using AtomUI.Labs.Controls.Led.Matrix;
using AtomUI.Labs.Controls.Led.Segment;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AtomUILabsGallery.ShowCases.Led;

public sealed class LedDynamicDisplays : StackPanel
{
    private readonly SegmentDisplay _clockDisplay;
    private readonly SegmentDisplay _segmentCounterDisplay;
    private readonly MatrixDisplay _matrixCounterDisplay;
    private readonly DispatcherTimer _timer;
    private int _counter;

    public LedDynamicDisplays()
    {
        Spacing = 12;
        _clockDisplay = CreateSegmentDisplay();
        _segmentCounterDisplay = CreateSegmentDisplay();
        _matrixCounterDisplay = new MatrixDisplay
        {
            DotSize = 7,
            DotSpacing = 2,
            CharacterSpacing = 9,
            Padding = new Thickness(14),
            Background = new SolidColorBrush(Color.FromRgb(9, 18, 14)),
            ActiveBrush = new SolidColorBrush(Color.FromRgb(97, 247, 157)),
            InactiveBrush = new SolidColorBrush(Color.FromArgb(34, 97, 247, 157)),
            CornerRadius = new CornerRadius(8),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        Children.Add(_clockDisplay);
        Children.Add(new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 12,
            Children =
            {
                _segmentCounterDisplay,
                PlaceInSecondColumn(_matrixCounterDisplay)
            }
        });

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += HandleTimerTick;
        AttachedToVisualTree += HandleAttachedToVisualTree;
        DetachedFromVisualTree += HandleDetachedFromVisualTree;
        UpdateDisplays();
    }

    private static SegmentDisplay CreateSegmentDisplay()
    {
        return new SegmentDisplay
        {
            CharacterHeight = 54,
            SegmentThickness = 6,
            SegmentGap = 2,
            CharacterSpacing = 7,
            Padding = new Thickness(14),
            Background = new SolidColorBrush(Color.FromRgb(8, 12, 16)),
            ActiveBrush = new SolidColorBrush(Color.FromRgb(83, 237, 255)),
            InactiveBrush = new SolidColorBrush(Color.FromArgb(34, 83, 237, 255)),
            CornerRadius = new CornerRadius(10),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private static Control PlaceInSecondColumn(Control control)
    {
        Grid.SetColumn(control, 1);
        return control;
    }

    private void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        UpdateDisplays();
        _timer.Start();
    }

    private void HandleDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
    }

    private void HandleTimerTick(object? sender, EventArgs e)
    {
        UpdateDisplays();
    }

    private void UpdateDisplays()
    {
        var now = DateTime.Now;
        _clockDisplay.Text = $"{now:HH}:{now:mm}:{now:ss}";
        _segmentCounterDisplay.Text = _counter.ToString("D4");
        _matrixCounterDisplay.Text = _counter.ToString("D6");
        _counter = (_counter + 1) % 1_000_000;
    }
}
