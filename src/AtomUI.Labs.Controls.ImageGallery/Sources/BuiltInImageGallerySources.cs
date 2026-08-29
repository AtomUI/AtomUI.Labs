using System.Net;
using System.Net.Http;
using Avalonia.Platform;

namespace AtomUI.Labs.Controls.ImageGallery;

internal abstract class BuiltInImageGallerySource(object identity) : IImageGallerySource
{
    public object Identity { get; } = identity ?? throw new ArgumentNullException(nameof(identity));

    public async ValueTask<ImageGalleryImageLease> LoadAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken)
    {
        ImageGallerySourceValidation.ValidateRequest(request);
        await using var stream = await OpenStreamAsync(request, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            throw new InvalidDataException("The image source returned a null stream.");
        }

        var encoded = await ImageGallerySourceValidation.ReadBoundedAsync(
            stream,
            request.Limits.MaximumEncodedBytes,
            cancellationToken).ConfigureAwait(false);

        return await ImageGallerySourceValidation.DecodeAsync(
            encoded,
            request,
            cancellationToken).ConfigureAwait(false);
    }

    protected abstract ValueTask<Stream> OpenStreamAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken);
}

internal sealed class FileImageGallerySource(string path, object identity)
    : BuiltInImageGallerySource(identity)
{
    protected override ValueTask<Stream> OpenStreamAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (info.Exists && info.Length > request.Limits.MaximumEncodedBytes)
        {
            throw new InvalidDataException(
                $"Encoded image length {info.Length} exceeds MaximumEncodedBytes {request.Limits.MaximumEncodedBytes}.");
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return ValueTask.FromResult(stream);
    }
}

internal sealed class AvaloniaResourceImageGallerySource(Uri uri, object identity)
    : BuiltInImageGallerySource(identity)
{
    protected override ValueTask<Stream> OpenStreamAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(AssetLoader.Open(uri));
    }
}

internal sealed class StreamImageGallerySource(
    object identity,
    Func<CancellationToken, ValueTask<Stream>> openStream)
    : BuiltInImageGallerySource(identity)
{
    protected override ValueTask<Stream> OpenStreamAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken) =>
        openStream(cancellationToken);
}

internal sealed class HttpImageGallerySource(
    Uri uri,
    HttpClient httpClient,
    object identity)
    : IImageGallerySource
{
    public object Identity { get; } = identity;

    public async ValueTask<ImageGalleryImageLease> LoadAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken)
    {
        ImageGallerySourceValidation.ValidateRequest(request);
        using var response = await httpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength is { } length &&
            length > request.Limits.MaximumEncodedBytes)
        {
            throw new InvalidDataException(
                $"HTTP image length {length} exceeds MaximumEncodedBytes {request.Limits.MaximumEncodedBytes}.");
        }

        await using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var encoded = await ImageGallerySourceValidation.ReadBoundedAsync(
            stream,
            request.Limits.MaximumEncodedBytes,
            cancellationToken).ConfigureAwait(false);

        return await ImageGallerySourceValidation.DecodeAsync(
            encoded,
            request,
            cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class DelegateImageGallerySource(
    object identity,
    Func<ImageGalleryImageRequest, CancellationToken, ValueTask<ImageGalleryImageLease>> loader)
    : IImageGallerySource
{
    public object Identity { get; } = identity;

    public async ValueTask<ImageGalleryImageLease> LoadAsync(
        ImageGalleryImageRequest request,
        CancellationToken cancellationToken)
    {
        ImageGallerySourceValidation.ValidateRequest(request);
        var lease = await loader(request, cancellationToken).ConfigureAwait(false);
        if (lease is null)
        {
            throw new InvalidDataException("The delegate image source returned a null lease.");
        }

        try
        {
            ImageGallerySourceValidation.ValidateLease(lease, request.Limits);
            return lease;
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }
}
