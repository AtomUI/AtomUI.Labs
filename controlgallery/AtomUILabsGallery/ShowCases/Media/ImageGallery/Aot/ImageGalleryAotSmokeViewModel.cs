using AtomUI.Labs.Controls.ImageGallery;

namespace AtomUILabsGallery.ShowCases.ImageGallery.Aot;

public sealed class ImageGalleryAotSmokeViewModel
{
    public ImageGalleryAotSmokeViewModel()
    {
        var resource = ImageGallerySources.FromAvaloniaResource(
            new Uri("avares://AtomUILabsGallery/Assets/atomui-labs.png"));
        Items = Enumerable.Range(0, 1_000)
            .Select(index => (IImageGalleryItem)new ImageGalleryItem
            {
                Key = $"aot-{index}",
                Title = $"NativeAOT image {index}",
                MainImageSource = index == 1 ? new ExpectedFailureSource(index) : resource,
            })
            .ToArray();
    }

    public IReadOnlyList<IImageGalleryItem> Items { get; }

    private sealed class ExpectedFailureSource(object identity) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<ImageGalleryImageLease>(
                new InvalidDataException("Expected NativeAOT smoke failure source."));
    }
}
