using System.Collections;
using System.Globalization;
using System.Windows.Input;
using AtomUI.Labs.Controls.ImageGallery.Appearance;
using AtomUI.Labs.Controls.ImageGallery.Data;
using AtomUI.Labs.Controls.ImageGallery.Filmstrip;
using AtomUI.Labs.Controls.ImageGallery.Layout;
using AtomUI.Labs.Controls.ImageGallery.Loading;
using AtomUI.Labs.Controls.ImageGallery.Selection;
using AtomUI.Labs.Controls.ImageGallery.Viewport;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AtomUI.Labs.Controls.ImageGallery;

public class ImageGallery : SelectingItemsControl, ICustomHitTest
{
    private readonly ImageGalleryCommand _previousCommand;
    private readonly ImageGalleryCommand _nextCommand;
    private readonly ImageGalleryCommand _zoomInCommand;
    private readonly ImageGalleryCommand _zoomOutCommand;
    private readonly ImageGalleryCommand _fitCommand;
    private readonly ImageGalleryCommand _actualSizeCommand;
    private readonly ImageGalleryCommand _rotateClockwiseCommand;
    private readonly ImageGalleryCoordinator _coordinator;
    private readonly ImageGalleryLoadCoordinator _loadCoordinator;
    private CurrentImageResourceSlot? _currentMainImageSlot;
    private Exception? _imageFailure;
    private ImageGalleryViewportPresenter? _viewportPresenter;
    private Size _viewportSize;
    private Vector _panOffset;
    private double _pinchStartZoom;
    private bool _suppressZoomReconciliation;
    private long _filmstripScrollRequest;
    private readonly HashSet<AvaloniaObject> _subscribedAppearances = [];
    private Border? _viewportHost;
    private TextBlock? _emptyTextBlock;
    private TextBlock? _loadingTextBlock;
    private TextBlock? _errorTextBlock;
    private Border? _toolbarHost;
    private ImageGalleryToolbarPanel? _toolbarItems;
    private Border? _filmstripHost;
    private ImageGalleryFilmstripScrollViewer? _filmstripScrollViewer;
    private BorderDefaults _viewportDefaults;
    private BorderDefaults _toolbarDefaults;
    private BorderDefaults _filmstripDefaults;
    private IBrush? _emptyForegroundDefault;
    private IBrush? _loadingForegroundDefault;
    private IBrush? _errorForegroundDefault;
    private ICommand? _subscribedAddImageCommand;

    private double _effectiveZoomFactor = 1;
    private string _effectiveZoomPercentageText = "100%";
    private bool _isPinchZooming;
    private bool _isPanning;
    private int _rotationAngle;
    private ImageGalleryEdgePlacement _effectiveToolbarPlacement = ImageGalleryEdgePlacement.Top;
    private ImageGalleryResponsiveState _responsiveState;
    private ImageGalleryImageState _imageState;
    private string? _currentTitle;
    private bool _isEffectiveToolbarVisible = true;
    private bool _isEffectiveToolbarTitleVisible = true;
    private bool _isEffectiveToolbarZoomControlsVisible = true;
    private bool _isEffectiveToolbarRotationVisible = true;
    private bool _isMainImagePresented = true;
    private bool _isEffectiveFilmstripVisible;
    private bool _isEffectivePreviousViewportNavigationVisible = true;
    private bool _isEffectiveNextViewportNavigationVisible = true;
    private bool _isPreviousViewportNavigationSideAvailable = true;
    private bool _isNextViewportNavigationSideAvailable = true;
    private bool _isAddImageButtonEnabled;
    private ImageGalleryGlyphKind _filmstripPreviousGlyph = ImageGalleryGlyphKind.Previous;
    private ImageGalleryGlyphKind _filmstripNextGlyph = ImageGalleryGlyphKind.Next;

    public static readonly StyledProperty<ImageGalleryZoomMode> ZoomModeProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryZoomMode>(
            nameof(ZoomMode),
            ImageGalleryZoomMode.Fit,
            validate: Enum.IsDefined);

    public static readonly StyledProperty<double> CustomZoomFactorProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(CustomZoomFactor),
            1,
            validate: IsPositiveFinite);

    public static readonly StyledProperty<bool> IsFitUpscalingEnabledProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsFitUpscalingEnabled));

    public static readonly StyledProperty<ImageGalleryZoomRange> ZoomRangeProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryZoomRange>(
            nameof(ZoomRange),
            ImageGalleryZoomRange.Default,
            validate: ImageGalleryZoomRange.IsValid);

    public static readonly StyledProperty<double> ZoomStepProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ZoomStep),
            1.2,
            validate: value => double.IsFinite(value) && value > 1);

    public static readonly StyledProperty<ImageGalleryWheelZoomMode> WheelZoomModeProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryWheelZoomMode>(
            nameof(WheelZoomMode),
            ImageGalleryWheelZoomMode.Always,
            validate: Enum.IsDefined);

    public static readonly StyledProperty<bool> IsPinchZoomEnabledProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsPinchZoomEnabled), true);

    public static readonly StyledProperty<bool> IsPanEnabledProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsPanEnabled), true);

    public static readonly StyledProperty<bool> IsViewportNavigationEnabledProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsViewportNavigationEnabled), true);

    public static readonly StyledProperty<bool> IsFilmstripNavigationEnabledProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsFilmstripNavigationEnabled), true);

    public static readonly StyledProperty<bool> IsLoopNavigationEnabledProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsLoopNavigationEnabled));

    public static readonly StyledProperty<bool> IsToolbarTitleVisibleProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsToolbarTitleVisible), true);

    public static readonly StyledProperty<bool> IsToolbarZoomControlsVisibleProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsToolbarZoomControlsVisible), true);

    public static readonly StyledProperty<bool> IsToolbarRotationVisibleProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsToolbarRotationVisible), true);

    public static readonly StyledProperty<bool> IsToolbarVisibleProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsToolbarVisible), true);

    public static readonly StyledProperty<object?> ToolbarContentProperty =
        AvaloniaProperty.Register<ImageGallery, object?>(nameof(ToolbarContent));

    public static readonly StyledProperty<IDataTemplate?> ToolbarContentTemplateProperty =
        AvaloniaProperty.Register<ImageGallery, IDataTemplate?>(nameof(ToolbarContentTemplate));

    public static readonly StyledProperty<ImageGalleryEdgePlacement> ToolbarPlacementProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryEdgePlacement>(
            nameof(ToolbarPlacement),
            ImageGalleryEdgePlacement.Top,
            validate: Enum.IsDefined);

    public static readonly StyledProperty<double> ToolbarEdgeGapProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ToolbarEdgeGap),
            16,
            validate: IsNonNegativeFinite);

    public static readonly StyledProperty<double> ToolbarAlongEdgeInsetProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ToolbarAlongEdgeInset),
            16,
            validate: IsNonNegativeFinite);

    public static readonly StyledProperty<double> OverlayElementGapProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(OverlayElementGap),
            8,
            validate: IsNonNegativeFinite);

    public static readonly StyledProperty<double> ToolbarRegionSpacingProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ToolbarRegionSpacing),
            16,
            validate: IsNonNegativeFinite);

    public static readonly StyledProperty<double> ToolbarItemSpacingProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ToolbarItemSpacing),
            8,
            validate: IsNonNegativeFinite);

    public static readonly StyledProperty<ImageGalleryEdgeAlignment> ToolbarAlongEdgeAlignmentProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryEdgeAlignment>(
            nameof(ToolbarAlongEdgeAlignment),
            ImageGalleryEdgeAlignment.Center,
            validate: Enum.IsDefined);

    public static readonly StyledProperty<ImageGalleryEdgePlacement> ThumbnailFilmstripPlacementProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryEdgePlacement>(
            nameof(ThumbnailFilmstripPlacement),
            ImageGalleryEdgePlacement.Bottom,
            validate: Enum.IsDefined);

    public static readonly StyledProperty<double> ThumbnailFilmstripExtentProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ThumbnailFilmstripExtent),
            120,
            validate: IsPositiveFinite);

    public static readonly StyledProperty<double> ThumbnailFilmstripEdgeGapProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ThumbnailFilmstripEdgeGap),
            16,
            validate: IsNonNegativeFinite);

    public static readonly StyledProperty<double> ThumbnailFilmstripAlongEdgeInsetProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ThumbnailFilmstripAlongEdgeInset),
            16,
            validate: IsNonNegativeFinite);

    public static readonly StyledProperty<bool> IsThumbnailFilmstripVisibleProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsThumbnailFilmstripVisible), true);

    public static readonly StyledProperty<double> ThumbnailItemExtentProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ThumbnailItemExtent),
            96,
            validate: IsPositiveFinite);

    public static readonly StyledProperty<double> ThumbnailItemSpacingProperty =
        AvaloniaProperty.Register<ImageGallery, double>(
            nameof(ThumbnailItemSpacing),
            8,
            validate: IsNonNegativeFinite);

    public static readonly StyledProperty<long> ThumbnailCacheMemoryBudgetBytesProperty =
        AvaloniaProperty.Register<ImageGallery, long>(
            nameof(ThumbnailCacheMemoryBudgetBytes),
            32L * 1024 * 1024,
            validate: value => value >= 0);

    public static readonly StyledProperty<bool> IsAddImageButtonVisibleProperty =
        AvaloniaProperty.Register<ImageGallery, bool>(nameof(IsAddImageButtonVisible), true);

    public static readonly StyledProperty<ICommand?> AddImageCommandProperty =
        AvaloniaProperty.Register<ImageGallery, ICommand?>(nameof(AddImageCommand));

    public static readonly StyledProperty<object?> AddImageCommandParameterProperty =
        AvaloniaProperty.Register<ImageGallery, object?>(nameof(AddImageCommandParameter));

    public static readonly StyledProperty<ImageGalleryLoadLimits> LoadLimitsProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryLoadLimits>(
            nameof(LoadLimits),
            ImageGalleryLoadLimits.Default,
            validate: ImageGalleryLoadLimits.IsValid);

    public static readonly StyledProperty<ImageGalleryMainImagePrefetchMode> MainImagePrefetchModeProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryMainImagePrefetchMode>(
            nameof(MainImagePrefetchMode),
            ImageGalleryMainImagePrefetchMode.Adjacent,
            validate: Enum.IsDefined);

    public static readonly StyledProperty<long> MainImageCacheMemoryBudgetBytesProperty =
        AvaloniaProperty.Register<ImageGallery, long>(
            nameof(MainImageCacheMemoryBudgetBytes),
            128L * 1024 * 1024,
            validate: value => value >= 0);

    public static readonly StyledProperty<ImageGalleryMainImageMode> MainImageModeProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryMainImageMode>(
            nameof(MainImageMode),
            ImageGalleryMainImageMode.Presented,
            validate: Enum.IsDefined);

    public static readonly StyledProperty<PixelSize?> MainImageDecodeSizeHintProperty =
        AvaloniaProperty.Register<ImageGallery, PixelSize?>(
            nameof(MainImageDecodeSizeHint),
            validate: IsNullOrPositivePixelSize);

    public static readonly StyledProperty<ImageGalleryViewportAppearance?> ViewportAppearanceProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryViewportAppearance?>(nameof(ViewportAppearance));

    public static readonly StyledProperty<ImageGalleryToolbarAppearance?> ToolbarAppearanceProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryToolbarAppearance?>(nameof(ToolbarAppearance));

    public static readonly StyledProperty<ImageGalleryButtonAppearance?> ToolbarButtonAppearanceProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryButtonAppearance?>(nameof(ToolbarButtonAppearance));

    public static readonly StyledProperty<ImageGalleryButtonAppearance?> ViewportNavigationButtonAppearanceProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryButtonAppearance?>(nameof(ViewportNavigationButtonAppearance));

    public static readonly StyledProperty<ImageGalleryButtonAppearance?> FilmstripNavigationButtonAppearanceProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryButtonAppearance?>(nameof(FilmstripNavigationButtonAppearance));

    public static readonly StyledProperty<ImageGalleryFilmstripAppearance?> FilmstripAppearanceProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryFilmstripAppearance?>(nameof(FilmstripAppearance));

    public static readonly StyledProperty<ImageGalleryThumbnailItemAppearance?> ThumbnailItemAppearanceProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryThumbnailItemAppearance?>(nameof(ThumbnailItemAppearance));

    public static readonly StyledProperty<ImageGalleryButtonAppearance?> AddImageButtonAppearanceProperty =
        AvaloniaProperty.Register<ImageGallery, ImageGalleryButtonAppearance?>(nameof(AddImageButtonAppearance));

    public static readonly DirectProperty<ImageGallery, double> EffectiveZoomFactorProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, double>(
            nameof(EffectiveZoomFactor),
            control => control.EffectiveZoomFactor);

    internal static readonly DirectProperty<ImageGallery, string> EffectiveZoomPercentageTextProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, string>(
            nameof(EffectiveZoomPercentageText),
            control => control.EffectiveZoomPercentageText);

    public static readonly DirectProperty<ImageGallery, bool> IsPinchZoomingProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsPinchZooming),
            control => control.IsPinchZooming);

    public static readonly DirectProperty<ImageGallery, bool> IsPanningProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsPanning),
            control => control.IsPanning);

    public static readonly DirectProperty<ImageGallery, int> RotationAngleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, int>(
            nameof(RotationAngle),
            control => control.RotationAngle);

    public static readonly DirectProperty<ImageGallery, ImageGalleryEdgePlacement> EffectiveToolbarPlacementProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ImageGalleryEdgePlacement>(
            nameof(EffectiveToolbarPlacement),
            control => control.EffectiveToolbarPlacement);

    public static readonly DirectProperty<ImageGallery, ImageGalleryResponsiveState> ResponsiveStateProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ImageGalleryResponsiveState>(
            nameof(ResponsiveState),
            control => control.ResponsiveState);

    public static readonly DirectProperty<ImageGallery, ImageGalleryImageState> ImageStateProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ImageGalleryImageState>(
            nameof(ImageState),
            control => control.ImageState);

    public static readonly DirectProperty<ImageGallery, ICommand> PreviousCommandProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ICommand>(
            nameof(PreviousCommand),
            control => control.PreviousCommand);

    public static readonly DirectProperty<ImageGallery, ICommand> NextCommandProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ICommand>(
            nameof(NextCommand),
            control => control.NextCommand);

    public static readonly DirectProperty<ImageGallery, ICommand> ZoomInCommandProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ICommand>(
            nameof(ZoomInCommand),
            control => control.ZoomInCommand);

    public static readonly DirectProperty<ImageGallery, ICommand> ZoomOutCommandProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ICommand>(
            nameof(ZoomOutCommand),
            control => control.ZoomOutCommand);

    public static readonly DirectProperty<ImageGallery, ICommand> FitCommandProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ICommand>(
            nameof(FitCommand),
            control => control.FitCommand);

    public static readonly DirectProperty<ImageGallery, ICommand> ActualSizeCommandProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ICommand>(
            nameof(ActualSizeCommand),
            control => control.ActualSizeCommand);

    public static readonly DirectProperty<ImageGallery, ICommand> RotateClockwiseCommandProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ICommand>(
            nameof(RotateClockwiseCommand),
            control => control.RotateClockwiseCommand);

    internal static readonly DirectProperty<ImageGallery, string?> CurrentTitleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, string?>(
            nameof(CurrentTitle),
            control => control.CurrentTitle);

    internal static readonly DirectProperty<ImageGallery, bool> IsEffectiveToolbarVisibleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsEffectiveToolbarVisible),
            control => control.IsEffectiveToolbarVisible);

    internal static readonly DirectProperty<ImageGallery, bool> IsEffectiveToolbarTitleVisibleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsEffectiveToolbarTitleVisible),
            control => control.IsEffectiveToolbarTitleVisible);

    internal static readonly DirectProperty<ImageGallery, bool> IsEffectiveToolbarZoomControlsVisibleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsEffectiveToolbarZoomControlsVisible),
            control => control.IsEffectiveToolbarZoomControlsVisible);

    internal static readonly DirectProperty<ImageGallery, bool> IsEffectiveToolbarRotationVisibleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsEffectiveToolbarRotationVisible),
            control => control.IsEffectiveToolbarRotationVisible);

    internal static readonly DirectProperty<ImageGallery, bool> IsMainImagePresentedProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsMainImagePresented),
            control => control.IsMainImagePresented);

    internal static readonly DirectProperty<ImageGallery, bool> IsEffectiveFilmstripVisibleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsEffectiveFilmstripVisible),
            control => control.IsEffectiveFilmstripVisible);

    internal static readonly DirectProperty<ImageGallery, bool> IsEffectivePreviousViewportNavigationVisibleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsEffectivePreviousViewportNavigationVisible),
            control => control.IsEffectivePreviousViewportNavigationVisible);

    internal static readonly DirectProperty<ImageGallery, bool> IsEffectiveNextViewportNavigationVisibleProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsEffectiveNextViewportNavigationVisible),
            control => control.IsEffectiveNextViewportNavigationVisible);

    internal static readonly DirectProperty<ImageGallery, bool> IsAddImageButtonEnabledProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, bool>(
            nameof(IsAddImageButtonEnabled),
            control => control.IsAddImageButtonEnabled);

    internal static readonly DirectProperty<ImageGallery, ImageGalleryGlyphKind> FilmstripPreviousGlyphProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ImageGalleryGlyphKind>(
            nameof(FilmstripPreviousGlyph),
            control => control.FilmstripPreviousGlyph);

    internal static readonly DirectProperty<ImageGallery, ImageGalleryGlyphKind> FilmstripNextGlyphProperty =
        AvaloniaProperty.RegisterDirect<ImageGallery, ImageGalleryGlyphKind>(
            nameof(FilmstripNextGlyph),
            control => control.FilmstripNextGlyph);

    static ImageGallery()
    {
        ZoomModeProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileZoomRequirements());
        CustomZoomFactorProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileZoomRequirements());
        ZoomRangeProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcilePublicZoom());
        IsFitUpscalingEnabledProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileZoomRequirements());
        IsViewportNavigationEnabledProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileViewportNavigationVisibility());
        ToolbarPlacementProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileEffectiveToolbarPlacement());
        ThumbnailFilmstripPlacementProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileEffectiveToolbarPlacement());
        ThumbnailFilmstripPlacementProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileFilmstripPlacement());
        IsThumbnailFilmstripVisibleProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileFilmstripVisibility());
        IsLoopNavigationEnabledProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RaiseNavigationCanExecuteChanged());
        MainImageCacheMemoryBudgetBytesProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control._loadCoordinator.ReconcileBudget());
        ThumbnailCacheMemoryBudgetBytesProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control._loadCoordinator.ReconcileBudget());
        MainImageModeProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.OnMainImageModeChanged());
        MainImageDecodeSizeHintProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control._loadCoordinator.ViewportRequirementsChanged());
        ThumbnailItemExtentProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.InvalidateFilmstripLayout());
        ThumbnailItemSpacingProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.InvalidateFilmstripLayout());
        IsToolbarVisibleProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileToolbarVisibility());
        IsToolbarTitleVisibleProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileEffectiveToolbarTitleVisibility());
        IsToolbarZoomControlsVisibleProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileToolbarVisibility());
        IsToolbarRotationVisibleProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileToolbarVisibility());
        ToolbarContentProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileToolbarVisibility());
        AddImageCommandProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAddImageCommand());
        AddImageCommandParameterProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.ReconcileAddImageCommand());
        ViewportAppearanceProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAppearanceSubscriptions());
        ToolbarAppearanceProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAppearanceSubscriptions());
        ToolbarButtonAppearanceProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAppearanceSubscriptions());
        ViewportNavigationButtonAppearanceProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAppearanceSubscriptions());
        FilmstripNavigationButtonAppearanceProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAppearanceSubscriptions());
        FilmstripAppearanceProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAppearanceSubscriptions());
        ThumbnailItemAppearanceProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAppearanceSubscriptions());
        AddImageButtonAppearanceProperty.Changed.AddClassHandler<ImageGallery>((control, _) => control.RewireAppearanceSubscriptions());
    }

    public ImageGallery()
    {
        AutoScrollToSelectedItem = false;
        _coordinator = new ImageGalleryCoordinator(this);
        _loadCoordinator = new ImageGalleryLoadCoordinator(this);
        PseudoClasses.Set(":empty", true);
        PseudoClasses.Set(":normal", true);
        PseudoClasses.Set(":toolbar-horizontal", true);
        PseudoClasses.Set(":presented", true);
        _previousCommand = new ImageGalleryCommand(RequestPreviousSelection, CanNavigatePrevious);
        _nextCommand = new ImageGalleryCommand(RequestNextSelection, CanNavigateNext);
        _zoomInCommand = new ImageGalleryCommand(() => ChangeZoom(true), IsImageReady);
        _zoomOutCommand = new ImageGalleryCommand(() => ChangeZoom(false), IsImageReady);
        _fitCommand = new ImageGalleryCommand(ApplyFit, IsImageReady);
        _actualSizeCommand = new ImageGalleryCommand(ApplyActualSize, IsImageReady);
        _rotateClockwiseCommand = new ImageGalleryCommand(RotateClockwise, IsImageReady);
        SelectionChanged += (_, _) =>
        {
            _coordinator.OnControlSelectionChanged();
            UpdateThumbnailSelection();
            RequestSelectedThumbnailIntoView();
            RaiseNavigationCanExecuteChanged();
        };
        ReconcileToolbarVisibility();
        ReconcileFilmstripVisibility();
    }

    public ImageGalleryZoomMode ZoomMode { get => GetValue(ZoomModeProperty); set => SetValue(ZoomModeProperty, value); }
    public double CustomZoomFactor { get => GetValue(CustomZoomFactorProperty); set => SetValue(CustomZoomFactorProperty, value); }
    public bool IsFitUpscalingEnabled { get => GetValue(IsFitUpscalingEnabledProperty); set => SetValue(IsFitUpscalingEnabledProperty, value); }
    public ImageGalleryZoomRange ZoomRange { get => GetValue(ZoomRangeProperty); set => SetValue(ZoomRangeProperty, value); }
    public double ZoomStep { get => GetValue(ZoomStepProperty); set => SetValue(ZoomStepProperty, value); }
    public ImageGalleryWheelZoomMode WheelZoomMode { get => GetValue(WheelZoomModeProperty); set => SetValue(WheelZoomModeProperty, value); }
    public bool IsPinchZoomEnabled { get => GetValue(IsPinchZoomEnabledProperty); set => SetValue(IsPinchZoomEnabledProperty, value); }
    public bool IsPanEnabled { get => GetValue(IsPanEnabledProperty); set => SetValue(IsPanEnabledProperty, value); }
    public bool IsViewportNavigationEnabled { get => GetValue(IsViewportNavigationEnabledProperty); set => SetValue(IsViewportNavigationEnabledProperty, value); }
    public bool IsFilmstripNavigationEnabled { get => GetValue(IsFilmstripNavigationEnabledProperty); set => SetValue(IsFilmstripNavigationEnabledProperty, value); }
    public bool IsLoopNavigationEnabled { get => GetValue(IsLoopNavigationEnabledProperty); set => SetValue(IsLoopNavigationEnabledProperty, value); }
    public bool IsToolbarTitleVisible { get => GetValue(IsToolbarTitleVisibleProperty); set => SetValue(IsToolbarTitleVisibleProperty, value); }
    public bool IsToolbarZoomControlsVisible { get => GetValue(IsToolbarZoomControlsVisibleProperty); set => SetValue(IsToolbarZoomControlsVisibleProperty, value); }
    public bool IsToolbarRotationVisible { get => GetValue(IsToolbarRotationVisibleProperty); set => SetValue(IsToolbarRotationVisibleProperty, value); }
    public bool IsToolbarVisible { get => GetValue(IsToolbarVisibleProperty); set => SetValue(IsToolbarVisibleProperty, value); }
    public object? ToolbarContent { get => GetValue(ToolbarContentProperty); set => SetValue(ToolbarContentProperty, value); }
    public IDataTemplate? ToolbarContentTemplate { get => GetValue(ToolbarContentTemplateProperty); set => SetValue(ToolbarContentTemplateProperty, value); }
    public ImageGalleryEdgePlacement ToolbarPlacement { get => GetValue(ToolbarPlacementProperty); set => SetValue(ToolbarPlacementProperty, value); }
    public double ToolbarEdgeGap { get => GetValue(ToolbarEdgeGapProperty); set => SetValue(ToolbarEdgeGapProperty, value); }
    public double ToolbarAlongEdgeInset { get => GetValue(ToolbarAlongEdgeInsetProperty); set => SetValue(ToolbarAlongEdgeInsetProperty, value); }
    public double OverlayElementGap { get => GetValue(OverlayElementGapProperty); set => SetValue(OverlayElementGapProperty, value); }
    public double ToolbarRegionSpacing { get => GetValue(ToolbarRegionSpacingProperty); set => SetValue(ToolbarRegionSpacingProperty, value); }
    public double ToolbarItemSpacing { get => GetValue(ToolbarItemSpacingProperty); set => SetValue(ToolbarItemSpacingProperty, value); }
    public ImageGalleryEdgeAlignment ToolbarAlongEdgeAlignment { get => GetValue(ToolbarAlongEdgeAlignmentProperty); set => SetValue(ToolbarAlongEdgeAlignmentProperty, value); }
    public ImageGalleryEdgePlacement ThumbnailFilmstripPlacement { get => GetValue(ThumbnailFilmstripPlacementProperty); set => SetValue(ThumbnailFilmstripPlacementProperty, value); }
    public double ThumbnailFilmstripExtent { get => GetValue(ThumbnailFilmstripExtentProperty); set => SetValue(ThumbnailFilmstripExtentProperty, value); }
    public double ThumbnailFilmstripEdgeGap { get => GetValue(ThumbnailFilmstripEdgeGapProperty); set => SetValue(ThumbnailFilmstripEdgeGapProperty, value); }
    public double ThumbnailFilmstripAlongEdgeInset { get => GetValue(ThumbnailFilmstripAlongEdgeInsetProperty); set => SetValue(ThumbnailFilmstripAlongEdgeInsetProperty, value); }
    public bool IsThumbnailFilmstripVisible { get => GetValue(IsThumbnailFilmstripVisibleProperty); set => SetValue(IsThumbnailFilmstripVisibleProperty, value); }
    public double ThumbnailItemExtent { get => GetValue(ThumbnailItemExtentProperty); set => SetValue(ThumbnailItemExtentProperty, value); }
    public double ThumbnailItemSpacing { get => GetValue(ThumbnailItemSpacingProperty); set => SetValue(ThumbnailItemSpacingProperty, value); }
    public long ThumbnailCacheMemoryBudgetBytes { get => GetValue(ThumbnailCacheMemoryBudgetBytesProperty); set => SetValue(ThumbnailCacheMemoryBudgetBytesProperty, value); }
    public bool IsAddImageButtonVisible { get => GetValue(IsAddImageButtonVisibleProperty); set => SetValue(IsAddImageButtonVisibleProperty, value); }
    public ICommand? AddImageCommand { get => GetValue(AddImageCommandProperty); set => SetValue(AddImageCommandProperty, value); }
    public object? AddImageCommandParameter { get => GetValue(AddImageCommandParameterProperty); set => SetValue(AddImageCommandParameterProperty, value); }
    public ImageGalleryLoadLimits LoadLimits { get => GetValue(LoadLimitsProperty); set => SetValue(LoadLimitsProperty, value); }
    public ImageGalleryMainImagePrefetchMode MainImagePrefetchMode { get => GetValue(MainImagePrefetchModeProperty); set => SetValue(MainImagePrefetchModeProperty, value); }
    public long MainImageCacheMemoryBudgetBytes { get => GetValue(MainImageCacheMemoryBudgetBytesProperty); set => SetValue(MainImageCacheMemoryBudgetBytesProperty, value); }
    public ImageGalleryMainImageMode MainImageMode { get => GetValue(MainImageModeProperty); set => SetValue(MainImageModeProperty, value); }
    public PixelSize? MainImageDecodeSizeHint { get => GetValue(MainImageDecodeSizeHintProperty); set => SetValue(MainImageDecodeSizeHintProperty, value); }
    public ImageGalleryViewportAppearance? ViewportAppearance { get => GetValue(ViewportAppearanceProperty); set => SetValue(ViewportAppearanceProperty, value); }
    public ImageGalleryToolbarAppearance? ToolbarAppearance { get => GetValue(ToolbarAppearanceProperty); set => SetValue(ToolbarAppearanceProperty, value); }
    public ImageGalleryButtonAppearance? ToolbarButtonAppearance { get => GetValue(ToolbarButtonAppearanceProperty); set => SetValue(ToolbarButtonAppearanceProperty, value); }
    public ImageGalleryButtonAppearance? ViewportNavigationButtonAppearance { get => GetValue(ViewportNavigationButtonAppearanceProperty); set => SetValue(ViewportNavigationButtonAppearanceProperty, value); }
    public ImageGalleryButtonAppearance? FilmstripNavigationButtonAppearance { get => GetValue(FilmstripNavigationButtonAppearanceProperty); set => SetValue(FilmstripNavigationButtonAppearanceProperty, value); }
    public ImageGalleryFilmstripAppearance? FilmstripAppearance { get => GetValue(FilmstripAppearanceProperty); set => SetValue(FilmstripAppearanceProperty, value); }
    public ImageGalleryThumbnailItemAppearance? ThumbnailItemAppearance { get => GetValue(ThumbnailItemAppearanceProperty); set => SetValue(ThumbnailItemAppearanceProperty, value); }
    public ImageGalleryButtonAppearance? AddImageButtonAppearance { get => GetValue(AddImageButtonAppearanceProperty); set => SetValue(AddImageButtonAppearanceProperty, value); }

    public double EffectiveZoomFactor => _effectiveZoomFactor;
    public bool IsPinchZooming => _isPinchZooming;
    public bool IsPanning => _isPanning;
    public int RotationAngle => _rotationAngle;
    public ImageGalleryEdgePlacement EffectiveToolbarPlacement => _effectiveToolbarPlacement;
    public ImageGalleryResponsiveState ResponsiveState => _responsiveState;
    public ImageGalleryImageState ImageState => _imageState;

    internal string? CurrentTitle => _currentTitle;

    internal string EffectiveZoomPercentageText => _effectiveZoomPercentageText;

    internal bool IsEffectiveToolbarVisible => _isEffectiveToolbarVisible;

    internal bool IsEffectiveToolbarTitleVisible => _isEffectiveToolbarTitleVisible;

    internal bool IsEffectiveToolbarZoomControlsVisible => _isEffectiveToolbarZoomControlsVisible;

    internal bool IsEffectiveToolbarRotationVisible => _isEffectiveToolbarRotationVisible;

    internal bool IsMainImagePresented => _isMainImagePresented;

    internal bool IsEffectiveFilmstripVisible => _isEffectiveFilmstripVisible;

    internal bool IsEffectivePreviousViewportNavigationVisible =>
        _isEffectivePreviousViewportNavigationVisible;

    internal bool IsEffectiveNextViewportNavigationVisible =>
        _isEffectiveNextViewportNavigationVisible;

    internal bool IsAddImageButtonEnabled => _isAddImageButtonEnabled;

    internal ImageGalleryGlyphKind FilmstripPreviousGlyph => _filmstripPreviousGlyph;

    internal ImageGalleryGlyphKind FilmstripNextGlyph => _filmstripNextGlyph;

    internal IReadOnlyList<ImageGalleryDescriptor> Descriptors => _coordinator.Descriptors;

    internal IImage? CurrentMainImage => _currentMainImageSlot?.Image;

    internal Size CurrentViewportSize => _viewportSize;

    internal Exception? ImageFailure => _imageFailure;

    internal ImageGalleryDescriptor? FindDescriptor(IImageGalleryItem item) =>
        _coordinator.FindDescriptor(item);

    internal ValueTask<ImageGalleryImageLease?> LoadThumbnailAsync(
        ImageGalleryDescriptor descriptor,
        PixelSize targetPixelSize,
        CancellationToken cancellationToken) =>
        _loadCoordinator.LoadThumbnailAsync(descriptor, targetPixelSize, cancellationToken);

    public ICommand PreviousCommand => _previousCommand;
    public ICommand NextCommand => _nextCommand;
    public ICommand ZoomInCommand => _zoomInCommand;
    public ICommand ZoomOutCommand => _zoomOutCommand;
    public ICommand FitCommand => _fitCommand;
    public ICommand ActualSizeCommand => _actualSizeCommand;
    public ICommand RotateClockwiseCommand => _rotateClockwiseCommand;

    public event EventHandler? CurrentImageResourceChanged;

    public bool TryAcquireCurrentImage(
        IImageGalleryItem expectedItem,
        out ImageGalleryImageLease? lease)
    {
        ArgumentNullException.ThrowIfNull(expectedItem);
        VerifyUiThread("acquire the current image resource");
        lease = null;

        var descriptor = _coordinator.CurrentDescriptor;
        var slot = _currentMainImageSlot;
        if (ImageState != ImageGalleryImageState.Ready || descriptor is null || slot is null ||
            !ReferenceEquals(descriptor, slot.Descriptor) ||
            !Equals(expectedItem.Key, slot.Key) ||
            !Equals(expectedItem.MainImageSource?.Identity, slot.SourceIdentity) ||
            !_loadCoordinator.IsCurrent(slot))
        {
            return false;
        }

        lease = slot.TryAcquire();
        return lease is not null;
    }

    bool ICustomHitTest.HitTest(Point point) =>
        MainImageMode == ImageGalleryMainImageMode.Presented &&
        new Rect(Bounds.Size).Contains(point);

    public void RotateClockwise()
    {
        if (!IsImageReady())
        {
            return;
        }

        SetAndRaise(RotationAngleProperty, ref _rotationAngle, (_rotationAngle + 90) % 360);
        ReconcilePublicZoom();
        ClampPanAndInvalidate();
        _loadCoordinator.ViewportRequirementsChanged();
    }

    internal void SetImageState(ImageGalleryImageState value)
    {
        if (SetAndRaise(ImageStateProperty, ref _imageState, value))
        {
            PseudoClasses.Set(":empty", value == ImageGalleryImageState.Empty);
            PseudoClasses.Set(":loading", value == ImageGalleryImageState.Loading);
            PseudoClasses.Set(":ready", value == ImageGalleryImageState.Ready);
            PseudoClasses.Set(":error", value == ImageGalleryImageState.Error);
            ApplyAppearances();
            RaiseImageCommandCanExecuteChanged();
        }
    }

    internal void OnCoordinatedSelectionChanged(ImageGalleryDescriptor? descriptor)
    {
        SetAndRaise(CurrentTitleProperty, ref _currentTitle, descriptor?.Title);
        _loadCoordinator.SelectionChanged(descriptor);
        ReconcileToolbarVisibility();
    }

    internal void OnDescriptorSetChanged()
    {
        ReconcileFilmstripVisibility();
        ReconcileToolbarVisibility();
    }

    internal void RequestSelectedThumbnailIntoView()
    {
        if (SelectedIndex >= 0)
        {
            RequestFilmstripScrollIntoView(SelectedIndex);
        }
    }

    internal void ClearCurrentMainImage(bool resetViewport, bool notify)
    {
        CommitMainImage(
            value: null,
            failure: null,
            state: ImageGalleryImageState.Empty,
            resetViewport: resetViewport,
            notify: notify);
    }

    internal void CommitLoadedMainImage(CurrentImageResourceSlot value, bool resetViewport)
    {
        ArgumentNullException.ThrowIfNull(value);
        CommitMainImage(
            value: value,
            failure: null,
            state: ImageGalleryImageState.Ready,
            resetViewport: resetViewport,
            notify: true);
    }

    internal bool CommitUpgradedMainImage(CurrentImageResourceSlot value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (_currentMainImageSlot is not { } current ||
            !IsDecodedQualityImprovement(current.DecodedPixelSize, value.DecodedPixelSize))
        {
            value.Dispose();
            return false;
        }

        CommitMainImage(
            value: value,
            failure: null,
            state: ImageGalleryImageState.Ready,
            resetViewport: false,
            notify: true);
        return true;
    }

    internal void CommitMainImageFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        CommitMainImage(
            value: null,
            failure: exception,
            state: ImageGalleryImageState.Error,
            resetViewport: true,
            notify: true);
    }

    internal void NotifyCurrentImageResourceUnavailable() =>
        CurrentImageResourceChanged?.Invoke(this, EventArgs.Empty);

    internal void CancelViewportInteractions()
    {
        _viewportPresenter?.CancelInteractions();
        SetPanning(false);
        SetPinchZooming(false);
    }

    internal void UpdateViewportSize(Size value)
    {
        _viewportSize = value;
        ReconcilePublicZoom();
        ClampPanAndInvalidate();
        _loadCoordinator.ViewportRequirementsChanged();
    }

    internal void RenderViewport(DrawingContext context, Size viewportSize)
    {
        if (MainImageMode != ImageGalleryMainImageMode.Presented ||
            _currentMainImageSlot is not { } slot ||
            ImageState is not (ImageGalleryImageState.Loading or ImageGalleryImageState.Ready))
        {
            return;
        }

        var image = slot.Image;
        var center = new Point(
            viewportSize.Width / 2 + _panOffset.X,
            viewportSize.Height / 2 + _panOffset.Y);
        var destination = new Rect(
            center.X - image.Size.Width * EffectiveZoomFactor / 2,
            center.Y - image.Size.Height * EffectiveZoomFactor / 2,
            image.Size.Width * EffectiveZoomFactor,
            image.Size.Height * EffectiveZoomFactor);

        using (context.PushTransform(Matrix.CreateRotation(
                   Math.PI * RotationAngle / 180,
                   center)))
        {
            context.DrawImage(image, new Rect(image.Size), destination);
        }
    }

    internal bool HandleViewportWheel(double delta, KeyModifiers modifiers, Point anchor)
    {
        if (!IsImageReady() || WheelZoomMode == ImageGalleryWheelZoomMode.Disabled || delta == 0)
        {
            return false;
        }

        if (WheelZoomMode == ImageGalleryWheelZoomMode.ControlModifier &&
            !modifiers.HasFlag(KeyModifiers.Control))
        {
            return false;
        }

        var requested = EffectiveZoomFactor * Math.Pow(ZoomStep, delta);
        ApplyZoomAt(requested, anchor);
        return true;
    }

    internal bool BeginViewportPan(Point _)
    {
        if (!IsPanEnabled || !IsImageReady() || !CanPan())
        {
            return false;
        }

        SetPanning(true);
        return true;
    }

    internal void ContinueViewportPan(Vector delta)
    {
        if (!IsPanning || CurrentMainImage is not { } image)
        {
            return;
        }

        _panOffset = ImageGalleryViewportMath.ClampPan(
            _panOffset + delta,
            _viewportSize,
            image.Size,
            RotationAngle,
            EffectiveZoomFactor);
        _viewportPresenter?.InvalidateVisual();
    }

    internal void EndViewportPan() => SetPanning(false);

    internal bool HandleViewportPinch(double scale, Point origin)
    {
        if (!IsPinchZoomEnabled || !IsImageReady() || !double.IsFinite(scale) || scale <= 0)
        {
            return false;
        }

        if (!IsPinchZooming)
        {
            _pinchStartZoom = EffectiveZoomFactor;
            SetPinchZooming(true);
        }

        ApplyZoomAt(_pinchStartZoom * scale, origin);
        return true;
    }

    internal bool EndViewportPinch()
    {
        if (!IsPinchZooming)
        {
            return false;
        }

        SetPinchZooming(false);
        return true;
    }

    internal void SetPanning(bool value) =>
        SetAndRaise(IsPanningProperty, ref _isPanning, value);

    internal void SetPinchZooming(bool value) =>
        SetAndRaise(IsPinchZoomingProperty, ref _isPinchZooming, value);

    internal void SetEffectiveViewportNavigationVisibility(bool previousVisible, bool nextVisible)
    {
        _isPreviousViewportNavigationSideAvailable = previousVisible;
        _isNextViewportNavigationSideAvailable = nextVisible;
        ReconcileViewportNavigationVisibility();
    }

    private void ReconcileViewportNavigationVisibility()
    {
        var navigationEnabled =
            MainImageMode == ImageGalleryMainImageMode.Presented &&
            IsViewportNavigationEnabled;
        SetAndRaise(
            IsEffectivePreviousViewportNavigationVisibleProperty,
            ref _isEffectivePreviousViewportNavigationVisible,
            navigationEnabled && _isPreviousViewportNavigationSideAvailable);
        SetAndRaise(
            IsEffectiveNextViewportNavigationVisibleProperty,
            ref _isEffectiveNextViewportNavigationVisible,
            navigationEnabled && _isNextViewportNavigationSideAvailable);
    }

    internal void SetResponsiveState(ImageGalleryResponsiveState value) =>
        SetResponsiveStateCore(value);

    private void SetResponsiveStateCore(ImageGalleryResponsiveState value)
    {
        if (SetAndRaise(ResponsiveStateProperty, ref _responsiveState, value))
        {
            PseudoClasses.Set(":normal", value == ImageGalleryResponsiveState.Normal);
            PseudoClasses.Set(":compact", value == ImageGalleryResponsiveState.Compact);
            PseudoClasses.Set(":minimal", value == ImageGalleryResponsiveState.Minimal);
        }
    }

    internal void SetEffectiveZoomFactor(double value) =>
        SetEffectiveZoomFactorCore(value);

    private void SetEffectiveZoomFactorCore(double value)
    {
        if (SetAndRaise(EffectiveZoomFactorProperty, ref _effectiveZoomFactor, value))
        {
            SetAndRaise(
                EffectiveZoomPercentageTextProperty,
                ref _effectiveZoomPercentageText,
                FormatZoomPercentage(value));
            _viewportPresenter?.InvalidateVisual();
            _loadCoordinator.ViewportRequirementsChanged();
        }
    }

    internal static string FormatZoomPercentage(double zoomFactor) =>
        string.Concat(
            Math.Truncate(zoomFactor * 100).ToString("0", CultureInfo.InvariantCulture),
            "%");

    private static bool IsPositiveFinite(double value) =>
        double.IsFinite(value) && value > 0;

    private static bool IsNonNegativeFinite(double value) =>
        double.IsFinite(value) && value >= 0;

    private static bool IsNullOrPositivePixelSize(PixelSize? value) =>
        value is null || value.Value.Width > 0 && value.Value.Height > 0;

    private bool IsImageReady() =>
        MainImageMode == ImageGalleryMainImageMode.Presented &&
        ImageState == ImageGalleryImageState.Ready;

    private void CommitMainImage(
        CurrentImageResourceSlot? value,
        Exception? failure,
        ImageGalleryImageState? state,
        bool resetViewport,
        bool notify)
    {
        var previous = _currentMainImageSlot;
        _currentMainImageSlot = value;
        _imageFailure = failure;
        try
        {
            if (resetViewport)
            {
                _panOffset = default;
                SetAndRaise(RotationAngleProperty, ref _rotationAngle, 0);
            }

            if (value is not null || resetViewport)
            {
                ReconcilePublicZoom();
            }
            if (state is { } nextState)
            {
                SetImageState(nextState);
            }

            if (value is null && !resetViewport)
            {
                _viewportPresenter?.InvalidateVisual();
            }
            else
            {
                ClampPanAndInvalidate();
            }
        }
        finally
        {
            if (!ReferenceEquals(previous, value))
            {
                previous?.Dispose();
            }
        }

        if (notify)
        {
            CurrentImageResourceChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnMainImageModeChanged()
    {
        var resourceOnly = MainImageMode == ImageGalleryMainImageMode.ResourceOnly;
        SetAndRaise(IsMainImagePresentedProperty, ref _isMainImagePresented, !resourceOnly);
        PseudoClasses.Set(":resource-only", resourceOnly);
        PseudoClasses.Set(":presented", !resourceOnly);
        if (resourceOnly)
        {
            CancelViewportInteractions();
        }

        ReconcileViewportNavigationVisibility();
        ReconcileToolbarVisibility();
        ApplyAppearances();
        RaiseImageCommandCanExecuteChanged();
        _viewportPresenter?.InvalidateVisual();
        _loadCoordinator.ViewportRequirementsChanged();
    }

    private static bool IsDecodedQualityImprovement(PixelSize current, PixelSize candidate) =>
        candidate.Width >= current.Width && candidate.Height >= current.Height &&
        (candidate.Width > current.Width || candidate.Height > current.Height);

    private void ReconcilePublicZoom()
    {
        if (_suppressZoomReconciliation)
        {
            return;
        }

        var value = ZoomMode switch
        {
            ImageGalleryZoomMode.ActualSize => 1,
            ImageGalleryZoomMode.Custom => Math.Clamp(
                CustomZoomFactor,
                ZoomRange.Minimum,
                ZoomRange.Maximum),
            _ when CurrentMainImage is { } image => ImageGalleryViewportMath.CalculateFitZoom(
                _viewportSize,
                image.Size,
                RotationAngle,
                IsFitUpscalingEnabled),
            _ => 1
        };

        var clamped = Math.Clamp(value, ZoomRange.Minimum, ZoomRange.Maximum);
        SetEffectiveZoomFactor(clamped);
        ClampPanAndInvalidate();
    }

    private void ReconcileZoomRequirements()
    {
        ReconcilePublicZoom();
        _loadCoordinator.ViewportRequirementsChanged();
    }

    private void ChangeZoom(bool increase)
    {
        var requested = increase
            ? EffectiveZoomFactor * ZoomStep
            : EffectiveZoomFactor / ZoomStep;
        var clamped = Math.Clamp(requested, ZoomRange.Minimum, ZoomRange.Maximum);

        ApplyZoomAt(clamped, new Point(_viewportSize.Width / 2, _viewportSize.Height / 2));
    }

    private void ApplyFit()
    {
        SetCurrentValue(ZoomModeProperty, ImageGalleryZoomMode.Fit);
        ReconcilePublicZoom();
    }

    private void ApplyActualSize()
    {
        SetCurrentValue(ZoomModeProperty, ImageGalleryZoomMode.ActualSize);
        ReconcilePublicZoom();
    }

    private void ReconcileEffectiveToolbarPlacement()
    {
        var value = ToolbarPlacement;
        if (IsThumbnailFilmstripVisible && value == ThumbnailFilmstripPlacement)
        {
            value = value switch
            {
                ImageGalleryEdgePlacement.Top => ImageGalleryEdgePlacement.Bottom,
                ImageGalleryEdgePlacement.Bottom => ImageGalleryEdgePlacement.Top,
                ImageGalleryEdgePlacement.Left => ImageGalleryEdgePlacement.Right,
                _ => ImageGalleryEdgePlacement.Left
            };
        }

        SetAndRaise(
            EffectiveToolbarPlacementProperty,
            ref _effectiveToolbarPlacement,
            value);
        var vertical = value is ImageGalleryEdgePlacement.Left or ImageGalleryEdgePlacement.Right;
        PseudoClasses.Set(":toolbar-vertical", vertical);
        PseudoClasses.Set(":toolbar-horizontal", !vertical);
        ReconcileEffectiveToolbarTitleVisibility();
        InvalidateArrange();
    }

    private void ReconcileFilmstripPlacement()
    {
        var vertical = ThumbnailFilmstripPlacement is ImageGalleryEdgePlacement.Left or ImageGalleryEdgePlacement.Right;
        SetAndRaise(
            FilmstripPreviousGlyphProperty,
            ref _filmstripPreviousGlyph,
            vertical ? ImageGalleryGlyphKind.Up : ImageGalleryGlyphKind.Previous);
        SetAndRaise(
            FilmstripNextGlyphProperty,
            ref _filmstripNextGlyph,
            vertical ? ImageGalleryGlyphKind.Down : ImageGalleryGlyphKind.Next);
        InvalidateFilmstripLayout();
    }

    private bool CanNavigatePrevious()
    {
        var count = Items.Count;
        return count > 1 && (SelectedIndex > 0 || IsLoopNavigationEnabled);
    }

    private bool CanNavigateNext()
    {
        var count = Items.Count;
        return count > 1 && (SelectedIndex < count - 1 || IsLoopNavigationEnabled);
    }

    private void RequestPreviousSelection()
    {
        var count = Items.Count;
        if (count <= 1)
        {
            return;
        }

        SelectedIndex = SelectedIndex > 0 ? SelectedIndex - 1 : count - 1;
    }

    private void RequestNextSelection()
    {
        var count = Items.Count;
        if (count <= 1)
        {
            return;
        }

        SelectedIndex = SelectedIndex < count - 1 ? SelectedIndex + 1 : 0;
    }

    internal void RaiseNavigationCanExecuteChanged()
    {
        _previousCommand.RaiseCanExecuteChanged();
        _nextCommand.RaiseCanExecuteChanged();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            _coordinator.AttachItemsSource(change.NewValue as IEnumerable);
        }
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = typeof(ImageGalleryThumbnailItem);
        return item is not ImageGalleryThumbnailItem;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new ImageGalleryThumbnailItem();

    protected override void PrepareContainerForItemOverride(Control element, object? item, int index)
    {
        base.PrepareContainerForItemOverride(element, item, index);
        if (element is not ImageGalleryThumbnailItem thumbnail || item is not IImageGalleryItem galleryItem)
        {
            throw new InvalidOperationException("ImageGallery generated an invalid thumbnail container.");
        }

        thumbnail.Prepare(this, galleryItem);
    }

    protected override void ClearContainerForItemOverride(Control element)
    {
        if (element is ImageGalleryThumbnailItem thumbnail)
        {
            thumbnail.Clear();
        }

        base.ClearContainerForItemOverride(element);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        UnwireFilmstripWheelHandling();
        base.OnApplyTemplate(e);
        _viewportPresenter = e.NameScope.Find<ImageGalleryViewportPresenter>("PART_Viewport") ??
                             throw MissingTemplatePart("PART_Viewport", typeof(ImageGalleryViewportPresenter));
        _viewportHost = e.NameScope.Find<Border>("PART_ViewportHost") ??
                        throw MissingTemplatePart("PART_ViewportHost", typeof(Border));
        _emptyTextBlock = e.NameScope.Find<TextBlock>("PART_EmptyText") ??
                          throw MissingTemplatePart("PART_EmptyText", typeof(TextBlock));
        _loadingTextBlock = e.NameScope.Find<TextBlock>("PART_LoadingText") ??
                            throw MissingTemplatePart("PART_LoadingText", typeof(TextBlock));
        _errorTextBlock = e.NameScope.Find<TextBlock>("PART_ErrorText") ??
                          throw MissingTemplatePart("PART_ErrorText", typeof(TextBlock));
        _toolbarHost = e.NameScope.Find<Border>("PART_Toolbar") ??
                       throw MissingTemplatePart("PART_Toolbar", typeof(Border));
        _toolbarItems = e.NameScope.Find<ImageGalleryToolbarPanel>("PART_ToolbarItems") ??
                        throw MissingTemplatePart("PART_ToolbarItems", typeof(ImageGalleryToolbarPanel));
        _filmstripHost = e.NameScope.Find<Border>("PART_Filmstrip") ??
                         throw MissingTemplatePart("PART_Filmstrip", typeof(Border));
        _filmstripScrollViewer = e.NameScope.Find<ImageGalleryFilmstripScrollViewer>("PART_ScrollViewer") ??
                                   throw MissingTemplatePart("PART_ScrollViewer", typeof(ImageGalleryFilmstripScrollViewer));
        _ = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter") ??
            throw MissingTemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter));
        _viewportDefaults = BorderDefaults.Capture(_viewportHost);
        _toolbarDefaults = BorderDefaults.Capture(_toolbarHost);
        _filmstripDefaults = BorderDefaults.Capture(_filmstripHost);
        _emptyForegroundDefault = _emptyTextBlock?.Foreground;
        _loadingForegroundDefault = _loadingTextBlock?.Foreground;
        _errorForegroundDefault = _errorTextBlock?.Foreground;
        if (_viewportPresenter is not null)
        {
            UpdateViewportSize(_viewportPresenter.Bounds.Size);
        }
        WireFilmstripWheelHandling();
        RewireAppearanceSubscriptions();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _coordinator.AttachItemsSource(ItemsSource);
        RewireAppearanceSubscriptions();
        RewireAddImageCommand();
        _loadCoordinator.Attach();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _loadCoordinator.Detach();
        UnsubscribeAddImageCommand();
        UnsubscribeAppearances();
        _coordinator.SuspendItemsSourceNotifications();
        base.OnDetachedFromVisualTree(e);
    }

    private void RaiseImageCommandCanExecuteChanged()
    {
        _zoomInCommand.RaiseCanExecuteChanged();
        _zoomOutCommand.RaiseCanExecuteChanged();
        _fitCommand.RaiseCanExecuteChanged();
        _actualSizeCommand.RaiseCanExecuteChanged();
        _rotateClockwiseCommand.RaiseCanExecuteChanged();
    }

    private void ReconcileToolbarVisibility()
    {
        var presented = MainImageMode == ImageGalleryMainImageMode.Presented;
        SetAndRaise(
            IsEffectiveToolbarZoomControlsVisibleProperty,
            ref _isEffectiveToolbarZoomControlsVisible,
            presented && IsToolbarZoomControlsVisible);
        SetAndRaise(
            IsEffectiveToolbarRotationVisibleProperty,
            ref _isEffectiveToolbarRotationVisible,
            presented && IsToolbarRotationVisible);
        var hasDefaultContent = Descriptors.Count > 0 &&
                                (IsEffectiveToolbarTitleVisible ||
                                 IsEffectiveToolbarZoomControlsVisible ||
                                 IsEffectiveToolbarRotationVisible);
        var value = IsToolbarVisible && (hasDefaultContent || ToolbarContent is not null);
        SetAndRaise(IsEffectiveToolbarVisibleProperty, ref _isEffectiveToolbarVisible, value);
        InvalidateMeasure();
    }

    private void ReconcileEffectiveToolbarTitleVisibility()
    {
        var isVertical = EffectiveToolbarPlacement is
            ImageGalleryEdgePlacement.Left or ImageGalleryEdgePlacement.Right;
        SetAndRaise(
            IsEffectiveToolbarTitleVisibleProperty,
            ref _isEffectiveToolbarTitleVisible,
            IsToolbarTitleVisible && !isVertical);
        ReconcileToolbarVisibility();
    }

    private void ReconcileFilmstripVisibility()
    {
        SetAndRaise(
            IsEffectiveFilmstripVisibleProperty,
            ref _isEffectiveFilmstripVisible,
            IsThumbnailFilmstripVisible && Descriptors.Count > 0);
        ReconcileEffectiveToolbarPlacement();
        InvalidateMeasure();
    }

    private void RewireAddImageCommand()
    {
        UnsubscribeAddImageCommand();
        if (TopLevel.GetTopLevel(this) is not null && AddImageCommand is { } command)
        {
            _subscribedAddImageCommand = command;
            command.CanExecuteChanged += OnAddImageCommandCanExecuteChanged;
        }

        ReconcileAddImageCommand();
    }

    private void UnsubscribeAddImageCommand()
    {
        if (_subscribedAddImageCommand is not null)
        {
            _subscribedAddImageCommand.CanExecuteChanged -= OnAddImageCommandCanExecuteChanged;
            _subscribedAddImageCommand = null;
        }
    }

    private void OnAddImageCommandCanExecuteChanged(object? sender, EventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ReconcileAddImageCommand();
        }
        else
        {
            Dispatcher.UIThread.Post(ReconcileAddImageCommand, DispatcherPriority.Render);
        }
    }

    private void ReconcileAddImageCommand()
    {
        var enabled = AddImageCommand?.CanExecute(AddImageCommandParameter) == true;
        SetAndRaise(IsAddImageButtonEnabledProperty, ref _isAddImageButtonEnabled, enabled);
    }

    private static InvalidOperationException MissingTemplatePart(string name, Type type) =>
        new($"ImageGallery template must contain '{name}' with type '{type.FullName}'.");

    private void WireFilmstripWheelHandling()
    {
        _filmstripHost?.AddHandler(
            PointerWheelChangedEvent,
            OnFilmstripPointerWheelChanged,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private void UnwireFilmstripWheelHandling()
    {
        _filmstripHost?.RemoveHandler(PointerWheelChangedEvent, OnFilmstripPointerWheelChanged);
        _filmstripScrollViewer = null;
    }

    private void OnFilmstripPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_filmstripScrollViewer?.TryScrollFromWheel(e.Delta) == true)
        {
            e.Handled = true;
        }
    }

    private void UpdateThumbnailSelection()
    {
        foreach (var container in GetRealizedContainers().OfType<ImageGalleryThumbnailItem>())
        {
            container.UpdateSelection();
        }
    }

    private void InvalidateFilmstripLayout()
    {
        foreach (var container in GetRealizedContainers().OfType<ImageGalleryThumbnailItem>())
        {
            container.ApplySlotMetrics();
        }

        InvalidateMeasure();
    }

    private void RequestFilmstripScrollIntoView(int index)
    {
        var request = ++_filmstripScrollRequest;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (request == _filmstripScrollRequest && index == SelectedIndex &&
                    TopLevel.GetTopLevel(this) is not null)
                {
                    _filmstripScrollViewer?.TryScrollIndexIntoView(index);
                }
            },
            DispatcherPriority.Loaded);
    }

    private void RewireAppearanceSubscriptions()
    {
        UnsubscribeAppearances();
        foreach (var appearance in EnumerateAppearances())
        {
            if (_subscribedAppearances.Add(appearance))
            {
                appearance.PropertyChanged += OnAppearancePropertyChanged;
            }
        }

        ApplyAppearances();
    }

    private IEnumerable<AvaloniaObject> EnumerateAppearances()
    {
        if (ViewportAppearance is not null) yield return ViewportAppearance;
        if (ToolbarAppearance is not null) yield return ToolbarAppearance;
        if (ToolbarButtonAppearance is not null) yield return ToolbarButtonAppearance;
        if (ViewportNavigationButtonAppearance is not null) yield return ViewportNavigationButtonAppearance;
        if (FilmstripNavigationButtonAppearance is not null) yield return FilmstripNavigationButtonAppearance;
        if (FilmstripAppearance is not null) yield return FilmstripAppearance;
        if (ThumbnailItemAppearance is not null) yield return ThumbnailItemAppearance;
        if (AddImageButtonAppearance is not null) yield return AddImageButtonAppearance;
    }

    private void UnsubscribeAppearances()
    {
        foreach (var appearance in _subscribedAppearances)
        {
            appearance.PropertyChanged -= OnAppearancePropertyChanged;
        }

        _subscribedAppearances.Clear();
    }

    private void OnAppearancePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyAppearances();
        }
        else
        {
            Dispatcher.UIThread.Post(ApplyAppearances, DispatcherPriority.Render);
        }
    }

    private void ApplyAppearances()
    {
        ApplyViewportAppearance();
        ApplyToolbarAppearance();
        ApplyFilmstripAppearance();
        foreach (var thumbnail in GetRealizedContainers().OfType<ImageGalleryThumbnailItem>())
        {
            thumbnail.Opacity = ThumbnailItemAppearance is { } appearance &&
                                appearance.IsSet(ImageGalleryThumbnailItemAppearance.OpacityProperty)
                ? appearance.Opacity
                : 1;
            thumbnail.InvalidateVisual();
        }
    }

    private void ApplyViewportAppearance()
    {
        if (_viewportHost is null)
        {
            return;
        }

        var appearance = ViewportAppearance;
        ApplyBorder(
            _viewportHost,
            _viewportDefaults,
            appearance,
            ImageGalleryViewportAppearance.BackgroundProperty,
            ImageGalleryViewportAppearance.BorderBrushProperty,
            ImageGalleryViewportAppearance.BorderThicknessProperty,
            ImageGalleryViewportAppearance.CornerRadiusProperty,
            ImageGalleryViewportAppearance.OpacityProperty);
        var presented = MainImageMode == ImageGalleryMainImageMode.Presented;
        _viewportHost.IsHitTestVisible = presented;
        if (!presented)
        {
            _viewportHost.Background = null;
        }
        if (_emptyTextBlock is not null)
        {
            _emptyTextBlock.Foreground = SparseValue(
                appearance,
                ImageGalleryViewportAppearance.EmptyForegroundProperty,
                _emptyForegroundDefault);
        }

        if (_loadingTextBlock is not null)
        {
            _loadingTextBlock.Foreground = SparseValue(
                appearance,
                ImageGalleryViewportAppearance.LoadingForegroundProperty,
                _loadingForegroundDefault);
        }

        if (_errorTextBlock is not null)
        {
            _errorTextBlock.Foreground = SparseValue(
                appearance,
                ImageGalleryViewportAppearance.ErrorForegroundProperty,
                _errorForegroundDefault);
        }
    }

    private void ApplyToolbarAppearance()
    {
        if (_toolbarHost is null)
        {
            return;
        }

        var appearance = ToolbarAppearance;
        ApplyBorder(
            _toolbarHost,
            _toolbarDefaults,
            appearance,
            ImageGalleryToolbarAppearance.BackgroundProperty,
            ImageGalleryToolbarAppearance.BorderBrushProperty,
            ImageGalleryToolbarAppearance.BorderThicknessProperty,
            ImageGalleryToolbarAppearance.CornerRadiusProperty,
            ImageGalleryToolbarAppearance.OpacityProperty);
        _toolbarHost.Padding = SparseValue(appearance, ImageGalleryToolbarAppearance.PaddingProperty, _toolbarDefaults.Padding);
        _toolbarHost.Width = SparseValue(appearance, ImageGalleryToolbarAppearance.WidthProperty, _toolbarDefaults.Width);
        _toolbarHost.Height = SparseValue(appearance, ImageGalleryToolbarAppearance.HeightProperty, _toolbarDefaults.Height);
        _toolbarHost.MinWidth = SparseValue(appearance, ImageGalleryToolbarAppearance.MinWidthProperty, _toolbarDefaults.MinWidth);
        _toolbarHost.MinHeight = SparseValue(appearance, ImageGalleryToolbarAppearance.MinHeightProperty, _toolbarDefaults.MinHeight);
        _toolbarHost.MaxWidth = SparseValue(appearance, ImageGalleryToolbarAppearance.MaxWidthProperty, _toolbarDefaults.MaxWidth);
        _toolbarHost.MaxHeight = SparseValue(appearance, ImageGalleryToolbarAppearance.MaxHeightProperty, _toolbarDefaults.MaxHeight);
        if (_toolbarItems is not null)
        {
            _toolbarItems.Spacing = SparseValue(appearance, ImageGalleryToolbarAppearance.ItemSpacingProperty, ToolbarItemSpacing);
            foreach (var text in _toolbarItems.GetVisualDescendants()
                         .OfType<ImageGalleryToolbarText>()
                         .Where(text =>
                             text.Classes.Contains("ImageGalleryToolbarTitle") ||
                             text.Classes.Contains("ImageGalleryToolbarZoomPercentage")))
            {
                text.Foreground = SparseValue(appearance, ImageGalleryToolbarAppearance.ForegroundProperty, (IBrush?)null);
            }

            foreach (var text in _toolbarItems.GetVisualDescendants().OfType<TextBlock>())
            {
                text.Foreground = SparseValue(appearance, ImageGalleryToolbarAppearance.ForegroundProperty, (IBrush?)null);
            }
        }
    }

    private void ApplyFilmstripAppearance()
    {
        if (_filmstripHost is null)
        {
            return;
        }

        var appearance = FilmstripAppearance;
        ApplyBorder(
            _filmstripHost,
            _filmstripDefaults,
            appearance,
            ImageGalleryFilmstripAppearance.BackgroundProperty,
            ImageGalleryFilmstripAppearance.BorderBrushProperty,
            ImageGalleryFilmstripAppearance.BorderThicknessProperty,
            ImageGalleryFilmstripAppearance.CornerRadiusProperty,
            ImageGalleryFilmstripAppearance.OpacityProperty);
        _filmstripHost.Padding = SparseValue(appearance, ImageGalleryFilmstripAppearance.PaddingProperty, _filmstripDefaults.Padding);
    }

    private static void ApplyBorder<TAppearance>(
        Border border,
        BorderDefaults defaults,
        TAppearance? appearance,
        StyledProperty<IBrush?> backgroundProperty,
        StyledProperty<IBrush?> borderBrushProperty,
        StyledProperty<Thickness> borderThicknessProperty,
        StyledProperty<CornerRadius> cornerRadiusProperty,
        StyledProperty<double> opacityProperty)
        where TAppearance : AvaloniaObject
    {
        border.Background = SparseValue(appearance, backgroundProperty, defaults.Background);
        border.BorderBrush = SparseValue(appearance, borderBrushProperty, defaults.BorderBrush);
        border.BorderThickness = SparseValue(appearance, borderThicknessProperty, defaults.BorderThickness);
        border.CornerRadius = SparseValue(appearance, cornerRadiusProperty, defaults.CornerRadius);
        border.Opacity = SparseValue(appearance, opacityProperty, defaults.Opacity);
    }

    private static T SparseValue<T>(
        AvaloniaObject? appearance,
        StyledProperty<T> property,
        T fallback) => appearance is not null && appearance.IsSet(property)
        ? appearance.GetValue(property)
        : fallback;

    private static void VerifyUiThread(string operation)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            throw new InvalidOperationException(
                $"ImageGallery must {operation} on the Avalonia UI thread.");
        }
    }

    private void ApplyZoomAt(double requested, Point anchor)
    {
        if (CurrentMainImage is not { } image)
        {
            return;
        }

        var clamped = Math.Clamp(requested, ZoomRange.Minimum, ZoomRange.Maximum);
        _panOffset = ImageGalleryViewportMath.ZoomAroundPoint(
            _panOffset,
            anchor,
            _viewportSize,
            EffectiveZoomFactor,
            clamped);
        _suppressZoomReconciliation = true;
        try
        {
            SetCurrentValue(CustomZoomFactorProperty, clamped);
            SetCurrentValue(ZoomModeProperty, ImageGalleryZoomMode.Custom);
        }
        finally
        {
            _suppressZoomReconciliation = false;
        }

        SetEffectiveZoomFactor(clamped);
        _panOffset = ImageGalleryViewportMath.ClampPan(
            _panOffset,
            _viewportSize,
            image.Size,
            RotationAngle,
            clamped);
        _viewportPresenter?.InvalidateVisual();
    }

    private bool CanPan()
    {
        if (CurrentMainImage is not { } image)
        {
            return false;
        }

        var rotated = ImageGalleryViewportMath.GetRotatedSize(image.Size, RotationAngle);
        return rotated.Width * EffectiveZoomFactor > _viewportSize.Width ||
               rotated.Height * EffectiveZoomFactor > _viewportSize.Height;
    }

    private void ClampPanAndInvalidate()
    {
        if (CurrentMainImage is { } image)
        {
            _panOffset = ImageGalleryViewportMath.ClampPan(
                _panOffset,
                _viewportSize,
                image.Size,
                RotationAngle,
                EffectiveZoomFactor);
        }
        else
        {
            _panOffset = default;
        }

        _viewportPresenter?.InvalidateVisual();
    }

    private readonly record struct BorderDefaults(
        IBrush? Background,
        IBrush? BorderBrush,
        Thickness BorderThickness,
        CornerRadius CornerRadius,
        Thickness Padding,
        double Opacity,
        double Width,
        double Height,
        double MinWidth,
        double MaxWidth,
        double MinHeight,
        double MaxHeight)
    {
        public static BorderDefaults Capture(Border? border) => border is null
            ? default
            : new BorderDefaults(
                border.Background,
                border.BorderBrush,
                border.BorderThickness,
                border.CornerRadius,
                border.Padding,
                border.Opacity,
                border.Width,
                border.Height,
                border.MinWidth,
                border.MaxWidth,
                border.MinHeight,
                border.MaxHeight);
    }
}
