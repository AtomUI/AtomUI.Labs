using Avalonia;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.ImageGallery.Appearance;

internal static class AppearanceValidation
{
    public static bool IsOpacity(double value) =>
        double.IsFinite(value) && value is >= 0 and <= 1;

    public static bool IsNonNegative(double value) =>
        double.IsFinite(value) && value >= 0;

    public static bool IsThickness(Thickness value) =>
        IsNonNegative(value.Left) &&
        IsNonNegative(value.Top) &&
        IsNonNegative(value.Right) &&
        IsNonNegative(value.Bottom);

    public static bool IsCornerRadius(CornerRadius value) =>
        IsNonNegative(value.TopLeft) &&
        IsNonNegative(value.TopRight) &&
        IsNonNegative(value.BottomRight) &&
        IsNonNegative(value.BottomLeft);
}

public sealed class ImageGalleryViewportAppearance : AvaloniaObject
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryViewportAppearance, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<ImageGalleryViewportAppearance, IBrush?>(nameof(BorderBrush));

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<ImageGalleryViewportAppearance, Thickness>(
            nameof(BorderThickness),
            validate: AppearanceValidation.IsThickness);

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ImageGalleryViewportAppearance, CornerRadius>(
            nameof(CornerRadius),
            validate: AppearanceValidation.IsCornerRadius);

    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<ImageGalleryViewportAppearance, double>(
            nameof(Opacity),
            1,
            validate: AppearanceValidation.IsOpacity);

    public static readonly StyledProperty<IBrush?> EmptyForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryViewportAppearance, IBrush?>(nameof(EmptyForeground));

    public static readonly StyledProperty<IBrush?> LoadingForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryViewportAppearance, IBrush?>(nameof(LoadingForeground));

    public static readonly StyledProperty<IBrush?> ErrorForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryViewportAppearance, IBrush?>(nameof(ErrorForeground));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double Opacity
    {
        get => GetValue(OpacityProperty);
        set => SetValue(OpacityProperty, value);
    }

    public IBrush? EmptyForeground
    {
        get => GetValue(EmptyForegroundProperty);
        set => SetValue(EmptyForegroundProperty, value);
    }

    public IBrush? LoadingForeground
    {
        get => GetValue(LoadingForegroundProperty);
        set => SetValue(LoadingForegroundProperty, value);
    }

    public IBrush? ErrorForeground
    {
        get => GetValue(ErrorForegroundProperty);
        set => SetValue(ErrorForegroundProperty, value);
    }
}

public sealed class ImageGalleryToolbarAppearance : AvaloniaObject
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, IBrush?>(nameof(Background));
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, IBrush?>(nameof(Foreground));
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, IBrush?>(nameof(BorderBrush));
    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, Thickness>(
            nameof(BorderThickness), validate: AppearanceValidation.IsThickness);
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, CornerRadius>(
            nameof(CornerRadius), validate: AppearanceValidation.IsCornerRadius);
    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, double>(
            nameof(Opacity), 1, validate: AppearanceValidation.IsOpacity);
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, Thickness>(
            nameof(Padding), validate: AppearanceValidation.IsThickness);
    public static readonly StyledProperty<double> WidthProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, double>(
            nameof(Width), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<double> HeightProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, double>(
            nameof(Height), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<double> MinWidthProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, double>(
            nameof(MinWidth), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<double> MaxWidthProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, double>(
            nameof(MaxWidth), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<double> MinHeightProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, double>(
            nameof(MinHeight), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<double> MaxHeightProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, double>(
            nameof(MaxHeight), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarAppearance, double>(
            nameof(ItemSpacing), validate: AppearanceValidation.IsNonNegative);

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
    public double Width { get => GetValue(WidthProperty); set => SetValue(WidthProperty, value); }
    public double Height { get => GetValue(HeightProperty); set => SetValue(HeightProperty, value); }
    public double MinWidth { get => GetValue(MinWidthProperty); set => SetValue(MinWidthProperty, value); }
    public double MaxWidth { get => GetValue(MaxWidthProperty); set => SetValue(MaxWidthProperty, value); }
    public double MinHeight { get => GetValue(MinHeightProperty); set => SetValue(MinHeightProperty, value); }
    public double MaxHeight { get => GetValue(MaxHeightProperty); set => SetValue(MaxHeightProperty, value); }
    public double ItemSpacing { get => GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }
}

public sealed class ImageGalleryButtonAppearance : AvaloniaObject
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(Background));
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(Foreground));
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(BorderBrush));
    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, Thickness>(
            nameof(BorderThickness), validate: AppearanceValidation.IsThickness);
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, CornerRadius>(
            nameof(CornerRadius), validate: AppearanceValidation.IsCornerRadius);
    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, double>(
            nameof(Opacity), 1, validate: AppearanceValidation.IsOpacity);
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, Thickness>(
            nameof(Padding), validate: AppearanceValidation.IsThickness);
    public static readonly StyledProperty<double> WidthProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, double>(
            nameof(Width), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<double> HeightProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, double>(
            nameof(Height), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, double>(
            nameof(IconSize), validate: AppearanceValidation.IsNonNegative);
    public static readonly StyledProperty<IBrush?> PointerOverBackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(PointerOverBackground));
    public static readonly StyledProperty<IBrush?> PointerOverForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(PointerOverForeground));
    public static readonly StyledProperty<IBrush?> PointerOverBorderBrushProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(PointerOverBorderBrush));
    public static readonly StyledProperty<IBrush?> PressedBackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(PressedBackground));
    public static readonly StyledProperty<IBrush?> PressedForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(PressedForeground));
    public static readonly StyledProperty<IBrush?> PressedBorderBrushProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(PressedBorderBrush));
    public static readonly StyledProperty<IBrush?> DisabledBackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(DisabledBackground));
    public static readonly StyledProperty<IBrush?> DisabledForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(DisabledForeground));
    public static readonly StyledProperty<IBrush?> DisabledBorderBrushProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, IBrush?>(nameof(DisabledBorderBrush));
    public static readonly StyledProperty<double> DisabledOpacityProperty =
        AvaloniaProperty.Register<ImageGalleryButtonAppearance, double>(
            nameof(DisabledOpacity), 0.5, validate: AppearanceValidation.IsOpacity);

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
    public double Width { get => GetValue(WidthProperty); set => SetValue(WidthProperty, value); }
    public double Height { get => GetValue(HeightProperty); set => SetValue(HeightProperty, value); }
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public IBrush? PointerOverBackground { get => GetValue(PointerOverBackgroundProperty); set => SetValue(PointerOverBackgroundProperty, value); }
    public IBrush? PointerOverForeground { get => GetValue(PointerOverForegroundProperty); set => SetValue(PointerOverForegroundProperty, value); }
    public IBrush? PointerOverBorderBrush { get => GetValue(PointerOverBorderBrushProperty); set => SetValue(PointerOverBorderBrushProperty, value); }
    public IBrush? PressedBackground { get => GetValue(PressedBackgroundProperty); set => SetValue(PressedBackgroundProperty, value); }
    public IBrush? PressedForeground { get => GetValue(PressedForegroundProperty); set => SetValue(PressedForegroundProperty, value); }
    public IBrush? PressedBorderBrush { get => GetValue(PressedBorderBrushProperty); set => SetValue(PressedBorderBrushProperty, value); }
    public IBrush? DisabledBackground { get => GetValue(DisabledBackgroundProperty); set => SetValue(DisabledBackgroundProperty, value); }
    public IBrush? DisabledForeground { get => GetValue(DisabledForegroundProperty); set => SetValue(DisabledForegroundProperty, value); }
    public IBrush? DisabledBorderBrush { get => GetValue(DisabledBorderBrushProperty); set => SetValue(DisabledBorderBrushProperty, value); }
    public double DisabledOpacity { get => GetValue(DisabledOpacityProperty); set => SetValue(DisabledOpacityProperty, value); }
}

public sealed class ImageGalleryFilmstripAppearance : AvaloniaObject
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryFilmstripAppearance, IBrush?>(nameof(Background));
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<ImageGalleryFilmstripAppearance, IBrush?>(nameof(BorderBrush));
    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<ImageGalleryFilmstripAppearance, Thickness>(
            nameof(BorderThickness), validate: AppearanceValidation.IsThickness);
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ImageGalleryFilmstripAppearance, CornerRadius>(
            nameof(CornerRadius), validate: AppearanceValidation.IsCornerRadius);
    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<ImageGalleryFilmstripAppearance, double>(
            nameof(Opacity), 1, validate: AppearanceValidation.IsOpacity);
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<ImageGalleryFilmstripAppearance, Thickness>(
            nameof(Padding), validate: AppearanceValidation.IsThickness);

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
}

public sealed class ImageGalleryThumbnailItemAppearance : AvaloniaObject
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, IBrush?>(nameof(Background));
    public static readonly StyledProperty<IBrush?> PointerOverBackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, IBrush?>(nameof(PointerOverBackground));
    public static readonly StyledProperty<IBrush?> SelectedBackgroundProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, IBrush?>(nameof(SelectedBackground));
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, IBrush?>(nameof(BorderBrush));
    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, Thickness>(
            nameof(BorderThickness), validate: AppearanceValidation.IsThickness);
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, CornerRadius>(
            nameof(CornerRadius), validate: AppearanceValidation.IsCornerRadius);
    public static readonly StyledProperty<double> OpacityProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, double>(
            nameof(Opacity), 1, validate: AppearanceValidation.IsOpacity);
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, Thickness>(
            nameof(Padding), validate: AppearanceValidation.IsThickness);
    public static readonly StyledProperty<IBrush?> SelectionIndicatorBrushProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, IBrush?>(
            nameof(SelectionIndicatorBrush),
            Brushes.DodgerBlue,
            validate: value => value is not null);
    public static readonly StyledProperty<Thickness> SelectionIndicatorThicknessProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, Thickness>(
            nameof(SelectionIndicatorThickness),
            new Thickness(2),
            validate: value => AppearanceValidation.IsThickness(value) &&
                               (value.Left > 0 || value.Top > 0 || value.Right > 0 || value.Bottom > 0));
    public static readonly StyledProperty<IBrush?> LoadingForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, IBrush?>(nameof(LoadingForeground));
    public static readonly StyledProperty<IBrush?> ErrorForegroundProperty =
        AvaloniaProperty.Register<ImageGalleryThumbnailItemAppearance, IBrush?>(nameof(ErrorForeground));

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? PointerOverBackground { get => GetValue(PointerOverBackgroundProperty); set => SetValue(PointerOverBackgroundProperty, value); }
    public IBrush? SelectedBackground { get => GetValue(SelectedBackgroundProperty); set => SetValue(SelectedBackgroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double Opacity { get => GetValue(OpacityProperty); set => SetValue(OpacityProperty, value); }
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
    public IBrush? SelectionIndicatorBrush { get => GetValue(SelectionIndicatorBrushProperty); set => SetValue(SelectionIndicatorBrushProperty, value); }
    public Thickness SelectionIndicatorThickness { get => GetValue(SelectionIndicatorThicknessProperty); set => SetValue(SelectionIndicatorThicknessProperty, value); }
    public IBrush? LoadingForeground { get => GetValue(LoadingForegroundProperty); set => SetValue(LoadingForegroundProperty, value); }
    public IBrush? ErrorForeground { get => GetValue(ErrorForegroundProperty); set => SetValue(ErrorForegroundProperty, value); }
}
