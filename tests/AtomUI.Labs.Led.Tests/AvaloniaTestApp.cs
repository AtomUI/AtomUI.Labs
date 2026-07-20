using System.Threading;
using AtomUI;
using AtomUI.Labs.Led;
using Avalonia;
using Avalonia.Headless;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(AtomUI.Labs.Led.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AtomUI.Labs.Led.Tests;

internal static class AvaloniaTestApp
{
    private static readonly object s_lock = new();
    private static int _initialized;

    public static void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized) == 1)
        {
            return;
        }

        lock (s_lock)
        {
            if (_initialized == 1)
            {
                return;
            }

            TestAppBuilder.BuildAvaloniaApp().SetupWithoutStarting();
            Volatile.Write(ref _initialized, 1);
        }
    }
}

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<TestApplication>()
                         .UseHeadless(new AvaloniaHeadlessPlatformOptions
                         {
                             UseHeadlessDrawing = false
                         })
                         .UseSkia();
    }
}

internal sealed class TestApplication : Application
{
    public override void Initialize()
    {
        this.UseAtomUI(builder => builder.UseLed());
    }
}
