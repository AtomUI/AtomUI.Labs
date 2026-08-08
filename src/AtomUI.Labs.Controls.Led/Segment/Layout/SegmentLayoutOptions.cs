using Avalonia;

namespace AtomUI.Labs.Controls.Led.Segment.Layout;

internal readonly record struct SegmentLayoutOptions(
    double CharacterHeight,
    double CharacterAspectRatio,
    double CharacterSpacing,
    Thickness Padding);
