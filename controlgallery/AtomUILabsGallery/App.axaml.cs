using System.Globalization;
using AtomUI;
using AtomUI.Desktop.Controls;
using AtomUI.Theme;
using AtomUILabsGallery.ShowCases.ImageGallery.Aot;
using AtomUILabsGallery.Workspace.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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
            desktop.MainWindow = desktop.Args?.Contains("--image-gallery-smoke", StringComparer.Ordinal) == true
                ? CreateImageGallerySmokeWindow(desktop)
                : new WorkspaceWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateImageGallerySmokeWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var view = new ImageGalleryAotSmokeView();
        var window = new Window
        {
            Width = 800,
            Height = 520,
            Content = view,
            ShowInTaskbar = false,
            Opacity = 0.01,
        };
        window.Opened += async (_, _) =>
        {
            var startedAt = DateTimeOffset.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var exitCode = 0;
            Exception? failure = null;
            IReadOnlyList<ImageGallerySmokeCheck> checks = [];
            var args = desktop.Args ?? [];
            var resultPath = GetArgumentValue(args, "--result");
            var timeoutText = GetArgumentValue(args, "--timeout-seconds");
            if (timeoutText is not null && (!int.TryParse(timeoutText, out var parsed) || parsed is < 1 or > 600))
            {
                exitCode = 2;
                failure = new ArgumentException("--timeout-seconds must be an integer from 1 through 600.");
            }
            else
            {
                var timeout = TimeSpan.FromSeconds(timeoutText is null ? 90 : int.Parse(timeoutText));
                using var cancellation = new CancellationTokenSource(timeout);
                try
                {
                    checks = await view.RunAsync(window, timeout, cancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    exitCode = 3;
                    failure = new TimeoutException("ImageGallery NativeAOT smoke timed out.");
                }
                catch (TimeoutException exception)
                {
                    exitCode = 3;
                    failure = exception;
                }
                catch (ImageGallerySmokeAssertionException exception)
                {
                    exitCode = 4;
                    failure = exception;
                }
                catch (Exception exception)
                {
                    exitCode = 5;
                    failure = exception;
                }
            }

            stopwatch.Stop();
            if (resultPath is not null)
            {
                WriteSmokeResult(resultPath, startedAt, stopwatch.Elapsed, checks, failure, exitCode);
            }

            window.Close();
            desktop.Shutdown(exitCode);
        };
        return window;
    }

    private static string? GetArgumentValue(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index + 1 < args.Count; index++)
        {
            if (string.Equals(args[index], name, StringComparison.Ordinal))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static void WriteSmokeResult(
        string path,
        DateTimeOffset startedAt,
        TimeSpan duration,
        IReadOnlyList<ImageGallerySmokeCheck> checks,
        Exception? failure,
        int exitCode)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"SchemaVersion\": 1,");
        builder.AppendLine($"  \"RuntimeIdentifier\": \"{RuntimeInformation.RuntimeIdentifier}\",");
        builder.AppendLine($"  \"FrameworkDescription\": \"{Escape(RuntimeInformation.FrameworkDescription)}\",");
        builder.AppendLine($"  \"StartedAt\": \"{startedAt:O}\",");
        builder.AppendLine($"  \"DurationMilliseconds\": {duration.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture)},");
        builder.AppendLine("  \"Checks\": [");
        for (var index = 0; index < checks.Count; index++)
        {
            var check = checks[index];
            builder.Append($"    {{ \"Name\": \"{Escape(check.Name)}\", \"Status\": \"Passed\", \"DurationMilliseconds\": {check.Duration.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} }}");
            builder.AppendLine(index + 1 == checks.Count ? string.Empty : ",");
        }

        builder.AppendLine("  ],");
        builder.AppendLine($"  \"UnhandledException\": {(failure is null ? "null" : $"\"{Escape(failure.ToString())}\"")},");
        builder.AppendLine($"  \"Result\": \"{(exitCode == 0 ? "Passed" : "Failed")}\"");
        builder.AppendLine("}");
        File.WriteAllText(fullPath, builder.ToString());
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);
}
