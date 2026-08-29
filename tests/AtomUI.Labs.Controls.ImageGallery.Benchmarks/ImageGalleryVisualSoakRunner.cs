using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AtomUI;
using AtomUI.Labs.Controls.ImageGallery.Filmstrip;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AtomUI.Labs.Controls.ImageGallery.Benchmarks;

internal static class ImageGalleryVisualSoakRunner
{
    private const int RandomSeed = 20260818;
    private const int ItemCount = 100_000;
    private const int SelectionBatchSize = 16;
    private const long MinimumThirtyMinuteOperations = 100_000;

    public static async Task<ImageGalleryVisualSoakResult> RunAsync(TimeSpan duration)
    {
        return await VisualSoakApplicationHost.RunAsync(() => RunCoreAsync(duration));
    }

    private static async Task<ImageGalleryVisualSoakResult> RunCoreAsync(TimeSpan duration)
    {
        var random = new Random(RandomSeed);
        var probe = new ImageLeaseProbe();
        var items = new ResettableImageCollection(CreateItems(ItemCount, probe, 0));
        var failures = new List<string>();
        var containerIdentities = new HashSet<int>();
        var externalLeases = new Queue<ImageGalleryImageLease>();
        var placements = new[]
        {
            ImageGalleryEdgePlacement.Bottom,
            ImageGalleryEdgePlacement.Left,
            ImageGalleryEdgePlacement.Top,
            ImageGalleryEdgePlacement.Right,
        };
        var placementIndex = 0;
        var nextKey = ItemCount;
        var detachAttachCycles = 0;
        var collectionMutations = 0;
        var placementChanges = 0;
        var resizeChanges = 0;
        var budgetChanges = 0;
        var mainImageModeChanges = 0;
        var decodeHintChanges = 0;
        var externalLeaseAcquisitions = 0;
        var externalLeaseDetachChecks = 0;
        var resourceChangedEvents = 0;
        var maximumHeldExternalLeases = 0;
        var maximumRealized = 0;
        var maximumVisualContainers = 0;
        var maximumAllowedRealized = 0;
        var maximumManagedBytes = 0L;
        var maximumWorkingSetBytes = 0L;
        var maximumPrivateBytes = 0L;
        var operations = 0L;
        var samples = 0L;
        var consecutiveRealizationBreaches = 0;
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                Width = 1200,
                Height = 720,
                ItemsSource = items,
                IsAddImageButtonVisible = false,
                ThumbnailCacheMemoryBudgetBytes = 8L * 1024 * 1024,
                MainImageCacheMemoryBudgetBytes = 32L * 1024 * 1024,
            };
            gallery.CurrentImageResourceChanged += (_, _) => resourceChangedEvents++;
            window = new Window
            {
                Width = 1200,
                Height = 720,
                Content = gallery,
            };
            window.Show();
        });

        await WaitForLayoutAsync();

        // Warm the complete compiled theme, layout, selection, loading and recycling path
        // before memory baselines are captured.
        for (var index = 0; index < 32; index++)
        {
            await Dispatcher.UIThread.InvokeAsync(() => gallery.SelectedIndex = random.Next(items.Count));
            await WaitForLayoutAsync(2);
        }

        ForceFullCollection();
        var process = Process.GetCurrentProcess();
        process.Refresh();
        var initialManagedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var initialWorkingSetBytes = process.WorkingSet64;
        var initialPrivateBytes = process.PrivateMemorySize64;
        maximumManagedBytes = initialManagedBytes;
        maximumWorkingSetBytes = initialWorkingSetBytes;
        maximumPrivateBytes = initialPrivateBytes;

        var stopwatch = Stopwatch.StartNew();
        var batch = 0L;
        try
        {
            while (stopwatch.Elapsed < duration)
            {
                var selectedIndexes = new int[SelectionBatchSize];
                for (var index = 0; index < selectedIndexes.Length; index++)
                {
                    selectedIndexes[index] = random.Next(items.Count);
                }

                var changePlacement = batch > 0 && batch % 127 == 0;
                var resize = batch > 0 && batch % 173 == 0;
                var changeBudget = batch > 0 && batch % 211 == 0;
                var detachAttach = batch > 0 && batch % 257 == 0;
                var mutateCollection = batch > 0 && batch % 389 == 0;
                var changeMainImageMode = batch > 0 && batch % 149 == 0;
                var changeDecodeHint = batch > 0 && batch % 191 == 0;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var selectedIndex in selectedIndexes)
                    {
                        gallery.SelectedIndex = selectedIndex;
                    }

                    if (changePlacement)
                    {
                        placementIndex = (placementIndex + 1) % placements.Length;
                        gallery.ThumbnailFilmstripPlacement = placements[placementIndex];
                        placementChanges++;
                    }

                    if (resize)
                    {
                        var wide = resizeChanges % 2 == 0;
                        window.Width = wide ? 960 : 1280;
                        window.Height = wide ? 640 : 760;
                        gallery.Width = window.Width;
                        gallery.Height = window.Height;
                        resizeChanges++;
                    }

                    if (changeBudget)
                    {
                        var compact = budgetChanges % 2 == 0;
                        gallery.ThumbnailCacheMemoryBudgetBytes = compact
                            ? 4L * 1024 * 1024
                            : 16L * 1024 * 1024;
                        gallery.MainImageCacheMemoryBudgetBytes = compact
                            ? 16L * 1024 * 1024
                            : 64L * 1024 * 1024;
                        budgetChanges++;
                    }

                    if (changeMainImageMode)
                    {
                        gallery.MainImageMode = gallery.MainImageMode == ImageGalleryMainImageMode.Presented
                            ? ImageGalleryMainImageMode.ResourceOnly
                            : ImageGalleryMainImageMode.Presented;
                        mainImageModeChanges++;
                    }

                    if (changeDecodeHint)
                    {
                        gallery.MainImageDecodeSizeHint = decodeHintChanges % 2 == 0
                            ? new PixelSize(2560, 1440)
                            : new PixelSize(768, 512);
                        decodeHintChanges++;
                    }

                    if (mutateCollection)
                    {
                        ApplyCollectionMutation(items, probe, ref nextKey, collectionMutations);
                        collectionMutations++;
                    }

                    if (detachAttach)
                    {
                        window.Content = null;
                        foreach (var externalLease in externalLeases)
                        {
                            _ = externalLease.Image.Size;
                            externalLeaseDetachChecks++;
                        }

                        window.Content = gallery;
                        detachAttachCycles++;
                    }
                });

                operations += SelectionBatchSize;
                if (changePlacement) operations++;
                if (resize) operations++;
                if (changeBudget) operations++;
                if (changeMainImageMode) operations++;
                if (changeDecodeHint) operations++;
                if (detachAttach) operations += 2;
                if (mutateCollection) operations++;

                await WaitForLayoutAsync(12);
                var sample = await CaptureSettledSampleAsync(gallery);
                if (batch % 31 == 0)
                {
                    var externalLease = await TryAcquireSettledCurrentAsync(
                        gallery,
                        TimeSpan.FromMilliseconds(250));
                    if (externalLease is not null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _ = externalLease.Image.Size;
                            while (externalLeases.Count >= 8)
                            {
                                externalLeases.Dequeue().Dispose();
                            }

                            externalLeases.Enqueue(externalLease);
                            externalLeaseAcquisitions++;
                            maximumHeldExternalLeases = Math.Max(
                                maximumHeldExternalLeases,
                                externalLeases.Count);
                        });
                    }

                    operations++;
                }

                samples++;
                maximumRealized = Math.Max(maximumRealized, sample.RealizedContainers);
                maximumVisualContainers = Math.Max(maximumVisualContainers, sample.VisualContainers);
                maximumAllowedRealized = Math.Max(maximumAllowedRealized, sample.AllowedRealizedContainers);
                foreach (var identity in sample.ContainerIdentities)
                {
                    containerIdentities.Add(identity);
                }

                if (sample.RealizedContainers > sample.AllowedRealizedContainers ||
                    sample.VisualContainers > sample.AllowedRealizedContainers)
                {
                    consecutiveRealizationBreaches++;
                    if (consecutiveRealizationBreaches >= 3 && failures.Count < 32)
                    {
                        failures.Add(
                            $"Realization bound exceeded for three samples: realized={sample.RealizedContainers}, " +
                            $"visual={sample.VisualContainers}, allowed={sample.AllowedRealizedContainers}, " +
                            $"placement={sample.Placement}, viewport={sample.MainAxisViewport:0.###}.");
                    }
                }
                else
                {
                    consecutiveRealizationBreaches = 0;
                }

                if (sample.RealizedContainers != sample.VisualContainers && failures.Count < 32)
                {
                    failures.Add(
                        $"Container cross-check failed: realized={sample.RealizedContainers}, " +
                        $"visual={sample.VisualContainers}.");
                }

                if (!sample.SelectedContainerRealized && failures.Count < 32)
                {
                    failures.Add(
                        $"Selected container {sample.SelectedIndex} was not realized after layout settled; " +
                        $"batch={batch}, placement={sample.Placement}, panel={sample.FirstRealizedIndex}..{sample.LastRealizedIndex}, " +
                        $"offset={sample.Offset}, extent={sample.Extent}, viewport={sample.Viewport}, " +
                        $"mutations={collectionMutations}, detachAttach={detachAttachCycles}.");
                }

                if (probe.PostReleaseImageAccessCount != 0 && failures.Count < 32)
                {
                    failures.Add($"Detected {probe.PostReleaseImageAccessCount} image accesses after source release.");
                }

                if (batch % 64 == 0)
                {
                    process.Refresh();
                    maximumManagedBytes = Math.Max(maximumManagedBytes, GC.GetTotalMemory(false));
                    maximumWorkingSetBytes = Math.Max(maximumWorkingSetBytes, process.WorkingSet64);
                    maximumPrivateBytes = Math.Max(maximumPrivateBytes, process.PrivateMemorySize64);
                }

                batch++;
            }
        }
        catch (Exception exception)
        {
            failures.Add($"Unhandled soak exception: {exception}");
        }
        finally
        {
            stopwatch.Stop();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                window.Content = null;
                foreach (var externalLease in externalLeases)
                {
                    _ = externalLease.Image.Size;
                    externalLeaseDetachChecks++;
                    externalLease.Dispose();
                }

                externalLeases.Clear();
                window.Close();
            });
            await WaitForProbeDrainAsync(probe, TimeSpan.FromSeconds(15));
        }

        ForceFullCollection();
        process.Refresh();
        var finalManagedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var finalWorkingSetBytes = process.WorkingSet64;
        var finalPrivateBytes = process.PrivateMemorySize64;
        maximumManagedBytes = Math.Max(maximumManagedBytes, finalManagedBytes);
        maximumWorkingSetBytes = Math.Max(maximumWorkingSetBytes, finalWorkingSetBytes);
        maximumPrivateBytes = Math.Max(maximumPrivateBytes, finalPrivateBytes);

        if (duration >= TimeSpan.FromMinutes(30) && operations < MinimumThirtyMinuteOperations)
        {
            failures.Add(
                $"A 30-minute soak must execute at least {MinimumThirtyMinuteOperations:N0} operations; " +
                $"actual={operations:N0}.");
        }

        if (duration >= TimeSpan.FromMinutes(30) &&
            (mainImageModeChanges == 0 ||
             decodeHintChanges == 0 ||
             externalLeaseAcquisitions == 0 ||
             externalLeaseDetachChecks == 0 ||
             resourceChangedEvents == 0))
        {
            failures.Add(
                "The 30-minute soak did not exercise every resource-only path: " +
                $"modeChanges={mainImageModeChanges}, hintChanges={decodeHintChanges}, " +
                $"externalAcquisitions={externalLeaseAcquisitions}, " +
                $"detachChecks={externalLeaseDetachChecks}, events={resourceChangedEvents}.");
        }

        if (probe.OutstandingLeaseCount != 0)
        {
            failures.Add($"Outstanding source leases after detach: {probe.OutstandingLeaseCount}.");
        }

        if (probe.CompletedLeaseCount != probe.ReleasedLeaseCount)
        {
            failures.Add(
                $"Lease imbalance: completed={probe.CompletedLeaseCount}, released={probe.ReleasedLeaseCount}.");
        }

        if (probe.PostReleaseImageAccessCount != 0)
        {
            failures.Add($"Post-release image accesses: {probe.PostReleaseImageAccessCount}.");
        }

        return new ImageGalleryVisualSoakResult(
            failures.Count == 0,
            RandomSeed,
            stopwatch.Elapsed,
            operations,
            samples,
            ItemCount,
            maximumRealized,
            maximumVisualContainers,
            maximumAllowedRealized,
            containerIdentities.Count,
            placementChanges,
            resizeChanges,
            budgetChanges,
            mainImageModeChanges,
            decodeHintChanges,
            collectionMutations,
            detachAttachCycles,
            externalLeaseAcquisitions,
            externalLeaseDetachChecks,
            maximumHeldExternalLeases,
            resourceChangedEvents,
            probe.StartedLoadCount,
            probe.CompletedLeaseCount,
            probe.CanceledLoadCount,
            probe.ReleasedLeaseCount,
            probe.MaximumOutstandingLeaseCount,
            probe.OutstandingLeaseCount,
            probe.PostReleaseImageAccessCount,
            initialManagedBytes,
            finalManagedBytes,
            finalManagedBytes - initialManagedBytes,
            maximumManagedBytes,
            initialWorkingSetBytes,
            finalWorkingSetBytes,
            maximumWorkingSetBytes,
            initialPrivateBytes,
            finalPrivateBytes,
            maximumPrivateBytes,
            failures);
    }

    private static IEnumerable<IImageGalleryItem> CreateItems(
        int count,
        ImageLeaseProbe probe,
        int firstKey)
    {
        for (var index = 0; index < count; index++)
        {
            yield return CreateItem(firstKey + index, probe);
        }
    }

    private static IImageGalleryItem CreateItem(int key, ImageLeaseProbe probe)
    {
        var source = new ProbeImageSource(key, probe);
        return new ImageGalleryItem
        {
            Key = key,
            Title = $"Soak image {key}",
            MainImageSource = source,
            ThumbnailImageSource = source,
        };
    }

    private static void ApplyCollectionMutation(
        ResettableImageCollection items,
        ImageLeaseProbe probe,
        ref int nextKey,
        int mutation)
    {
        switch (mutation % 5)
        {
            case 0:
                items.Add(CreateItem(nextKey++, probe));
                items.RemoveAt(0);
                break;
            case 1:
                items.Move(items.Count - 1, 0);
                break;
            case 2:
                items[items.Count / 2] = CreateItem(nextKey++, probe);
                break;
            case 3:
                items.RemoveAt(items.Count / 3);
                items.Add(CreateItem(nextKey++, probe));
                break;
            default:
                items.ResetWith(items.ToArray());
                break;
        }
    }

    private static VisualSample CaptureSample(ImageGallery gallery)
    {
        var scroller = gallery.GetVisualDescendants()
            .OfType<ImageGalleryFilmstripScrollViewer>()
            .Single();
        var panel = gallery.GetVisualDescendants()
            .OfType<ImageGalleryVirtualizingPanel>()
            .Single();
        var realized = gallery.GetRealizedContainers().OfType<ImageGalleryThumbnailItem>().ToArray();
        var visual = gallery.GetVisualDescendants().OfType<ImageGalleryThumbnailItem>().Count();
        var mainAxisViewport = gallery.ThumbnailFilmstripPlacement is
            ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom
                ? scroller.Viewport.Width
                : scroller.Viewport.Height;
        var slotStride = gallery.ThumbnailItemExtent + gallery.ThumbnailItemSpacing;
        var allowed = (int)Math.Ceiling(2 * mainAxisViewport / slotStride) + 4;
        return new VisualSample(
            realized.Length,
            visual,
            Math.Max(4, allowed),
            gallery.SelectedIndex,
            gallery.ContainerFromIndex(gallery.SelectedIndex) is ImageGalleryThumbnailItem,
            gallery.ThumbnailFilmstripPlacement,
            mainAxisViewport,
            realized.Select(RuntimeHelpers.GetHashCode).ToArray(),
            panel.FirstRealizedIndex,
            panel.LastRealizedIndex,
            scroller.Offset,
            scroller.Extent,
            scroller.Viewport);
    }

    private static async Task<VisualSample> CaptureSettledSampleAsync(ImageGallery gallery)
    {
        var sample = await Dispatcher.UIThread.InvokeAsync(() => CaptureSample(gallery));
        for (var retry = 0; retry < 10 && !sample.SelectedContainerRealized; retry++)
        {
            await WaitForLayoutAsync(10);
            sample = await Dispatcher.UIThread.InvokeAsync(() => CaptureSample(gallery));
        }

        return sample;
    }

    private static async Task WaitForLayoutAsync(int delayMilliseconds = 10)
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        await Task.Delay(delayMilliseconds);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private static async Task WaitForProbeDrainAsync(ImageLeaseProbe probe, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (probe.OutstandingLeaseCount != 0 && stopwatch.Elapsed < timeout)
        {
            await Task.Delay(10);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        }
    }

    private static async Task<ImageGalleryImageLease?> TryAcquireSettledCurrentAsync(
        ImageGallery gallery,
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var lease = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (gallery.SelectedItem is IImageGalleryItem selectedItem &&
                    gallery.TryAcquireCurrentImage(selectedItem, out var acquired))
                {
                    return acquired;
                }

                return null;
            });
            if (lease is not null)
            {
                return lease;
            }

            await WaitForLayoutAsync(5);
        }

        return null;
    }

    private static void ForceFullCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private sealed record VisualSample(
        int RealizedContainers,
        int VisualContainers,
        int AllowedRealizedContainers,
        int SelectedIndex,
        bool SelectedContainerRealized,
        ImageGalleryEdgePlacement Placement,
        double MainAxisViewport,
        IReadOnlyList<int> ContainerIdentities,
        int FirstRealizedIndex,
        int LastRealizedIndex,
        Vector Offset,
        Size Extent,
        Size Viewport);

    private sealed class ResettableImageCollection(IEnumerable<IImageGalleryItem> items)
        : ObservableCollection<IImageGalleryItem>(items)
    {
        public void ResetWith(IReadOnlyList<IImageGalleryItem> replacement)
        {
            Items.Clear();
            foreach (var item in replacement)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(
                new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                    System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
    }

    private sealed class ProbeImageSource(object identity, ImageLeaseProbe probe) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public async ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            probe.LoadStarted();
            try
            {
                await Task.Delay(request.Purpose == ImageGalleryImagePurpose.MainImage ? 3 : 1, cancellationToken)
                    .ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                var target = request.TargetPixelSize ?? new PixelSize(640, 360);
                var image = new ProbeImage(target.Width, target.Height, probe);
                probe.LeaseCompleted();
                return ImageGalleryImageLease.Create(
                    image,
                    target,
                    target,
                    checked((long)target.Width * target.Height * 4),
                    () =>
                    {
                        image.MarkReleased();
                        probe.LeaseReleased();
                    });
            }
            catch (OperationCanceledException)
            {
                probe.LoadCanceled();
                throw;
            }
        }
    }

    private sealed class ProbeImage(double width, double height, ImageLeaseProbe probe) : IImage
    {
        private int _released;

        public Size Size { get; } = new(width, height);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            if (Volatile.Read(ref _released) != 0)
            {
                probe.PostReleaseImageAccess();
            }
        }

        public void MarkReleased() => Interlocked.Exchange(ref _released, 1);
    }

    private sealed class ImageLeaseProbe
    {
        private long _outstandingLeaseCount;
        private long _maximumOutstandingLeaseCount;

        public long StartedLoadCount;
        public long CompletedLeaseCount;
        public long CanceledLoadCount;
        public long ReleasedLeaseCount;
        public long PostReleaseImageAccessCount;
        public long OutstandingLeaseCount => Interlocked.Read(ref _outstandingLeaseCount);
        public long MaximumOutstandingLeaseCount => Interlocked.Read(ref _maximumOutstandingLeaseCount);

        public void LoadStarted() => Interlocked.Increment(ref StartedLoadCount);
        public void LoadCanceled() => Interlocked.Increment(ref CanceledLoadCount);

        public void LeaseCompleted()
        {
            Interlocked.Increment(ref CompletedLeaseCount);
            var current = Interlocked.Increment(ref _outstandingLeaseCount);
            while (true)
            {
                var maximum = Interlocked.Read(ref _maximumOutstandingLeaseCount);
                if (current <= maximum ||
                    Interlocked.CompareExchange(ref _maximumOutstandingLeaseCount, current, maximum) == maximum)
                {
                    break;
                }
            }
        }

        public void LeaseReleased()
        {
            Interlocked.Increment(ref ReleasedLeaseCount);
            Interlocked.Decrement(ref _outstandingLeaseCount);
        }

        public void PostReleaseImageAccess() => Interlocked.Increment(ref PostReleaseImageAccessCount);
    }
}

internal sealed record ImageGalleryVisualSoakResult(
    bool Passed,
    int RandomSeed,
    TimeSpan Duration,
    long Operations,
    long Samples,
    int ItemCount,
    int MaximumRealizedContainers,
    int MaximumVisualContainers,
    int MaximumAllowedRealizedContainers,
    int UniqueObservedContainerIdentities,
    int PlacementChanges,
    int ResizeChanges,
    int BudgetChanges,
    int MainImageModeChanges,
    int DecodeHintChanges,
    int CollectionMutations,
    int DetachAttachCycles,
    int ExternalLeaseAcquisitions,
    int ExternalLeaseDetachChecks,
    int MaximumHeldExternalLeases,
    int ResourceChangedEvents,
    long StartedLoads,
    long CompletedLeases,
    long CanceledLoads,
    long ReleasedLeases,
    long MaximumOutstandingLeases,
    long OutstandingLeasesAfterDetach,
    long PostReleaseImageAccesses,
    long InitialManagedBytes,
    long FinalManagedBytes,
    long ManagedByteDelta,
    long MaximumManagedBytes,
    long InitialWorkingSetBytes,
    long FinalWorkingSetBytes,
    long MaximumWorkingSetBytes,
    long InitialPrivateBytes,
    long FinalPrivateBytes,
    long MaximumPrivateBytes,
    IReadOnlyList<string> Failures);

internal static class VisualSoakApplicationHost
{
    public static Task<T> RunAsync<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        AppBuilder.Configure<VisualSoakApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
            .SetupWithoutStarting();

        using var dispatcherCancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                completion.TrySetResult(await operation());
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
            finally
            {
                dispatcherCancellation.Cancel();
            }
        });

        Dispatcher.UIThread.MainLoop(dispatcherCancellation.Token);
        return completion.Task;
    }
}

internal sealed class VisualSoakApplication : Application
{
    public override void Initialize()
    {
        this.UseAtomUI(builder => builder.UseImageGallery());
    }
}
