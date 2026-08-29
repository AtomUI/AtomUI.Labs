using System.Collections.ObjectModel;
using System.Windows.Input;
using AtomUI.Controls;
using AtomUI.Labs.Controls.ImageGallery;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using ReactiveUI;

namespace AtomUILabsGallery.ShowCases.ImageGallery;

public sealed class ImageGalleryViewModel : ReactiveObject, IRoutableViewModel
{
    private static readonly GalleryAsset[] s_galleryAssets =
    [
        new("ahmetyuksek-autumn-bend-10069119_1920.jpg", "Autumn bend — Ahmetyuksek"),
        new("alexas_fotos-bee-10382439_1920.jpg", "Bee — Alexas Fotos"),
        new("alexas_fotos-soap-bubble-10379655_1920.jpg", "Soap bubble — Alexas Fotos"),
        new("beto_mdp-hummingbird-10407977_1920.jpg", "Hummingbird — Beto MDP"),
        new("christels-bee-9705160_1920.jpg", "Bee — Christels"),
        new("citroenaut-nature-10414523_1920.jpg", "Nature — Citroenaut"),
        new("davidclode-nature-10426343_1920.jpg", "Nature — David Clode"),
        new("engin_akyurt-butterfly-10427082_1920.jpg", "Butterfly — Engin Akyurt"),
        new("georg_wietschorke-swallow-10423828_1920.jpg", "Swallow — Georg Wietschorke"),
        new("joshwiiieee-black-cat-10388436_1920.jpg", "Black cat — Joshwiiieee"),
        new("joshwiiieee-pine-10388366_1920.jpg", "Pine — Joshwiiieee"),
        new("kodl68-forest-10394495_1920.jpg", "Forest — Kodl68"),
        new("mamunsheikh121-lizard-10389913_1920.jpg", "Lizard — Mamun Sheikh"),
        new("manseok_kim-tulip-10404468_1920.jpg", "Tulip — Manseok Kim"),
        new("mbrajeev-bird-10386158_1920.jpg", "Bird — Mbrajeev"),
        new("mbrajeev-nature-10425350_1920.jpg", "Nature — Mbrajeev"),
        new("sergei_spas-dew-10415661_1920.jpg", "Dew — Sergei Spas"),
        new("suju-foto-nature-10402711_1920.jpg", "Nature — Suju Foto"),
        new("wolfgang_hasselmann-desert-10407209_1920.jpg", "Desert — Wolfgang Hasselmann"),
        new("wvrede-fog-10401662_1920.jpg", "Fog — Wvrede"),
    ];

    private static readonly Color[] s_palette =
    [
        Color.Parse("#246BFD"),
        Color.Parse("#7C3AED"),
        Color.Parse("#0F9D78"),
        Color.Parse("#E15C3A"),
        Color.Parse("#C58A16"),
        Color.Parse("#1C7ED6"),
        Color.Parse("#D6336C"),
        Color.Parse("#5C940D"),
    ];

    public static EntityKey ID => "ImageGalleryShowCase";

    public ImageGalleryViewModel(IScreen hostScreen)
    {
        HostScreen = hostScreen;
        Images = new ObservableCollection<IImageGalleryItem>(
            Enumerable.Range(0, s_galleryAssets.Length)
                .Select(index => CreateResourceItem(index, index)));
        ErrorImages =
        [
            new ImageGalleryItem
            {
                Key = "broken-image",
                Title = "Unavailable image",
                MainImageSource = new GalleryFailureSource("broken-image"),
            },
            CreateResourceItem(0, 0),
        ];
        LargeImages = CreateSyntheticItems(10_000).ToArray();
        AddImageCommand = new DelegateCommand(AddImage);
    }

    public IScreen HostScreen { get; }

    public string UrlPathSegment => ID.ToString();

    public ObservableCollection<IImageGalleryItem> Images { get; }

    public IReadOnlyList<IImageGalleryItem> ErrorImages { get; }

    public IReadOnlyList<IImageGalleryItem> LargeImages { get; }

    public ICommand AddImageCommand { get; }

    private void AddImage()
    {
        var itemIndex = Images.Count;
        Images.Add(CreateResourceItem(itemIndex % s_galleryAssets.Length, itemIndex));
    }

    private static ImageGalleryItem CreateResourceItem(int assetIndex, int itemIndex)
    {
        var asset = s_galleryAssets[assetIndex];
        var uri = new Uri($"avares://AtomUILabsGallery/Assets/ImageGallery/{asset.FileName}");
        return new ImageGalleryItem
        {
            Key = $"gallery-photo-{itemIndex}",
            Title = asset.Title,
            MainImageSource = ImageGallerySources.FromAvaloniaResource(uri),
        };
    }

    private static IEnumerable<IImageGalleryItem> CreateSyntheticItems(int count)
    {
        for (var index = 0; index < count; index++)
        {
            yield return CreateSyntheticItem(index);
        }
    }

    private static ImageGalleryItem CreateSyntheticItem(int index)
    {
        var color = s_palette[index % s_palette.Length];
        return new ImageGalleryItem
        {
            Key = $"gallery-art-{index}",
            Title = $"Gallery artwork {index + 1}",
            MainImageSource = new GalleryArtworkSource($"gallery-art-{index}", color, index),
        };
    }

    private sealed record GalleryAsset(string FileName, string Title);

    private sealed class DelegateCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }

    private sealed class GalleryArtworkSource(object identity, Color color, int variant) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var requested = request.TargetPixelSize ?? new PixelSize(1600, 1000);
            var decoded = new PixelSize(
                Math.Clamp(requested.Width, 64, 2400),
                Math.Clamp(requested.Height, 64, 1600));
            var image = new GalleryArtworkImage(decoded, color, variant);
            return ValueTask.FromResult(ImageGalleryImageLease.Create(
                image,
                new PixelSize(2400, 1600),
                decoded,
                checked((long)decoded.Width * decoded.Height * 4),
                () => { }));
        }
    }

    private sealed class GalleryFailureSource(object identity) : IImageGallerySource
    {
        public object Identity { get; } = identity;

        public ValueTask<ImageGalleryImageLease> LoadAsync(
            ImageGalleryImageRequest request,
            CancellationToken cancellationToken) =>
            ValueTask.FromException<ImageGalleryImageLease>(
                new InvalidDataException("The Gallery intentionally supplied a damaged image."));
    }

    private sealed class GalleryArtworkImage(PixelSize size, Color color, int variant) : IImage
    {
        private readonly IBrush _background = new ImmutableSolidColorBrush(color);
        private readonly IBrush _highlight = new ImmutableSolidColorBrush(Color.FromArgb(150, 255, 255, 255));
        private readonly IBrush _shadow = new ImmutableSolidColorBrush(Color.FromArgb(90, 0, 0, 0));

        public Size Size { get; } = new(size.Width, size.Height);

        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            context.DrawRectangle(_background, null, destRect);
            var unit = Math.Min(destRect.Width, destRect.Height);
            var shift = variant % 5 * unit * 0.045;
            context.DrawEllipse(
                _highlight,
                null,
                new Point(destRect.X + destRect.Width * 0.34 + shift, destRect.Y + destRect.Height * 0.35),
                unit * 0.22,
                unit * 0.22);
            context.DrawRectangle(
                _shadow,
                null,
                new Rect(
                    destRect.X + destRect.Width * 0.52 - shift,
                    destRect.Y + destRect.Height * 0.22,
                    Math.Max(1, destRect.Width * 0.24),
                    Math.Max(1, destRect.Height * 0.56)),
                unit * 0.04);
            var pen = new Pen(_highlight, Math.Max(2, unit * 0.018));
            context.DrawLine(
                pen,
                new Point(destRect.X + destRect.Width * 0.1, destRect.Bottom - destRect.Height * 0.16),
                new Point(destRect.Right - destRect.Width * 0.1, destRect.Y + destRect.Height * 0.14));
        }
    }
}
