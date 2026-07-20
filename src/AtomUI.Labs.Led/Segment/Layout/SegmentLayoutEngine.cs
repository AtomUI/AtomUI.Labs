using Avalonia;
using AtomUI.Labs.Led.Segment;
using AtomUI.Labs.Led.Segment.Character;

namespace AtomUI.Labs.Led.Segment.Layout;

internal static class SegmentLayoutEngine
{
    private const double NarrowSymbolWidthRatio = 0.32;

    public static SegmentDisplayLayout Calculate(
        string? text,
        SegmentLayoutOptions options)
    {
        var patterns = BuildPatterns(text);
        if (patterns.Count == 0)
        {
            var emptyPadding = SegmentValueSanitizer.CoerceThickness(options.Padding);
            return new SegmentDisplayLayout(
                new Size(emptyPadding.Left + emptyPadding.Right, emptyPadding.Top + emptyPadding.Bottom),
                Array.Empty<SegmentCharacterSlot>());
        }

        var padding         = SegmentValueSanitizer.CoerceThickness(options.Padding);
        var characterHeight = SegmentValueSanitizer.CoerceNonNegative(options.CharacterHeight);
        var characterWidth = SegmentValueSanitizer.CoerceNonNegative(
            characterHeight * SegmentValueSanitizer.CoerceAtLeast(options.CharacterAspectRatio, 0.1));
        var spacing        = SegmentValueSanitizer.CoerceNonNegative(options.CharacterSpacing);
        var x              = padding.Left;
        var slots          = new List<SegmentCharacterSlot>(patterns.Count);

        for (var i = 0; i < patterns.Count; i++)
        {
            var pattern = patterns[i];
            var width   = GetCharacterWidth(pattern, characterWidth);
            var bounds  = new Rect(x, padding.Top, width, characterHeight);
            slots.Add(new SegmentCharacterSlot(pattern, bounds));

            x += width;
            if (i < patterns.Count - 1)
            {
                x += spacing;
            }
        }

        var desiredSize = new Size(
            x + padding.Right,
            characterHeight + padding.Top + padding.Bottom);

        return new SegmentDisplayLayout(desiredSize, slots);
    }

    private static List<SegmentCharacterPattern> BuildPatterns(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return new List<SegmentCharacterPattern>();
        }

        var patterns = new List<SegmentCharacterPattern>(text.Length);
        foreach (var character in text)
        {
            patterns.Add(SegmentCharacterMap.GetPattern(character));
        }

        return patterns;
    }

    private static double GetCharacterWidth(SegmentCharacterPattern pattern, double defaultWidth)
    {
        return pattern.Kind is SegmentCharacterKind.Colon or SegmentCharacterKind.Dot
            ? defaultWidth * NarrowSymbolWidthRatio
            : defaultWidth;
    }
}
