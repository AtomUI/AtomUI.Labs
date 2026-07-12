using AtomUI.Theme;
using AtomUI.Toolkits.GalleryBase;

namespace AtomUILabsGallery;

public static class ThemeManagerBuilderExtensions
{
    public static IThemeManagerBuilder UseLabsGalleryControls(this IThemeManagerBuilder themeManagerBuilder)
    {
        themeManagerBuilder.UseGalleryBase(AtomUILabsGalleryModule.Configure);
        return themeManagerBuilder;
    }
}
