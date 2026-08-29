using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AtomUI.Labs.Controls.ImageGallery.Appearance;

internal sealed class ImageGalleryButton : Button
{
    public static readonly StyledProperty<ImageGalleryButtonAppearance?> AppearanceProperty =
        AvaloniaProperty.Register<ImageGalleryButton, ImageGalleryButtonAppearance?>(nameof(Appearance));

    private ImageGalleryButtonAppearance? _subscribedAppearance;
    private Defaults _defaults;
    private bool _defaultsCaptured;

    static ImageGalleryButton()
    {
        AppearanceProperty.Changed.AddClassHandler<ImageGalleryButton>((button, _) =>
            button.ReplaceAppearance(button._subscribedAppearance, button.Appearance));
        IsPointerOverProperty.Changed.AddClassHandler<ImageGalleryButton>((button, _) => button.ApplyAppearance());
        Button.IsPressedProperty.Changed.AddClassHandler<ImageGalleryButton>((button, _) => button.ApplyAppearance());
        IsEffectivelyEnabledProperty.Changed.AddClassHandler<ImageGalleryButton>((button, _) => button.ApplyAppearance());
    }

    public ImageGalleryButtonAppearance? Appearance
    {
        get => GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Unsubscribe();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        CaptureDefaults();
        Subscribe(Appearance);
        ApplyAppearance();
    }

    private void ReplaceAppearance(
        ImageGalleryButtonAppearance? oldValue,
        ImageGalleryButtonAppearance? newValue)
    {
        if (ReferenceEquals(oldValue, newValue))
        {
            return;
        }

        Unsubscribe();
        Subscribe(newValue);
        ApplyAppearance();
    }

    private void Subscribe(ImageGalleryButtonAppearance? appearance)
    {
        if (appearance is null || ReferenceEquals(appearance, _subscribedAppearance))
        {
            return;
        }

        _subscribedAppearance = appearance;
        appearance.PropertyChanged += OnAppearancePropertyChanged;
    }

    private void Unsubscribe()
    {
        if (_subscribedAppearance is not null)
        {
            _subscribedAppearance.PropertyChanged -= OnAppearancePropertyChanged;
            _subscribedAppearance = null;
        }
    }

    private void OnAppearancePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) =>
        ApplyAppearance();

    private void ApplyAppearance()
    {
        if (!_defaultsCaptured)
        {
            return;
        }

        var appearance = Appearance;
        if (appearance is null)
        {
            ClearAppearanceValues();
            return;
        }

        var disabled = !IsEffectivelyEnabled;
        var pressed = IsPressed;
        var pointerOver = IsPointerOver;
        var background = SparseValue(
            appearance,
            ImageGalleryButtonAppearance.BackgroundProperty,
            _defaults.Background);
        var foreground = SparseValue(
            appearance,
            ImageGalleryButtonAppearance.ForegroundProperty,
            _defaults.Foreground);
        var borderBrush = SparseValue(
            appearance,
            ImageGalleryButtonAppearance.BorderBrushProperty,
            _defaults.BorderBrush);
        Background = disabled
            ? SparseValue(appearance, ImageGalleryButtonAppearance.DisabledBackgroundProperty, background)
            : pressed
                ? SparseValue(appearance, ImageGalleryButtonAppearance.PressedBackgroundProperty, background)
                : pointerOver
                    ? SparseValue(appearance, ImageGalleryButtonAppearance.PointerOverBackgroundProperty, background)
                    : background;
        Foreground = disabled
            ? SparseValue(appearance, ImageGalleryButtonAppearance.DisabledForegroundProperty, foreground)
            : pressed
                ? SparseValue(appearance, ImageGalleryButtonAppearance.PressedForegroundProperty, foreground)
                : pointerOver
                    ? SparseValue(appearance, ImageGalleryButtonAppearance.PointerOverForegroundProperty, foreground)
                    : foreground;
        BorderBrush = disabled
            ? SparseValue(appearance, ImageGalleryButtonAppearance.DisabledBorderBrushProperty, borderBrush)
            : pressed
                ? SparseValue(appearance, ImageGalleryButtonAppearance.PressedBorderBrushProperty, borderBrush)
                : pointerOver
                    ? SparseValue(appearance, ImageGalleryButtonAppearance.PointerOverBorderBrushProperty, borderBrush)
                    : borderBrush;
        BorderThickness = SparseValue(appearance, ImageGalleryButtonAppearance.BorderThicknessProperty, _defaults.BorderThickness);
        CornerRadius = SparseValue(appearance, ImageGalleryButtonAppearance.CornerRadiusProperty, _defaults.CornerRadius);
        Padding = SparseValue(appearance, ImageGalleryButtonAppearance.PaddingProperty, _defaults.Padding);
        // Opacity has explicit, state-independent defaults on the appearance object.
        // Falling back to the value captured from the control is incorrect because
        // a navigation button can first attach while its command is disabled, at
        // which point the control theme has already applied its disabled opacity.
        Opacity = disabled ? appearance.DisabledOpacity : appearance.Opacity;
        Width = SparseValue(appearance, ImageGalleryButtonAppearance.WidthProperty, _defaults.Width);
        Height = SparseValue(appearance, ImageGalleryButtonAppearance.HeightProperty, _defaults.Height);
        FontSize = SparseValue(appearance, ImageGalleryButtonAppearance.IconSizeProperty, _defaults.FontSize);
    }

    private void ClearAppearanceValues()
    {
        if (!_defaultsCaptured)
        {
            return;
        }

        ClearValue(BackgroundProperty);
        ClearValue(ForegroundProperty);
        ClearValue(BorderBrushProperty);
        ClearValue(BorderThicknessProperty);
        ClearValue(CornerRadiusProperty);
        ClearValue(PaddingProperty);
        ClearValue(OpacityProperty);
        ClearValue(WidthProperty);
        ClearValue(HeightProperty);
        ClearValue(FontSizeProperty);
    }

    private static T SparseValue<T>(
        AvaloniaObject appearance,
        StyledProperty<T> property,
        T fallback) => appearance.IsSet(property) ? appearance.GetValue(property) : fallback;

    private void CaptureDefaults()
    {
        if (_defaultsCaptured)
        {
            return;
        }

        _defaults = new Defaults(
            Background,
            Foreground,
            BorderBrush,
            BorderThickness,
            CornerRadius,
            Padding,
            Width,
            Height,
            FontSize);
        _defaultsCaptured = true;
    }

    private readonly record struct Defaults(
        IBrush? Background,
        IBrush? Foreground,
        IBrush? BorderBrush,
        Thickness BorderThickness,
        CornerRadius CornerRadius,
        Thickness Padding,
        double Width,
        double Height,
        double FontSize);
}
