using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Loading;

public sealed class ImageGalleryResourceAccessTests
{
    public ImageGalleryResourceAccessTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public async Task ResourceOnlyLoadsWithoutDrawingAndAllowsAnIndependentLease()
    {
        var source = new RecordingSource("resource-only");
        var item = CreateItem("resource-only", source);
        ImageGallery gallery = null!;
        Window window = null!;
        ImageGalleryImageLease? externalLease = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImageMode = ImageGalleryMainImageMode.ResourceOnly,
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
                IsToolbarVisible = false,
                ItemsSource = new[] { item },
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var _ = window.CaptureRenderedFrame();
            source.LastImage!.DrawCount.ShouldBe(0);
            gallery.TryAcquireCurrentImage(item, out externalLease).ShouldBeTrue();
            externalLease.ShouldNotBeNull();
        });

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        source.ReleaseCount.ShouldBe(0);
        externalLease!.Image.ShouldBeSameAs(source.LastImage);
        externalLease.Dispose();
        await EventuallyAsync(() => source.ReleaseCount == 1);
    }

    [Fact]
    public async Task ModeSwitchDoesNotReloadResetOrRaiseResourceEvent()
    {
        var source = new RecordingSource("mode-switch");
        var item = CreateItem("mode-switch", source);
        ImageGallery gallery = null!;
        Window window = null!;
        var eventCount = 0;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                ZoomMode = ImageGalleryZoomMode.Custom,
                CustomZoomFactor = 2,
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
                ItemsSource = new[] { item },
            };
            gallery.CurrentImageResourceChanged += (_, _) => eventCount++;
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var before = eventCount;
            gallery.MainImageMode = ImageGalleryMainImageMode.ResourceOnly;
            gallery.MainImageMode = ImageGalleryMainImageMode.Presented;
            gallery.EffectiveZoomFactor.ShouldBe(2);
            eventCount.ShouldBe(before);
            gallery.TryAcquireCurrentImage(item, out var lease).ShouldBeTrue();
            lease!.Dispose();
        });

        source.CallCount.ShouldBe(1);
        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => source.ReleaseCount == 1);
    }

    [Fact]
    public async Task TryAcquireRejectsWrongLoadingAndDetachedIdentity()
    {
        var first = new ControlledSource("first");
        var second = new ControlledSource("second");
        var firstItem = CreateItem("first", first);
        var secondItem = CreateItem("second", second);
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
                ItemsSource = new ObservableCollection<IImageGalleryItem> { firstItem, secondItem },
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        first.Complete(new TestImage(640, 480));
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        Should.Throw<InvalidOperationException>(() =>
            gallery.TryAcquireCurrentImage(firstItem, out _));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.TryAcquireCurrentImage(secondItem, out var wrong).ShouldBeFalse();
            wrong.ShouldBeNull();
            gallery.SelectedIndex = 1;
            gallery.TryAcquireCurrentImage(firstItem, out var obsolete).ShouldBeFalse();
            obsolete.ShouldBeNull();
        });

        second.Complete(new TestImage(640, 480));
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.TryAcquireCurrentImage(secondItem, out var detached).ShouldBeFalse();
            detached.ShouldBeNull();
        });
    }

    [Fact]
    public async Task DecodeHintUpgradesCurrentOnlyAfterThresholdAndRaisesAfterCommit()
    {
        var source = new RecordingSource("upgrade");
        var item = CreateItem("upgrade", source);
        ImageGallery gallery = null!;
        Window window = null!;
        var readyEvents = 0;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImageMode = ImageGalleryMainImageMode.ResourceOnly,
                MainImageDecodeSizeHint = new PixelSize(1024, 768),
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
                ItemsSource = new[] { item },
            };
            gallery.CurrentImageResourceChanged += (_, _) =>
            {
                if (gallery.ImageState == ImageGalleryImageState.Ready)
                {
                    readyEvents++;
                    gallery.TryAcquireCurrentImage(item, out var lease).ShouldBeTrue();
                    lease!.Dispose();
                }
            };
            window = new Window { Width = 320, Height = 240, Content = gallery };
            window.Show();
        });

        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        var initialCalls = source.CallCount;
        await Dispatcher.UIThread.InvokeAsync(() =>
            gallery.MainImageDecodeSizeHint = new PixelSize(1100, 825));
        await Task.Delay(250, TestContext.Current.CancellationToken);
        source.CallCount.ShouldBe(initialCalls);

        await Dispatcher.UIThread.InvokeAsync(() =>
            gallery.MainImageDecodeSizeHint = new PixelSize(2800, 2000));
        await EventuallyAsync(() => source.CallCount == initialCalls + 1);
        await EventuallyAsync(() => readyEvents == 2);
        source.Requests[^1].TargetPixelSize!.Value.Width.ShouldBeGreaterThanOrEqualTo(2800);

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => source.ReleaseCount == source.CallCount);
    }

    [Fact]
    public async Task ResourceOnlyBlankViewportLetsPointerReachUnderlyingSibling()
    {
        var source = new RecordingSource("hit-through");
        var item = CreateItem("hit-through", source);
        var clickCount = 0;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var target = new Border { Background = Brushes.Red };
            target.PointerPressed += (_, _) => clickCount++;
            var gallery = new ImageGallery
            {
                MainImageMode = ImageGalleryMainImageMode.ResourceOnly,
                IsThumbnailFilmstripVisible = false,
                IsToolbarVisible = false,
                ItemsSource = new[] { item },
            };
            window = new Window
            {
                Width = 400,
                Height = 300,
                Content = new Grid { Children = { target, gallery } },
            };
            window.Show();
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var _ = window.CaptureRenderedFrame();
            var point = new Point(200, 150);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
        });
        clickCount.ShouldBe(1);
        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    [Fact]
    public async Task ResourceOnlyCustomToolbarRemainsInteractive()
    {
        var source = new RecordingSource("toolbar-input");
        var item = CreateItem("toolbar-input", source);
        var clickCount = 0;
        Border target = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            target = new Border
            {
                Width = 80,
                Height = 40,
                Background = Brushes.Green,
            };
            target.PointerPressed += (_, _) => clickCount++;
            var gallery = new ImageGallery
            {
                MainImageMode = ImageGalleryMainImageMode.ResourceOnly,
                IsThumbnailFilmstripVisible = false,
                IsToolbarTitleVisible = false,
                ToolbarContent = target,
                ItemsSource = new[] { item },
            };
            window = new Window { Width = 400, Height = 300, Content = gallery };
            window.Show();
        });

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            using var _ = window.CaptureRenderedFrame();
            var point = target.TranslatePoint(new Point(40, 20), window);
            point.ShouldNotBeNull();
            window.MouseDown(point.Value, MouseButton.Left);
            window.MouseUp(point.Value, MouseButton.Left);
        });
        clickCount.ShouldBe(1);
        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    [Fact]
    public async Task ZeroViewportWaitsForExplicitHintInsteadOfRequestingFullDecode()
    {
        var source = new RecordingSource("zero-viewport");
        var item = CreateItem("zero-viewport", source);
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                Width = 0,
                Height = 0,
                MainImageMode = ImageGalleryMainImageMode.ResourceOnly,
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
                ItemsSource = new[] { item },
            };
            window = new Window { Width = 400, Height = 300, Content = gallery };
            window.Show();
        });

        await Task.Delay(250, TestContext.Current.CancellationToken);
        source.CallCount.ShouldBe(0);
        (await Dispatcher.UIThread.InvokeAsync(() => gallery.ImageState))
            .ShouldBe(ImageGalleryImageState.Loading);

        await Dispatcher.UIThread.InvokeAsync(() =>
            gallery.MainImageDecodeSizeHint = new PixelSize(256, 128));
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        source.CallCount.ShouldBe(1);
        source.Requests.Single().TargetPixelSize.ShouldBe(new PixelSize(256, 128));

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => source.ReleaseCount == 1);
    }

    [Fact]
    public async Task HintChangeDuringInitialLoadDoesNotCancelBaseAndCatchesUpAfterCommit()
    {
        var source = new SequencedSource("loading-hint");
        var item = CreateItem("loading-hint", source);
        ImageGallery gallery = null!;
        Window window = null!;
        var readyEvents = 0;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImageMode = ImageGalleryMainImageMode.ResourceOnly,
                MainImageDecodeSizeHint = new PixelSize(512, 384),
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
                ItemsSource = new[] { item },
            };
            gallery.CurrentImageResourceChanged += (_, _) =>
            {
                if (gallery.ImageState == ImageGalleryImageState.Ready)
                {
                    readyEvents++;
                }
            };
            window = new Window { Width = 320, Height = 240, Content = gallery };
            window.Show();
        });

        await EventuallyAsync(() => source.CallCount == 1);
        await Dispatcher.UIThread.InvokeAsync(() =>
            gallery.MainImageDecodeSizeHint = new PixelSize(2048, 1536));
        await Task.Delay(250, TestContext.Current.CancellationToken);
        source.CallCount.ShouldBe(1);

        source.Complete(0);
        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await EventuallyAsync(() => source.CallCount == 2);
        source.Requests[1].TargetPixelSize.ShouldBe(new PixelSize(2048, 1536));
        source.Complete(1);
        await EventuallyAsync(() => readyEvents == 2);

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => source.ReleaseCount == 2);
    }

    [Fact]
    public async Task ResourceChangedEventReportsUnavailableCommitFailureAndDetachInOrder()
    {
        var readySource = new RecordingSource("ready");
        var failingSource = new ControlledSource("failing");
        var readyItem = CreateItem("ready", readySource);
        var failingItem = CreateItem("failing", failingSource);
        var states = new List<ImageGalleryImageState>();
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
            };
            gallery.CurrentImageResourceChanged += (_, _) => states.Add(gallery.ImageState);
            gallery.ItemsSource = new[] { readyItem, failingItem };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() => gallery.SelectedIndex = 1);
        failingSource.Fail(new InvalidDataException("Expected failure."));
        await WaitForStateAsync(gallery, ImageGalleryImageState.Error);
        await Dispatcher.UIThread.InvokeAsync(window.Close);

        states.ShouldBe([
            ImageGalleryImageState.Loading,
            ImageGalleryImageState.Ready,
            ImageGalleryImageState.Loading,
            ImageGalleryImageState.Error,
            ImageGalleryImageState.Empty,
        ]);
    }

    [Fact]
    public async Task ZeroMainCacheBudgetCannotReleaseResourceBeforeCurrentSlotAcquiresIt()
    {
        var source = new RecordingSource("zero-budget");
        var item = CreateItem("zero-budget", source);
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                MainImageCacheMemoryBudgetBytes = 0,
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
                ItemsSource = new[] { item },
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.TryAcquireCurrentImage(item, out var lease).ShouldBeTrue();
            lease!.Dispose();
        });
        source.ReleaseCount.ShouldBe(0);

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => source.ReleaseCount == 1);
    }

    [Fact]
    public async Task DetachReattachSameSelectionPreservesViewConfigurationButReloadsResource()
    {
        var source = new RecordingSource("reattach");
        var item = CreateItem("reattach", source);
        ImageGallery gallery = null!;
        Window window = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                ZoomMode = ImageGalleryZoomMode.Custom,
                CustomZoomFactor = 2,
                MainImagePrefetchMode = ImageGalleryMainImagePrefetchMode.Disabled,
                IsThumbnailFilmstripVisible = false,
                ItemsSource = new[] { item },
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });

        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.RotateClockwise();
            gallery.RotationAngle.ShouldBe(90);
            gallery.EffectiveZoomFactor.ShouldBe(2);
            window.Content = null;
            gallery.ImageState.ShouldBe(ImageGalleryImageState.Empty);
            gallery.RotationAngle.ShouldBe(90);
            gallery.EffectiveZoomFactor.ShouldBe(2);
            window.Content = gallery;
        });

        await WaitForStateAsync(gallery, ImageGalleryImageState.Ready);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery.SelectedItem.ShouldBeSameAs(item);
            gallery.RotationAngle.ShouldBe(90);
            gallery.EffectiveZoomFactor.ShouldBe(2);
        });
        source.CallCount.ShouldBeGreaterThanOrEqualTo(2);

        await Dispatcher.UIThread.InvokeAsync(window.Close);
        await EventuallyAsync(() => source.ReleaseCount == source.CallCount);
    }

    private static ImageGalleryItem CreateItem(object key, IImageGallerySource source) =>
        new() { Key = key, MainImageSource = source };

    private static async Task WaitForStateAsync(ImageGallery gallery, ImageGalleryImageState state) =>
        await EventuallyAsync(async () =>
            await Dispatcher.UIThread.InvokeAsync(() => gallery.ImageState == state));

    private static async Task EventuallyAsync(Func<bool> condition) =>
        await EventuallyAsync(() => Task.FromResult(condition()));

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

    private sealed class RecordingSource(object identity) : IImageGallerySource
    {
        private int _callCount;
        private int _releaseCount;

        public object Identity { get; } = identity;

        public int CallCount => Volatile.Read(ref _callCount);

        public int ReleaseCount => Volatile.Read(ref _releaseCount);

        public List<ImageGalleryImageRequest> Requests { get; } = [];

        public TestImage? LastImage { get; private set; }

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            lock (Requests)
            {
                Requests.Add(request);
            }

            Interlocked.Increment(ref _callCount);
            var size = request.TargetPixelSize ?? throw new InvalidOperationException("Main loads require a decode target.");
            var image = new TestImage(size.Width, size.Height);
            LastImage = image;
            return ValueTask.FromResult(ImageGalleryImageLease.Create(
                image,
                new PixelSize(8000, 6000),
                size,
                checked((long)size.Width * size.Height * 4),
                () => Interlocked.Increment(ref _releaseCount)));
        }
    }

    private sealed class ControlledSource(object identity) : IImageGallerySource
    {
        private readonly TaskCompletionSource<IImage> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public object Identity { get; } = identity;

        public void Complete(IImage image) => _completion.TrySetResult(image);

        public void Fail(Exception exception) => _completion.TrySetException(exception);

        public async ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            var image = await _completion.Task;
            var size = new PixelSize((int)image.Size.Width, (int)image.Size.Height);
            return ImageGalleryImageLease.Create(image, size, size, (long)size.Width * size.Height * 4, () => { });
        }
    }

    private sealed class SequencedSource(object identity) : IImageGallerySource
    {
        private readonly List<TaskCompletionSource<ImageGalleryImageLease>> _pending = [];
        private int _releaseCount;

        public object Identity { get; } = identity;

        public int CallCount
        {
            get
            {
                lock (_pending)
                {
                    return _pending.Count;
                }
            }
        }

        public int ReleaseCount => Volatile.Read(ref _releaseCount);

        public List<ImageGalleryImageRequest> Requests { get; } = [];

        public async ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<ImageGalleryImageLease>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_pending)
            {
                Requests.Add(request);
                _pending.Add(completion);
            }

            return await completion.Task.WaitAsync(cancellationToken);
        }

        public void Complete(int index)
        {
            TaskCompletionSource<ImageGalleryImageLease> completion;
            ImageGalleryImageRequest request;
            lock (_pending)
            {
                completion = _pending[index];
                request = Requests[index];
            }

            var size = request.TargetPixelSize ?? throw new InvalidOperationException();
            completion.TrySetResult(ImageGalleryImageLease.Create(
                new TestImage(size.Width, size.Height),
                new PixelSize(8000, 6000),
                size,
                checked((long)size.Width * size.Height * 4),
                () => Interlocked.Increment(ref _releaseCount)));
        }
    }

    private sealed class TestImage(double width, double height) : IImage
    {
        private int _drawCount;

        public Size Size { get; } = new(width, height);

        public int DrawCount => Volatile.Read(ref _drawCount);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) =>
            Interlocked.Increment(ref _drawCount);
    }
}
