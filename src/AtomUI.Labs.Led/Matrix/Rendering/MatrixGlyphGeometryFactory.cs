using AtomUI.Labs.Led.Matrix.Character;
using Avalonia;
using Avalonia.Media;

namespace AtomUI.Labs.Led.Matrix.Rendering;

internal static class MatrixGlyphGeometryFactory
{
    public static MatrixGlyphGeometry Create(
        MatrixGlyph glyph,
        double dotSize,
        double dotSpacing,
        MatrixDotShape dotShape,
        double dotCornerRadiusRatio)
    {
        dotSize    = MatrixValueSanitizer.CoerceAtLeast(dotSize, 1);
        dotSpacing = MatrixValueSanitizer.CoerceNonNegative(dotSpacing);
        dotShape   = MatrixDotShapeResolver.CoerceShape(dotShape);
        dotCornerRadiusRatio = MatrixDotShapeResolver.GetEffectiveCornerRadiusRatio(
            dotShape,
            dotCornerRadiusRatio);

        var activeGeometry   = new StreamGeometry();
        var inactiveGeometry = new StreamGeometry();
        var activeDotCount   = 0;
        var inactiveDotCount = 0;
        var step             = dotSize + dotSpacing;

        using (var activeContext = activeGeometry.Open())
        using (var inactiveContext = inactiveGeometry.Open())
        {
            for (var row = 0; row < MatrixGlyph.Height; row++)
            {
                for (var column = 0; column < MatrixGlyph.Width; column++)
                {
                    var origin = new Point(column * step, row * step);
                    if (glyph.IsActive(row, column))
                    {
                        AppendDot(
                            activeContext,
                            origin,
                            dotSize,
                            dotShape,
                            dotCornerRadiusRatio);
                        activeDotCount++;
                    }
                    else
                    {
                        AppendDot(
                            inactiveContext,
                            origin,
                            dotSize,
                            dotShape,
                            dotCornerRadiusRatio);
                        inactiveDotCount++;
                    }
                }
            }
        }

        return new MatrixGlyphGeometry(
            activeGeometry,
            inactiveGeometry,
            activeDotCount,
            inactiveDotCount);
    }

    private static void AppendDot(
        StreamGeometryContext context,
        Point origin,
        double dotSize,
        MatrixDotShape dotShape,
        double dotCornerRadiusRatio)
    {
        switch (dotShape)
        {
            case MatrixDotShape.Square:
                AppendSquare(context, origin, dotSize);
                break;
            case MatrixDotShape.RoundedSquare:
                AppendRoundedSquare(context, origin, dotSize, dotSize * dotCornerRadiusRatio);
                break;
            default:
                var radius = dotSize / 2;
                AppendCircle(
                    context,
                    new Point(origin.X + radius, origin.Y + radius),
                    radius);
                break;
        }
    }

    private static void AppendCircle(StreamGeometryContext context, Point center, double radius)
    {
        var left  = new Point(center.X - radius, center.Y);
        var right = new Point(center.X + radius, center.Y);
        var size  = new Size(radius, radius);

        context.BeginFigure(left, true);
        context.ArcTo(right, size, 0, false, SweepDirection.Clockwise, true);
        context.ArcTo(left, size, 0, false, SweepDirection.Clockwise, true);
        context.EndFigure(true);
    }

    private static void AppendSquare(StreamGeometryContext context, Point origin, double dotSize)
    {
        var topRight = new Point(origin.X + dotSize, origin.Y);
        var bottomRight = new Point(origin.X + dotSize, origin.Y + dotSize);
        var bottomLeft = new Point(origin.X, origin.Y + dotSize);

        context.BeginFigure(origin, true);
        context.LineTo(topRight, true);
        context.LineTo(bottomRight, true);
        context.LineTo(bottomLeft, true);
        context.EndFigure(true);
    }

    private static void AppendRoundedSquare(
        StreamGeometryContext context,
        Point origin,
        double dotSize,
        double radius)
    {
        if (radius <= 0)
        {
            AppendSquare(context, origin, dotSize);
            return;
        }

        var left = origin.X;
        var top = origin.Y;
        var right = left + dotSize;
        var bottom = top + dotSize;
        var arcSize = new Size(radius, radius);

        context.BeginFigure(new Point(left + radius, top), true);
        context.LineTo(new Point(right - radius, top), true);
        context.ArcTo(new Point(right, top + radius), arcSize, 0, false, SweepDirection.Clockwise, true);
        context.LineTo(new Point(right, bottom - radius), true);
        context.ArcTo(new Point(right - radius, bottom), arcSize, 0, false, SweepDirection.Clockwise, true);
        context.LineTo(new Point(left + radius, bottom), true);
        context.ArcTo(new Point(left, bottom - radius), arcSize, 0, false, SweepDirection.Clockwise, true);
        context.LineTo(new Point(left, top + radius), true);
        context.ArcTo(new Point(left + radius, top), arcSize, 0, false, SweepDirection.Clockwise, true);
        context.EndFigure(true);
    }
}
