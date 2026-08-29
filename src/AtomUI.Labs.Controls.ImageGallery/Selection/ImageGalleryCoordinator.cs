using System.Collections;
using System.Collections.Specialized;
using AtomUI.Labs.Controls.ImageGallery.Data;
using Avalonia.Threading;

namespace AtomUI.Labs.Controls.ImageGallery.Selection;

internal sealed class ImageGalleryCoordinator : IDisposable
{
    private readonly ImageGallery _owner;
    private readonly List<ImageGalleryDescriptor> _descriptors = [];
    private readonly Dictionary<object, int> _indexByKey = [];
    private INotifyCollectionChanged? _observableSource;
    private object? _selectedKey;
    private int _selectedIndex = -1;
    private bool _disposed;

    public ImageGalleryCoordinator(ImageGallery owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public IReadOnlyList<ImageGalleryDescriptor> Descriptors => _descriptors;

    public ImageGalleryDescriptor? CurrentDescriptor =>
        _selectedIndex >= 0 && _selectedIndex < _descriptors.Count
            ? _descriptors[_selectedIndex]
            : null;

    public ImageGalleryDescriptor? FindDescriptor(IImageGalleryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Key is not null && _indexByKey.TryGetValue(item.Key, out var index)
            ? _descriptors[index]
            : null;
    }

    public void AttachItemsSource(IEnumerable? source)
    {
        VerifyUiThread("assign ItemsSource");

        var snapshot = BuildSnapshot(source);
        var oldKey = _selectedKey;
        var oldIndex = _selectedIndex;

        if (_observableSource is not null)
        {
            _observableSource.CollectionChanged -= OnCollectionChanged;
        }

        _observableSource = source as INotifyCollectionChanged;
        if (_observableSource is not null)
        {
            _observableSource.CollectionChanged += OnCollectionChanged;
        }

        CommitSnapshot(snapshot);

        var currentItem = _owner.SelectedItem as IImageGalleryItem;
        var currentKey = currentItem?.Key;
        var selectedIndex = ResolveIndex(oldKey, oldIndex, currentKey);
        CommitSelection(selectedIndex);
    }

    public void OnControlSelectionChanged()
    {
        if (_descriptors.Count != _owner.Items.Count)
        {
            return;
        }

        var index = _owner.SelectedIndex;
        if (index < 0 || index >= _descriptors.Count)
        {
            if (_descriptors.Count == 0)
            {
                _selectedIndex = -1;
                _selectedKey = null;
            }

            return;
        }

        if (_owner.SelectedItem is not IImageGalleryItem selectedItem)
        {
            return;
        }

        var descriptor = _descriptors[index];
        if (!ReferenceEquals(descriptor.Item, selectedItem) &&
            !Equals(descriptor.Key, selectedItem.Key))
        {
            return;
        }

        var changed = _selectedIndex != index || !Equals(_selectedKey, descriptor.Key);
        _selectedIndex = index;
        _selectedKey = descriptor.Key;
        if (changed)
        {
            _owner.OnCoordinatedSelectionChanged(descriptor);
        }
    }

    public void SuspendItemsSourceNotifications()
    {
        if (_observableSource is not null)
        {
            _observableSource.CollectionChanged -= OnCollectionChanged;
            _observableSource = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_observableSource is not null)
        {
            _observableSource.CollectionChanged -= OnCollectionChanged;
            _observableSource = null;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        VerifyUiThread($"process collection action {args.Action}");

        var oldKey = _selectedKey;
        var oldIndex = _selectedIndex;
        var snapshot = BuildSnapshot(sender as IEnumerable ?? _owner.ItemsSource);

        CommitSnapshot(snapshot);
        CommitSelection(ResolveIndex(oldKey, oldIndex, null));
    }

    private int ResolveIndex(object? oldKey, int oldIndex, object? currentKey)
    {
        if (_descriptors.Count == 0)
        {
            return -1;
        }

        if (oldKey is not null && _indexByKey.TryGetValue(oldKey, out var oldIdentityIndex))
        {
            return oldIdentityIndex;
        }

        if (currentKey is not null && _indexByKey.TryGetValue(currentKey, out var currentIndex))
        {
            return currentIndex;
        }

        return oldIndex >= 0
            ? Math.Min(oldIndex, _descriptors.Count - 1)
            : 0;
    }

    private void CommitSelection(int index)
    {
        var selectionChangedByCommit = _owner.SelectedIndex != index;
        if (selectionChangedByCommit)
        {
            _owner.SelectedIndex = index;
        }

        _selectedIndex = index;
        _selectedKey = index >= 0 ? _descriptors[index].Key : null;
        _owner.SetImageState(index >= 0
            ? ImageGalleryImageState.Loading
            : ImageGalleryImageState.Empty);
        if (!selectionChangedByCommit)
        {
            _owner.OnCoordinatedSelectionChanged(index >= 0 ? _descriptors[index] : null);
        }
        _owner.RaiseNavigationCanExecuteChanged();
        _owner.RequestSelectedThumbnailIntoView();
    }

    private void CommitSnapshot(Snapshot snapshot)
    {
        _descriptors.Clear();
        _descriptors.AddRange(snapshot.Descriptors);
        _indexByKey.Clear();
        foreach (var pair in snapshot.IndexByKey)
        {
            _indexByKey.Add(pair.Key, pair.Value);
        }

        _owner.OnDescriptorSetChanged();
    }

    private static Snapshot BuildSnapshot(IEnumerable? source)
    {
        var capacity = source is ICollection collection ? collection.Count : 0;
        var descriptors = capacity > 0
            ? new List<ImageGalleryDescriptor>(capacity)
            : [];
        var indexByKey = capacity > 0
            ? new Dictionary<object, int>(capacity)
            : [];
        if (source is null)
        {
            return new Snapshot(descriptors, indexByKey);
        }

        var index = 0;
        foreach (var value in source)
        {
            if (value is not IImageGalleryItem item)
            {
                throw new InvalidOperationException(
                    $"ImageGallery ItemsSource item at index {index} must implement {nameof(IImageGalleryItem)}; actual type is '{value?.GetType().FullName ?? "null"}'.");
            }

            if (item.Key is null)
            {
                throw new InvalidOperationException(
                    $"ImageGallery ItemsSource item at index {index} has a null Key.");
            }

            if (item.MainImageSource is null)
            {
                throw new InvalidOperationException(
                    $"ImageGallery ItemsSource item at index {index} has a null MainImageSource.");
            }

            if (item.MainImageSource.Identity is null)
            {
                throw new InvalidOperationException(
                    $"ImageGallery MainImageSource at index {index} has a null Identity.");
            }

            if (item.ThumbnailImageSource is not null &&
                item.ThumbnailImageSource.Identity is null)
            {
                throw new InvalidOperationException(
                    $"ImageGallery ThumbnailImageSource at index {index} has a null Identity.");
            }

            if (!indexByKey.TryAdd(item.Key, index))
            {
                throw new InvalidOperationException(
                    $"ImageGallery ItemsSource contains duplicate Key '{item.Key}' at index {index}.");
            }

            descriptors.Add(new ImageGalleryDescriptor(
                item,
                item.Key,
                item.Title,
                item.MainImageSource,
                item.ThumbnailImageSource));
            index++;
        }

        return new Snapshot(descriptors, indexByKey);
    }

    private static void VerifyUiThread(string operation)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            throw new InvalidOperationException(
                $"ImageGallery must {operation} on the Avalonia UI thread.");
        }
    }

    private sealed record Snapshot(
        List<ImageGalleryDescriptor> Descriptors,
        Dictionary<object, int> IndexByKey);
}
