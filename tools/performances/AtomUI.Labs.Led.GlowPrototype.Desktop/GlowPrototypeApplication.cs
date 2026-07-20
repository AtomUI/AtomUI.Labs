using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace AtomUI.Labs.Led.GlowPrototype.Desktop;

internal sealed class GlowPrototypeApplication : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            desktop.MainWindow = FormalGlowDesktopOptions.TryParse(args, out var formalOptions)
                ? new FormalGlowDesktopWindow(formalOptions)
                : GlowLifecycleOptions.TryParse(args, out var lifecycleOptions)
                ? new GlowLifecycleCoordinatorWindow(lifecycleOptions)
                : GlowDesktopBenchmarkOptions.TryParse(args, out var options)
                    ? new GlowDesktopBenchmarkWindow(options)
                    : new GlowPrototypeWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
