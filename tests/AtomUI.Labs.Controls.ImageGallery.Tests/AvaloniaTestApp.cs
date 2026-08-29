using System.Threading;
using AtomUI;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(AtomUI.Labs.Controls.ImageGallery.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AtomUI.Labs.Controls.ImageGallery.Tests;

internal static class AvaloniaTestApp
{
    private static readonly object s_lock = new();
    private static readonly CancellationTokenSource s_dispatcherCancellation = new();
    private static readonly ManualResetEventSlim s_ready = new();
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

            var thread = new Thread(() =>
            {
                TestAppBuilder.BuildAvaloniaApp().SetupWithoutStarting();
                Volatile.Write(ref _initialized, 1);
                s_ready.Set();
                Dispatcher.UIThread.MainLoop(s_dispatcherCancellation.Token);
            })
            {
                IsBackground = true,
                Name = "ImageGallery Tests UI Thread"
            };
            if (OperatingSystem.IsWindows())
            {
                thread.SetApartmentState(ApartmentState.STA);
            }
            thread.Start();
        }

        s_ready.Wait();
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
        this.UseAtomUI(builder => builder.UseImageGallery());
    }
}
