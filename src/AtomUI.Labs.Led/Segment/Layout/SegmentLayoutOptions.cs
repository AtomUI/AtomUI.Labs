using Avalonia;

namespace AtomUI.Labs.Led.Segment.Layout;

internal readonly record struct SegmentLayoutOptions(
    double CharacterHeight,
    double CharacterAspectRatio,
    double CharacterSpacing,
    Thickness Padding);
