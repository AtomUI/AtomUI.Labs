using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;

namespace AtomUI.Labs.Controls.ImageGallery.Appearance;

/// <summary>
/// Renders a single toolbar label by centering its visible ink rather than its
/// font line box. Font ascenders, descenders and fallback fonts therefore do not
/// push the visible label below the toolbar's geometric center line.
/// </summary>
internal sealed class ImageGalleryToolbarText : Control
{
    public static readonly StyledProperty<string?> TextProperty =
        TextBlock.TextProperty.AddOwner<ImageGalleryToolbarText>();

    public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
        TextBlock.TextTrimmingProperty.AddOwner<ImageGalleryToolbarText>();

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<ImageGalleryToolbarText>();

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<ImageGalleryToolbarText>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<ImageGalleryToolbarText>();

    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        TextElement.FontStyleProperty.AddOwner<ImageGalleryToolbarText>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner<ImageGalleryToolbarText>();

    public static readonly StyledProperty<FontStretch> FontStretchProperty =
        TextElement.FontStretchProperty.AddOwner<ImageGalleryToolbarText>();

    public static readonly StyledProperty<double> LetterSpacingProperty =
        TextElement.LetterSpacingProperty.AddOwner<ImageGalleryToolbarText>();

    private TextLayout? _textLayout;
    private double _layoutMaxWidth = double.NaN;

    static ImageGalleryToolbarText()
    {
        AffectsMeasure<ImageGalleryToolbarText>(
            TextProperty,
            TextTrimmingProperty,
            FontFamilyProperty,
            FontSizeProperty,
            FontStyleProperty,
            FontWeightProperty,
            FontStretchProperty,
            LetterSpacingProperty);
        AffectsRender<ImageGalleryToolbarText>(ForegroundProperty);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public FontStretch FontStretch
    {
        get => GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    public double LetterSpacing
    {
        get => GetValue(LetterSpacingProperty);
        set => SetValue(LetterSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = EnsureTextLayout(availableSize.Width);
        return new Size(
            ClampToConstraint(layout.WidthIncludingTrailingWhitespace, availableSize.Width),
            ClampToConstraint(layout.Height, availableSize.Height));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var layout = EnsureTextLayout(Bounds.Width);
        var origin = new Point(
            Math.Max(0, (Bounds.Width - layout.WidthIncludingTrailingWhitespace) / 2),
            CalculateOpticalOriginY(
                Bounds.Height,
                layout.Height,
                layout.Extent,
                layout.OverhangAfter));
        layout.Draw(context, origin);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty ||
            change.Property == TextTrimmingProperty ||
            change.Property == ForegroundProperty ||
            change.Property == FontFamilyProperty ||
            change.Property == FontSizeProperty ||
            change.Property == FontStyleProperty ||
            change.Property == FontWeightProperty ||
            change.Property == FontStretchProperty ||
            change.Property == LetterSpacingProperty ||
            change.Property == FlowDirectionProperty)
        {
            ResetTextLayout();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ResetTextLayout();
        base.OnDetachedFromVisualTree(e);
    }

    internal static double CalculateOpticalOriginY(
        double containerHeight,
        double layoutHeight,
        double inkExtent,
        double overhangAfter)
    {
        if (!double.IsFinite(inkExtent) || inkExtent <= 0 ||
            !double.IsFinite(layoutHeight) || !double.IsFinite(overhangAfter))
        {
            return Math.Max(0, (containerHeight - layoutHeight) / 2);
        }

        var inkTop = layoutHeight + overhangAfter - inkExtent;
        var inkCenter = inkTop + inkExtent / 2;
        return containerHeight / 2 - inkCenter;
    }

    internal double GetRenderedInkCenterY()
    {
        var layout = EnsureTextLayout(Bounds.Width);
        if (!double.IsFinite(layout.Extent) || layout.Extent <= 0 ||
            !double.IsFinite(layout.Height) || !double.IsFinite(layout.OverhangAfter))
        {
            return Bounds.Height / 2;
        }

        var origin = CalculateOpticalOriginY(
            Bounds.Height,
            layout.Height,
            layout.Extent,
            layout.OverhangAfter);
        var inkTop = layout.Height + layout.OverhangAfter - layout.Extent;
        return origin + inkTop + layout.Extent / 2;
    }

    private TextLayout EnsureTextLayout(double maxWidth)
    {
        var normalizedMaxWidth = double.IsFinite(maxWidth)
            ? Math.Max(0, maxWidth)
            : double.PositiveInfinity;
        if (_textLayout is not null && _layoutMaxWidth.Equals(normalizedMaxWidth))
        {
            return _textLayout;
        }

        ResetTextLayout();
        _layoutMaxWidth = normalizedMaxWidth;
        _textLayout = new TextLayout(
            Text ?? string.Empty,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Foreground,
            textTrimming: TextTrimming,
            flowDirection: FlowDirection,
            maxWidth: normalizedMaxWidth,
            maxLines: 1,
            letterSpacing: LetterSpacing);
        return _textLayout;
    }

    private void ResetTextLayout()
    {
        _textLayout?.Dispose();
        _textLayout = null;
        _layoutMaxWidth = double.NaN;
    }

    private static double ClampToConstraint(double requested, double constraint) =>
        double.IsFinite(constraint) ? Math.Min(requested, constraint) : requested;
}
