using AtomUI.Labs.Controls.Led.Matrix;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixDisplayContractTests
{
    [Fact]
    public void DefaultValues_ShouldMatchDocumentedContract()
    {
        var display = new MatrixDisplay();

        display.Text.ShouldBeNull();
        display.DotSize.ShouldBe(6);
        display.DotSpacing.ShouldBe(2);
        display.DotShape.ShouldBe(MatrixDotShape.Circle);
        display.DotCornerRadiusRatio.ShouldBe(0.25);
        display.CharacterSpacing.ShouldBe(8);
        display.Padding.ShouldBe(default);
        display.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Left);
        display.VerticalContentAlignment.ShouldBe(VerticalAlignment.Top);
        display.OverflowMode.ShouldBe(MatrixOverflowMode.Clip);
        display.Background.ShouldBeNull();
        display.BorderBrush.ShouldBeNull();
        display.BorderThickness.ShouldBe(default);
        display.CornerRadius.ShouldBe(default);
        display.ActiveBrush.ShouldBeNull();
        display.InactiveBrush.ShouldBeNull();
        display.GlowBrush.ShouldBeNull();
        display.GlowOpacity.ShouldBe(0.35);
        display.GlowRadius.ShouldBe(6);
        display.IsMarqueeEnabled.ShouldBeFalse();
        display.MarqueeSpeed.ShouldBe(48);
        display.MarqueeRepeatDelay.ShouldBe(TimeSpan.FromMilliseconds(500));
        display.ShowInactiveDots.ShouldBeTrue();
    }

    [Fact]
    public void StyledProperties_ShouldKeepRegisteredNamesAndDefaults()
    {
        MatrixDisplay.TextProperty.Name.ShouldBe(nameof(MatrixDisplay.Text));
        MatrixDisplay.DotSizeProperty.Name.ShouldBe(nameof(MatrixDisplay.DotSize));
        MatrixDisplay.DotSpacingProperty.Name.ShouldBe(nameof(MatrixDisplay.DotSpacing));
        MatrixDisplay.DotShapeProperty.Name.ShouldBe(nameof(MatrixDisplay.DotShape));
        MatrixDisplay.DotCornerRadiusRatioProperty.Name.ShouldBe(nameof(MatrixDisplay.DotCornerRadiusRatio));
        MatrixDisplay.CharacterSpacingProperty.Name.ShouldBe(nameof(MatrixDisplay.CharacterSpacing));
        MatrixDisplay.PaddingProperty.Name.ShouldBe(nameof(MatrixDisplay.Padding));
        MatrixDisplay.HorizontalContentAlignmentProperty.Name.ShouldBe(nameof(MatrixDisplay.HorizontalContentAlignment));
        MatrixDisplay.VerticalContentAlignmentProperty.Name.ShouldBe(nameof(MatrixDisplay.VerticalContentAlignment));
        MatrixDisplay.OverflowModeProperty.Name.ShouldBe(nameof(MatrixDisplay.OverflowMode));
        MatrixDisplay.BackgroundProperty.Name.ShouldBe(nameof(MatrixDisplay.Background));
        MatrixDisplay.BorderBrushProperty.Name.ShouldBe(nameof(MatrixDisplay.BorderBrush));
        MatrixDisplay.BorderThicknessProperty.Name.ShouldBe(nameof(MatrixDisplay.BorderThickness));
        MatrixDisplay.CornerRadiusProperty.Name.ShouldBe(nameof(MatrixDisplay.CornerRadius));
        MatrixDisplay.ActiveBrushProperty.Name.ShouldBe(nameof(MatrixDisplay.ActiveBrush));
        MatrixDisplay.InactiveBrushProperty.Name.ShouldBe(nameof(MatrixDisplay.InactiveBrush));
        MatrixDisplay.GlowBrushProperty.Name.ShouldBe(nameof(MatrixDisplay.GlowBrush));
        MatrixDisplay.GlowOpacityProperty.Name.ShouldBe(nameof(MatrixDisplay.GlowOpacity));
        MatrixDisplay.GlowRadiusProperty.Name.ShouldBe(nameof(MatrixDisplay.GlowRadius));
        MatrixDisplay.IsMarqueeEnabledProperty.Name.ShouldBe(nameof(MatrixDisplay.IsMarqueeEnabled));
        MatrixDisplay.MarqueeSpeedProperty.Name.ShouldBe(nameof(MatrixDisplay.MarqueeSpeed));
        MatrixDisplay.MarqueeRepeatDelayProperty.Name.ShouldBe(nameof(MatrixDisplay.MarqueeRepeatDelay));
        MatrixDisplay.ShowInactiveDotsProperty.Name.ShouldBe(nameof(MatrixDisplay.ShowInactiveDots));

        MatrixDisplay.DotSizeProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(6);
        MatrixDisplay.DotSpacingProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(2);
        MatrixDisplay.DotShapeProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(MatrixDotShape.Circle);
        MatrixDisplay.DotCornerRadiusRatioProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(0.25);
        MatrixDisplay.CharacterSpacingProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(8);
        MatrixDisplay.OverflowModeProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(MatrixOverflowMode.Clip);
        MatrixDisplay.ShowInactiveDotsProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(true);
        MatrixDisplay.GlowBrushProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBeNull();
        MatrixDisplay.GlowOpacityProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(0.35);
        MatrixDisplay.GlowRadiusProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(6);
        MatrixDisplay.IsMarqueeEnabledProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(false);
        MatrixDisplay.MarqueeSpeedProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue.ShouldBe(48);
        MatrixDisplay.MarqueeRepeatDelayProperty.GetMetadata(typeof(MatrixDisplay)).DefaultValue
                     .ShouldBe(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void StyledProperties_ShouldAcceptConfiguredValues()
    {
        var display = new MatrixDisplay
        {
            Text                       = "MATRIX",
            DotSize                    = 9,
            DotSpacing                 = 3,
            DotShape                   = MatrixDotShape.RoundedSquare,
            DotCornerRadiusRatio       = 0.35,
            CharacterSpacing           = 11,
            Padding                    = new Thickness(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment   = VerticalAlignment.Bottom,
            OverflowMode               = MatrixOverflowMode.ScaleDown,
            Background                 = Brushes.Black,
            BorderBrush                = Brushes.Blue,
            BorderThickness            = new Thickness(1, 2, 3, 4),
            CornerRadius               = new CornerRadius(6),
            ActiveBrush                = Brushes.Red,
            InactiveBrush              = Brushes.Gray,
            GlowBrush                  = Brushes.Yellow,
            GlowOpacity                = 0.4,
            GlowRadius                 = 12,
            IsMarqueeEnabled           = true,
            MarqueeSpeed               = 96,
            MarqueeRepeatDelay         = TimeSpan.FromSeconds(2),
            ShowInactiveDots           = false
        };

        display.Text.ShouldBe("MATRIX");
        display.DotSize.ShouldBe(9);
        display.DotSpacing.ShouldBe(3);
        display.DotShape.ShouldBe(MatrixDotShape.RoundedSquare);
        display.DotCornerRadiusRatio.ShouldBe(0.35);
        display.CharacterSpacing.ShouldBe(11);
        display.Padding.ShouldBe(new Thickness(4));
        display.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Center);
        display.VerticalContentAlignment.ShouldBe(VerticalAlignment.Bottom);
        display.OverflowMode.ShouldBe(MatrixOverflowMode.ScaleDown);
        display.Background.ShouldBeSameAs(Brushes.Black);
        display.BorderBrush.ShouldBeSameAs(Brushes.Blue);
        display.BorderThickness.ShouldBe(new Thickness(1, 2, 3, 4));
        display.CornerRadius.ShouldBe(new CornerRadius(6));
        display.ActiveBrush.ShouldBeSameAs(Brushes.Red);
        display.InactiveBrush.ShouldBeSameAs(Brushes.Gray);
        display.GlowBrush.ShouldBeSameAs(Brushes.Yellow);
        display.GlowOpacity.ShouldBe(0.4);
        display.GlowRadius.ShouldBe(12);
        display.IsMarqueeEnabled.ShouldBeTrue();
        display.MarqueeSpeed.ShouldBe(96);
        display.MarqueeRepeatDelay.ShouldBe(TimeSpan.FromSeconds(2));
        display.ShowInactiveDots.ShouldBeFalse();
    }
}
