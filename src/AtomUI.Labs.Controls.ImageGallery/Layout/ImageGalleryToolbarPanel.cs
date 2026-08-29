using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AtomUI.Labs.Controls.ImageGallery.Layout;

/// <summary>
/// Lays out toolbar roles sequentially while forcing every role onto the same
/// cross-axis center line. StackPanel deliberately does not provide that
/// guarantee for children with different desired sizes.
/// </summary>
internal sealed class ImageGalleryToolbarPanel : Panel
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarPanel, Orientation>(
            nameof(Orientation),
            Orientation.Horizontal);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<ImageGalleryToolbarPanel, double>(nameof(Spacing));

    static ImageGalleryToolbarPanel()
    {
        AffectsMeasure<ImageGalleryToolbarPanel>(OrientationProperty, SpacingProperty);
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var childConstraint = horizontal
            ? new Size(double.PositiveInfinity, availableSize.Height)
            : new Size(availableSize.Width, double.PositiveInfinity);
        var mainExtent = 0d;
        var crossExtent = 0d;
        var visibleCount = 0;

        foreach (var child in Children)
        {
            child.Measure(childConstraint);
            if (!child.IsVisible)
            {
                continue;
            }

            visibleCount++;
            mainExtent += horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            crossExtent = Math.Max(
                crossExtent,
                horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
        }

        if (visibleCount > 1)
        {
            mainExtent += Math.Max(0, Spacing) * (visibleCount - 1);
        }

        return horizontal
            ? new Size(mainExtent, crossExtent)
            : new Size(crossExtent, mainExtent);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var mainOffset = 0d;
        var visibleIndex = 0;
        var spacing = Math.Max(0, Spacing);

        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                child.Arrange(default);
                continue;
            }

            if (visibleIndex++ > 0)
            {
                mainOffset += spacing;
            }

            var desired = child.DesiredSize;
            if (horizontal)
            {
                var height = Math.Min(desired.Height, finalSize.Height);
                child.Arrange(new Rect(
                    mainOffset,
                    Math.Max(0, (finalSize.Height - height) / 2),
                    desired.Width,
                    height));
                mainOffset += desired.Width;
            }
            else
            {
                var width = Math.Min(desired.Width, finalSize.Width);
                child.Arrange(new Rect(
                    Math.Max(0, (finalSize.Width - width) / 2),
                    mainOffset,
                    width,
                    desired.Height));
                mainOffset += desired.Height;
            }
        }

        return finalSize;
    }
}
