using AtomUI.Labs.Led.Segment;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Segment;

public class SegmentDisplayContractTests
{
    [Fact]
    public void DefaultValues_ShouldRemainStable()
    {
        var display = new SegmentDisplay();

        display.Text.ShouldBeNull();
        display.CharacterHeight.ShouldBe(72);
        display.CharacterAspectRatio.ShouldBe(0.58);
        display.CharacterSpacing.ShouldBe(8);
        display.SegmentThickness.ShouldBe(8);
        display.SegmentGap.ShouldBe(2);
        display.SegmentBevelRatio.ShouldBe(0.5);
        display.DotScale.ShouldBe(0.72);
        display.Padding.ShouldBe(default);
        display.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Left);
        display.VerticalContentAlignment.ShouldBe(VerticalAlignment.Top);
        display.OverflowMode.ShouldBe(SegmentOverflowMode.Clip);
        display.Background.ShouldBeNull();
        display.CornerRadius.ShouldBe(default);
        display.ActiveBrush.ShouldBeNull();
        display.InactiveBrush.ShouldBeNull();
        display.GlowBrush.ShouldBeNull();
        display.GlowOpacity.ShouldBe(0.35);
        display.GlowRadius.ShouldBe(6);
        display.ShowInactiveSegments.ShouldBeTrue();
    }

    [Fact]
    public void StyledProperties_ShouldKeepRegisteredNames()
    {
        SegmentDisplay.TextProperty.Name.ShouldBe(nameof(SegmentDisplay.Text));
        SegmentDisplay.CharacterHeightProperty.Name.ShouldBe(nameof(SegmentDisplay.CharacterHeight));
        SegmentDisplay.CharacterAspectRatioProperty.Name.ShouldBe(nameof(SegmentDisplay.CharacterAspectRatio));
        SegmentDisplay.CharacterSpacingProperty.Name.ShouldBe(nameof(SegmentDisplay.CharacterSpacing));
        SegmentDisplay.SegmentThicknessProperty.Name.ShouldBe(nameof(SegmentDisplay.SegmentThickness));
        SegmentDisplay.SegmentGapProperty.Name.ShouldBe(nameof(SegmentDisplay.SegmentGap));
        SegmentDisplay.SegmentBevelRatioProperty.Name.ShouldBe(nameof(SegmentDisplay.SegmentBevelRatio));
        SegmentDisplay.DotScaleProperty.Name.ShouldBe(nameof(SegmentDisplay.DotScale));
        SegmentDisplay.PaddingProperty.Name.ShouldBe(nameof(SegmentDisplay.Padding));
        SegmentDisplay.HorizontalContentAlignmentProperty.Name.ShouldBe(nameof(SegmentDisplay.HorizontalContentAlignment));
        SegmentDisplay.VerticalContentAlignmentProperty.Name.ShouldBe(nameof(SegmentDisplay.VerticalContentAlignment));
        SegmentDisplay.OverflowModeProperty.Name.ShouldBe(nameof(SegmentDisplay.OverflowMode));
        SegmentDisplay.BackgroundProperty.Name.ShouldBe(nameof(SegmentDisplay.Background));
        SegmentDisplay.CornerRadiusProperty.Name.ShouldBe(nameof(SegmentDisplay.CornerRadius));
        SegmentDisplay.ActiveBrushProperty.Name.ShouldBe(nameof(SegmentDisplay.ActiveBrush));
        SegmentDisplay.InactiveBrushProperty.Name.ShouldBe(nameof(SegmentDisplay.InactiveBrush));
        SegmentDisplay.GlowBrushProperty.Name.ShouldBe(nameof(SegmentDisplay.GlowBrush));
        SegmentDisplay.GlowOpacityProperty.Name.ShouldBe(nameof(SegmentDisplay.GlowOpacity));
        SegmentDisplay.GlowRadiusProperty.Name.ShouldBe(nameof(SegmentDisplay.GlowRadius));
        SegmentDisplay.ShowInactiveSegmentsProperty.Name.ShouldBe(nameof(SegmentDisplay.ShowInactiveSegments));
    }

    [Fact]
    public void StyledProperties_ShouldKeepRegisteredDefaults()
    {
        SegmentDisplay.TextProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBeNull();
        SegmentDisplay.CharacterHeightProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(72);
        SegmentDisplay.CharacterAspectRatioProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(0.58);
        SegmentDisplay.CharacterSpacingProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(8);
        SegmentDisplay.SegmentThicknessProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(8);
        SegmentDisplay.SegmentGapProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(2);
        SegmentDisplay.SegmentBevelRatioProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(0.5);
        SegmentDisplay.DotScaleProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(0.72);
        SegmentDisplay.PaddingProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(default);
        SegmentDisplay.HorizontalContentAlignmentProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(HorizontalAlignment.Left);
        SegmentDisplay.VerticalContentAlignmentProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(VerticalAlignment.Top);
        SegmentDisplay.OverflowModeProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(SegmentOverflowMode.Clip);
        SegmentDisplay.BackgroundProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBeNull();
        SegmentDisplay.CornerRadiusProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(default);
        SegmentDisplay.ActiveBrushProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBeNull();
        SegmentDisplay.InactiveBrushProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBeNull();
        SegmentDisplay.GlowBrushProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBeNull();
        SegmentDisplay.GlowOpacityProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(0.35);
        SegmentDisplay.GlowRadiusProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(6);
        SegmentDisplay.ShowInactiveSegmentsProperty.GetMetadata(typeof(SegmentDisplay)).DefaultValue.ShouldBe(true);
    }

    [Fact]
    public void StyledProperties_ShouldAcceptConfiguredValues()
    {
        var activeBrush   = Brushes.Red;
        var inactiveBrush = Brushes.Gray;
        var glowBrush     = Brushes.Yellow;
        var background    = Brushes.Black;
        var display = new SegmentDisplay
        {
            Text                 = "A-01",
            CharacterHeight      = 90,
            CharacterAspectRatio = 0.6,
            CharacterSpacing     = 10,
            SegmentThickness     = 12,
            SegmentGap           = 3,
            SegmentBevelRatio    = 0.25,
            DotScale             = 0.6,
            Padding              = new Thickness(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment   = VerticalAlignment.Bottom,
            OverflowMode               = SegmentOverflowMode.ScaleDown,
            Background           = background,
            CornerRadius         = new CornerRadius(6),
            ActiveBrush          = activeBrush,
            InactiveBrush        = inactiveBrush,
            GlowBrush            = glowBrush,
            GlowOpacity          = 0.4,
            GlowRadius           = 12,
            ShowInactiveSegments = false
        };

        display.Text.ShouldBe("A-01");
        display.CharacterHeight.ShouldBe(90);
        display.CharacterAspectRatio.ShouldBe(0.6);
        display.CharacterSpacing.ShouldBe(10);
        display.SegmentThickness.ShouldBe(12);
        display.SegmentGap.ShouldBe(3);
        display.SegmentBevelRatio.ShouldBe(0.25);
        display.DotScale.ShouldBe(0.6);
        display.Padding.ShouldBe(new Thickness(4));
        display.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Center);
        display.VerticalContentAlignment.ShouldBe(VerticalAlignment.Bottom);
        display.OverflowMode.ShouldBe(SegmentOverflowMode.ScaleDown);
        display.Background.ShouldBeSameAs(background);
        display.CornerRadius.ShouldBe(new CornerRadius(6));
        display.ActiveBrush.ShouldBeSameAs(activeBrush);
        display.InactiveBrush.ShouldBeSameAs(inactiveBrush);
        display.GlowBrush.ShouldBeSameAs(glowBrush);
        display.GlowOpacity.ShouldBe(0.4);
        display.GlowRadius.ShouldBe(12);
        display.ShowInactiveSegments.ShouldBeFalse();
    }
}
