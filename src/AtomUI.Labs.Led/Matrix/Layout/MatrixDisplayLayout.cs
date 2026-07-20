using Avalonia;

namespace AtomUI.Labs.Led.Matrix.Layout;

internal sealed class MatrixDisplayLayout
{
    public MatrixDisplayLayout(Size desiredSize, Size glyphSize, IReadOnlyList<MatrixGlyphSlot> slots)
    {
        DesiredSize = desiredSize;
        GlyphSize   = glyphSize;
        Slots       = slots;
    }

    public Size DesiredSize { get; }

    public Size GlyphSize { get; }

    public IReadOnlyList<MatrixGlyphSlot> Slots { get; }
}
