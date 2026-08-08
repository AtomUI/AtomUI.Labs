namespace AtomUI.Labs.Controls.Led.Matrix.Character;

internal readonly record struct MatrixGlyph(ulong Bits)
{
    public const int Width = 5;
    public const int Height = 7;

    public bool IsActive(int row, int column)
    {
        if ((uint)row >= Height || (uint)column >= Width)
        {
            return false;
        }

        var bitIndex = row * Width + column;
        return ((Bits >> bitIndex) & 1UL) != 0;
    }
}
