using System.Collections.ObjectModel;
using System.Windows.Input;
using AtomUI.Labs.Controls.ImageGallery.Appearance;
using AtomUI.Theme;
using AtomUI.Theme.Language;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Appearance;

public sealed class ImageGalleryAppearanceAndLocalizationTests
{
    public ImageGalleryAppearanceAndLocalizationTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Fact]
    public async Task SparseAppearanceUpdatesAtRuntimeAndReplacementUnsubscribesOldObject()
    {
        ImageGallery gallery = null!;
        Window window = null!;
        ImageGalleryToolbarAppearance first = null!;
        ImageGalleryToolbarAppearance second = null!;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            first = new ImageGalleryToolbarAppearance { Background = Brushes.Red };
            second = new ImageGalleryToolbarAppearance { Background = Brushes.Green };
            gallery = new ImageGallery
            {
                Width = 640,
                Height = 480,
                ToolbarAppearance = first,
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });
        await FlushAsync();

        (await GetToolbarBackgroundAsync(gallery)).ShouldBeSameAs(Brushes.Red);
        await Dispatcher.UIThread.InvokeAsync(() => first.Background = Brushes.Blue);
        (await GetToolbarBackgroundAsync(gallery)).ShouldBeSameAs(Brushes.Blue);

        await Dispatcher.UIThread.InvokeAsync(() => gallery.ToolbarAppearance = second);
        (await GetToolbarBackgroundAsync(gallery)).ShouldBeSameAs(Brushes.Green);
        await Dispatcher.UIThread.InvokeAsync(() => first.Background = Brushes.Yellow);
        (await GetToolbarBackgroundAsync(gallery)).ShouldBeSameAs(Brushes.Green);

        await Dispatcher.UIThread.InvokeAsync(() => gallery.ToolbarAppearance = null);
        (await GetToolbarBackgroundAsync(gallery)).ShouldNotBeSameAs(Brushes.Green);
        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    [Fact]
    public async Task EmptyCollectionHidesFilmstripAndAddCommandStateIsForwarded()
    {
        var items = new ObservableCollection<IImageGalleryItem>();
        var addCommand = new ToggleCommand();
        ImageGallery gallery = null!;
        Window window = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            gallery = new ImageGallery
            {
                ItemsSource = items,
                AddImageCommand = addCommand,
            };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });
        await FlushAsync();

        (await Dispatcher.UIThread.InvokeAsync(() => gallery.IsEffectiveFilmstripVisible)).ShouldBeFalse();
        (await Dispatcher.UIThread.InvokeAsync(() => gallery.IsAddImageButtonEnabled)).ShouldBeFalse();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            items.Add(new ImageGalleryItem
            {
                Key = 1,
                MainImageSource = new EmptySource(1),
            });
            addCommand.SetEnabled(true);
        });
        await FlushAsync();
        (await Dispatcher.UIThread.InvokeAsync(() => gallery.IsEffectiveFilmstripVisible)).ShouldBeTrue();
        (await Dispatcher.UIThread.InvokeAsync(() => gallery.IsAddImageButtonEnabled)).ShouldBeTrue();

        await Dispatcher.UIThread.InvokeAsync(items.Clear);
        (await Dispatcher.UIThread.InvokeAsync(() => gallery.IsEffectiveFilmstripVisible)).ShouldBeFalse();
        await Dispatcher.UIThread.InvokeAsync(window.Close);
    }

    [Fact]
    public async Task ToolbarButtonAppearanceOwnsRenderedPointerPressedAndDisabledStates()
    {
        SolidColorBrush normalBackground = null!;
        SolidColorBrush normalForeground = null!;
        SolidColorBrush hoverBackground = null!;
        SolidColorBrush hoverForeground = null!;
        SolidColorBrush pressedBackground = null!;
        SolidColorBrush pressedForeground = null!;
        SolidColorBrush disabledBackground = null!;
        SolidColorBrush disabledForeground = null!;
        ImageGallery gallery = null!;
        Window window = null!;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                normalBackground = new SolidColorBrush(Colors.Black);
                normalForeground = new SolidColorBrush(Colors.White);
                hoverBackground = new SolidColorBrush(Colors.DarkSlateBlue);
                hoverForeground = new SolidColorBrush(Colors.Gold);
                pressedBackground = new SolidColorBrush(Colors.Navy);
                pressedForeground = new SolidColorBrush(Colors.LightCyan);
                disabledBackground = new SolidColorBrush(Colors.DimGray);
                disabledForeground = new SolidColorBrush(Colors.LightGray);
                var appearance = new ImageGalleryButtonAppearance
                {
                    Background = normalBackground,
                    Foreground = normalForeground,
                    PointerOverBackground = hoverBackground,
                    PointerOverForeground = hoverForeground,
                    PressedBackground = pressedBackground,
                    PressedForeground = pressedForeground,
                    DisabledBackground = disabledBackground,
                    DisabledForeground = disabledForeground,
                    DisabledOpacity = 0.8,
                };
                gallery = CreateGallery(appearance, appearance);
                window = new Window { Width = 720, Height = 520, Content = gallery };
                window.Show();
            });
            await FlushAsync();

            ImageGalleryButton button = null!;
            ContentPresenter presenter = null!;
            Point point = default;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                button = gallery.GetVisualDescendants()
                    .OfType<ImageGalleryButton>()
                    .First(control => control.Classes.Contains("ImageGalleryToolbarButton") && control.IsEffectivelyEnabled);
                presenter = GetButtonPresenter(button);
                point = GetCenterPoint(button, window);
                presenter.Background.ShouldBeSameAs(normalBackground);
                presenter.Foreground.ShouldBeSameAs(normalForeground);
                window.MouseMove(point);
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                presenter.Background.ShouldBeSameAs(hoverBackground);
                presenter.Foreground.ShouldBeSameAs(hoverForeground);
                window.MouseDown(point, MouseButton.Left);
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                presenter.Background.ShouldBeSameAs(pressedBackground);
                presenter.Foreground.ShouldBeSameAs(pressedForeground);
                window.MouseUp(point, MouseButton.Left);
                button.IsEnabled = false;
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                presenter.Background.ShouldBeSameAs(disabledBackground);
                presenter.Foreground.ShouldBeSameAs(disabledForeground);
                button.Opacity.ShouldBe(0.8);
            });
        }
        finally
        {
            if (window is not null)
            {
                await Dispatcher.UIThread.InvokeAsync(window.Close);
            }
        }
    }

    [Fact]
    public async Task ViewportNavigationButtonsRemainSymmetricVisibleAndNextClickChangesSelection()
    {
        SolidColorBrush normalBackground = null!;
        SolidColorBrush normalForeground = null!;
        SolidColorBrush hoverBackground = null!;
        SolidColorBrush hoverForeground = null!;
        SolidColorBrush disabledBackground = null!;
        SolidColorBrush disabledForeground = null!;
        ImageGallery gallery = null!;
        Window window = null!;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                normalBackground = new SolidColorBrush(Colors.Black);
                normalForeground = new SolidColorBrush(Colors.White);
                hoverBackground = new SolidColorBrush(Colors.DarkSlateGray);
                hoverForeground = new SolidColorBrush(Colors.WhiteSmoke);
                disabledBackground = new SolidColorBrush(Colors.Gray);
                disabledForeground = new SolidColorBrush(Colors.Gainsboro);
                var appearance = new ImageGalleryButtonAppearance
                {
                    Background = normalBackground,
                    Foreground = normalForeground,
                    PointerOverBackground = hoverBackground,
                    PointerOverForeground = hoverForeground,
                    PressedBackground = hoverBackground,
                    PressedForeground = hoverForeground,
                    DisabledBackground = disabledBackground,
                    DisabledForeground = disabledForeground,
                    Width = 48,
                    Height = 64,
                    IconSize = 30,
                };
                gallery = CreateGallery(null, appearance);
                window = new Window { Width = 720, Height = 520, Content = gallery };
                window.Show();
            });
            await FlushAsync();

            ImageGalleryButton previous = null!;
            ImageGalleryButton next = null!;
            ContentPresenter previousPresenter = null!;
            ContentPresenter nextPresenter = null!;
            ImageGalleryGlyph nextGlyph = null!;
            Point nextPoint = default;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                gallery.SelectedIndex = 1;
                previous = FindViewportNavigation(gallery, "Previous");
                next = FindViewportNavigation(gallery, "Next");
                previous.Bounds.Size.ShouldBe(next.Bounds.Size);
                previous.Bounds.Width.ShouldBe(48);
                previous.Bounds.Height.ShouldBe(64);
                previous.IsVisible.ShouldBeTrue();
                next.IsVisible.ShouldBeTrue();
                previous.IsEffectivelyEnabled.ShouldBeTrue();
                next.IsEffectivelyEnabled.ShouldBeTrue();
                previous.IsPointerOver.ShouldBeFalse();
                next.IsPointerOver.ShouldBeFalse();
                previous.Opacity.ShouldBe(1);
                next.Opacity.ShouldBe(1);
                previousPresenter = GetButtonPresenter(previous);
                nextPresenter = GetButtonPresenter(next);
                previousPresenter.Background.ShouldBeSameAs(normalBackground);
                previousPresenter.Foreground.ShouldBeSameAs(normalForeground);
                nextGlyph = next.GetVisualDescendants().OfType<ImageGalleryGlyph>().Single();
                nextPresenter.Background.ShouldBeSameAs(normalBackground);
                nextPresenter.Foreground.ShouldBeSameAs(normalForeground);
                nextGlyph.Foreground.ShouldBeSameAs(normalForeground);
                nextGlyph.Bounds.Width.ShouldBe(30, tolerance: 0.01);
                nextGlyph.Bounds.Height.ShouldBe(30, tolerance: 0.01);
                var glyphCenter = nextGlyph.TranslatePoint(
                    new Point(nextGlyph.Bounds.Width / 2, nextGlyph.Bounds.Height / 2),
                    next).ShouldNotBeNull();
                glyphCenter.X.ShouldBe(next.Bounds.Width / 2, tolerance: 0.01);
                glyphCenter.Y.ShouldBe(next.Bounds.Height / 2, tolerance: 0.01);
                nextPoint = GetCenterPoint(next, window);
                window.MouseMove(nextPoint);
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                nextPresenter.Background.ShouldBeSameAs(hoverBackground);
                nextPresenter.Foreground.ShouldBeSameAs(hoverForeground);
                nextGlyph.Foreground.ShouldBeSameAs(hoverForeground);
                window.MouseDown(nextPoint, MouseButton.Left);
                window.MouseUp(nextPoint, MouseButton.Left);
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                gallery.SelectedIndex.ShouldBe(2);
                next.IsEffectivelyEnabled.ShouldBeFalse();
                nextPresenter.Background.ShouldBeSameAs(disabledBackground);
                nextPresenter.Foreground.ShouldBeSameAs(disabledForeground);
            });
        }
        finally
        {
            if (window is not null)
            {
                await Dispatcher.UIThread.InvokeAsync(window.Close);
            }
        }
    }

    [Fact]
    public void GlyphGeometryCenterUsesTheControlsLocalCoordinateSpace()
    {
        var size = new Size(30, 30);

        var center = ImageGalleryGlyph.CalculateLocalCenter(size);

        center.ShouldBe(new Point(15, 15));
        center.ShouldNotBe(new Rect(9, 13, size.Width, size.Height).Center);
    }

    [Fact]
    public async Task AtomUiLanguageVariantSwitchUpdatesCompiledGalleryResources()
    {
        ImageGallery gallery = null!;
        Window window = null!;
        IThemeManager themeManager = null!;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            themeManager = Application.Current!.Styles.OfType<IThemeManager>().Single();
            themeManager.LanguageVariant = LanguageVariant.zh_CN;
            gallery = new ImageGallery { Width = 640, Height = 480 };
            window = new Window { Width = 640, Height = 480, Content = gallery };
            window.Show();
        });
        await FlushAsync();

        (await GetEmptyTextAsync(gallery)).ShouldBe("暂无图片");
        await Dispatcher.UIThread.InvokeAsync(() => themeManager.LanguageVariant = LanguageVariant.en_US);
        await FlushAsync();
        (await GetEmptyTextAsync(gallery)).ShouldBe("No images");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            themeManager.LanguageVariant = LanguageVariant.zh_CN;
            window.Close();
        });
    }

    private static async Task<IBrush?> GetToolbarBackgroundAsync(ImageGallery gallery) =>
        await Dispatcher.UIThread.InvokeAsync(() =>
            gallery.GetVisualDescendants().OfType<Border>().Single(border => border.Name == "PART_Toolbar").Background);

    private static async Task<string?> GetEmptyTextAsync(ImageGallery gallery) =>
        await Dispatcher.UIThread.InvokeAsync(() =>
            gallery.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Name == "PART_EmptyText").Text);

    private static ImageGallery CreateGallery(
        ImageGalleryButtonAppearance? toolbarAppearance,
        ImageGalleryButtonAppearance? viewportAppearance) => new()
    {
        Width = 720,
        Height = 520,
        ToolbarButtonAppearance = toolbarAppearance,
        ViewportNavigationButtonAppearance = viewportAppearance,
        ItemsSource = Enumerable.Range(0, 3)
            .Select(index => (IImageGalleryItem)new ImageGalleryItem
            {
                Key = index,
                Title = $"Image {index}",
                MainImageSource = new ReadySource(index),
            })
            .ToArray(),
    };

    private static ImageGalleryButton FindViewportNavigation(ImageGallery gallery, string role) =>
        gallery.GetVisualDescendants()
            .OfType<ImageGalleryButton>()
            .Single(button =>
                button.Classes.Contains("ImageGalleryViewportNavigation") && button.Classes.Contains(role));

    private static ContentPresenter GetButtonPresenter(ImageGalleryButton button) =>
        button.GetVisualDescendants()
            .OfType<ContentPresenter>()
            .Single(presenter => presenter.Name == "PART_ContentPresenter");

    private static Point GetCenterPoint(Control control, Window window) =>
        control.TranslatePoint(
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2),
            window) ?? throw new InvalidOperationException("Could not translate the button into the test window.");

    private static async Task FlushAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        await Task.Delay(20, TestContext.Current.CancellationToken);
    }

    private sealed class ToggleCommand : ICommand
    {
        private bool _enabled;

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _enabled;

        public void Execute(object? parameter)
        {
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class EmptySource(object identity) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<ImageGalleryImageLease>(new InvalidOperationException("Expected test failure."));
    }

    private sealed class ReadySource(object identity) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
        public Size Size { get; } = new(width, height);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
        }
    }
}
