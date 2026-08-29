using Avalonia;

namespace AtomUI.Labs.Controls.ImageGallery.Loading;

internal readonly record struct ImageGalleryCacheKey(
    object SourceIdentity,
    ImageGalleryImagePurpose Purpose,
    PixelSize? TargetPixelSize);
