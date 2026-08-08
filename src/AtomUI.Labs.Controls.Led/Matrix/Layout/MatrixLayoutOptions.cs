using Avalonia;

namespace AtomUI.Labs.Controls.Led.Matrix.Layout;

internal readonly record struct MatrixLayoutOptions(
    double DotSize,
    double DotSpacing,
    double CharacterSpacing,
    Thickness Padding);
