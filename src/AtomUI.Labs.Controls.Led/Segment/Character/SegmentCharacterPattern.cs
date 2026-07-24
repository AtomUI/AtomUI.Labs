namespace AtomUI.Labs.Controls.Led.Segment.Character;

internal readonly record struct SegmentCharacterPattern(
    char Character,
    SegmentCharacterKind Kind,
    SegmentParts Parts);
