using AtomUI.Theme;
using Avalonia.Markup.Xaml;

namespace AtomUI.Labs.Controls.ImageGallery;

internal sealed class ImageGalleryThemesProvider : ControlThemesProvider
{
    public ImageGalleryThemesProvider()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
