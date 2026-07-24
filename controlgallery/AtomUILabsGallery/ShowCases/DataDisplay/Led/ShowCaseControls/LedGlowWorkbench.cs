using AtomUI.Labs.Controls.Led.Matrix;
using AtomUI.Labs.Controls.Led.Segment;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AtomComboBox = AtomUI.Desktop.Controls.ComboBox;
using AtomSegmented = AtomUI.Desktop.Controls.Segmented;
using AtomSlider = AtomUI.Desktop.Controls.Slider;
using AtomTextBox = AtomUI.Desktop.Controls.TextBox;
using AtomToggleSwitch = AtomUI.Desktop.Controls.ToggleSwitch;

namespace AtomUILabsGallery.ShowCases.Led;

public sealed class LedGlowWorkbench : StackPanel
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
    private readonly AtomToggleSwitch _enabled;
    private readonly AtomComboBox _brushPreset;
    private readonly AtomTextBox _brushHex;
    private readonly TextBlock _brushValidation;
    private readonly AtomSegmented _mode;
    private readonly AtomSlider _opacity;
    private readonly AtomSlider _radius;
    private readonly TextBlock _opacityValue;
    private readonly TextBlock _radiusValue;
    private CancellationTokenSource? _animationCancellation;
    private Color _selectedBrushColor = BrushOptions[0].Color;
    private bool _isSynchronizingBrushEditor;
    private bool _isAttachedToVisualTree;

    internal bool HasActiveAnimation => _animationCancellation is { IsCancellationRequested: false };

    internal bool GlowEnabled
    {
        get => _enabled.IsChecked == true;
        set => _enabled.IsChecked = value;
    }

    internal int AnimationModeIndex
    {
        get => _mode.SelectedIndex;
        set => _mode.SelectedIndex = value;
    }

    internal int BrushPresetIndex
    {
        get => _brushPreset.SelectedIndex;
        set => _brushPreset.SelectedIndex = value;
    }

    internal string? BrushHexText
    {
        get => _brushHex.Text;
        set => _brushHex.Text = value;
    }

    internal bool HasBrushValidationError => _brushValidation.IsVisible;

    internal double GlowOpacity
    {
        get => _opacity.Value;
        set => _opacity.Value = value;
    }

    internal double GlowRadius
    {
        get => _radius.Value;
        set => _radius.Value = value;
    }

    internal IBrush? MatrixGlowBrush => _matrix.GlowBrush;

    internal IBrush? SegmentGlowBrush => _segment.GlowBrush;

    internal double MatrixGlowOpacity => _matrix.GlowOpacity;

    internal double SegmentGlowOpacity => _segment.GlowOpacity;

    internal double MatrixGlowRadius => _matrix.GlowRadius;

    internal double SegmentGlowRadius => _segment.GlowRadius;

    public LedGlowWorkbench()
    {
        Spacing = 12;
        _matrix = CreateMatrixPreview();
        _segment = CreateSegmentPreview();
        _enabled = new AtomToggleSwitch
        {
            OnContent = "Enabled",
            OffContent = "Disabled",
            IsChecked = true
        };
        _brushPreset = new AtomComboBox
        {
            ItemsSource = BrushOptions.Select(option => option.Name).ToArray(),
            SelectedIndex = 0,
            Width = 150
        };
        _brushHex = new AtomTextBox
        {
            Text = FormatHexColor(_selectedBrushColor),
            PlaceholderText = "#RRGGBB",
            Width = 140
        };
        _brushValidation = new TextBlock
        {
            Text = "Use #RRGGBB or #AARRGGBB.",
            Foreground = Brushes.OrangeRed,
            IsVisible = false
        };
        _mode = new AtomSegmented
        {
            ItemsSource = new[] { "Static", "Breathe", "Pulse" },
            SelectedIndex = 0,
            MinWidth = 300,
            IsExpanding = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _opacity = new AtomSlider
        {
            Minimum = 0,
            Maximum = 1,
            Value = 0.35,
            TickFrequency = 0.05,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _radius = new AtomSlider
        {
            Minimum = 0,
            Maximum = 24,
            Value = 6,
            TickFrequency = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _opacityValue = new TextBlock { Width = 48, VerticalAlignment = VerticalAlignment.Center };
        _radiusValue = new TextBlock { Width = 48, VerticalAlignment = VerticalAlignment.Center };

        Children.Add(new TextBlock
        {
            Text = "Glow workbench",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold
        });
        Children.Add(CreateConfigurationPanel());
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
        _brushPreset.SelectionChanged += HandleBrushPresetChanged;
        _brushHex.PropertyChanged += HandleBrushHexPropertyChanged;
        _mode.SelectionChanged += HandleConfigurationChanged;
        _opacity.PropertyChanged += HandleSliderPropertyChanged;
        _radius.PropertyChanged += HandleSliderPropertyChanged;
        AttachedToVisualTree += HandleAttachedToVisualTree;
        DetachedFromVisualTree += HandleDetachedFromVisualTree;
        ApplyConfiguration();
    }

    private Control CreateBrushEditor()
    {
        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        _brushPreset,
                        _brushHex
                    }
                },
                _brushValidation
            }
        };
    }

    private Control CreateConfigurationPanel()
    {
        var panel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("72,*,56"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 12,
            MaxWidth = 620,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        AddConfigurationRow(panel, 0, "State", _enabled);
        AddConfigurationRow(panel, 1, "Brush", CreateBrushEditor());
        AddConfigurationRow(panel, 2, "Mode", _mode);
        AddConfigurationRow(panel, 3, "Opacity", _opacity, _opacityValue);
        AddConfigurationRow(panel, 4, "Radius", _radius, _radiusValue);
        return panel;
    }

    private static void AddConfigurationRow(
        Grid panel,
        int row,
        string label,
        Control control,
        Control? value = null)
    {
        var labelBlock = new TextBlock
        {
            Text = label,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(labelBlock, row);
        panel.Children.Add(labelBlock);

        Grid.SetRow(control, row);
        Grid.SetColumn(control, 1);
        panel.Children.Add(control);

        if (value is null)
        {
            return;
        }

        Grid.SetRow(value, row);
        Grid.SetColumn(value, 2);
        panel.Children.Add(value);
    }

    private void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _isAttachedToVisualTree = true;
        ApplyConfiguration();
    }

    private void HandleDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _isAttachedToVisualTree = false;
        CancelAnimations();
    }

    private void HandleConfigurationChanged(object? sender, EventArgs e)
    {
        ApplyConfiguration();
    }

    private void HandleBrushPresetChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isSynchronizingBrushEditor ||
            _brushPreset.SelectedIndex < 0 ||
            _brushPreset.SelectedIndex >= BrushOptions.Length)
        {
            return;
        }

        _selectedBrushColor = BrushOptions[_brushPreset.SelectedIndex].Color;
        _isSynchronizingBrushEditor = true;
        try
        {
            _brushHex.Text = FormatHexColor(_selectedBrushColor);
        }
        finally
        {
            _isSynchronizingBrushEditor = false;
        }

        SetBrushValidationError(false);
        ApplyConfiguration();
    }

    private void HandleBrushHexPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_isSynchronizingBrushEditor || e.Property != AtomTextBox.TextProperty)
        {
            return;
        }

        if (!TryParseHexColor(_brushHex.Text, out var color))
        {
            SetBrushValidationError(true);
            return;
        }

        _selectedBrushColor = color;
        _isSynchronizingBrushEditor = true;
        try
        {
            _brushPreset.SelectedIndex = FindBrushPresetIndex(color);
        }
        finally
        {
            _isSynchronizingBrushEditor = false;
        }

        SetBrushValidationError(false);
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
        if (!_isAttachedToVisualTree || !enabled || _mode.SelectedIndex == 0)
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
        return new SolidColorBrush(_selectedBrushColor);
    }

    private void SetBrushValidationError(bool hasError)
    {
        _brushValidation.IsVisible = hasError;
    }

    private static int FindBrushPresetIndex(Color color)
    {
        for (var index = 0; index < BrushOptions.Length; index++)
        {
            if (BrushOptions[index].Color == color)
            {
                return index;
            }
        }

        return -1;
    }

    private static string FormatHexColor(Color color)
    {
        return color.A == byte.MaxValue
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static bool TryParseHexColor(string? value, out Color color)
    {
        color = default;
        if (value is null || value.Length is not (7 or 9) || value[0] != '#')
        {
            return false;
        }

        var hex = value.AsSpan(1);
        var alpha = byte.MaxValue;
        if (hex.Length == 8)
        {
            if (!byte.TryParse(
                    hex[..2],
                    System.Globalization.NumberStyles.HexNumber,
                    null,
                    out alpha))
            {
                return false;
            }

            hex = hex[2..];
        }

        if (!byte.TryParse(
                hex[..2],
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var red) ||
            !byte.TryParse(
                hex.Slice(2, 2),
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var green) ||
            !byte.TryParse(
                hex.Slice(4, 2),
                System.Globalization.NumberStyles.HexNumber,
                null,
                out var blue))
        {
            return false;
        }

        color = Color.FromArgb(alpha, red, green, blue);
        return true;
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
