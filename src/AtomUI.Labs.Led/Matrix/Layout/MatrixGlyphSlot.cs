using AtomUI.Labs.Led.Matrix.Character;
using Avalonia;

namespace AtomUI.Labs.Led.Matrix.Layout;

internal readonly record struct MatrixGlyphSlot(
    MatrixCharacterPattern Pattern,
    Point Origin);
