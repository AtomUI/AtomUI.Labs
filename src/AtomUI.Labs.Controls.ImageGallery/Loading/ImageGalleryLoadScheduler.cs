namespace AtomUI.Labs.Controls.ImageGallery.Loading;

internal enum ImageGalleryLoadPriority
{
    CurrentMainImage = 0,
    VisibleThumbnail = 1,
    ThumbnailOverscan = 2,
    ExplicitNavigation = 3,
    AdjacentMainImage = 4,
    Speculative = 5,
}

/// <summary>
/// Per-gallery stable priority scheduler. Six regular operations may execute
/// concurrently; one bounded escape operation prevents a newly selected main
/// image from being trapped behind non-cooperative obsolete work.
/// </summary>
internal sealed class ImageGalleryLoadScheduler : IDisposable
{
    internal const int MaximumRegularConcurrency = 6;
    internal const int MaximumEscapeConcurrency = 1;

    private readonly object _gate = new();
    private readonly PriorityQueue<WorkItem, (int Priority, long Sequence)> _queue = new();
    private readonly CancellationTokenSource _lifetime = new();
    private long _nextSequence;
    private int _activeRegular;
    private bool _escapeActive;
    private bool _disposed;

    public Task<T> ScheduleAsync<T>(
        ImageGalleryLoadPriority priority,
        Func<CancellationToken, ValueTask<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        var item = new WorkItem<T>(operation, cancellationToken);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var sequence = _nextSequence++;
            if (priority == ImageGalleryLoadPriority.CurrentMainImage &&
                _activeRegular >= MaximumRegularConcurrency &&
                !_escapeActive)
            {
                _escapeActive = true;
                item.IsEscape = true;
                Start(item);
            }
            else
            {
                _queue.Enqueue(item, ((int)priority, sequence));
                DispatchLocked();
            }
        }

        return item.Task;
    }

    public void Dispose()
    {
        List<WorkItem> pending = [];
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetime.Cancel();
            while (_queue.TryDequeue(out var item, out _))
            {
                pending.Add(item);
            }
        }

        foreach (var item in pending)
        {
            item.Cancel();
        }

        _lifetime.Dispose();
    }

    private void DispatchLocked()
    {
        while (_activeRegular < MaximumRegularConcurrency &&
               _queue.TryDequeue(out var item, out _))
        {
            if (item.IsCallerCancellationRequested)
            {
                item.Cancel();
                continue;
            }

            _activeRegular++;
            Start(item);
        }
    }

    private void Start(WorkItem item)
    {
        var lifetimeToken = _lifetime.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    lifetimeToken,
                    item.CallerCancellationToken);
                await item.ExecuteAsync(linked.Token).ConfigureAwait(false);
            }
            finally
            {
                lock (_gate)
                {
                    if (item.IsEscape)
                    {
                        _escapeActive = false;
                    }
                    else
                    {
                        _activeRegular--;
                    }

                    if (!_disposed)
                    {
                        DispatchLocked();
                    }
                }
            }
        });
    }

    private abstract class WorkItem
    {
        protected WorkItem(CancellationToken callerCancellationToken)
        {
            CallerCancellationToken = callerCancellationToken;
        }

        public CancellationToken CallerCancellationToken { get; }

        public bool IsCallerCancellationRequested => CallerCancellationToken.IsCancellationRequested;

        public bool IsEscape { get; set; }

        public abstract Task ExecuteAsync(CancellationToken cancellationToken);

        public abstract void Cancel();
    }

    private sealed class WorkItem<T> : WorkItem
    {
        private readonly Func<CancellationToken, ValueTask<T>> _operation;
        private readonly TaskCompletionSource<T> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public WorkItem(
            Func<CancellationToken, ValueTask<T>> operation,
            CancellationToken callerCancellationToken)
            : base(callerCancellationToken)
        {
            _operation = operation;
        }

        public Task<T> Task => _completion.Task;

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _operation(cancellationToken).ConfigureAwait(false);
                _completion.TrySetResult(result);
            }
            catch (OperationCanceledException exception)
            {
                _completion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                _completion.TrySetException(exception);
            }
        }

        public override void Cancel() =>
            _completion.TrySetCanceled(CallerCancellationToken.IsCancellationRequested
                ? CallerCancellationToken
                : new CancellationToken(canceled: true));
    }
}
