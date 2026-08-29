using AtomUI.Labs.Controls.ImageGallery.Data;
using Avalonia;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.ImageGallery.Loading;

internal sealed class CurrentImageResourceSlot : IDisposable
{
    private ImageGalleryImageLease? _galleryLease;
    private int _disposed;

    public CurrentImageResourceSlot(
        ImageGalleryDescriptor descriptor,
        long generation,
        PixelSize targetPixelSize,
        SharedImageResource resource,
        ImageGalleryImageLease galleryLease)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _galleryLease = galleryLease ?? throw new ArgumentNullException(nameof(galleryLease));
        Generation = generation;
        TargetPixelSize = targetPixelSize;
    }

    public ImageGalleryDescriptor Descriptor { get; }

    public object Key => Descriptor.Key;

    public object SourceIdentity => Descriptor.MainSourceIdentity;

    public long Generation { get; }

    public PixelSize TargetPixelSize { get; }

    public IImage Image => GalleryLease.Image;

    public PixelSize DecodedPixelSize => GalleryLease.DecodedPixelSize;

    public ImageGalleryImageLease? TryAcquire() =>
        Volatile.Read(ref _disposed) == 0 ? Resource.TryAcquire() : null;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        Interlocked.Exchange(ref _galleryLease, null)?.Dispose();
    }

    private SharedImageResource Resource { get; }

    private ImageGalleryImageLease GalleryLease =>
        Volatile.Read(ref _galleryLease) ??
        throw new ObjectDisposedException(nameof(CurrentImageResourceSlot));
}
