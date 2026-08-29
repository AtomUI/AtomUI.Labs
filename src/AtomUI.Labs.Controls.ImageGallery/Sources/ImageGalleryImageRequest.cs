using Avalonia;

namespace AtomUI.Labs.Controls.ImageGallery;

public readonly record struct ImageGalleryImageRequest(
    ImageGalleryImagePurpose Purpose,
    PixelSize? TargetPixelSize,
    ImageGalleryLoadLimits Limits);
