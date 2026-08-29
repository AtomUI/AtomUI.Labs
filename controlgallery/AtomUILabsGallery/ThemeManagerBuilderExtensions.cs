using AtomUI.Theme;
using AtomUI.Labs.Controls.ImageGallery;
using AtomUI.Labs.Controls.Led;
using AtomUI.Toolkits.GalleryBase;

namespace AtomUILabsGallery;

public static class ThemeManagerBuilderExtensions
{
    public static IThemeManagerBuilder UseLabsGalleryControls(this IThemeManagerBuilder themeManagerBuilder)
    {
        themeManagerBuilder.UseImageGallery();
        themeManagerBuilder.UseLed();
        themeManagerBuilder.UseGalleryBase(AtomUILabsGalleryModule.Configure);
        return themeManagerBuilder;
    }
}
