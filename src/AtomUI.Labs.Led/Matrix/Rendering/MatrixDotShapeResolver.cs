namespace AtomUI.Labs.Led.Matrix.Rendering;

internal static class MatrixDotShapeResolver
{
    public const double DefaultCornerRadiusRatio = 0.25;
    public const double MaximumCornerRadiusRatio = 0.5;

    public static MatrixDotShape CoerceShape(MatrixDotShape shape)
    {
        return shape is MatrixDotShape.Circle or MatrixDotShape.Square or MatrixDotShape.RoundedSquare
            ? shape
            : MatrixDotShape.Circle;
    }

    public static double GetEffectiveCornerRadiusRatio(MatrixDotShape shape, double cornerRadiusRatio)
    {
        return CoerceShape(shape) == MatrixDotShape.RoundedSquare
            ? MatrixValueSanitizer.CoerceRange(cornerRadiusRatio, 0, MaximumCornerRadiusRatio)
            : 0;
    }
}
