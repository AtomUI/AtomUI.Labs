using AtomUI;
using Avalonia;
using ReactiveUI.Avalonia;

namespace AtomUILabsGallery.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<AtomUILabsGallery.App>()
                         .UseReactiveUI(build =>
                             build.ConfigureViewLocator(locator => AtomUILabsGalleryModule.RegisterViews(locator)))
                         .UsePlatformDetect()
                         .WithAtomUIDefaultOptions()
                         .LogToTrace();
    }
}
