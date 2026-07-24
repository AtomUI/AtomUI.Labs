using AtomUI.Theme;
using Avalonia.Markup.Xaml;

namespace AtomUI.Labs.Controls.Led;

internal class LedThemesProvider : ControlThemesProvider
{
    public LedThemesProvider()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
