namespace AtomUI.Labs.Controls.ImageGallery;

public readonly record struct ImageGalleryLoadLimits(
    long MaximumEncodedBytes,
    long MaximumSourcePixelCount,
    int MaximumDimension,
    long MaximumDecodedBytes)
{
    public static ImageGalleryLoadLimits Default { get; } = new(
        256L * 1024 * 1024,
        100_000_000,
        32_768,
        512L * 1024 * 1024);

    internal static bool IsValid(ImageGalleryLoadLimits value)
    {
        return value.MaximumEncodedBytes > 0 &&
               value.MaximumSourcePixelCount > 0 &&
               value.MaximumDimension > 0 &&
               value.MaximumDecodedBytes > 0;
    }
}
