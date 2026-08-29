using System.Collections.ObjectModel;
using AtomUI.Labs.Controls.ImageGallery.Data;
using AtomUI.Labs.Controls.ImageGallery.Viewport;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Loading;

public sealed class ImageGalleryLoadLifecycleTests
{
    public ImageGalleryLoadLifecycleTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public async Task Latest_selection_wins_when_obsolete_source_finishes_late()
    {
        var slow = new ControlledSource("slow");
        var fast = new ControlledSource("fast");
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                ItemsSource = new ObservableCollection<IImageGalleryItem>
                {
                    new ImageGalleryItem { Key = "slow", Title = "Slow", MainImageSource = slow },
                    new ImageGalleryItem { Key = "fast", Title = "Fast", MainImageSource = fast },
                },
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
            gallery.SelectedIndex = 1;
        });

        fast.Complete(new TestImage(22, 11));
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        var selectedImage = await Dispatcher.UIThread.InvokeAsync(() => gallery.CurrentMainImage);
        selectedImage.ShouldBeOfType<TestImage>().Width.ShouldBe(22);

        slow.Complete(new TestImage(99, 33));
        await Task.Delay(100, TestContext.Current.CancellationToken);
        selectedImage = await Dispatcher.UIThread.InvokeAsync(() => gallery.CurrentMainImage);
        selectedImage.ShouldBeOfType<TestImage>().Width.ShouldBe(22);

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => slow.ReleaseCount > 0 && fast.ReleaseCount > 0);
    }

    [Fact]
    public async Task Detach_releases_current_image_and_ignores_late_completion()
    {
        var source = new ControlledSource("only");
        ImageGallery gallery = null!;
        Window window = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                ItemsSource = new[]
                {
                    new ImageGalleryItem { Key = "only", Title = "Only", MainImageSource = source },
                },
            };
            window = new Window { Content = gallery };
            window.Show();
        });

        await source.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        await Dispatcher.UIThread.InvokeAsync(window.Close);

        source.Complete(new TestImage(10, 10));
        await Task.Delay(100, TestContext.Current.CancellationToken);

        (await Dispatcher.UIThread.InvokeAsync(() => gallery.CurrentMainImage)).ShouldBeNull();
        await EventuallyAsync(() => source.ReleaseCount > 0);
    }

    [Fact]
    public async Task Selection_retains_and_renders_previous_image_until_candidate_commits()
    {
        var firstSource = new ControlledSource("first");
        var secondSource = new ControlledSource("second");
        var firstImage = new TestImage(40, 20);
        var secondImage = new TestImage(30, 30);
        IImage? imageObservedWhenFirstReleased = null;
        ImageGallery gallery = null!;
        Window window = null!;

        firstSource.Released = () => imageObservedWhenFirstReleased = gallery.CurrentMainImage;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                ItemsSource = new ObservableCollection<IImageGalleryItem>
                {
                    new ImageGalleryItem { Key = "first", MainImageSource = firstSource },
                    new ImageGalleryItem { Key = "second", MainImageSource = secondSource },
                },
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        firstSource.Complete(firstImage);
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var _ = window.CaptureRenderedFrame();
            gallery.RotateClockwise();
            gallery.RotationAngle.ShouldBe(90);
            gallery.SelectedIndex = 1;
        });
        await secondSource.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        var drawCountBeforeLoadingFrame = firstImage.DrawCount;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.ImageState.ShouldBe(ImageGalleryImageState.Loading);
            gallery.CurrentMainImage.ShouldBeSameAs(firstImage);
            gallery.RotationAngle.ShouldBe(90);
            gallery.GetVisualDescendants()
                .OfType<ImageGalleryViewportPresenter>()
                .Single()
                .InvalidateVisual();
            using var _ = window.CaptureRenderedFrame();
        });
        firstImage.DrawCount.ShouldBeGreaterThan(drawCountBeforeLoadingFrame);
        firstSource.ReleaseCount.ShouldBe(0);

        secondSource.Complete(secondImage);
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.CurrentMainImage.ShouldBeSameAs(secondImage);
            gallery.RotationAngle.ShouldBe(0);
        });

        await Dispatcher.UIThread.InvokeAsync(() => gallery.MainImageCacheMemoryBudgetBytes = 0);
        await EventuallyAsync(() => firstSource.ReleaseCount == 1);
        imageObservedWhenFirstReleased.ShouldBeSameAs(secondImage);

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => secondSource.ReleaseCount > 0);
    }

    [Fact]
    public async Task Rapid_selection_discards_every_obsolete_completion_and_commits_only_latest()
    {
        var firstSource = new ControlledSource("first");
        var secondSource = new ControlledSource("second");
        var latestSource = new ControlledSource("latest");
        var firstImage = new TestImage(10, 10);
        var obsoleteImage = new TestImage(20, 20);
        var latestImage = new TestImage(30, 30);
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                ItemsSource = new ObservableCollection<IImageGalleryItem>
                {
                    new ImageGalleryItem { Key = "first", MainImageSource = firstSource },
                    new ImageGalleryItem { Key = "second", MainImageSource = secondSource },
                    new ImageGalleryItem { Key = "latest", MainImageSource = latestSource },
                },
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        firstSource.Complete(firstImage);
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() => gallery.SelectedIndex = 1);
        await secondSource.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        await Dispatcher.UIThread.InvokeAsync(() => gallery.SelectedIndex = 2);
        await latestSource.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);

        secondSource.Complete(obsoleteImage);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.ImageState.ShouldBe(ImageGalleryImageState.Loading);
            gallery.CurrentMainImage.ShouldBeSameAs(firstImage);
        });

        latestSource.Complete(latestImage);
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        (await Dispatcher.UIThread.InvokeAsync(() => gallery.CurrentMainImage)).ShouldBeSameAs(latestImage);

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() =>
            firstSource.ReleaseCount > 0 &&
            secondSource.ReleaseCount > 0 &&
            latestSource.ReleaseCount > 0);
    }

    [Fact]
    public async Task Current_candidate_failure_replaces_retained_image_with_error_state()
    {
        var firstSource = new ControlledSource("first");
        var failingSource = new ControlledSource("failing");
        var firstImage = new TestImage(10, 10);
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                ItemsSource = new ObservableCollection<IImageGalleryItem>
                {
                    new ImageGalleryItem { Key = "first", MainImageSource = firstSource },
                    new ImageGalleryItem { Key = "failing", MainImageSource = failingSource },
                },
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        firstSource.Complete(firstImage);
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() => gallery.SelectedIndex = 1);
        await failingSource.Started.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        (await Dispatcher.UIThread.InvokeAsync(() => gallery.CurrentMainImage)).ShouldBeSameAs(firstImage);

        failingSource.Fail(new InvalidDataException("Expected current image failure."));
        await WaitForStateAsync(gallery, ImageGalleryImageState.Error);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.CurrentMainImage.ShouldBeNull();
            gallery.ImageFailure.ShouldBeOfType<InvalidDataException>();
        });

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => firstSource.ReleaseCount > 0);
    }

    [Fact]
    public async Task RepeatedThumbnailFailureIsSuppressedForFiveSeconds()
    {
        var thumbnail = new CountingFailureSource("thumbnail");
        ImageGallery gallery = null!;
        ImageGalleryDescriptor descriptor = null!;
        Window window = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var item = new ImageGalleryItem
            {
                Key = "item",
                MainImageSource = new ImmediateSource("main"),
                ThumbnailImageSource = thumbnail,
            };
            gallery = new ImageGallery
            {
                IsThumbnailFilmstripVisible = false,
                ItemsSource = new[] { item },
            };
            descriptor = gallery.FindDescriptor(item)!;
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        await Should.ThrowAsync<InvalidDataException>(async () =>
            await gallery.LoadThumbnailAsync(descriptor, new PixelSize(96, 96), TestContext.Current.CancellationToken));
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await gallery.LoadThumbnailAsync(descriptor, new PixelSize(96, 96), TestContext.Current.CancellationToken));
        thumbnail.CallCount.ShouldBe(1);
        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    private static async Task WaitForStateAsync(ImageGallery gallery, ImageGalleryImageState state)
    {
        await EventuallyAsync(async () =>
            await Dispatcher.UIThread.InvokeAsync(() => gallery.ImageState == state));
    }

    private static async Task EventuallyAsync(Func<bool> condition)
    {
        await EventuallyAsync(() => Task.FromResult(condition()));
    }

    private static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!await condition())
        {
            if (DateTime.UtcNow >= timeout)
            {
                throw new TimeoutException("Expected condition did not become true.");
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private sealed class ControlledSource(object identity) : IImageGallerySource
    {
        private readonly TaskCompletionSource<IImage> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public object Identity { get; } = identity;

        private int _releaseCount;

        public int ReleaseCount => Volatile.Read(ref _releaseCount);

        public Action? Released { get; set; }

        public void Complete(IImage image) => _completion.TrySetResult(image);

        public void Fail(Exception exception) => _completion.TrySetException(exception);

        public async ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            var image = await _completion.Task;
            var size = new PixelSize((int)image.Size.Width, (int)image.Size.Height);
            return ImageGalleryImageLease.Create(
                image,
                size,
                size,
                checked((long)size.Width * size.Height * 4),
                () =>
                {
                    Released?.Invoke();
                    Interlocked.Increment(ref _releaseCount);
                });
        }
    }

    private sealed class CountingFailureSource(object identity) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public int CallCount;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref CallCount);
            return ValueTask.FromException<ImageGalleryImageLease>(new InvalidDataException("Expected failure."));
        }
    }

    private sealed class ImmediateSource(object identity) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            var size = request.TargetPixelSize ?? new PixelSize(320, 180);
            return ValueTask.FromResult(ImageGalleryImageLease.Create(
                new TestImage(size.Width, size.Height),
                size,
                size,
                checked((long)size.Width * size.Height * 4),
                () => { }));
        }
    }

    private sealed class TestImage(double width, double height) : IImage
    {
        private int _drawCount;

        public double Width { get; } = width;

        public Size Size => new(Width, height);

        public int DrawCount => Volatile.Read(ref _drawCount);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            Interlocked.Increment(ref _drawCount);
        }
    }
}
