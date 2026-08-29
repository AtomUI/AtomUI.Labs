using System.Collections.ObjectModel;
using AtomUI.Labs.Controls.ImageGallery.Filmstrip;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Filmstrip;

public sealed class ImageGalleryVirtualizationTests
{
    public ImageGalleryVirtualizationTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData(10_000)]
    [InlineData(100_000)]
    public async Task Large_descriptor_sets_realize_only_a_bounded_thumbnail_window(int itemCount)
    {
        ImageGallery gallery = null!;
        Window window = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var items = Enumerable.Range(0, itemCount)
                .Select(index => (IImageGalleryItem)new ImageGalleryItem
                {
                    Key = index,
                    Title = $"Image {index}",
                    MainImageSource = new ImmediateSource(index),
                })
                .ToArray();
            gallery = new ImageGallery
            {
                Width = 1000,
                Height = 600,
                ItemsSource = items,
            };
            window = new Window { Width = 1000, Height = 600, Content = gallery };
            window.Show();
        });

        await WaitForLayoutAsync();
        var firstWindowCount = await Dispatcher.UIThread.InvokeAsync(() =>
            gallery.GetRealizedContainers().Count());
        firstWindowCount.ShouldBeGreaterThan(0);
        firstWindowCount.ShouldBeLessThan(40);
        var firstMetrics = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var scroller = gallery.GetVisualDescendants().OfType<ImageGalleryFilmstripScrollViewer>().Single();
            var presenter = gallery.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ItemsPresenter>().Single();
            return new
            {
                ScrollerBounds = scroller.Bounds,
                scroller.Extent,
                scroller.Viewport,
                PresenterBounds = presenter.Bounds,
            };
        });
        Assert.True(
            firstMetrics.Viewport.Width > 0 || firstMetrics.Viewport.Height > 0,
            $"Initial scroll metrics invalid: Bounds={firstMetrics.ScrollerBounds}; Extent={firstMetrics.Extent}; " +
            $"Viewport={firstMetrics.Viewport}; Presenter={firstMetrics.PresenterBounds}");

        await Dispatcher.UIThread.InvokeAsync(() => gallery.SelectedIndex = itemCount - 1);
        await WaitForLayoutAsync();
        var lastWindow = await Dispatcher.UIThread.InvokeAsync(() => new
        {
            Count = gallery.GetRealizedContainers().Count(),
            Last = gallery.ContainerFromIndex(itemCount - 1),
            Panel = gallery.GetVisualDescendants().OfType<ImageGalleryVirtualizingPanel>().SingleOrDefault() is { } panel
                ? new { panel.FirstRealizedIndex, panel.LastRealizedIndex, panel.Bounds, panel.CacheLength }
                : null,
            Scroller = gallery.GetVisualDescendants().OfType<ImageGalleryFilmstripScrollViewer>().SingleOrDefault() is { } scroller
                ? new { scroller.Extent, scroller.Viewport, scroller.Offset }
                : null,
        });

        var mainAxisViewport = Math.Max(lastWindow.Scroller?.Viewport.Width ?? 0, lastWindow.Scroller?.Viewport.Height ?? 0);
        var slotStride = await Dispatcher.UIThread.InvokeAsync(() =>
            gallery.ThumbnailItemExtent + gallery.ThumbnailItemSpacing);
        var maximumRealized = (int)Math.Ceiling(2 * mainAxisViewport / slotStride) + 4;
        Assert.True(
            lastWindow.Count <= maximumRealized,
            $"Realized={lastWindow.Count}; PanelFirst={lastWindow.Panel?.FirstRealizedIndex}; " +
            $"PanelLast={lastWindow.Panel?.LastRealizedIndex}; PanelBounds={lastWindow.Panel?.Bounds}; " +
            $"Extent={lastWindow.Scroller?.Extent}; Viewport={lastWindow.Scroller?.Viewport}; " +
            $"Offset={lastWindow.Scroller?.Offset}");
        lastWindow.Last.ShouldNotBeNull();
        lastWindow.Panel.ShouldNotBeNull();
        lastWindow.Panel.CacheLength.ShouldBe(0.5);

        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    [Fact]
    public async Task Observable_collection_reindex_keeps_the_selected_thumbnail_realized()
    {
        const int itemCount = 100_000;
        ObservableCollection<IImageGalleryItem> items = null!;
        ImageGallery gallery = null!;
        Window window = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            items = new ObservableCollection<IImageGalleryItem>(
                Enumerable.Range(0, itemCount)
                    .Select(index => (IImageGalleryItem)new ImageGalleryItem
                    {
                        Key = index,
                        Title = $"Image {index}",
                        MainImageSource = new ImmediateSource(index),
                    }));
            gallery = new ImageGallery
            {
                Width = 1000,
                Height = 600,
                ItemsSource = items,
            };
            window = new Window { Width = 1000, Height = 600, Content = gallery };
            window.Show();
            gallery.SelectedIndex = 24_175;
        });

        await WaitForLayoutAsync();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            items.Add(new ImageGalleryItem
            {
                Key = itemCount,
                MainImageSource = new ImmediateSource(itemCount),
            });
            items.RemoveAt(0);
        });
        await WaitForLayoutAsync();
        var afterRemove = await Dispatcher.UIThread.InvokeAsync(() => new
        {
            gallery.SelectedIndex,
            SelectedKey = (gallery.SelectedItem as IImageGalleryItem)?.Key,
            Container = gallery.ContainerFromIndex(gallery.SelectedIndex),
        });
        afterRemove.SelectedIndex.ShouldBe(24_174);
        afterRemove.SelectedKey.ShouldBe(24_175);
        afterRemove.Container.ShouldBeOfType<ImageGalleryThumbnailItem>();

        await Dispatcher.UIThread.InvokeAsync(() => items.Move(items.Count - 1, 0));
        await WaitForLayoutAsync();
        var afterMove = await Dispatcher.UIThread.InvokeAsync(() => new
        {
            gallery.SelectedIndex,
            SelectedKey = (gallery.SelectedItem as IImageGalleryItem)?.Key,
            Container = gallery.ContainerFromIndex(gallery.SelectedIndex),
        });
        afterMove.SelectedIndex.ShouldBe(24_175);
        afterMove.SelectedKey.ShouldBe(24_175);
        afterMove.Container.ShouldBeOfType<ImageGalleryThumbnailItem>();

        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Top, Avalonia.Layout.Orientation.Horizontal)]
    [InlineData(ImageGalleryEdgePlacement.Bottom, Avalonia.Layout.Orientation.Horizontal)]
    [InlineData(ImageGalleryEdgePlacement.Left, Avalonia.Layout.Orientation.Vertical)]
    [InlineData(ImageGalleryEdgePlacement.Right, Avalonia.Layout.Orientation.Vertical)]
    public async Task Filmstrip_orientation_is_derived_from_placement(
        ImageGalleryEdgePlacement placement,
        Avalonia.Layout.Orientation expected)
    {
        ImageGallery gallery = null!;
        Window window = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                Width = 600,
                Height = 400,
                ThumbnailFilmstripPlacement = placement,
                ItemsSource = new[]
                {
                    new ImageGalleryItem
                    {
                        Key = 1,
                        MainImageSource = new ImmediateSource(1),
                    },
                },
            };
            window = new Window { Width = 600, Height = 400, Content = gallery };
            window.Show();
        });

        await WaitForLayoutAsync();
        var orientation = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var panel = gallery.GetVisualDescendants().OfType<ImageGalleryVirtualizingPanel>().SingleOrDefault();
            Assert.True(
                panel is not null,
                "ImageGalleryVirtualizingPanel missing. Visuals: " +
                string.Join(", ", gallery.GetVisualDescendants().Select(control => control.GetType().FullName)));
            return panel.Orientation;
        });
        orientation.ShouldBe(expected);
        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    [Fact]
    public async Task Initial_thumbnail_selection_does_not_scroll_an_outer_page()
    {
        ScrollViewer outerScroller = null!;
        Window window = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var gallery = new ImageGallery
            {
                Width = 800,
                Height = 420,
                ItemsSource = Enumerable.Range(0, 12)
                    .Select(index => (IImageGalleryItem)new ImageGalleryItem
                    {
                        Key = index,
                        Title = $"Image {index}",
                        MainImageSource = new ImmediateSource(index),
                    })
                    .ToArray(),
            };
            var page = new StackPanel
            {
                Children =
                {
                    new Border { Height = 900 },
                    gallery,
                },
            };
            outerScroller = new ScrollViewer
            {
                Width = 900,
                Height = 600,
                Content = page,
            };
            window = new Window { Width = 900, Height = 600, Content = outerScroller };
            window.Show();
        });

        await WaitForLayoutAsync();
        (await Dispatcher.UIThread.InvokeAsync(() => outerScroller.Offset.Y)).ShouldBe(0);
        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Bottom)]
    [InlineData(ImageGalleryEdgePlacement.Right)]
    public async Task Selecting_a_distant_thumbnail_scrolls_only_the_internal_filmstrip(
        ImageGalleryEdgePlacement placement)
    {
        ImageGallery gallery = null!;
        ScrollViewer outerScroller = null!;
        Window window = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                Width = 800,
                Height = 420,
                ThumbnailFilmstripPlacement = placement,
                ItemsSource = Enumerable.Range(0, 200)
                    .Select(index => (IImageGalleryItem)new ImageGalleryItem
                    {
                        Key = index,
                        Title = $"Image {index}",
                        MainImageSource = new ImmediateSource(index),
                    })
                    .ToArray(),
            };
            var page = new StackPanel
            {
                Children =
                {
                    gallery,
                    new Border { Height = 900 },
                },
            };
            outerScroller = new ScrollViewer
            {
                Width = 900,
                Height = 600,
                Content = page,
            };
            window = new Window { Width = 900, Height = 600, Content = outerScroller };
            window.Show();
        });

        await WaitForLayoutAsync();
        await Dispatcher.UIThread.InvokeAsync(() => gallery.SelectedIndex = 175);
        await WaitForLayoutAsync();

        var result = await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var scroller = gallery.GetVisualDescendants()
                .OfType<ImageGalleryFilmstripScrollViewer>()
                .Single();
            var mainOffset = placement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom
                ? scroller.Offset.X
                : scroller.Offset.Y;
            return new
            {
                MainOffset = mainOffset,
                OuterOffset = outerScroller.Offset.Y,
                SelectedContainer = gallery.ContainerFromIndex(175),
            };
        });

        result.MainOffset.ShouldBeGreaterThan(0);
        result.OuterOffset.ShouldBe(0);
        result.SelectedContainer.ShouldBeOfType<ImageGalleryThumbnailItem>();
        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    private static async Task WaitForLayoutAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(
            () => { },
            DispatcherPriority.Background,
            TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
    }

    private sealed class ImmediateSource(object identity) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = request.TargetPixelSize ?? new PixelSize(320, 180);
            var image = new TestImage(target.Width, target.Height);
            return ValueTask.FromResult(ImageGalleryImageLease.Create(
                image,
                target,
                target,
                checked((long)target.Width * target.Height * 4),
                () => { }));
        }
    }

    private sealed class TestImage(double width, double height) : IImage
    {
        public Size Size { get; } = new(width, height);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }
}
