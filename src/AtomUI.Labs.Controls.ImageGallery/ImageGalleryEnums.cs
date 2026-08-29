namespace AtomUI.Labs.Controls.ImageGallery;

public enum ImageGalleryZoomMode
{
    Fit,
    ActualSize,
    Custom
}

public enum ImageGalleryWheelZoomMode
{
    Disabled,
    Always,
    ControlModifier
}

public enum ImageGalleryEdgePlacement
{
    Top,
    Bottom,
    Left,
    Right
}

public enum ImageGalleryEdgeAlignment
{
    Start,
    Center,
    End,
    Stretch
}

public enum ImageGalleryResponsiveState
{
    Normal,
    Compact,
    Minimal
}

public enum ImageGalleryImageState
{
    Empty,
    Loading,
    Ready,
    Error
}

public enum ImageGalleryMainImageMode
{
    Presented,
    ResourceOnly
}

public enum ImageGalleryMainImagePrefetchMode
{
    Disabled,
    Adjacent
}

public enum ImageGalleryImagePurpose
{
    MainImage,
    Thumbnail
}
