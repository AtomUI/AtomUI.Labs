using AtomUI.Labs.Controls.ImageGallery.Appearance;
using AtomUI.Labs.Controls.ImageGallery.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AtomUI.Labs.Controls.ImageGallery.Filmstrip;

internal sealed class ImageGalleryThumbnailItem : Control
{
    private ImageGallery? _owner;
    private ImageGalleryDescriptor? _descriptor;
    private ImageGalleryImageLease? _lease;
    private CancellationTokenSource? _loadCancellation;
    private PixelSize? _lastTarget;
    private long _generation;
    private ThumbnailState _state;
    private bool _isSelected;

    public void Prepare(ImageGallery owner, IImageGalleryItem item)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(item);
        Clear();
        _owner = owner;
        _descriptor = owner.FindDescriptor(item) ??
                      throw new InvalidOperationException("The thumbnail item has no matching descriptor.");
        _isSelected = Equals(owner.SelectedItem, item);
        Opacity = owner.ThumbnailItemAppearance is { } appearance &&
                  appearance.IsSet(ImageGalleryThumbnailItemAppearance.OpacityProperty)
            ? appearance.Opacity
            : 1;
        ApplySlotMetrics();
        if (TopLevel.GetTopLevel(this) is not null)
        {
            StartLoad();
        }

        InvalidateVisual();
    }

    public void Clear()
    {
        _generation++;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        _lease?.Dispose();
        _lease = null;
        _descriptor = null;
        _owner = null;
        _lastTarget = null;
        _state = ThumbnailState.Empty;
        _isSelected = false;
        InvalidateVisual();
    }

    public void UpdateSelection() 
    {
        var selected = _owner is not null && _descriptor is not null &&
                       Equals(_owner.SelectedItem, _descriptor.Item);
        if (_isSelected != selected)
        {
            _isSelected = selected;
            InvalidateVisual();
        }
    }

    public void ApplySlotMetrics()
    {
        if (_owner is null)
        {
            return;
        }

        if (_owner.ThumbnailFilmstripPlacement is ImageGalleryEdgePlacement.Top or ImageGalleryEdgePlacement.Bottom)
        {
            Width = _owner.ThumbnailItemExtent;
            Height = double.NaN;
            Margin = new Thickness(0, 0, _owner.ThumbnailItemSpacing, 0);
        }
        else
        {
            Width = double.NaN;
            Height = _owner.ThumbnailItemExtent;
            Margin = new Thickness(0, 0, 0, _owner.ThumbnailItemSpacing);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var appearance = _owner?.ThumbnailItemAppearance;
        var background = _isSelected
            ? appearance?.SelectedBackground ?? Brushes.Transparent
            : IsPointerOver
                ? appearance?.PointerOverBackground ?? Brushes.Transparent
                : appearance?.Background ?? Brushes.Transparent;
        var borderBrush = appearance?.BorderBrush;
        var borderThickness = appearance?.BorderThickness ?? default;
        var cornerRadius = appearance?.CornerRadius ?? default;
        var outerRect = new Rect(Bounds.Size);
        context.DrawRectangle(
            background,
            borderBrush is null || Maximum(borderThickness) <= 0
                ? null
                : new Pen(borderBrush, Maximum(borderThickness)),
            new RoundedRect(outerRect, cornerRadius));

        var padding = appearance?.Padding ?? default;
        var imageRect = new Rect(
            padding.Left,
            padding.Top,
            Math.Max(0, Bounds.Width - padding.Left - padding.Right),
            Math.Max(0, Bounds.Height - padding.Top - padding.Bottom));

        if (_lease is { } lease && imageRect.Width > 0 && imageRect.Height > 0)
        {
            using (context.PushClip(imageRect))
            {
                DrawUniformToFill(context, lease.Image, imageRect);
            }
        }
        else if (_state == ThumbnailState.Loading)
        {
            var radius = Math.Max(2, Math.Min(Bounds.Width, Bounds.Height) * 0.06);
            context.DrawEllipse(
                appearance?.LoadingForeground ?? Brushes.Gray,
                null,
                outerRect.Center,
                radius,
                radius);
        }
        else if (_state == ThumbnailState.Error)
        {
            var pen = new Pen(appearance?.ErrorForeground ?? Brushes.Gray, 2);
            var inset = Math.Max(6, Math.Min(Bounds.Width, Bounds.Height) * 0.25);
            context.DrawLine(pen, new Point(inset, inset), new Point(Bounds.Width - inset, Bounds.Height - inset));
            context.DrawLine(pen, new Point(Bounds.Width - inset, inset), new Point(inset, Bounds.Height - inset));
        }

        if (_isSelected)
        {
            var indicator = appearance?.SelectionIndicatorBrush ?? Brushes.DodgerBlue;
            var thickness = Maximum(appearance?.SelectionIndicatorThickness ?? new Thickness(2));
            context.DrawRectangle(
                null,
                new Pen(indicator, thickness),
                new RoundedRect(outerRect.Deflate(thickness / 2), cornerRadius));
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        StartLoad();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelLoadAndRelease();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        StartLoad();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left &&
            _owner is { } owner && _descriptor is { } descriptor)
        {
            owner.SelectedItem = descriptor.Item;
            e.Handled = true;
            return;
        }

        base.OnPointerReleased(e);
    }

    private void StartLoad()
    {
        if (_owner is not { } owner || _descriptor is not { } descriptor ||
            Bounds.Width <= 0 || Bounds.Height <= 0 || TopLevel.GetTopLevel(this) is null)
        {
            return;
        }

        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var target = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(Bounds.Width * scaling)),
            Math.Max(1, (int)Math.Ceiling(Bounds.Height * scaling)));
        if (_lastTarget == target && (_lease is not null || _state == ThumbnailState.Loading))
        {
            return;
        }

        _lastTarget = target;
        _generation++;
        var generation = _generation;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        _lease?.Dispose();
        _lease = null;
        _state = ThumbnailState.Loading;
        InvalidateVisual();
        _ = LoadAsync(owner, descriptor, target, generation, _loadCancellation.Token);
    }

    private async Task LoadAsync(
        ImageGallery owner,
        ImageGalleryDescriptor descriptor,
        PixelSize target,
        long generation,
        CancellationToken cancellationToken)
    {
        ImageGalleryImageLease? lease = null;
        try
        {
            lease = await owner.LoadThumbnailAsync(descriptor, target, cancellationToken);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (generation != _generation || !ReferenceEquals(descriptor, _descriptor))
                {
                    lease?.Dispose();
                    lease = null;
                    return;
                }

                _lease?.Dispose();
                _lease = lease;
                lease = null;
                _state = _lease is null ? ThumbnailState.Empty : ThumbnailState.Ready;
                InvalidateVisual();
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            lease?.Dispose();
        }
        catch
        {
            lease?.Dispose();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (generation == _generation)
                {
                    _state = ThumbnailState.Error;
                    InvalidateVisual();
                }
            });
        }
    }

    private void CancelLoadAndRelease()
    {
        _generation++;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        _lease?.Dispose();
        _lease = null;
        _lastTarget = null;
        _state = ThumbnailState.Empty;
    }

    private static void DrawUniformToFill(DrawingContext context, IImage image, Rect destination)
    {
        if (image.Size.Width <= 0 || image.Size.Height <= 0 ||
            destination.Width <= 0 || destination.Height <= 0)
        {
            return;
        }

        var scale = Math.Max(
            destination.Width / image.Size.Width,
            destination.Height / image.Size.Height);
        var sourceWidth = destination.Width / scale;
        var sourceHeight = destination.Height / scale;
        var source = new Rect(
            (image.Size.Width - sourceWidth) / 2,
            (image.Size.Height - sourceHeight) / 2,
            sourceWidth,
            sourceHeight);
        context.DrawImage(image, source, destination);
    }

    private static double Maximum(Thickness value) =>
        Math.Max(Math.Max(value.Left, value.Top), Math.Max(value.Right, value.Bottom));

    private enum ThumbnailState
    {
        Empty,
        Loading,
        Ready,
        Error,
    }
}
