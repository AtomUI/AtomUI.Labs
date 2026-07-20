using Avalonia;

namespace AtomUI.Labs.Led.Matrix.Layout;

internal readonly record struct MatrixLayoutOptions(
    double DotSize,
    double DotSpacing,
    double CharacterSpacing,
    Thickness Padding);
