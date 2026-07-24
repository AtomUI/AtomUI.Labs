using AtomUI.Labs.Controls.Led.Matrix.Character;
using Avalonia;

namespace AtomUI.Labs.Controls.Led.Matrix.Layout;

internal readonly record struct MatrixGlyphSlot(
    MatrixCharacterPattern Pattern,
    Point Origin);
