using System.Diagnostics;
using AtomUI.Labs.Controls.ImageGallery;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AtomUILabsGallery.ShowCases.ImageGallery.Aot;

public partial class ImageGalleryAotSmokeView : UserControl
{
    public ImageGalleryAotSmokeView()
    {
        DataContext = new ImageGalleryAotSmokeViewModel();
        InitializeComponent();
    }

    public async Task<IReadOnlyList<ImageGallerySmokeCheck>> RunAsync(
        ContentControl host,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        var checks = new List<ImageGallerySmokeCheck>();
        await CheckAsync("InitialReady", async () =>
        {
            await WaitForStateAsync(ImageGalleryImageState.Ready, deadline, cancellationToken);
            return Gallery.SelectedIndex == 0;
        }, checks);
        await CheckAsync("TemplateAndLayout", () =>
        {
            var required = new[]
            {
                "PART_Viewport", "PART_Toolbar", "PART_Filmstrip", "PART_ScrollViewer", "PART_ItemsPresenter"
            };
            var visuals = Gallery.GetVisualDescendants().OfType<Control>().ToArray();
            return Task.FromResult(required.All(name =>
                visuals.Any(control => control.Name == name && control.Bounds.Width > 0 && control.Bounds.Height > 0)));
        }, checks);
        await CheckAsync("ResourceOnlyLease", () =>
        {
            var item = Gallery.SelectedItem as IImageGalleryItem;
            Gallery.MainImageDecodeSizeHint = new Avalonia.PixelSize(1024, 768);
            Gallery.MainImageMode = ImageGalleryMainImageMode.ResourceOnly;
            ImageGalleryImageLease? lease = null;
            var acquired = item is not null && Gallery.TryAcquireCurrentImage(item, out lease);
            var valid = acquired && lease is not null && lease.DecodedPixelSize.Width > 0;
            lease?.Dispose();
            Gallery.MainImageMode = ImageGalleryMainImageMode.Presented;
            return Task.FromResult(valid && Gallery.ImageState == ImageGalleryImageState.Ready);
        }, checks);
        await CheckAsync("VirtualizedThousandItems", () =>
        {
            var realized = Gallery.GetRealizedContainers().Count();
            return Task.FromResult(realized is > 0 and < 40);
        }, checks);
        await CheckAsync("StableNavigationCommands", async () =>
        {
            Gallery.NextCommand.Execute(null);
            await WaitForStateAsync(ImageGalleryImageState.Error, deadline, cancellationToken);
            Gallery.NextCommand.Execute(null);
            await WaitForStateAsync(ImageGalleryImageState.Ready, deadline, cancellationToken);
            Gallery.PreviousCommand.Execute(null);
            await WaitForStateAsync(ImageGalleryImageState.Error, deadline, cancellationToken);
            return Gallery.SelectedIndex == 1 && Gallery.ImageState == ImageGalleryImageState.Error;
        }, checks);
        await CheckAsync("ViewportCommands", async () =>
        {
            Gallery.NextCommand.Execute(null);
            await WaitForStateAsync(ImageGalleryImageState.Ready, deadline, cancellationToken);
            Gallery.ZoomInCommand.Execute(null);
            Gallery.RotateClockwise();
            var valid = Gallery.EffectiveZoomFactor > 0 && Gallery.RotationAngle == 90;
            Gallery.FitCommand.Execute(null);
            Gallery.ActualSizeCommand.Execute(null);
            return valid && Gallery.EffectiveZoomFactor > 0;
        }, checks);
        await CheckAsync("FarSelection", async () =>
        {
            Gallery.SelectedIndex = 999;
            await WaitForStateAsync(ImageGalleryImageState.Ready, deadline, cancellationToken);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            return Gallery.SelectedIndex == 999 && Gallery.GetRealizedContainers().Count() < 40;
        }, checks);
        await CheckAsync("DetachAttach", async () =>
        {
            host.Content = null;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
            host.Content = this;
            await WaitForStateAsync(ImageGalleryImageState.Ready, deadline, cancellationToken);
            return Gallery.SelectedIndex == 999;
        }, checks);
        return checks;
    }

    private async Task WaitForStateAsync(
        ImageGalleryImageState state,
        DateTime deadline,
        CancellationToken cancellationToken)
    {
        while (Gallery.ImageState != state)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"ImageGallery did not enter {state}; actual state is {Gallery.ImageState}.");
            }

            await Task.Delay(20, cancellationToken);
        }
    }

    private static async Task CheckAsync(
        string name,
        Func<Task<bool>> action,
        ICollection<ImageGallerySmokeCheck> checks)
    {
        var stopwatch = Stopwatch.StartNew();
        var passed = await action();
        stopwatch.Stop();
        checks.Add(new ImageGallerySmokeCheck(name, passed, stopwatch.Elapsed));
        if (!passed)
        {
            throw new ImageGallerySmokeAssertionException(name);
        }
    }
}

public sealed record ImageGallerySmokeCheck(string Name, bool Passed, TimeSpan Duration);

public sealed class ImageGallerySmokeAssertionException(string checkName)
    : Exception($"ImageGallery NativeAOT smoke check '{checkName}' failed.");
