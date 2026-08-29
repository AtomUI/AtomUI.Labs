using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AtomUI.Labs.Controls.ImageGallery.Viewport;

internal sealed class ImageGalleryViewportPresenter : Control
{
    private Point? _lastPointerPosition;
    private IPointer? _capturedPointer;

    public ImageGalleryViewportPresenter()
    {
        ClipToBounds = true;
        Focusable = true;
        GestureRecognizers.Add(new PinchGestureRecognizer());
        Pinch += OnPinch;
        PinchEnded += OnPinchEnded;
    }

    private ImageGallery? Owner => TemplatedParent as ImageGallery;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        Owner?.RenderViewport(context, Bounds.Size);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Owner?.UpdateViewportSize(e.NewSize);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var owner = Owner;
        if (owner is not null && owner.HandleViewportWheel(e.Delta.Y, e.KeyModifiers, e.GetPosition(this)))
        {
            e.Handled = true;
            return;
        }

        base.OnPointerWheelChanged(e);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var owner = Owner;
        var point = e.GetCurrentPoint(this);
        if (owner is not null && point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed &&
            owner.BeginViewportPan(point.Position))
        {
            _lastPointerPosition = point.Position;
            _capturedPointer = e.Pointer;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_lastPointerPosition is { } previous && Owner is { } owner)
        {
            var current = e.GetPosition(this);
            owner.ContinueViewportPan(current - previous);
            _lastPointerPosition = current;
            e.Handled = true;
            return;
        }

        base.OnPointerMoved(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_lastPointerPosition is not null)
        {
            _lastPointerPosition = null;
            _capturedPointer = null;
            Owner?.EndViewportPan();
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        base.OnPointerReleased(e);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _lastPointerPosition = null;
        _capturedPointer = null;
        Owner?.EndViewportPan();
        base.OnPointerCaptureLost(e);
    }

    internal void CancelInteractions()
    {
        _lastPointerPosition = null;
        var pointer = _capturedPointer;
        _capturedPointer = null;
        pointer?.Capture(null);
    }

    private void OnPinch(object? sender, PinchEventArgs e)
    {
        if (Owner?.HandleViewportPinch(e.Scale, e.ScaleOrigin) == true)
        {
            e.Handled = true;
        }
    }

    private void OnPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        if (Owner?.EndViewportPinch() == true)
        {
            e.Handled = true;
        }
    }
}
