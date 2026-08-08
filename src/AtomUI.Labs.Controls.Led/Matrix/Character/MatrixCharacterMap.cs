using AtomUI.Labs.Controls.Led;
using System.Text;

namespace AtomUI.Labs.Controls.Led.Matrix.Character;

internal static class MatrixCharacterMap
{
    public static MatrixCharacterPattern GetPattern(char character)
    {
        var normalized = LedCharacterNormalizer.NormalizeAscii(character);
        if (MatrixFiveBySevenGlyphMap.TryGetGlyph(normalized, out var glyph))
        {
            return new MatrixCharacterPattern(normalized, glyph);
        }

        MatrixFiveBySevenGlyphMap.TryGetGlyph('?', out var fallbackGlyph);
        return new MatrixCharacterPattern('?', fallbackGlyph);
    }

    public static MatrixCharacterPattern GetPattern(Rune rune)
    {
        return rune.Value <= char.MaxValue
            ? GetPattern((char)rune.Value)
            : GetPattern('?');
    }

    public static int GetPatternCount(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var count = 0;
        foreach (var _ in text.EnumerateRunes())
        {
            count++;
        }

        return count;
    }

    public static string GetDisplayText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder? normalized = null;
        var consumedLength = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            var displayCharacter = GetPattern(rune).Character;
            if (normalized is null
                && rune.Utf16SequenceLength == 1
                && displayCharacter == text[consumedLength])
            {
                consumedLength++;
                continue;
            }

            normalized ??= new StringBuilder(text.Length).Append(text, 0, consumedLength);
            normalized.Append(displayCharacter);
            consumedLength += rune.Utf16SequenceLength;
        }

        return normalized?.ToString() ?? text;
    }
}
