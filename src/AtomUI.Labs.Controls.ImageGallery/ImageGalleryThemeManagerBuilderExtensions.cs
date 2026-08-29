using AtomUI.Theme;
using AtomUI.Theme.Language;

namespace AtomUI.Labs.Controls.ImageGallery;

public static class ImageGalleryThemeManagerBuilderExtensions
{
    public static IThemeManagerBuilder UseImageGallery(this IThemeManagerBuilder themeManagerBuilder)
    {
        ArgumentNullException.ThrowIfNull(themeManagerBuilder);
        if (!themeManagerBuilder.ControlThemesProviders.Any(provider => provider is ImageGalleryThemesProvider))
        {
            themeManagerBuilder.AddControlThemesProvider(new ImageGalleryThemesProvider());
        }

        foreach (var provider in LanguageProviderPool.GetLanguageProviders())
        {
            if (!themeManagerBuilder.LanguageProviders.Any(existing =>
                    existing.LangCode == provider.LangCode && existing.LangId == provider.LangId))
            {
                themeManagerBuilder.AddLanguageProviders(provider);
            }
        }

        return themeManagerBuilder;
    }
}
