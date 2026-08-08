using Avalonia;

namespace AtomUI.Labs.Controls.Led.GlowPrototype.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<GlowPrototypeApplication>()
                         .UsePlatformDetect()
                         .LogToTrace();
    }
}
