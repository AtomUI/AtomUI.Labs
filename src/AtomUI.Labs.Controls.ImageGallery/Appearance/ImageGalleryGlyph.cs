using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.ImageGallery.Appearance;

internal enum ImageGalleryGlyphKind
{
    Previous,
    Next,
    Up,
    Down,
    Add,
    Subtract,
    RotateClockwise,
}

internal sealed class ImageGalleryGlyph : Control
{
    public static readonly StyledProperty<ImageGalleryGlyphKind> KindProperty =
        AvaloniaProperty.Register<ImageGalleryGlyph, ImageGalleryGlyphKind>(nameof(Kind));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<ImageGalleryGlyph>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<ImageGalleryGlyph>();

    static ImageGalleryGlyph()
    {
        AffectsMeasure<ImageGalleryGlyph>(FontSizeProperty);
        AffectsRender<ImageGalleryGlyph>(KindProperty, ForegroundProperty, FontSizeProperty);
    }

    public ImageGalleryGlyphKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var requested = double.IsFinite(FontSize) && FontSize > 0 ? FontSize : 0;
        return new Size(
            double.IsFinite(availableSize.Width) ? Math.Min(requested, availableSize.Width) : requested,
            double.IsFinite(availableSize.Height) ? Math.Min(requested, availableSize.Height) : requested);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Foreground is not { } foreground || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var extent = Math.Min(Bounds.Width, Bounds.Height);
        // Render coordinates are local to this control. Bounds.Center includes the
        // control's offset inside its parent and would apply that offset twice.
        var center = CalculateLocalCenter(Bounds.Size);
        var halfLength = extent * 0.32;
        var halfDepth = extent * 0.18;
        var pen = new Pen(foreground, Math.Max(1.5, extent * 0.09));

        switch (Kind)
        {
            case ImageGalleryGlyphKind.Previous:
                DrawChevron(
                    context,
                    pen,
                    new Point(center.X + halfDepth, center.Y - halfLength),
                    new Point(center.X - halfDepth, center.Y),
                    new Point(center.X + halfDepth, center.Y + halfLength));
                break;
            case ImageGalleryGlyphKind.Next:
                DrawChevron(
                    context,
                    pen,
                    new Point(center.X - halfDepth, center.Y - halfLength),
                    new Point(center.X + halfDepth, center.Y),
                    new Point(center.X - halfDepth, center.Y + halfLength));
                break;
            case ImageGalleryGlyphKind.Up:
                DrawChevron(
                    context,
                    pen,
                    new Point(center.X - halfLength, center.Y + halfDepth),
                    new Point(center.X, center.Y - halfDepth),
                    new Point(center.X + halfLength, center.Y + halfDepth));
                break;
            case ImageGalleryGlyphKind.Down:
                DrawChevron(
                    context,
                    pen,
                    new Point(center.X - halfLength, center.Y - halfDepth),
                    new Point(center.X, center.Y + halfDepth),
                    new Point(center.X + halfLength, center.Y - halfDepth));
                break;
            case ImageGalleryGlyphKind.Add:
                context.DrawLine(
                    pen,
                    new Point(center.X - halfLength, center.Y),
                    new Point(center.X + halfLength, center.Y));
                context.DrawLine(
                    pen,
                    new Point(center.X, center.Y - halfLength),
                    new Point(center.X, center.Y + halfLength));
                break;
            case ImageGalleryGlyphKind.Subtract:
                context.DrawLine(
                    pen,
                    new Point(center.X - halfLength, center.Y),
                    new Point(center.X + halfLength, center.Y));
                break;
            case ImageGalleryGlyphKind.RotateClockwise:
                DrawRotateClockwise(context, pen, center, extent);
                break;
        }
    }

    internal static Point CalculateLocalCenter(Size size) =>
        new(size.Width / 2, size.Height / 2);

    private static void DrawChevron(
        DrawingContext context,
        Pen pen,
        Point start,
        Point center,
        Point end)
    {
        context.DrawLine(pen, start, center);
        context.DrawLine(pen, center, end);
    }

    private static void DrawRotateClockwise(
        DrawingContext context,
        Pen pen,
        Point center,
        double extent)
    {
        // The path is normalized to a centered 24 x 24 icon box. Its visible
        // bounds are 3..21 on both axes, so scaling preserves the optical and
        // geometric center instead of inheriting a font glyph's baseline.
        var scale = extent / 24;
        Point Map(double x, double y) =>
            new(center.X + (x - 12) * scale, center.Y + (y - 12) * scale);

        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(Map(21, 12), false);
            geometryContext.ArcTo(
                Map(18.36, 5.64),
                new Size(9 * scale, 9 * scale),
                0,
                true,
                SweepDirection.Clockwise);
            geometryContext.LineTo(Map(21, 8));
            geometryContext.EndFigure(false);

            geometryContext.BeginFigure(Map(21, 3), false);
            geometryContext.LineTo(Map(21, 8));
            geometryContext.LineTo(Map(16, 8));
            geometryContext.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }
}
