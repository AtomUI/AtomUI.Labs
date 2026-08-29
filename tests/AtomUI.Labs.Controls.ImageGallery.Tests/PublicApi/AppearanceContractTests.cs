using AtomUI.Labs.Controls.ImageGallery.Appearance;
using Avalonia;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.PublicApi;

public sealed class AppearanceContractTests
{
    [Fact]
    public void NumericAppearanceValuesAreValidated()
    {
        var button = new ImageGalleryButtonAppearance();
        var thumbnail = new ImageGalleryThumbnailItemAppearance();

        Should.Throw<ArgumentException>(() => button.Opacity = 1.1);
        Should.Throw<ArgumentException>(() => button.IconSize = -1);
        Should.Throw<ArgumentException>(() => button.Padding = new Thickness(-1));
        Should.Throw<ArgumentException>(() => thumbnail.SelectionIndicatorThickness = default);
    }

    [Fact]
    public void ButtonStateDefaultsAreNonDestructive()
    {
        var appearance = new ImageGalleryButtonAppearance();

        appearance.Opacity.ShouldBe(1);
        appearance.DisabledOpacity.ShouldBe(0.5);
    }
}
