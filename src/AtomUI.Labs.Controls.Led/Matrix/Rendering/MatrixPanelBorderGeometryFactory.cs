using Avalonia;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.Led.Matrix.Rendering;

internal readonly record struct MatrixPanelBorderGeometryCacheKey(
    Size Size,
    Thickness BorderThickness,
    CornerRadius CornerRadius);

internal static class MatrixPanelBorderGeometryFactory
{
    public static Geometry Create(Size size, Thickness borderThickness, CornerRadius cornerRadius)
    {
        var bounds = new Rect(size);
        var outer = CreateRoundedRectangle(
            bounds,
            new CornerRadii(cornerRadius));
        var innerBounds = new Rect(
            borderThickness.Left,
            borderThickness.Top,
            Math.Max(0, size.Width - borderThickness.Left - borderThickness.Right),
            Math.Max(0, size.Height - borderThickness.Top - borderThickness.Bottom));
        if (innerBounds.Width <= 0 || innerBounds.Height <= 0)
        {
            return outer;
        }

        var inner = CreateRoundedRectangle(
            innerBounds,
            CornerRadii.CreateInner(cornerRadius, borderThickness));
        return new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner);
    }

    private static StreamGeometry CreateRoundedRectangle(Rect bounds, CornerRadii radii)
    {
        radii = radii.Normalize(bounds.Size);
        var geometry = new StreamGeometry();
        using var context = geometry.Open();

        context.BeginFigure(new Point(bounds.Left + radii.TopLeftX, bounds.Top), true);
        context.LineTo(new Point(bounds.Right - radii.TopRightX, bounds.Top), true);
        AppendCorner(
            context,
            new Point(bounds.Right, bounds.Top + radii.TopRightY),
            radii.TopRightX,
            radii.TopRightY);
        context.LineTo(new Point(bounds.Right, bounds.Bottom - radii.BottomRightY), true);
        AppendCorner(
            context,
            new Point(bounds.Right - radii.BottomRightX, bounds.Bottom),
            radii.BottomRightX,
            radii.BottomRightY);
        context.LineTo(new Point(bounds.Left + radii.BottomLeftX, bounds.Bottom), true);
        AppendCorner(
            context,
            new Point(bounds.Left, bounds.Bottom - radii.BottomLeftY),
            radii.BottomLeftX,
            radii.BottomLeftY);
        context.LineTo(new Point(bounds.Left, bounds.Top + radii.TopLeftY), true);
        AppendCorner(
            context,
            new Point(bounds.Left + radii.TopLeftX, bounds.Top),
            radii.TopLeftX,
            radii.TopLeftY);
        context.EndFigure(true);
        return geometry;
    }

    private static void AppendCorner(
        StreamGeometryContext context,
        Point endPoint,
        double radiusX,
        double radiusY)
    {
        if (radiusX <= 0 || radiusY <= 0)
        {
            context.LineTo(endPoint, true);
            return;
        }

        context.ArcTo(
            endPoint,
            new Size(radiusX, radiusY),
            0,
            false,
            SweepDirection.Clockwise,
            true);
    }

    private readonly record struct CornerRadii(
        double TopLeftX,
        double TopLeftY,
        double TopRightX,
        double TopRightY,
        double BottomRightX,
        double BottomRightY,
        double BottomLeftX,
        double BottomLeftY)
    {
        public CornerRadii(CornerRadius cornerRadius)
            : this(
                cornerRadius.TopLeft,
                cornerRadius.TopLeft,
                cornerRadius.TopRight,
                cornerRadius.TopRight,
                cornerRadius.BottomRight,
                cornerRadius.BottomRight,
                cornerRadius.BottomLeft,
                cornerRadius.BottomLeft)
        {
        }

        public static CornerRadii CreateInner(CornerRadius cornerRadius, Thickness thickness)
        {
            return new CornerRadii(
                Math.Max(0, cornerRadius.TopLeft - thickness.Left),
                Math.Max(0, cornerRadius.TopLeft - thickness.Top),
                Math.Max(0, cornerRadius.TopRight - thickness.Right),
                Math.Max(0, cornerRadius.TopRight - thickness.Top),
                Math.Max(0, cornerRadius.BottomRight - thickness.Right),
                Math.Max(0, cornerRadius.BottomRight - thickness.Bottom),
                Math.Max(0, cornerRadius.BottomLeft - thickness.Left),
                Math.Max(0, cornerRadius.BottomLeft - thickness.Bottom));
        }

        public CornerRadii Normalize(Size size)
        {
            var scale = 1d;
            scale = LimitScale(scale, size.Width, TopLeftX + TopRightX);
            scale = LimitScale(scale, size.Width, BottomLeftX + BottomRightX);
            scale = LimitScale(scale, size.Height, TopLeftY + BottomLeftY);
            scale = LimitScale(scale, size.Height, TopRightY + BottomRightY);
            return scale >= 1
                ? this
                : new CornerRadii(
                    TopLeftX * scale,
                    TopLeftY * scale,
                    TopRightX * scale,
                    TopRightY * scale,
                    BottomRightX * scale,
                    BottomRightY * scale,
                    BottomLeftX * scale,
                    BottomLeftY * scale);
        }

        private static double LimitScale(double current, double available, double required)
        {
            return required > available && required > 0
                ? Math.Min(current, available / required)
                : current;
        }
    }
}
