namespace AtomUI.Labs.Led.Matrix.Character;

internal static class MatrixFiveBySevenGlyphMap
{
    public const int Width = MatrixGlyph.Width;
    public const int Height = MatrixGlyph.Height;
    public const string SupportedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ?-.:_+=/";

    private static readonly MatrixGlyph[] s_glyphs = CreateGlyphs();

    public static bool TryGetGlyph(char character, out MatrixGlyph glyph)
    {
        if (character < s_glyphs.Length)
        {
            glyph = s_glyphs[character];
            return character == ' ' || glyph.Bits != 0;
        }

        glyph = default;
        return false;
    }

    private static MatrixGlyph[] CreateGlyphs()
    {
        var glyphs = new MatrixGlyph[128];

        void Add(char character, byte r0, byte r1, byte r2, byte r3, byte r4, byte r5, byte r6)
        {
            glyphs[character] = FromRows(r0, r1, r2, r3, r4, r5, r6);
        }

        Add('A', 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001);
        Add('B', 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110);
        Add('C', 0b01111, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b01111);
        Add('D', 0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110);
        Add('E', 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111);
        Add('F', 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000);
        Add('G', 0b01111, 0b10000, 0b10000, 0b10111, 0b10001, 0b10001, 0b01110);
        Add('H', 0b10001, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001);
        Add('I', 0b01110, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110);
        Add('J', 0b00111, 0b00010, 0b00010, 0b00010, 0b10010, 0b10010, 0b01100);
        Add('K', 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001);
        Add('L', 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111);
        Add('M', 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001);
        Add('N', 0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001, 0b10001);
        Add('O', 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110);
        Add('P', 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000);
        Add('Q', 0b01110, 0b10001, 0b10001, 0b10001, 0b10101, 0b10010, 0b01101);
        Add('R', 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001);
        Add('S', 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110);
        Add('T', 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100);
        Add('U', 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110);
        Add('V', 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100);
        Add('W', 0b10001, 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b01010);
        Add('X', 0b10001, 0b10001, 0b01010, 0b00100, 0b01010, 0b10001, 0b10001);
        Add('Y', 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100, 0b00100);
        Add('Z', 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b10000, 0b11111);

        Add('0', 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110);
        Add('1', 0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110);
        Add('2', 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111);
        Add('3', 0b11110, 0b00001, 0b00001, 0b01110, 0b00001, 0b00001, 0b11110);
        Add('4', 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010);
        Add('5', 0b11111, 0b10000, 0b10000, 0b11110, 0b00001, 0b00001, 0b11110);
        Add('6', 0b01110, 0b10000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110);
        Add('7', 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000);
        Add('8', 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110);
        Add('9', 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00001, 0b01110);

        glyphs[' '] = default;
        Add('?', 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b00000, 0b00100);
        Add('-', 0b00000, 0b00000, 0b00000, 0b11111, 0b00000, 0b00000, 0b00000);
        Add('.', 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00110, 0b00110);
        Add(':', 0b00000, 0b00110, 0b00110, 0b00000, 0b00110, 0b00110, 0b00000);
        Add('_', 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b00000, 0b11111);
        Add('+', 0b00000, 0b00100, 0b00100, 0b11111, 0b00100, 0b00100, 0b00000);
        Add('=', 0b00000, 0b00000, 0b11111, 0b00000, 0b11111, 0b00000, 0b00000);
        Add('/', 0b00001, 0b00010, 0b00010, 0b00100, 0b01000, 0b01000, 0b10000);

        return glyphs;
    }

    private static MatrixGlyph FromRows(byte r0, byte r1, byte r2, byte r3, byte r4, byte r5, byte r6)
    {
        var bits = PackRow(r0, 0)
                   | PackRow(r1, 1)
                   | PackRow(r2, 2)
                   | PackRow(r3, 3)
                   | PackRow(r4, 4)
                   | PackRow(r5, 5)
                   | PackRow(r6, 6);
        return new MatrixGlyph(bits);
    }

    private static ulong PackRow(byte rowBits, int row)
    {
        ulong bits = 0;
        for (var column = 0; column < Width; column++)
        {
            if ((rowBits & (1 << (Width - 1 - column))) != 0)
            {
                bits |= 1UL << (row * Width + column);
            }
        }

        return bits;
    }
}
