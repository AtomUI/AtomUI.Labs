using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using AtomUI.Labs.Controls.ImageGallery.Viewport;
using Avalonia;

namespace AtomUI.Labs.Controls.ImageGallery.Benchmarks;

internal static class Program
{
    private const int WarmupRounds = 3;
    private const int MeasurementRounds = 7;

    private static async Task<int> Main(string[] args)
    {
        var soakMinutes = ReadOptionalDouble(args, "--soak-minutes");
        var soak = soakMinutes is > 0
            ? await ImageGalleryVisualSoakRunner.RunAsync(TimeSpan.FromMinutes(soakMinutes.Value))
            : null;
        var source = new NeverLoadedSource();
        var tenThousand = CreateItems(10_000, source);
        var hundredThousand = CreateItems(100_000, source);
        var results = new[]
        {
            Measure("DescriptorSnapshot_10K", () => AssignItems(tenThousand)),
            Measure("DescriptorSnapshot_100K_Nightly", () => AssignItems(hundredThousand)),
            Measure("ViewportMath_1M", ExerciseViewportMath),
        };
        var report = new BenchmarkReport(
            DateTimeOffset.UtcNow,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.ProcessorCount,
            WarmupRounds,
            MeasurementRounds,
            results,
            soak);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);

        var outputIndex = Array.IndexOf(args, "--output");
        if (outputIndex >= 0 && outputIndex + 1 < args.Length)
        {
            var path = Path.GetFullPath(args[outputIndex + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
        }

        var descriptor = results.Single(result => result.Name == "DescriptorSnapshot_100K_Nightly");
        var math = results.Single(result => result.Name == "ViewportMath_1M");
        return descriptor.MedianMilliseconds > 30_000 ||
               descriptor.MedianAllocatedBytes > 512L * 1024 * 1024 ||
               math.MedianMilliseconds > 30_000 ||
               soak is { Passed: false }
            ? 2
            : 0;
    }

    private static BenchmarkResult Measure(string name, Action action)
    {
        for (var index = 0; index < WarmupRounds; index++)
        {
            action();
        }

        var elapsed = new double[MeasurementRounds];
        var allocated = new long[MeasurementRounds];
        for (var index = 0; index < MeasurementRounds; index++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var before = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            elapsed[index] = stopwatch.Elapsed.TotalMilliseconds;
            allocated[index] = GC.GetAllocatedBytesForCurrentThread() - before;
        }

        Array.Sort(elapsed);
        Array.Sort(allocated);
        return new BenchmarkResult(
            name,
            elapsed[MeasurementRounds / 2],
            allocated[MeasurementRounds / 2],
            elapsed,
            allocated);
    }

    private static IImageGalleryItem[] CreateItems(int count, IImageGallerySource source) =>
        Enumerable.Range(0, count)
            .Select(index => (IImageGalleryItem)new ImageGalleryItem
            {
                Key = index,
                Title = $"Image {index}",
                MainImageSource = source,
            })
            .ToArray();

    private static void AssignItems(IImageGalleryItem[] items)
    {
        var gallery = new ImageGallery { ItemsSource = items };
        if (gallery.SelectedIndex != 0)
        {
            throw new InvalidOperationException("Descriptor benchmark produced an invalid initial selection.");
        }
    }

    private static void ExerciseViewportMath()
    {
        var viewport = new Size(1280, 720);
        var image = new Size(4032, 3024);
        var pan = default(Vector);
        var checksum = 0d;
        for (var index = 0; index < 1_000_000; index++)
        {
            var rotation = index % 4 * 90;
            var fit = ImageGalleryViewportMath.CalculateFitZoom(viewport, image, rotation, false);
            pan = ImageGalleryViewportMath.ClampPan(pan, viewport, image, rotation, fit * 1.5);
            checksum += fit + pan.X + pan.Y;
        }

        GC.KeepAlive(checksum);
    }

    private static double? ReadOptionalDouble(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index + 1 < args.Count; index++)
        {
            if (!string.Equals(args[index], name, StringComparison.Ordinal))
            {
                continue;
            }

            if (double.TryParse(
                    args[index + 1],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var value) && value is > 0 and <= 1_440)
            {
                return value;
            }

            throw new ArgumentException("--soak-minutes must be a positive number no greater than 1440.");
        }

        return null;
    }

    private sealed class NeverLoadedSource : IImageGallerySource
    {
        public object Identity { get; } = "benchmark-source";

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The descriptor benchmark must not attach or load images.");
    }

    private sealed record BenchmarkReport(
        DateTimeOffset Timestamp,
        string Framework,
        string OperatingSystem,
        string Architecture,
        int ProcessorCount,
        int WarmupRounds,
        int MeasurementRounds,
        IReadOnlyList<BenchmarkResult> Results,
        ImageGalleryVisualSoakResult? Soak);

    private sealed record BenchmarkResult(
        string Name,
        double MedianMilliseconds,
        long MedianAllocatedBytes,
        IReadOnlyList<double> ElapsedMilliseconds,
        IReadOnlyList<long> AllocatedBytes);

}
