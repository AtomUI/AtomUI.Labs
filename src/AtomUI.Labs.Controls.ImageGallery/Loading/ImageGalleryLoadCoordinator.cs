using AtomUI.Labs.Controls.ImageGallery.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace AtomUI.Labs.Controls.ImageGallery.Loading;

internal sealed class ImageGalleryLoadCoordinator : IDisposable
{
    private readonly ImageGallery _owner;
    private readonly Dictionary<ImageGalleryCacheKey, CacheEntry> _mainCache = [];
    private readonly Dictionary<ImageGalleryCacheKey, CacheEntry> _thumbnailCache = [];
    private readonly Dictionary<ImageGalleryCacheKey, InflightEntry> _inflight = [];
    private readonly Dictionary<ImageGalleryCacheKey, DateTimeOffset> _thumbnailFailureSuppressions = [];
    private readonly LinkedList<ImageGalleryCacheKey> _lru = [];
    private readonly LinkedList<ImageGalleryCacheKey> _thumbnailLru = [];
    private CancellationTokenSource? _lifetime;
    private CancellationTokenSource? _selection;
    private CancellationTokenSource? _upgrade;
    private ImageGalleryLoadScheduler? _scheduler;
    private ImageGalleryDescriptor? _currentDescriptor;
    private long _cacheSize;
    private long _thumbnailCacheSize;
    private long _generation;
    private long _attachmentGeneration;
    private bool _attached;
    private bool _disposed;
    private bool _initialLoadStarted;
    private bool _isAttaching;
    private bool _preserveViewportOnNextCommit;
    private PixelSize? _lastMainTarget;

    public ImageGalleryLoadCoordinator(ImageGallery owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public void Attach()
    {
        VerifyUiThread();
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_attached)
        {
            return;
        }

        _attached = true;
        _attachmentGeneration++;
        _lifetime = new CancellationTokenSource();
        _scheduler = new ImageGalleryLoadScheduler();
        _isAttaching = true;
        try
        {
            SelectionChanged(_currentDescriptor);
        }
        finally
        {
            _isAttaching = false;
        }
    }

    public void Detach()
    {
        VerifyUiThread();
        if (!_attached)
        {
            return;
        }

        _attached = false;
        _attachmentGeneration++;
        _generation++;
        CancelAndDispose(ref _selection);
        CancelAndDispose(ref _upgrade);
        CancelAndDispose(ref _lifetime);
        _scheduler?.Dispose();
        _scheduler = null;
        foreach (var entry in _inflight.Values)
        {
            entry.IsAbandoned = true;
        }
        _inflight.Clear();
        _thumbnailFailureSuppressions.Clear();
        _lastMainTarget = null;
        _initialLoadStarted = false;
        _preserveViewportOnNextCommit = true;
        ClearCache();
        _owner.ClearCurrentMainImage(resetViewport: false, notify: true);
    }

    public void SelectionChanged(ImageGalleryDescriptor? descriptor)
    {
        VerifyUiThread();
        var previousDescriptor = _currentDescriptor;
        var logicallySameSelection = previousDescriptor is not null && descriptor is not null &&
                                     Equals(previousDescriptor.Key, descriptor.Key) &&
                                     Equals(previousDescriptor.MainSourceIdentity, descriptor.MainSourceIdentity);
        var resourceWasReady = _owner.ImageState == ImageGalleryImageState.Ready;
        _currentDescriptor = descriptor;
        _generation++;
        CancelAndDispose(ref _selection);
        CancelAndDispose(ref _upgrade);
        _lastMainTarget = null;
        _initialLoadStarted = false;
        if (!_isAttaching && (!logicallySameSelection || _attached))
        {
            _preserveViewportOnNextCommit = false;
        }
        _owner.CancelViewportInteractions();

        if (descriptor is null)
        {
            if (_isAttaching)
            {
                _owner.SetImageState(ImageGalleryImageState.Empty);
            }
            else
            {
                _owner.ClearCurrentMainImage(resetViewport: true, notify: true);
            }
            return;
        }

        _owner.SetImageState(ImageGalleryImageState.Loading);
        if (!_isAttaching && (!logicallySameSelection || resourceWasReady))
        {
            _owner.NotifyCurrentImageResourceUnavailable();
        }
        if (!_attached || _lifetime is null || _scheduler is null)
        {
            return;
        }

        _selection = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        TryStartInitialLoad();
    }

    public void ReconcileBudget()
    {
        VerifyUiThread();
        TrimCache(ImageGalleryImagePurpose.MainImage);
        TrimCache(ImageGalleryImagePurpose.Thumbnail);
    }

    internal bool IsCurrent(CurrentImageResourceSlot slot)
    {
        VerifyUiThread();
        ArgumentNullException.ThrowIfNull(slot);
        return _attached &&
               slot.Generation == _generation &&
               ReferenceEquals(slot.Descriptor, _currentDescriptor);
    }

    public async ValueTask<ImageGalleryImageLease?> LoadThumbnailAsync(
        ImageGalleryDescriptor descriptor,
        PixelSize targetPixelSize,
        CancellationToken cancellationToken)
    {
        if (!_attached)
        {
            return null;
        }

        var bucket = new PixelSize(
            RoundUpToBucket(targetPixelSize.Width),
            RoundUpToBucket(targetPixelSize.Height));
        var source = descriptor.EffectiveThumbnailSource;
        var key = new ImageGalleryCacheKey(
            source.Identity,
            ImageGalleryImagePurpose.Thumbnail,
            bucket);
        var suppressed = false;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_thumbnailFailureSuppressions.TryGetValue(key, out var until))
            {
                if (until > DateTimeOffset.UtcNow)
                {
                    suppressed = true;
                }
                else
                {
                    _thumbnailFailureSuppressions.Remove(key);
                }
            }
        });
        if (suppressed)
        {
            throw new InvalidOperationException(
                $"Thumbnail load for '{source.Identity}' is temporarily suppressed after a recent failure.");
        }

        try
        {
            var acquisition = await GetSharedLeaseAsync(
                key,
                source,
                ImageGalleryLoadPriority.VisibleThumbnail,
                cancellationToken).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() => _thumbnailFailureSuppressions.Remove(key));
            return acquisition.Lease;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
                _thumbnailFailureSuppressions[key] = DateTimeOffset.UtcNow.AddSeconds(5));
            throw;
        }
    }

    public void ViewportRequirementsChanged()
    {
        VerifyUiThread();
        var target = GetMainTargetPixelSize();
        if (!_attached || _currentDescriptor is null || _selection is null)
        {
            return;
        }

        if (_owner.ImageState == ImageGalleryImageState.Loading)
        {
            TryStartInitialLoad();
            return;
        }

        if (_owner.ImageState != ImageGalleryImageState.Ready || target is null)
        {
            return;
        }

        var upgradeTarget = target.Value;
        if (!RequiresQualityUpgrade(_lastMainTarget, upgradeTarget))
        {
            return;
        }

        CancelAndDispose(ref _upgrade);
        _upgrade = CancellationTokenSource.CreateLinkedTokenSource(_selection.Token);
        _ = UpgradeCurrentAfterDebounceAsync(_currentDescriptor, _generation, upgradeTarget, _upgrade.Token);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Detach();
    }

    private void TryStartInitialLoad()
    {
        if (_initialLoadStarted || !_attached || _currentDescriptor is null ||
            _selection is null || _owner.ImageState != ImageGalleryImageState.Loading)
        {
            return;
        }

        var target = GetMainTargetPixelSize();
        if (target is null)
        {
            return;
        }

        _initialLoadStarted = true;
        _ = LoadCurrentAsync(_currentDescriptor, _generation, target.Value, _selection.Token);
    }

    private async Task LoadCurrentAsync(
        ImageGalleryDescriptor descriptor,
        long generation,
        PixelSize target,
        CancellationToken cancellationToken)
    {
        ImageGalleryImageLease? lease = null;
        SharedImageResource? resource = null;
        try
        {
            var key = new ImageGalleryCacheKey(
                descriptor.MainSourceIdentity,
                ImageGalleryImagePurpose.MainImage,
                target);
            var acquisition = await GetSharedLeaseAsync(
                key,
                descriptor.MainImageSource,
                ImageGalleryLoadPriority.CurrentMainImage,
                cancellationToken).ConfigureAwait(false);
            resource = acquisition.Resource;
            lease = acquisition.Lease;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_attached || generation != _generation ||
                    !ReferenceEquals(descriptor, _currentDescriptor))
                {
                    lease.Dispose();
                    lease = null;
                    return;
                }

                var candidate = new CurrentImageResourceSlot(
                    descriptor,
                    generation,
                    target,
                    resource,
                    lease);
                lease = null;
                var resetViewport = !_preserveViewportOnNextCommit;
                _preserveViewportOnNextCommit = false;
                _owner.CommitLoadedMainImage(candidate, resetViewport);
                _lastMainTarget = target;
                StartAdjacentPrefetch(descriptor);
                ViewportRequirementsChanged();
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_attached && generation == _generation &&
                    ReferenceEquals(descriptor, _currentDescriptor))
                {
                    _owner.CommitMainImageFailure(exception);
                }
            });
        }
        finally
        {
            lease?.Dispose();
        }
    }

    private async Task<SharedImageAcquisition> GetSharedLeaseAsync(
        ImageGalleryCacheKey key,
        IImageGallerySource source,
        ImageGalleryLoadPriority priority,
        CancellationToken callerCancellationToken)
    {
        InflightEntry? inflight = null;
        ImageGalleryImageLease? cachedLease = null;
        SharedImageResource? cachedResource = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var cache = GetCache(key.Purpose);
            if (cache.TryGetValue(key, out var cached))
            {
                Touch(key, cached);
                cachedLease = cached.Resource.TryAcquire();
                if (cachedLease is not null)
                {
                    cachedResource = cached.Resource;
                    return;
                }

                RemoveCacheEntry(key, cached);
            }

            if (!_inflight.TryGetValue(key, out inflight))
            {
                var scheduler = _scheduler ?? throw new OperationCanceledException();
                var lifetime = _lifetime?.Token ?? throw new OperationCanceledException();
                var request = new ImageGalleryImageRequest(
                    key.Purpose,
                    key.TargetPixelSize,
                    _owner.LoadLimits);
                var operation = scheduler.ScheduleAsync(
                    priority,
                    async token =>
                    {
                        var loaded = await source.LoadAsync(request, token).ConfigureAwait(false);
                        try
                        {
                            ImageGallerySourceValidation.ValidateLease(loaded, request.Limits);
                            return new SharedImageResource(loaded);
                        }
                        catch
                        {
                            loaded.Dispose();
                            throw;
                        }
                    },
                    lifetime);
                inflight = new InflightEntry(operation, _attachmentGeneration);
                _inflight.Add(key, inflight);
                _ = CompleteOperationAsync(key, inflight);
            }

            inflight.WaiterCount++;
        });

        if (cachedLease is not null)
        {
            return new SharedImageAcquisition(cachedResource!, cachedLease);
        }

        try
        {
            var resource = await inflight!.Operation
                .WaitAsync(callerCancellationToken)
                .ConfigureAwait(false);
            var lease = resource.TryAcquire() ??
                        throw new InvalidOperationException("The shared image resource was released before acquisition.");
            return new SharedImageAcquisition(resource, lease);
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                inflight!.WaiterCount--;
                TryFinalizeInflight(key, inflight);
            });
        }
    }

    private async Task CompleteOperationAsync(
        ImageGalleryCacheKey key,
        InflightEntry entry)
    {
        SharedImageResource? resource = null;
        try
        {
            resource = await entry.Operation.ConfigureAwait(false);
        }
        catch
        {
            // Every interested consumer observes the original operation. This
            // continuation only owns bookkeeping and therefore does not report.
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            entry.Resource = resource;
            entry.IsCompleted = true;
            TryFinalizeInflight(key, entry);
        });
    }

    private void TryFinalizeInflight(ImageGalleryCacheKey key, InflightEntry entry)
    {
        if (!entry.IsCompleted || entry.WaiterCount != 0 || entry.IsFinalized)
        {
            return;
        }

        entry.IsFinalized = true;
        if (_inflight.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
        {
            _inflight.Remove(key);
        }

        if (entry.Resource is not { } resource)
        {
            return;
        }

        if (entry.IsAbandoned || !_attached || entry.AttachmentGeneration != _attachmentGeneration)
        {
            resource.Dispose();
            return;
        }

        AddToCache(key, resource);
    }

    private void StartAdjacentPrefetch(ImageGalleryDescriptor current)
    {
        if (_owner.MainImagePrefetchMode != ImageGalleryMainImagePrefetchMode.Adjacent)
        {
            return;
        }

        var descriptors = _owner.Descriptors;
        var index = -1;
        for (var descriptorIndex = 0; descriptorIndex < descriptors.Count; descriptorIndex++)
        {
            if (ReferenceEquals(descriptors[descriptorIndex], current))
            {
                index = descriptorIndex;
                break;
            }
        }
        if (index < 0)
        {
            return;
        }

        var next = index + 1 < descriptors.Count ? descriptors[index + 1] : null;
        var previous = index > 0 ? descriptors[index - 1] : null;
        var token = _selection?.Token ?? new CancellationToken(canceled: true);
        var target = GetBaselineMainTargetPixelSize();
        if (target is not null)
        {
            _ = PrefetchAdjacentAfterDelayAsync(next, previous, target.Value, token);
        }
    }

    private async Task PrefetchAdjacentAfterDelayAsync(
        ImageGalleryDescriptor? next,
        ImageGalleryDescriptor? previous,
        PixelSize target,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
            if (next is not null)
            {
                await PrefetchAsync(next, target, cancellationToken).ConfigureAwait(false);
            }

            if (previous is not null)
            {
                await PrefetchAsync(previous, target, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task PrefetchAsync(
        ImageGalleryDescriptor descriptor,
        PixelSize target,
        CancellationToken cancellationToken)
    {
        ImageGalleryImageLease? lease = null;
        try
        {
            var key = new ImageGalleryCacheKey(
                descriptor.MainSourceIdentity,
                ImageGalleryImagePurpose.MainImage,
                target);
            var acquisition = await GetSharedLeaseAsync(
                key,
                descriptor.MainImageSource,
                ImageGalleryLoadPriority.AdjacentMainImage,
                cancellationToken).ConfigureAwait(false);
            lease = acquisition.Lease;
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // Speculative work must never replace the current image state.
        }
        finally
        {
            lease?.Dispose();
        }
    }

    private PixelSize? GetMainTargetPixelSize() =>
        BuildMainTarget(
            includeExplicitHint: true,
            includePresentedTransform: _owner.MainImageMode == ImageGalleryMainImageMode.Presented);

    private PixelSize? GetBaselineMainTargetPixelSize() =>
        BuildMainTarget(includeExplicitHint: false, includePresentedTransform: false);

    private PixelSize? BuildMainTarget(
        bool includeExplicitHint,
        bool includePresentedTransform)
    {
        var scaling = TopLevel.GetTopLevel(_owner)?.RenderScaling ?? 1;
        var viewport = _owner.CurrentViewportSize;
        var width = double.IsFinite(viewport.Width) && viewport.Width > 0
            ? viewport.Width * scaling
            : 0;
        var height = double.IsFinite(viewport.Height) && viewport.Height > 0
            ? viewport.Height * scaling
            : 0;

        if (includePresentedTransform)
        {
            var factor = Math.Max(1, _owner.EffectiveZoomFactor);
            width *= factor;
            height *= factor;
            if (_owner.RotationAngle is 90 or 270)
            {
                (width, height) = (Math.Max(width, height), Math.Max(width, height));
            }
        }

        if (includeExplicitHint && _owner.MainImageDecodeSizeHint is { } hint)
        {
            width = Math.Max(width, hint.Width);
            height = Math.Max(height, hint.Height);
        }

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var limits = _owner.LoadLimits;
        var maximumDimension = limits.MaximumDimension;
        var requestedWidth = Math.Min(width, maximumDimension);
        var requestedHeight = Math.Min(height, maximumDimension);
        var maximumPixels = Math.Min(
            limits.MaximumSourcePixelCount,
            Math.Max(1, limits.MaximumDecodedBytes / 4));
        var requestedPixels = requestedWidth * requestedHeight;
        if (requestedPixels > maximumPixels)
        {
            var scale = Math.Sqrt(maximumPixels / requestedPixels);
            requestedWidth *= scale;
            requestedHeight *= scale;
        }

        var pixelWidth = Math.Clamp((int)Math.Ceiling(requestedWidth), 1, maximumDimension);
        var pixelHeight = Math.Clamp((int)Math.Ceiling(requestedHeight), 1, maximumDimension);
        pixelWidth = Math.Min(maximumDimension, RoundUpToBucket(pixelWidth, 128));
        pixelHeight = Math.Min(maximumDimension, RoundUpToBucket(pixelHeight, 128));
        return new PixelSize(pixelWidth, pixelHeight);
    }

    private static bool RequiresQualityUpgrade(PixelSize? current, PixelSize requested)
    {
        if (current is null)
        {
            return true;
        }

        const double threshold = 1.25;
        return requested.Width >= Math.Ceiling(current.Value.Width * threshold) ||
               requested.Height >= Math.Ceiling(current.Value.Height * threshold);
    }

    private async Task UpgradeCurrentAfterDebounceAsync(
        ImageGalleryDescriptor descriptor,
        long generation,
        PixelSize target,
        CancellationToken cancellationToken)
    {
        ImageGalleryImageLease? lease = null;
        SharedImageResource? resource = null;
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken).ConfigureAwait(false);
            if (!RequiresQualityUpgrade(_lastMainTarget, target))
            {
                return;
            }

            var key = new ImageGalleryCacheKey(
                descriptor.MainSourceIdentity,
                ImageGalleryImagePurpose.MainImage,
                target);
            var acquisition = await GetSharedLeaseAsync(
                key,
                descriptor.MainImageSource,
                ImageGalleryLoadPriority.CurrentMainImage,
                cancellationToken).ConfigureAwait(false);
            resource = acquisition.Resource;
            lease = acquisition.Lease;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!_attached || generation != _generation ||
                    !ReferenceEquals(descriptor, _currentDescriptor) ||
                    target != GetMainTargetPixelSize())
                {
                    lease.Dispose();
                    lease = null;
                    return;
                }

                var candidate = new CurrentImageResourceSlot(
                    descriptor,
                    generation,
                    target,
                    resource,
                    lease);
                lease = null;
                _owner.CommitUpgradedMainImage(candidate);
                _lastMainTarget = target;
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch
        {
            // A clarity upgrade is opportunistic; keep the already ready image.
        }
        finally
        {
            lease?.Dispose();
        }
    }

    private void AddToCache(ImageGalleryCacheKey key, SharedImageResource resource)
    {
        var cache = GetCache(key.Purpose);
        var lru = GetLru(key.Purpose);
        var budget = GetBudget(key.Purpose);
        if (cache.TryGetValue(key, out var existing))
        {
            resource.Dispose();
            Touch(key, existing);
            return;
        }

        var size = Math.Max(0, resource.EstimatedMemorySizeBytes);
        if (budget == 0 || size > budget)
        {
            resource.Dispose();
            return;
        }

        var node = lru.AddFirst(key);
        cache.Add(key, new CacheEntry(resource, node, size));
        AddCacheSize(key.Purpose, size);
        TrimCache(key.Purpose);
    }

    private void TrimCache(ImageGalleryImagePurpose purpose)
    {
        var cache = GetCache(purpose);
        var lru = GetLru(purpose);
        var budget = GetBudget(purpose);
        while (GetCacheSize(purpose) > budget && lru.Last is { } node)
        {
            var key = node.Value;
            if (cache.TryGetValue(key, out var entry))
            {
                RemoveCacheEntry(key, entry);
            }
            else
            {
                lru.Remove(node);
            }
        }
    }

    private void Touch(ImageGalleryCacheKey key, CacheEntry entry)
    {
        var lru = GetLru(key.Purpose);
        lru.Remove(entry.Node);
        lru.AddFirst(entry.Node);
    }

    private void RemoveCacheEntry(ImageGalleryCacheKey key, CacheEntry entry)
    {
        GetCache(key.Purpose).Remove(key);
        GetLru(key.Purpose).Remove(entry.Node);
        AddCacheSize(key.Purpose, -entry.Size);
        entry.Resource.Dispose();
    }

    private void ClearCache()
    {
        foreach (var entry in _mainCache.Values)
        {
            entry.Resource.Dispose();
        }

        _mainCache.Clear();
        _lru.Clear();
        _cacheSize = 0;
        foreach (var entry in _thumbnailCache.Values)
        {
            entry.Resource.Dispose();
        }

        _thumbnailCache.Clear();
        _thumbnailLru.Clear();
        _thumbnailCacheSize = 0;
    }

    private Dictionary<ImageGalleryCacheKey, CacheEntry> GetCache(ImageGalleryImagePurpose purpose) =>
        purpose == ImageGalleryImagePurpose.Thumbnail ? _thumbnailCache : _mainCache;

    private LinkedList<ImageGalleryCacheKey> GetLru(ImageGalleryImagePurpose purpose) =>
        purpose == ImageGalleryImagePurpose.Thumbnail ? _thumbnailLru : _lru;

    private long GetBudget(ImageGalleryImagePurpose purpose) =>
        purpose == ImageGalleryImagePurpose.Thumbnail
            ? _owner.ThumbnailCacheMemoryBudgetBytes
            : _owner.MainImageCacheMemoryBudgetBytes;

    private long GetCacheSize(ImageGalleryImagePurpose purpose) =>
        purpose == ImageGalleryImagePurpose.Thumbnail ? _thumbnailCacheSize : _cacheSize;

    private void AddCacheSize(ImageGalleryImagePurpose purpose, long delta)
    {
        if (purpose == ImageGalleryImagePurpose.Thumbnail)
        {
            _thumbnailCacheSize = checked(_thumbnailCacheSize + delta);
        }
        else
        {
            _cacheSize = checked(_cacheSize + delta);
        }
    }

    private static int RoundUpToBucket(int value) =>
        RoundUpToBucket(value, 16);

    private static int RoundUpToBucket(int value, int bucket) =>
        checked(Math.Max(bucket, (value + bucket - 1) / bucket * bucket));

    private static void CancelAndDispose(ref CancellationTokenSource? source)
    {
        source?.Cancel();
        source?.Dispose();
        source = null;
    }

    private static void VerifyUiThread()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            throw new InvalidOperationException("ImageGallery load lifecycle must run on the Avalonia UI thread.");
        }
    }

    private sealed record CacheEntry(
        SharedImageResource Resource,
        LinkedListNode<ImageGalleryCacheKey> Node,
        long Size);

    private sealed class InflightEntry(
        Task<SharedImageResource> operation,
        long attachmentGeneration)
    {
        public Task<SharedImageResource> Operation { get; } = operation;

        public long AttachmentGeneration { get; } = attachmentGeneration;

        public SharedImageResource? Resource { get; set; }

        public int WaiterCount { get; set; }

        public bool IsCompleted { get; set; }

        public bool IsAbandoned { get; set; }

        public bool IsFinalized { get; set; }
    }

    private readonly record struct SharedImageAcquisition(
        SharedImageResource Resource,
        ImageGalleryImageLease Lease);
}
