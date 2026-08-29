using System.Net.Http;

namespace AtomUI.Labs.Controls.ImageGallery;

public static class ImageGallerySources
{
    public static IImageGallerySource FromFile(string path, object? identity = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        return new FileImageGallerySource(fullPath, identity ?? fullPath);
    }

    public static IImageGallerySource FromAvaloniaResource(Uri uri, object? identity = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri || !string.Equals(uri.Scheme, "avares", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Avalonia resource source requires an absolute avares:// URI.",
                nameof(uri));
        }

        return new AvaloniaResourceImageGallerySource(uri, identity ?? uri);
    }

    public static IImageGallerySource FromHttp(
        Uri uri,
        HttpClient httpClient,
        object? identity = null)
    {
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(httpClient);
        if (!uri.IsAbsoluteUri ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "HTTP source requires an absolute HTTP or HTTPS URI.",
                nameof(uri));
        }

        return new HttpImageGallerySource(uri, httpClient, identity ?? uri);
    }

    public static IImageGallerySource FromStream(
        object identity,
        Func<CancellationToken, ValueTask<Stream>> openStream)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(openStream);
        return new StreamImageGallerySource(identity, openStream);
    }

    public static IImageGallerySource Create(
        object identity,
        Func<ImageGalleryImageRequest, CancellationToken, ValueTask<ImageGalleryImageLease>> loader)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(loader);
        return new DelegateImageGallerySource(identity, loader);
    }
}
