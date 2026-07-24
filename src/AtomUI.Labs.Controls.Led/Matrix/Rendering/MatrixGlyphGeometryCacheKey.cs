namespace AtomUI.Labs.Controls.Led.Matrix.Rendering;

internal readonly record struct MatrixGlyphGeometryCacheKey(
    ulong Bits,
    double DotSize,
    double DotSpacing,
    MatrixDotShape DotShape,
    double DotCornerRadiusRatio);
