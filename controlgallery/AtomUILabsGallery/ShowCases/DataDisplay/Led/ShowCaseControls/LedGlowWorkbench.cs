using AtomUI.Labs.Led.Matrix;
using AtomUI.Labs.Led.Segment;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AtomUILabsGallery.ShowCases.Led;

public sealed class LedGlowWorkbench : StackPanel, IDisposable
{
    private static readonly GlowBrushOption[] BrushOptions =
    [
        new("Cyan", Color.FromRgb(74, 222, 255)),
        new("Red", Color.FromRgb(255, 48, 48)),
        new("Magenta", Color.FromRgb(255, 76, 210)),
        new("Green", Color.FromRgb(105, 255, 168)),
        new("Amber", Color.FromRgb(255, 190, 82))
    ];

    private readonly MatrixDisplay _matrix;
    private readonly SegmentDisplay _segment;
    private readonly CheckBox _enabled;
    private readonly ComboBox _brush;
    private readonly ComboBox _mode;
    private readonly Slider _opacity;
    private readonly Slider _radius;
    private readonly TextBlock _opacityValue;
    private readonly TextBlock _radiusValue;
    private CancellationTokenSource? _animationCancellation;
    private bool _disposed;

    public LedGlowWorkbench()
    {
        Spacing = 12;
        _matrix = CreateMatrixPreview();
        _segment = CreateSegmentPreview();
        _enabled = new CheckBox { Content = "Enabled", IsChecked = true };
        _brush = new ComboBox
        {
            ItemsSource = BrushOptions.Select(option => option.Name).ToArray(),
            SelectedIndex = 0,
            MinWidth = 130
        };
        _mode = new ComboBox
        {
            ItemsSource = new[] { "Static", "Breathe", "Pulse" },
            SelectedIndex = 0,
            MinWidth = 130
        };
        _opacity = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = 0.35,
            TickFrequency = 0.05,
            Width = 220
        };
        _radius = new Slider
        {
            Minimum = 0,
            Maximum = 24,
            Value = 6,
            TickFrequency = 1,
            Width = 220
        };
        _opacityValue = new TextBlock { Width = 48, VerticalAlignment = VerticalAlignment.Center };
        _radiusValue = new TextBlock { Width = 48, VerticalAlignment = VerticalAlignment.Center };

        Children.Add(new TextBlock
        {
            Text = "Glow workbench",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold
        });
        Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Children =
            {
                CreateLabeledControl("State", _enabled),
                CreateLabeledControl("Brush", _brush),
                CreateLabeledControl("Mode", _mode)
            }
        });
        Children.Add(CreateSliderRow("Opacity", _opacity, _opacityValue));
        Children.Add(CreateSliderRow("Radius", _radius, _radiusValue));
        Children.Add(new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 12,
            Children =
            {
                _matrix,
                PlaceInSecondColumn(_segment)
            }
        });

        _enabled.IsCheckedChanged += HandleConfigurationChanged;
        _brush.SelectionChanged += HandleConfigurationChanged;
        _mode.SelectionChanged += HandleConfigurationChanged;
        _opacity.PropertyChanged += HandleSliderPropertyChanged;
        _radius.PropertyChanged += HandleSliderPropertyChanged;
        DetachedFromVisualTree += HandleDetachedFromVisualTree;
        ApplyConfiguration();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _enabled.IsCheckedChanged -= HandleConfigurationChanged;
        _brush.SelectionChanged -= HandleConfigurationChanged;
        _mode.SelectionChanged -= HandleConfigurationChanged;
        _opacity.PropertyChanged -= HandleSliderPropertyChanged;
        _radius.PropertyChanged -= HandleSliderPropertyChanged;
        DetachedFromVisualTree -= HandleDetachedFromVisualTree;
        CancelAnimations();
    }

    private void HandleDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Dispose();
    }

    private void HandleConfigurationChanged(object? sender, EventArgs e)
    {
        ApplyConfiguration();
    }

    private void HandleSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == RangeBase.ValueProperty)
        {
            ApplyConfiguration();
        }
    }

    private void ApplyConfiguration()
    {
        CancelAnimations();
        _opacityValue.Text = _opacity.Value.ToString("0.00");
        _radiusValue.Text = _radius.Value.ToString("0");
        var enabled = _enabled.IsChecked == true;
        var brush = enabled ? CreateSelectedBrush() : null;
        ApplyStaticValues(_matrix, brush);
        ApplyStaticValues(_segment, brush);
        if (!enabled || _mode.SelectedIndex == 0)
        {
            return;
        }

        _animationCancellation = new CancellationTokenSource();
        var mode = _mode.SelectedIndex == 1 ? GlowAnimationMode.Breathe : GlowAnimationMode.Pulse;
        _ = RunAnimationAsync(
            _matrix,
            CreateAnimation(
                mode,
                MatrixDisplay.GlowOpacityProperty,
                MatrixDisplay.GlowRadiusProperty,
                _opacity.Value,
                _radius.Value),
            _animationCancellation.Token);
        _ = RunAnimationAsync(
            _segment,
            CreateAnimation(
                mode,
                SegmentDisplay.GlowOpacityProperty,
                SegmentDisplay.GlowRadiusProperty,
                _opacity.Value,
                _radius.Value),
            _animationCancellation.Token);
    }

    private void ApplyStaticValues(MatrixDisplay display, IBrush? brush)
    {
        display.GlowBrush = brush;
        display.GlowOpacity = _opacity.Value;
        display.GlowRadius = _radius.Value;
    }

    private void ApplyStaticValues(SegmentDisplay display, IBrush? brush)
    {
        display.GlowBrush = brush;
        display.GlowOpacity = _opacity.Value;
        display.GlowRadius = _radius.Value;
    }

    private Animation CreateAnimation(
        GlowAnimationMode mode,
        AvaloniaProperty opacityProperty,
        AvaloniaProperty radiusProperty,
        double opacity,
        double radius)
    {
        var animation = new Animation
        {
            Duration = mode == GlowAnimationMode.Breathe
                ? TimeSpan.FromSeconds(2.4)
                : TimeSpan.FromSeconds(1.2),
            IterationCount = IterationCount.Infinite,
            FillMode = FillMode.Both,
            Easing = new SineEaseInOut()
        };
        if (mode == GlowAnimationMode.Breathe)
        {
            var minimumOpacity = opacity * 0.35;
            AddKeyFrame(animation, 0, opacityProperty, minimumOpacity);
            AddKeyFrame(animation, 0.5, opacityProperty, opacity);
            AddKeyFrame(animation, 1, opacityProperty, minimumOpacity);
        }
        else
        {
            var minimumOpacity = opacity * 0.5;
            var minimumRadius = radius * 0.5;
            AddKeyFrame(animation, 0, opacityProperty, minimumOpacity, radiusProperty, minimumRadius);
            AddKeyFrame(animation, 0.5, opacityProperty, opacity, radiusProperty, radius);
            AddKeyFrame(animation, 1, opacityProperty, minimumOpacity, radiusProperty, minimumRadius);
        }

        return animation;
    }

    private static void AddKeyFrame(
        Animation animation,
        double cue,
        AvaloniaProperty property,
        double value)
    {
        animation.Children.Add(new KeyFrame
        {
            Cue = new Cue(cue),
            Setters = { new Setter(property, value) }
        });
    }

    private static void AddKeyFrame(
        Animation animation,
        double cue,
        AvaloniaProperty firstProperty,
        double firstValue,
        AvaloniaProperty secondProperty,
        double secondValue)
    {
        animation.Children.Add(new KeyFrame
        {
            Cue = new Cue(cue),
            Setters =
            {
                new Setter(firstProperty, firstValue),
                new Setter(secondProperty, secondValue)
            }
        });
    }

    private static async Task RunAnimationAsync(Control target, Animation animation, CancellationToken token)
    {
        try
        {
            await animation.RunAsync(target, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private void CancelAnimations()
    {
        _animationCancellation?.Cancel();
        _animationCancellation?.Dispose();
        _animationCancellation = null;
    }

    private IBrush CreateSelectedBrush()
    {
        var index = Math.Clamp(_brush.SelectedIndex, 0, BrushOptions.Length - 1);
        return new SolidColorBrush(BrushOptions[index].Color);
    }

    private static Control CreateLabeledControl(string label, Control control)
    {
        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = label, FontWeight = FontWeight.SemiBold },
                control
            }
        };
    }

    private static Control CreateSliderRow(string label, Slider slider, TextBlock value)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = label,
                    Width = 64,
                    VerticalAlignment = VerticalAlignment.Center
                },
                slider,
                value
            }
        };
    }

    private static Control PlaceInSecondColumn(Control control)
    {
        Grid.SetColumn(control, 1);
        return control;
    }

    private static MatrixDisplay CreateMatrixPreview()
    {
        return new MatrixDisplay
        {
            Text = "GLOW 2026",
            Height = 132,
            DotSize = 7,
            DotSpacing = 3,
            CharacterSpacing = 9,
            Padding = new Thickness(20),
            Background = new SolidColorBrush(Color.FromRgb(6, 10, 14)),
            ActiveBrush = Brushes.White,
            InactiveBrush = new SolidColorBrush(Color.FromArgb(28, 110, 138, 148)),
            CornerRadius = new CornerRadius(8),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private static SegmentDisplay CreateSegmentPreview()
    {
        return new SegmentDisplay
        {
            Text = "88:88",
            Height = 132,
            CharacterHeight = 72,
            SegmentThickness = 8,
            SegmentGap = 2,
            CharacterSpacing = 8,
            Padding = new Thickness(20),
            Background = new SolidColorBrush(Color.FromRgb(6, 10, 14)),
            ActiveBrush = Brushes.White,
            InactiveBrush = new SolidColorBrush(Color.FromArgb(28, 110, 138, 148)),
            CornerRadius = new CornerRadius(8),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private enum GlowAnimationMode
    {
        Breathe,
        Pulse
    }

    private sealed record GlowBrushOption(string Name, Color Color);
}
