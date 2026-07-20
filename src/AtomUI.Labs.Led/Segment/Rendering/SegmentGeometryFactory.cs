using Avalonia;
using AtomUI.Labs.Led.Segment;
using AtomUI.Labs.Led.Segment.Character;
using Avalonia.Media;

namespace AtomUI.Labs.Led.Segment.Rendering;

internal static class SegmentGeometryFactory
{
    public static SegmentGeometrySet Create(Rect bounds, SegmentGeometryOptions options)
    {
        if (!IsUsableBounds(bounds))
        {
            return CreateEmptySet();
        }

        var maxThickness = Math.Min(bounds.Width, bounds.Height) / 4;
        var thickness    = SegmentValueSanitizer.CoerceRange(options.Thickness, 0, maxThickness);
        if (thickness <= 0)
        {
            return CreateEmptySet();
        }

        var maxGap = Math.Max(0, Math.Min(bounds.Width - thickness, bounds.Height - thickness) / 4);
        var gap    = SegmentValueSanitizer.CoerceRange(options.Gap, 0, maxGap);
        var bevel  = thickness * SegmentValueSanitizer.CoerceRange(options.BevelRatio, 0, 1);
        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var left   = bounds.X;
        var right  = bounds.Right;
        var top    = bounds.Y;
        var bottom = bounds.Bottom;

        var items = new[]
        {
            new SegmentGeometryItem(
                SegmentParts.Top,
                CreateHorizontalSegment(left + bevel + gap, right - bevel - gap, top, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.MiddleLeft,
                CreateHorizontalSegment(left + bevel + gap, centerX - gap, centerY - thickness / 2, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.MiddleRight,
                CreateHorizontalSegment(centerX + gap, right - bevel - gap, centerY - thickness / 2, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.Bottom,
                CreateHorizontalSegment(left + bevel + gap, right - bevel - gap, bottom - thickness, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.UpperLeft,
                CreateVerticalSegment(left, top + bevel + gap, centerY - gap, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.LowerLeft,
                CreateVerticalSegment(left, centerY + gap, bottom - bevel - gap, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.UpperRight,
                CreateVerticalSegment(right - thickness, top + bevel + gap, centerY - gap, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.LowerRight,
                CreateVerticalSegment(right - thickness, centerY + gap, bottom - bevel - gap, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.UpperCenter,
                CreateVerticalSegment(centerX - thickness / 2, top + bevel + gap, centerY - gap, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.LowerCenter,
                CreateVerticalSegment(centerX - thickness / 2, centerY + gap, bottom - bevel - gap, thickness, bevel)),
            new SegmentGeometryItem(
                SegmentParts.UpperLeftDiagonal,
                CreateDiagonalSegment(new Point(left + thickness + gap, top + thickness + gap),
                                      new Point(centerX - gap, centerY - gap),
                                      thickness)),
            new SegmentGeometryItem(
                SegmentParts.UpperRightDiagonal,
                CreateDiagonalSegment(new Point(right - thickness - gap, top + thickness + gap),
                                      new Point(centerX + gap, centerY - gap),
                                      thickness)),
            new SegmentGeometryItem(
                SegmentParts.LowerLeftDiagonal,
                CreateDiagonalSegment(new Point(left + thickness + gap, bottom - thickness - gap),
                                      new Point(centerX - gap, centerY + gap),
                                      thickness)),
            new SegmentGeometryItem(
                SegmentParts.LowerRightDiagonal,
                CreateDiagonalSegment(new Point(right - thickness - gap, bottom - thickness - gap),
                                      new Point(centerX + gap, centerY + gap),
                                      thickness))
        };

        return new SegmentGeometrySet(items);
    }

    public static IReadOnlyList<Geometry> CreateColon(Rect bounds, SegmentGeometryOptions options)
    {
        return new[]
        {
            CreateDot(
                new Rect(bounds.X, bounds.Y + bounds.Height * 0.25, bounds.Width, bounds.Height * 0.2),
                options,
                true),
            CreateDot(
                new Rect(bounds.X, bounds.Y + bounds.Height * 0.58, bounds.Width, bounds.Height * 0.2),
                options,
                true)
        };
    }

    public static Geometry CreateDot(Rect bounds, SegmentGeometryOptions options, bool centerVertically)
    {
        if (!IsUsableBounds(bounds))
        {
            return new StreamGeometry();
        }

        var size = Math.Min(bounds.Width, bounds.Height) * SegmentValueSanitizer.CoerceRange(options.DotScale, 0, 1);
        if (size <= 0)
        {
            return new StreamGeometry();
        }

        var x = bounds.X + (bounds.Width - size) / 2;
        var y = centerVertically
            ? bounds.Y + (bounds.Height - size) / 2
            : bounds.Bottom - size;
        return new EllipseGeometry(new Rect(x, y, size, size));
    }

    private static bool IsUsableBounds(Rect bounds)
    {
        return SegmentValueSanitizer.IsFinite(bounds.X)
               && SegmentValueSanitizer.IsFinite(bounds.Y)
               && SegmentValueSanitizer.IsFinite(bounds.Width)
               && SegmentValueSanitizer.IsFinite(bounds.Height)
               && bounds.Width > 0
               && bounds.Height > 0;
    }

    private static SegmentGeometrySet CreateEmptySet()
    {
        var items = new[]
        {
            new SegmentGeometryItem(SegmentParts.Top, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.MiddleLeft, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.MiddleRight, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.Bottom, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.UpperLeft, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.LowerLeft, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.UpperRight, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.LowerRight, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.UpperCenter, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.LowerCenter, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.UpperLeftDiagonal, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.UpperRightDiagonal, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.LowerLeftDiagonal, new StreamGeometry()),
            new SegmentGeometryItem(SegmentParts.LowerRightDiagonal, new StreamGeometry())
        };

        return new SegmentGeometrySet(items);
    }

    private static Geometry CreateHorizontalSegment(double x1, double x2, double y, double thickness, double bevel)
    {
        var midY = y + thickness / 2;
        return CreatePolygon(
            new Point(x1 + bevel, y),
            new Point(x2 - bevel, y),
            new Point(x2, midY),
            new Point(x2 - bevel, y + thickness),
            new Point(x1 + bevel, y + thickness),
            new Point(x1, midY));
    }

    private static Geometry CreateVerticalSegment(double x, double y1, double y2, double thickness, double bevel)
    {
        var midX = x + thickness / 2;
        return CreatePolygon(
            new Point(midX, y1),
            new Point(x + thickness, y1 + bevel),
            new Point(x + thickness, y2 - bevel),
            new Point(midX, y2),
            new Point(x, y2 - bevel),
            new Point(x, y1 + bevel));
    }

    private static Geometry CreateDiagonalSegment(Point start, Point end, double thickness)
    {
        var dx     = end.X - start.X;
        var dy     = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0)
        {
            return new StreamGeometry();
        }

        var nx   = -dy / length;
        var ny   = dx / length;
        var half = thickness / 2;
        var offset = new Point(nx * half, ny * half);

        return CreatePolygon(
            new Point(start.X + offset.X, start.Y + offset.Y),
            new Point(end.X + offset.X, end.Y + offset.Y),
            new Point(end.X - offset.X, end.Y - offset.Y),
            new Point(start.X - offset.X, start.Y - offset.Y));
    }

    private static Geometry CreatePolygon(params Point[] points)
    {
        if (points.Length == 0)
        {
            return new StreamGeometry();
        }

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(points[0], true);
            for (var i = 1; i < points.Length; i++)
            {
                context.LineTo(points[i]);
            }
            context.EndFigure(true);
        }

        return geometry;
    }
}
