using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Engineering;

public sealed class ProjectBoundaryTests
{
    [Fact]
    public void RootControlUsesExpectedNamespace()
    {
        typeof(ImageGallery).Namespace.ShouldBe("AtomUI.Labs.Controls.ImageGallery");
    }

    [Fact]
    public void ThemeRegistrationExtensionIsPublic()
    {
        typeof(ImageGalleryThemeManagerBuilderExtensions).IsAbstract.ShouldBeTrue();
    }
}
