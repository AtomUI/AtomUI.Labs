namespace AtomUI.Labs.Led.Segment.Character;

[Flags]
internal enum SegmentParts
{
    None               = 0,
    Top                = 1 << 0,
    UpperLeft          = 1 << 1,
    UpperRight         = 1 << 2,
    MiddleLeft         = 1 << 3,
    MiddleRight        = 1 << 4,
    LowerLeft          = 1 << 5,
    LowerRight         = 1 << 6,
    Bottom             = 1 << 7,
    UpperCenter        = 1 << 8,
    LowerCenter        = 1 << 9,
    UpperLeftDiagonal  = 1 << 10,
    UpperRightDiagonal = 1 << 11,
    LowerLeftDiagonal  = 1 << 12,
    LowerRightDiagonal = 1 << 13
}
