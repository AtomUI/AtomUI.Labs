namespace AtomUI.Labs.Controls.ImageGallery.Loading;

internal sealed class SharedImageResource : IDisposable
{
    private ImageGalleryImageLease? _sourceLease;
    private int _referenceCount = 1;
    private int _ownerReferenceReleased;

    public SharedImageResource(ImageGalleryImageLease sourceLease)
    {
        _sourceLease = sourceLease ?? throw new ArgumentNullException(nameof(sourceLease));
    }

    public long EstimatedMemorySizeBytes
    {
        get
        {
            var lease = Volatile.Read(ref _sourceLease);
            return lease?.EstimatedMemorySizeBytes ?? 0;
        }
    }

    public ImageGalleryImageLease? TryAcquire()
    {
        while (true)
        {
            var count = Volatile.Read(ref _referenceCount);
            if (count == 0)
            {
                return null;
            }

            if (Interlocked.CompareExchange(ref _referenceCount, count + 1, count) != count)
            {
                continue;
            }

            var source = Volatile.Read(ref _sourceLease);
            if (source is null)
            {
                ReleaseReference();
                return null;
            }

            try
            {
                return ImageGalleryImageLease.Create(
                    source.Image,
                    source.SourcePixelSize,
                    source.DecodedPixelSize,
                    source.EstimatedMemorySizeBytes,
                    ReleaseReference);
            }
            catch
            {
                ReleaseReference();
                throw;
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _ownerReferenceReleased, 1) == 0)
        {
            ReleaseReference();
        }
    }

    private void ReleaseReference()
    {
        if (Interlocked.Decrement(ref _referenceCount) != 0)
        {
            return;
        }

        Interlocked.Exchange(ref _sourceLease, null)?.Dispose();
    }
}
