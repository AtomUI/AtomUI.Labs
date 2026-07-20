namespace AtomUI.Labs.Led.Segment.Character;

internal readonly record struct SegmentCharacterPattern(
    char Character,
    SegmentCharacterKind Kind,
    SegmentParts Parts);
