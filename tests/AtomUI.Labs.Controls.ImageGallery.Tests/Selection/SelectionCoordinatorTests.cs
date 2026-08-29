using System.Collections.ObjectModel;
using Shouldly;
using Avalonia.Threading;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Selection;

public sealed class SelectionCoordinatorTests
{
    public SelectionCoordinatorTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public async Task NonEmptyCollectionAutomaticallySelectsFirstItem()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var items = CreateItems("A", "B", "C");
            var control = new ImageGallery { ItemsSource = items };

            control.SelectedIndex.ShouldBe(0);
            control.SelectedItem.ShouldBeSameAs(items[0]);
            control.ImageState.ShouldBe(ImageGalleryImageState.Loading);
        });
    }

    [Fact]
    public async Task RemovingCurrentItemPrefersSuccessorThenPredecessor()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var items = CreateItems("A", "B", "C", "D");
            var control = new ImageGallery { ItemsSource = items, SelectedIndex = 2 };

            items.RemoveAt(2);

            ((IImageGalleryItem)control.SelectedItem!).Key.ShouldBe("D");
            control.SelectedIndex.ShouldBe(2);

            items.RemoveAt(2);

            ((IImageGalleryItem)control.SelectedItem!).Key.ShouldBe("B");
            control.SelectedIndex.ShouldBe(1);
        });
    }

    [Fact]
    public async Task InsertAndMovePreserveSelectedIdentity()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var items = CreateItems("A", "B", "C");
            var control = new ImageGallery { ItemsSource = items, SelectedIndex = 1 };
            var selected = items[1];

            items.Insert(0, Item("X"));
            control.SelectedItem.ShouldBeSameAs(selected);
            control.SelectedIndex.ShouldBe(2);

            items.Move(2, 3);
            control.SelectedItem.ShouldBeSameAs(selected);
            control.SelectedIndex.ShouldBe(3);
        });
    }

    [Fact]
    public async Task ReplaceWithSameKeyUpdatesSelectedItemAtSameIndex()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var items = CreateItems("A", "B", "C");
            var control = new ImageGallery { ItemsSource = items, SelectedIndex = 1 };
            var replacement = Item("B", "new title");

            items[1] = replacement;

            control.SelectedIndex.ShouldBe(1);
            control.SelectedItem.ShouldBeSameAs(replacement);
        });
    }

    [Fact]
    public async Task DuplicateKeysAreRejected()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var control = new ImageGallery();
            var items = new ObservableCollection<ImageGalleryItem>
            {
                Item("A"),
                Item("A")
            };

            var exception = Should.Throw<InvalidOperationException>(() => control.ItemsSource = items);

            exception.Message.ShouldContain("duplicate Key");
        });
    }

    [Fact]
    public async Task ClearingCollectionProducesEmptyState()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var items = CreateItems("A", "B");
            var control = new ImageGallery { ItemsSource = items };

            items.Clear();

            control.SelectedIndex.ShouldBe(-1);
            control.SelectedItem.ShouldBeNull();
            control.ImageState.ShouldBe(ImageGalleryImageState.Empty);
        });
    }

    private static ObservableCollection<ImageGalleryItem> CreateItems(params string[] keys)
    {
        return new ObservableCollection<ImageGalleryItem>(keys.Select(key => Item(key)));
    }

    private static ImageGalleryItem Item(string key, string? title = null)
    {
        return new ImageGalleryItem
        {
            Key = key,
            Title = title ?? key,
            MainImageSource = new StubSource(key)
        };
    }

    private sealed class StubSource(object identity) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
