using AtomUI.Labs.Controls.ImageGallery.Appearance;
using AtomUI.Labs.Controls.ImageGallery.Filmstrip;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Filmstrip;

public sealed class ImageGalleryFilmstripWheelTests
{
    public ImageGalleryFilmstripWheelTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public async Task Every_visual_region_of_an_overflowing_filmstrip_owns_the_wheel()
    {
        var host = await CreateHostAsync(200, ImageGalleryEdgePlacement.Bottom);
        try
        {
            var targets = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var filmstrip = FindFilmstripHost(host.Gallery);
                var layout = host.Gallery.GetVisualDescendants()
                    .OfType<ImageGalleryFilmstripLayoutPanel>()
                    .Single();
                var scroller = FindScroller(host.Gallery);
                var add = FindButton(host.Gallery, "ImageGalleryAddButton");
                var previous = FindButton(host.Gallery, "ImageGalleryFilmstripNavigation", "Previous");
                var next = FindButton(host.Gallery, "ImageGalleryFilmstripNavigation", "Next");
                return new (string Name, Point Point)[]
                {
                    ("thumbnail scroller", CenterInWindow(scroller, host.Window)),
                    ("add button", CenterInWindow(add, host.Window)),
                    ("previous button", CenterInWindow(previous, host.Window)),
                    ("next button", CenterInWindow(next, host.Window)),
                    ("button spacing", TranslateToWindow(
                        layout,
                        new Point(
                            add.Bounds.Right + host.Gallery.ThumbnailItemSpacing / 2,
                            layout.Bounds.Height / 2),
                        host.Window)),
                    ("filmstrip padding", TranslateToWindow(
                        filmstrip,
                        new Point(filmstrip.Bounds.Width / 2, 4),
                        host.Window)),
                };
            });

            foreach (var target in targets)
            {
                await ResetOffsetsAsync(host);
                await Dispatcher.UIThread.InvokeAsync(() =>
                    host.Window.MouseWheel(target.Point, new Vector(0, -1)));
                await WaitForLayoutAsync();

                var offsets = await Dispatcher.UIThread.InvokeAsync(() => new
                {
                    Filmstrip = FindScroller(host.Gallery).Offset.X,
                    Outer = host.OuterScroller.Offset.Y,
                });
                Assert.True(offsets.Filmstrip > 0, $"The {target.Name} did not scroll the filmstrip.");
                offsets.Outer.ShouldBe(0, $"The {target.Name} leaked the wheel to the outer page.");
                host.OuterScroller.UnhandledWheelCount.ShouldBe(
                    0,
                    $"The {target.Name} leaked an unhandled wheel event to the outer page.");
            }
        }
        finally
        {
            await CloseAsync(host);
        }
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Top)]
    [InlineData(ImageGalleryEdgePlacement.Bottom)]
    [InlineData(ImageGalleryEdgePlacement.Left)]
    [InlineData(ImageGalleryEdgePlacement.Right)]
    public async Task Overflowing_filmstrip_scrolls_its_main_axis_for_every_placement(
        ImageGalleryEdgePlacement placement)
    {
        var host = await CreateHostAsync(200, placement);
        try
        {
            var point = await Dispatcher.UIThread.InvokeAsync(() =>
                CenterInWindow(FindScroller(host.Gallery), host.Window));
            await ResetOffsetsAsync(host);
            await Dispatcher.UIThread.InvokeAsync(() =>
                host.Window.MouseWheel(point, new Vector(0, -1)));
            await WaitForLayoutAsync();

            var offsets = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var offset = FindScroller(host.Gallery).Offset;
                return new
                {
                    Main = placement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom
                        ? offset.X
                        : offset.Y,
                    Cross = placement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom
                        ? offset.Y
                        : offset.X,
                    Outer = host.OuterScroller.Offset.Y,
                };
            });
            offsets.Main.ShouldBeGreaterThan(0);
            offsets.Cross.ShouldBe(0);
            offsets.Outer.ShouldBe(0);
            host.OuterScroller.UnhandledWheelCount.ShouldBe(0);
        }
        finally
        {
            await CloseAsync(host);
        }
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Top, -1, 0)]
    [InlineData(ImageGalleryEdgePlacement.Bottom, -1, 0)]
    [InlineData(ImageGalleryEdgePlacement.Left, -1, 0)]
    [InlineData(ImageGalleryEdgePlacement.Right, -1, 0)]
    public async Task Overflowing_filmstrip_accepts_the_touchpad_cross_axis_fallback(
        ImageGalleryEdgePlacement placement,
        double deltaX,
        double deltaY)
    {
        var host = await CreateHostAsync(200, placement);
        try
        {
            var point = await Dispatcher.UIThread.InvokeAsync(() =>
                CenterInWindow(FindScroller(host.Gallery), host.Window));
            await ResetOffsetsAsync(host);
            await Dispatcher.UIThread.InvokeAsync(() =>
                host.Window.MouseWheel(point, new Vector(deltaX, deltaY)));
            await WaitForLayoutAsync();

            var mainOffset = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var offset = FindScroller(host.Gallery).Offset;
                return placement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom
                    ? offset.X
                    : offset.Y;
            });
            mainOffset.ShouldBeGreaterThan(0);
            host.OuterScroller.UnhandledWheelCount.ShouldBe(0);
        }
        finally
        {
            await CloseAsync(host);
        }
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Bottom)]
    [InlineData(ImageGalleryEdgePlacement.Left)]
    public async Task Wheel_at_filmstrip_end_does_not_chain_to_the_outer_page(
        ImageGalleryEdgePlacement placement)
    {
        var host = await CreateHostAsync(200, placement);
        try
        {
            Point point = default;
            double maximum = 0;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var scroller = FindScroller(host.Gallery);
                var horizontal = placement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom;
                maximum = horizontal
                    ? scroller.Extent.Width - scroller.Viewport.Width
                    : scroller.Extent.Height - scroller.Viewport.Height;
                maximum.ShouldBeGreaterThan(0);
                scroller.Offset = horizontal ? new Vector(maximum, 0) : new Vector(0, maximum);
                host.OuterScroller.Offset = default;
                point = CenterInWindow(
                    FindButton(host.Gallery, "ImageGalleryFilmstripNavigation", "Next"),
                    host.Window);
            });
            await WaitForLayoutAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
                host.Window.MouseWheel(point, new Vector(0, -1)));
            await WaitForLayoutAsync();

            var offsets = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var offset = FindScroller(host.Gallery).Offset;
                return new
                {
                    Main = placement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom
                        ? offset.X
                        : offset.Y,
                    Outer = host.OuterScroller.Offset.Y,
                };
            });
            offsets.Main.ShouldBe(maximum, tolerance: 0.01);
            offsets.Outer.ShouldBe(0);
            host.OuterScroller.UnhandledWheelCount.ShouldBe(0);
        }
        finally
        {
            await CloseAsync(host);
        }
    }

    [Fact]
    public async Task Filmstrip_without_overflow_allows_the_outer_page_to_scroll()
    {
        var host = await CreateHostAsync(1, ImageGalleryEdgePlacement.Bottom);
        try
        {
            var point = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var scroller = FindScroller(host.Gallery);
                Assert.True(scroller.Extent.Width <= scroller.Viewport.Width);
                return CenterInWindow(FindFilmstripHost(host.Gallery), host.Window);
            });
            await ResetOffsetsAsync(host);

            await Dispatcher.UIThread.InvokeAsync(() =>
                host.Window.MouseWheel(point, new Vector(0, -1)));
            await WaitForLayoutAsync();

            var offsets = await Dispatcher.UIThread.InvokeAsync(() => new
            {
                Filmstrip = FindScroller(host.Gallery).Offset.X,
                Outer = host.OuterScroller.Offset.Y,
            });
            offsets.Filmstrip.ShouldBe(0);
            host.OuterScroller.UnhandledWheelCount.ShouldBeGreaterThan(0);
        }
        finally
        {
            await CloseAsync(host);
        }
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Top)]
    [InlineData(ImageGalleryEdgePlacement.Bottom)]
    [InlineData(ImageGalleryEdgePlacement.Left)]
    [InlineData(ImageGalleryEdgePlacement.Right)]
    public async Task Buttons_and_thumbnails_share_the_filmstrip_cross_axis_center(
        ImageGalleryEdgePlacement placement)
    {
        var host = await CreateHostAsync(12, placement);
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var layout = host.Gallery.GetVisualDescendants()
                    .OfType<ImageGalleryFilmstripLayoutPanel>()
                    .Single();
                var controls = layout.Children
                    .Where(child =>
                        child.IsVisible &&
                        ImageGalleryFilmstripLayoutPanel.GetRole(child) != ImageGalleryFilmstripRole.Items)
                    .ToArray();
                var thumbnail = host.Gallery.GetRealizedContainers()
                    .OfType<ImageGalleryThumbnailItem>()
                    .First();
                var horizontal = placement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom;
                var expected = horizontal ? layout.Bounds.Height / 2 : layout.Bounds.Width / 2;

                foreach (var control in controls.Append<Control>(thumbnail))
                {
                    var center = control.TranslatePoint(
                        new Point(control.Bounds.Width / 2, control.Bounds.Height / 2),
                        layout).ShouldNotBeNull();
                    (horizontal ? center.Y : center.X).ShouldBe(expected, tolerance: 0.01);
                }
            });
        }
        finally
        {
            await CloseAsync(host);
        }
    }

    private static async Task<TestHost> CreateHostAsync(
        int itemCount,
        ImageGalleryEdgePlacement placement)
    {
        TestHost host = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var source = new ImmediateSource();
            var items = Enumerable.Range(0, itemCount)
                .Select(index => (IImageGalleryItem)new ImageGalleryItem
                {
                    Key = index,
                    Title = $"Image {index}",
                    MainImageSource = source,
                })
                .ToArray();
            var gallery = new ImageGallery
            {
                Width = 1000,
                Height = 600,
                ItemsSource = items,
                ThumbnailFilmstripPlacement = placement,
                FilmstripAppearance = new ImageGalleryFilmstripAppearance
                {
                    Padding = new Thickness(12),
                },
            };
            var content = new StackPanel { Width = 1000 };
            content.Children.Add(gallery);
            content.Children.Add(new Border { Height = 1200 });
            var outerScroller = new TrackingScrollViewer
            {
                Width = 1000,
                Height = 700,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
                Content = content,
            };
            var window = new Window
            {
                Width = 1000,
                Height = 700,
                Content = outerScroller,
            };
            window.Show();
            host = new TestHost(window, outerScroller, gallery);
        });
        await WaitForLayoutAsync();
        return host;
    }

    private static async Task ResetOffsetsAsync(TestHost host)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            FindScroller(host.Gallery).Offset = default;
            host.OuterScroller.Offset = default;
            host.OuterScroller.UnhandledWheelCount = 0;
        });
        await WaitForLayoutAsync();
    }

    private static Border FindFilmstripHost(ImageGallery gallery) =>
        gallery.GetVisualDescendants()
            .OfType<Border>()
            .Single(control => control.Name == "PART_Filmstrip");

    private static ImageGalleryFilmstripScrollViewer FindScroller(ImageGallery gallery) =>
        gallery.GetVisualDescendants()
            .OfType<ImageGalleryFilmstripScrollViewer>()
            .Single();

    private static ImageGalleryButton FindButton(ImageGallery gallery, params string[] classes) =>
        gallery.GetVisualDescendants()
            .OfType<ImageGalleryButton>()
            .Single(button => classes.All(button.Classes.Contains));

    private static Point CenterInWindow(Control control, Window window) =>
        TranslateToWindow(
            control,
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2),
            window);

    private static Point TranslateToWindow(Control control, Point point, Window window) =>
        control.TranslatePoint(point, window) ??
        throw new InvalidOperationException($"Could not translate {control.GetType().Name} into the test window.");

    private static async Task WaitForLayoutAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(
            () => { },
            DispatcherPriority.Background,
            TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
    }

    private static async Task CloseAsync(TestHost host)
    {
        await Dispatcher.UIThread.InvokeAsync(host.Window.Close);
        await WaitForLayoutAsync();
    }

    private sealed record TestHost(
        Window Window,
        TrackingScrollViewer OuterScroller,
        ImageGallery Gallery);

    private sealed class TrackingScrollViewer : ScrollViewer
    {
        public int UnhandledWheelCount { get; set; }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            UnhandledWheelCount++;
            base.OnPointerWheelChanged(e);
        }
    }

    private sealed class ImmediateSource : IImageGallerySource
    {
        public object Identity { get; } = "filmstrip-wheel-test";

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = request.TargetPixelSize ?? new PixelSize(320, 180);
            return ValueTask.FromResult(ImageGalleryImageLease.Create(
                new TestImage(target.Width, target.Height),
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
