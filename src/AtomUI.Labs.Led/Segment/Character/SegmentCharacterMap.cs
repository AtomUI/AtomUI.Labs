using AtomUI.Labs.Led;

namespace AtomUI.Labs.Led.Segment.Character;

internal static class SegmentCharacterMap
{
    public static string GetDisplayText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        char[]? normalized = null;
        for (var i = 0; i < text.Length; i++)
        {
            var displayCharacter = GetPattern(text[i]).Character;
            if (displayCharacter == text[i])
            {
                continue;
            }

            normalized ??= text.ToCharArray();
            normalized[i] = displayCharacter;
        }

        return normalized is null ? text : new string(normalized);
    }

    public static SegmentCharacterPattern GetPattern(char character)
    {
        var normalized = LedCharacterNormalizer.NormalizeAscii(character);
        if (normalized == ' ')
        {
            return new SegmentCharacterPattern(normalized, SegmentCharacterKind.Empty, SegmentParts.None);
        }

        if (normalized == ':')
        {
            return new SegmentCharacterPattern(normalized, SegmentCharacterKind.Colon, SegmentParts.None);
        }

        if (normalized == '.')
        {
            return new SegmentCharacterPattern(normalized, SegmentCharacterKind.Dot, SegmentParts.None);
        }

        var parts = normalized switch
        {
            '0' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.LowerLeft |
                   SegmentParts.LowerRight | SegmentParts.Bottom,
            '1' => SegmentParts.UpperRight | SegmentParts.LowerRight,
            '2' => SegmentParts.Top | SegmentParts.UpperRight | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.Bottom,
            '3' => SegmentParts.Top | SegmentParts.UpperRight | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerRight | SegmentParts.Bottom,
            '4' => SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerRight,
            '5' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerRight | SegmentParts.Bottom,
            '6' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.LowerRight | SegmentParts.Bottom,
            '7' => SegmentParts.Top | SegmentParts.UpperRight | SegmentParts.LowerRight,
            '8' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerLeft |
                   SegmentParts.LowerRight | SegmentParts.Bottom,
            '9' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerRight | SegmentParts.Bottom,
            'A' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.LowerRight,
            'B' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerLeft |
                   SegmentParts.LowerRight | SegmentParts.Bottom | SegmentParts.LowerCenter,
            'C' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.LowerLeft | SegmentParts.Bottom,
            'D' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.LowerLeft | SegmentParts.LowerRight | SegmentParts.Bottom | SegmentParts.UpperCenter |
                   SegmentParts.LowerCenter,
            'E' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.Bottom,
            'F' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerLeft,
            'G' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.LowerLeft | SegmentParts.Bottom |
                   SegmentParts.LowerRight | SegmentParts.MiddleRight,
            'H' => SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.LowerRight,
            'I' => SegmentParts.Top | SegmentParts.Bottom | SegmentParts.UpperCenter | SegmentParts.LowerCenter,
            'J' => SegmentParts.UpperRight | SegmentParts.LowerRight | SegmentParts.LowerLeft | SegmentParts.Bottom,
            'K' => SegmentParts.UpperLeft | SegmentParts.LowerLeft | SegmentParts.UpperRightDiagonal |
                   SegmentParts.LowerRightDiagonal,
            'L' => SegmentParts.UpperLeft | SegmentParts.LowerLeft | SegmentParts.Bottom,
            'M' => SegmentParts.UpperLeft | SegmentParts.LowerLeft | SegmentParts.UpperRight |
                   SegmentParts.LowerRight | SegmentParts.UpperLeftDiagonal | SegmentParts.UpperRightDiagonal,
            'N' => SegmentParts.UpperLeft | SegmentParts.LowerLeft | SegmentParts.UpperRight |
                   SegmentParts.LowerRight | SegmentParts.UpperLeftDiagonal | SegmentParts.LowerRightDiagonal,
            'O' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.LowerLeft | SegmentParts.LowerRight | SegmentParts.Bottom,
            'P' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerLeft,
            'Q' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.LowerLeft | SegmentParts.LowerRight | SegmentParts.Bottom | SegmentParts.LowerRightDiagonal,
            'R' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight |
                   SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.LowerRightDiagonal,
            'S' => SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.MiddleLeft |
                   SegmentParts.MiddleRight | SegmentParts.LowerRight | SegmentParts.Bottom,
            'T' => SegmentParts.Top | SegmentParts.UpperCenter | SegmentParts.LowerCenter,
            'U' => SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.LowerLeft |
                   SegmentParts.LowerRight | SegmentParts.Bottom,
            'V' => SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.LowerLeftDiagonal |
                   SegmentParts.LowerRightDiagonal,
            'W' => SegmentParts.UpperLeft | SegmentParts.LowerLeft | SegmentParts.UpperRight |
                   SegmentParts.LowerRight | SegmentParts.LowerLeftDiagonal | SegmentParts.LowerRightDiagonal,
            'X' => SegmentParts.UpperLeftDiagonal | SegmentParts.UpperRightDiagonal |
                   SegmentParts.LowerLeftDiagonal | SegmentParts.LowerRightDiagonal,
            'Y' => SegmentParts.UpperLeftDiagonal | SegmentParts.UpperRightDiagonal | SegmentParts.LowerCenter,
            'Z' => SegmentParts.Top | SegmentParts.UpperRightDiagonal | SegmentParts.LowerLeftDiagonal | SegmentParts.Bottom,
            '-' => SegmentParts.MiddleLeft | SegmentParts.MiddleRight,
            '_' => SegmentParts.Bottom,
            _   => SegmentParts.None
        };

        return parts == SegmentParts.None
            ? new SegmentCharacterPattern(' ', SegmentCharacterKind.Empty, SegmentParts.None)
            : new SegmentCharacterPattern(normalized, SegmentCharacterKind.Segments, parts);
    }
}
