using AtomUI.Labs.Controls.Led.Matrix.Character;
using Avalonia;
using System.Text;

namespace AtomUI.Labs.Controls.Led.Matrix.Layout;

internal static class MatrixLayoutEngine
{
    public static MatrixDisplayLayout Calculate(string? text, MatrixLayoutOptions options)
    {
        var dotSize         = MatrixValueSanitizer.CoerceAtLeast(options.DotSize, 1);
        var dotSpacing      = MatrixValueSanitizer.CoerceNonNegative(options.DotSpacing);
        var characterSpacing = MatrixValueSanitizer.CoerceNonNegative(options.CharacterSpacing);
        var padding         = MatrixValueSanitizer.CoerceThickness(options.Padding);
        var glyphSize = new Size(
            MatrixFiveBySevenGlyphMap.Width * dotSize
            + (MatrixFiveBySevenGlyphMap.Width - 1) * dotSpacing,
            MatrixFiveBySevenGlyphMap.Height * dotSize
            + (MatrixFiveBySevenGlyphMap.Height - 1) * dotSpacing);

        if (string.IsNullOrEmpty(text))
        {
            return new MatrixDisplayLayout(
                new Size(padding.Left + padding.Right, padding.Top + padding.Bottom),
                glyphSize,
                Array.Empty<MatrixGlyphSlot>());
        }

        var slots = new MatrixGlyphSlot[MatrixCharacterMap.GetPatternCount(text)];
        var x     = padding.Left;
        var slotIndex = 0;

        foreach (var rune in text.EnumerateRunes())
        {
            slots[slotIndex] = new MatrixGlyphSlot(
                MatrixCharacterMap.GetPattern(rune),
                new Point(x, padding.Top));
            x += glyphSize.Width;
            if (slotIndex < slots.Length - 1)
            {
                x += characterSpacing;
            }

            slotIndex++;
        }

        return new MatrixDisplayLayout(
            new Size(x + padding.Right, glyphSize.Height + padding.Top + padding.Bottom),
            glyphSize,
            slots);
    }
}
