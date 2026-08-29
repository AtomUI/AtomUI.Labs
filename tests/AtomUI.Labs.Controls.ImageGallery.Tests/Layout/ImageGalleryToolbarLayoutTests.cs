using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AtomUI.Labs.Controls.ImageGallery.Appearance;
using AtomUI.Labs.Controls.ImageGallery.Layout;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Layout;

public sealed class ImageGalleryToolbarLayoutTests
{
    public ImageGalleryToolbarLayoutTests()
    {
        AvaloniaTestApp.EnsureInitialized();
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Top, Orientation.Horizontal)]
    [InlineData(ImageGalleryEdgePlacement.Bottom, Orientation.Horizontal)]
    [InlineData(ImageGalleryEdgePlacement.Left, Orientation.Vertical)]
    [InlineData(ImageGalleryEdgePlacement.Right, Orientation.Vertical)]
    public async Task BuiltInToolbarFollowsEffectivePlacement(
        ImageGalleryEdgePlacement placement,
        Orientation expectedOrientation)
    {
        ImageGallery gallery = null!;
        Window window = null!;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                gallery = new ImageGallery
                {
                    Width = 720,
                    Height = 520,
                    ToolbarPlacement = placement,
                    IsThumbnailFilmstripVisible = false,
                };
                window = new Window { Width = 720, Height = 520, Content = gallery };
                window.Show();
            });
            await FlushAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                gallery.EffectiveToolbarPlacement.ShouldBe(placement);
                FindToolbarItems(gallery).Orientation.ShouldBe(expectedOrientation);
                FindToolbarZoomItems(gallery).Orientation.ShouldBe(expectedOrientation);
                gallery.IsEffectiveToolbarTitleVisible.ShouldBe(expectedOrientation == Orientation.Horizontal);
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

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Top, Orientation.Horizontal)]
    [InlineData(ImageGalleryEdgePlacement.Bottom, Orientation.Horizontal)]
    [InlineData(ImageGalleryEdgePlacement.Left, Orientation.Vertical)]
    [InlineData(ImageGalleryEdgePlacement.Right, Orientation.Vertical)]
    public async Task ToolbarContentsShareOneCenteredCrossAxis(
        ImageGalleryEdgePlacement placement,
        Orientation orientation)
    {
        Border customContent = null!;
        await AssertInWindowAsync(
            () =>
            {
                customContent = new Border
                {
                    Width = 28,
                    Height = 18,
                    Child = new TextBlock { Text = "Custom" },
                };
                var gallery = CreateGallery();
                gallery.IsThumbnailFilmstripVisible = false;
                gallery.ToolbarPlacement = placement;
                gallery.ToolbarContent = customContent;
                return gallery;
            },
            gallery =>
            {
                var toolbarItems = FindToolbarItems(gallery);
                var zoomItems = FindToolbarZoomItems(gallery);
                var contentPresenter = gallery.GetVisualDescendants()
                    .OfType<ContentPresenter>()
                    .Single(presenter => presenter.Classes.Contains("ImageGalleryToolbarContent"));

                toolbarItems.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
                toolbarItems.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
                zoomItems.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
                zoomItems.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
                contentPresenter.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
                contentPresenter.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
                contentPresenter.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Center);
                contentPresenter.VerticalContentAlignment.ShouldBe(VerticalAlignment.Center);

                foreach (var button in gallery.GetVisualDescendants()
                             .OfType<Button>()
                             .Where(button => button.Classes.Contains("ImageGalleryToolbarButton")))
                {
                    button.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
                    button.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
                    button.HorizontalContentAlignment.ShouldBe(HorizontalAlignment.Center);
                    button.VerticalContentAlignment.ShouldBe(VerticalAlignment.Center);
                }

                foreach (var text in gallery.GetVisualDescendants()
                             .OfType<ImageGalleryToolbarText>()
                             .Where(text =>
                                 text.Classes.Contains("ImageGalleryToolbarTitle") ||
                                 text.Classes.Contains("ImageGalleryToolbarZoomPercentage")))
                {
                    text.HorizontalAlignment.ShouldBe(HorizontalAlignment.Center);
                    text.VerticalAlignment.ShouldBe(VerticalAlignment.Center);
                }

                var visibleRoles = gallery.GetVisualDescendants()
                    .OfType<Control>()
                    .Where(control => control.IsVisible &&
                                      (control.Classes.Contains("ImageGalleryToolbarButton") ||
                                       control.Classes.Contains("ImageGalleryToolbarTitle") ||
                                       control.Classes.Contains("ImageGalleryToolbarZoomPercentage") ||
                                       ReferenceEquals(control, customContent)))
                    .ToArray();
                visibleRoles.ShouldNotBeEmpty();
                foreach (var role in visibleRoles)
                {
                    var center = CenterRelativeTo(role, toolbarItems);
                    var crossAxisDelta = orientation == Orientation.Horizontal
                        ? center.Y - toolbarItems.Bounds.Height / 2
                        : center.X - toolbarItems.Bounds.Width / 2;
                    Math.Abs(crossAxisDelta).ShouldBeLessThan(0.51);
                }
            });
    }

    [Fact]
    public async Task HorizontalToolbarUsesCenteredVectorGlyphsAndOpticallyCenteredLabels()
    {
        await AssertInWindowAsync(
            () =>
            {
                var gallery = CreateGallery();
                gallery.IsThumbnailFilmstripVisible = false;
                gallery.ToolbarPlacement = ImageGalleryEdgePlacement.Top;
                return gallery;
            },
            gallery =>
            {
                var toolbarItems = FindToolbarItems(gallery);
                var toolbarButtons = toolbarItems.GetVisualDescendants()
                    .OfType<Button>()
                    .Where(button => button.Classes.Contains("ImageGalleryToolbarButton"))
                    .ToArray();
                var glyphButtons = toolbarButtons
                    .Where(button => button.Content is ImageGalleryGlyph)
                    .ToArray();

                glyphButtons.Length.ShouldBe(3);
                glyphButtons
                    .Select(button => ((ImageGalleryGlyph)button.Content!).Kind)
                    .ShouldBe(new[]
                    {
                        ImageGalleryGlyphKind.Subtract,
                        ImageGalleryGlyphKind.Add,
                        ImageGalleryGlyphKind.RotateClockwise,
                    });
                foreach (var button in toolbarButtons)
                {
                    (button.Content is string).ShouldBeFalse();
                }

                foreach (var button in glyphButtons)
                {
                    var glyph = (ImageGalleryGlyph)button.Content!;
                    glyph.Bounds.Width.ShouldBeGreaterThan(0);
                    glyph.Bounds.Height.ShouldBeGreaterThan(0);
                    var center = glyph.TranslatePoint(
                        new Point(glyph.Bounds.Width / 2, glyph.Bounds.Height / 2),
                        button).ShouldNotBeNull();
                    center.X.ShouldBe(button.Bounds.Width / 2, tolerance: 0.01);
                    center.Y.ShouldBe(button.Bounds.Height / 2, tolerance: 0.01);
                }

                var labels = toolbarItems.GetVisualDescendants()
                    .OfType<ImageGalleryToolbarText>()
                    .Where(label => label.IsVisible)
                    .ToArray();
                labels.Length.ShouldBe(4);
                foreach (var label in labels)
                {
                    label.GetRenderedInkCenterY()
                        .ShouldBe(label.Bounds.Height / 2, tolerance: 0.01);
                }
            });
    }

    [Fact]
    public void OpticalTextOriginCentersVisibleInkInsteadOfTheFontLineBox()
    {
        const double containerHeight = 34;
        const double layoutHeight = 20;
        const double inkExtent = 16;
        const double overhangAfter = 1;

        var origin = ImageGalleryToolbarText.CalculateOpticalOriginY(
            containerHeight,
            layoutHeight,
            inkExtent,
            overhangAfter);
        var inkTop = layoutHeight + overhangAfter - inkExtent;

        (origin + inkTop + inkExtent / 2).ShouldBe(containerHeight / 2);
        origin.ShouldNotBe((containerHeight - layoutHeight) / 2);
    }

    [Fact]
    public async Task SameEdgeConflictUsesOppositeEdgeOrientationWithoutMutatingCustomContent()
    {
        ImageGallery gallery = null!;
        StackPanel customContent = null!;
        Window window = null!;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                customContent = new StackPanel { Orientation = Orientation.Horizontal };
                gallery = new ImageGallery
                {
                    Width = 720,
                    Height = 520,
                    ToolbarPlacement = ImageGalleryEdgePlacement.Left,
                    ThumbnailFilmstripPlacement = ImageGalleryEdgePlacement.Left,
                    ToolbarContent = customContent,
                    ItemsSource = new[]
                    {
                        new ImageGalleryItem
                        {
                            Key = 1,
                            MainImageSource = new FailingSource(),
                        },
                    },
                };
                window = new Window { Width = 720, Height = 520, Content = gallery };
                window.Show();
            });
            await FlushAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                gallery.EffectiveToolbarPlacement.ShouldBe(ImageGalleryEdgePlacement.Right);
                FindToolbarItems(gallery).Orientation.ShouldBe(Orientation.Vertical);
                FindToolbarZoomItems(gallery).Orientation.ShouldBe(Orientation.Vertical);
                gallery.IsEffectiveToolbarTitleVisible.ShouldBeFalse();
                FindToolbarTitle(gallery).IsVisible.ShouldBeFalse();
                customContent.Orientation.ShouldBe(Orientation.Horizontal);
                AssertViewportNavigationVisibility(gallery, previousVisible: false, nextVisible: false);
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

    [Theory]
    [InlineData(0.429, "42%")]
    [InlineData(1, "100%")]
    [InlineData(1.999, "199%")]
    [InlineData(2, "200%")]
    public void ZoomPercentageDropsFractionalDigitsWithoutRounding(double zoomFactor, string expected)
    {
        ImageGallery.FormatZoomPercentage(zoomFactor).ShouldBe(expected);
    }

    [Fact]
    public async Task VerticalToolbarIgnoresConfiguredTitleVisibilityAndHorizontalToolbarRestoresIt()
    {
        await AssertInWindowAsync(
            () =>
            {
                var gallery = CreateGallery();
                gallery.IsThumbnailFilmstripVisible = false;
                gallery.IsToolbarTitleVisible = true;
                gallery.ToolbarPlacement = ImageGalleryEdgePlacement.Left;
                return gallery;
            },
            gallery =>
            {
                gallery.IsToolbarTitleVisible.ShouldBeTrue();
                gallery.IsEffectiveToolbarTitleVisible.ShouldBeFalse();
                FindToolbarTitle(gallery).IsVisible.ShouldBeFalse();

                gallery.ToolbarPlacement = ImageGalleryEdgePlacement.Top;

                gallery.IsEffectiveToolbarTitleVisible.ShouldBeTrue();
                FindToolbarTitle(gallery).IsVisible.ShouldBeTrue();
            });
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Top, true, true)]
    [InlineData(ImageGalleryEdgePlacement.Bottom, true, true)]
    [InlineData(ImageGalleryEdgePlacement.Left, false, true)]
    [InlineData(ImageGalleryEdgePlacement.Right, true, false)]
    public async Task FilmstripOnSideHidesSameSideViewportNavigation(
        ImageGalleryEdgePlacement placement,
        bool previousVisible,
        bool nextVisible)
    {
        await AssertInWindowAsync(
            () =>
            {
                var gallery = CreateGallery();
                gallery.ThumbnailFilmstripPlacement = placement;
                return gallery;
            },
            gallery => AssertViewportNavigationVisibility(gallery, previousVisible, nextVisible));
    }

    [Theory]
    [InlineData(ImageGalleryEdgePlacement.Top, true, true)]
    [InlineData(ImageGalleryEdgePlacement.Bottom, true, true)]
    [InlineData(ImageGalleryEdgePlacement.Left, false, true)]
    [InlineData(ImageGalleryEdgePlacement.Right, true, false)]
    public async Task ToolbarOnSideHidesSameSideViewportNavigation(
        ImageGalleryEdgePlacement placement,
        bool previousVisible,
        bool nextVisible)
    {
        await AssertInWindowAsync(
            () =>
            {
                var gallery = CreateGallery();
                gallery.ThumbnailFilmstripPlacement = ImageGalleryEdgePlacement.Top;
                gallery.ToolbarPlacement = placement;
                return gallery;
            },
            gallery => AssertViewportNavigationVisibility(gallery, previousVisible, nextVisible));
    }

    [Fact]
    public async Task EffectiveVisibilityAndNavigationSwitchRestoreSideButtonsAtRuntime()
    {
        ImageGallery gallery = null!;
        Window window = null!;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                gallery = CreateGallery();
                gallery.ThumbnailFilmstripPlacement = ImageGalleryEdgePlacement.Left;
                gallery.ToolbarPlacement = ImageGalleryEdgePlacement.Top;
                window = new Window { Width = 720, Height = 520, Content = gallery };
                window.Show();
            });
            await FlushAsync();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AssertViewportNavigationVisibility(gallery, previousVisible: false, nextVisible: true);
                gallery.IsThumbnailFilmstripVisible = false;
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AssertViewportNavigationVisibility(gallery, previousVisible: true, nextVisible: true);
                gallery.IsThumbnailFilmstripVisible = true;
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AssertViewportNavigationVisibility(gallery, previousVisible: false, nextVisible: true);
                gallery.IsViewportNavigationEnabled = false;
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AssertViewportNavigationVisibility(gallery, previousVisible: false, nextVisible: false);
                gallery.IsViewportNavigationEnabled = true;
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
                AssertViewportNavigationVisibility(gallery, previousVisible: false, nextVisible: true));
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
    public async Task StructurallyHiddenSideOverlaysDoNotHideViewportNavigation()
    {
        await AssertInWindowAsync(
            () => new ImageGallery
            {
                Width = 720,
                Height = 520,
                ThumbnailFilmstripPlacement = ImageGalleryEdgePlacement.Left,
                ToolbarPlacement = ImageGalleryEdgePlacement.Right,
            },
            gallery => AssertViewportNavigationVisibility(gallery, previousVisible: true, nextVisible: true));
    }

    private static ImageGalleryToolbarPanel FindToolbarItems(ImageGallery gallery) =>
        gallery.GetVisualDescendants()
            .OfType<ImageGalleryToolbarPanel>()
            .Single(panel => panel.Name == "PART_ToolbarItems");

    private static ImageGalleryToolbarPanel FindToolbarZoomItems(ImageGallery gallery) =>
        gallery.GetVisualDescendants()
            .OfType<ImageGalleryToolbarPanel>()
            .Single(panel => panel.Classes.Contains("ImageGalleryToolbarZoomItems"));

    private static ImageGalleryToolbarText FindToolbarTitle(ImageGallery gallery) =>
        gallery.GetVisualDescendants()
            .OfType<ImageGalleryToolbarText>()
            .Single(text => text.Classes.Contains("ImageGalleryToolbarTitle"));

    private static Point CenterRelativeTo(Control control, Control relativeTo) =>
        control.TranslatePoint(
            new Point(control.Bounds.Width / 2, control.Bounds.Height / 2),
            relativeTo) ?? throw new InvalidOperationException("Toolbar role is not connected to its toolbar host.");

    private static ImageGallery CreateGallery() => new()
    {
        Width = 720,
        Height = 520,
        ItemsSource = new[]
        {
            new ImageGalleryItem
            {
                Key = 1,
                Title = "Configured title",
                MainImageSource = new FailingSource(),
            },
            new ImageGalleryItem
            {
                Key = 2,
                MainImageSource = new FailingSource(),
            },
        },
    };

    private static void AssertViewportNavigationVisibility(
        ImageGallery gallery,
        bool previousVisible,
        bool nextVisible)
    {
        gallery.IsEffectivePreviousViewportNavigationVisible.ShouldBe(previousVisible);
        gallery.IsEffectiveNextViewportNavigationVisible.ShouldBe(nextVisible);
        FindViewportNavigation(gallery, "Previous").IsVisible.ShouldBe(previousVisible);
        FindViewportNavigation(gallery, "Next").IsVisible.ShouldBe(nextVisible);
    }

    private static Button FindViewportNavigation(ImageGallery gallery, string role) =>
        gallery.GetVisualDescendants()
            .OfType<Button>()
            .Single(button =>
                button.Classes.Contains("ImageGalleryViewportNavigation") && button.Classes.Contains(role));

    private static async Task AssertInWindowAsync(
        Func<ImageGallery> createGallery,
        Action<ImageGallery> assertion)
    {
        ImageGallery gallery = null!;
        Window window = null!;
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                gallery = createGallery();
                window = new Window { Width = 720, Height = 520, Content = gallery };
                window.Show();
            });
            await FlushAsync();
            await Dispatcher.UIThread.InvokeAsync(() => assertion(gallery));
        }
        finally
        {
            if (window is not null)
            {
                await Dispatcher.UIThread.InvokeAsync(window.Close);
            }
        }
    }

    private static async Task FlushAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
        await Task.Delay(20, TestContext.Current.CancellationToken);
    }

    private sealed class FailingSource : IImageGallerySource
    {
        public object Identity { get; } = new();

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<ImageGalleryImageLease>(new InvalidOperationException("Expected test failure."));
    }
}
