using System.Globalization;
using AtomUI;
using AtomUI.Desktop.Controls;
using AtomUI.Theme;
using AtomUILabsGallery.Workspace.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AtomUILabsGallery;

public partial class App : Application
{
    public override void Initialize()
    {
        this.UseAtomUI(builder =>
        {
            builder.WithDefaultCultureInfo(CultureInfo.CurrentUICulture);
            builder.WithDefaultTheme(IThemeManager.DEFAULT_THEME_ID);
            builder.UseAlibabaSansFont();
            builder.UseDesktopControls();
            builder.UseLabsGalleryControls();
        });

        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new WorkspaceWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
