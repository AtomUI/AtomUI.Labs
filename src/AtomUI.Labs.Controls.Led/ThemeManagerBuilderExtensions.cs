using AtomUI.Theme;

namespace AtomUI.Labs.Controls.Led;

public static class LedThemeManagerBuilderExtensions
{
    public static IThemeManagerBuilder UseLed(this IThemeManagerBuilder themeManagerBuilder)
    {
        themeManagerBuilder.AddControlThemesProvider(new LedThemesProvider());
        return themeManagerBuilder;
    }
}
