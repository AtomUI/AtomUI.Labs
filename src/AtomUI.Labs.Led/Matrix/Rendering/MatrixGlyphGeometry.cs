using Avalonia.Media;

namespace AtomUI.Labs.Led.Matrix.Rendering;

internal sealed class MatrixGlyphGeometry
{
    public MatrixGlyphGeometry(
        Geometry activeGeometry,
        Geometry inactiveGeometry,
        int activeDotCount,
        int inactiveDotCount)
    {
        ActiveGeometry   = activeGeometry;
        InactiveGeometry = inactiveGeometry;
        ActiveDotCount   = activeDotCount;
        InactiveDotCount = inactiveDotCount;
    }

    public Geometry ActiveGeometry { get; }

    public Geometry InactiveGeometry { get; }

    public int ActiveDotCount { get; }

    public int InactiveDotCount { get; }
}
