using AtomUI.Labs.Controls.Led;
using AtomUI.Labs.Controls.Led.Glow;
using AtomUI.Labs.Controls.Led.Marquee;
using AtomUI.Labs.Controls.Led.Matrix.Character;
using AtomUI.Labs.Controls.Led.Matrix.Layout;
using AtomUI.Labs.Controls.Led.Matrix.Rendering;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaMatrix = Avalonia.Matrix;

namespace AtomUI.Labs.Controls.Led.Matrix;

public class MatrixDisplay : Control
{
    #region 公共属性定义

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MatrixDisplay, string?>(nameof(Text));

    public static readonly StyledProperty<double> DotSizeProperty =
        AvaloniaProperty.Register<MatrixDisplay, double>(nameof(DotSize), 6);

    public static readonly StyledProperty<double> DotSpacingProperty =
        AvaloniaProperty.Register<MatrixDisplay, double>(nameof(DotSpacing), 2);

    public static readonly StyledProperty<MatrixDotShape> DotShapeProperty =
        AvaloniaProperty.Register<MatrixDisplay, MatrixDotShape>(nameof(DotShape));

    public static readonly StyledProperty<double> DotCornerRadiusRatioProperty =
        AvaloniaProperty.Register<MatrixDisplay, double>(
            nameof(DotCornerRadiusRatio),
            MatrixDotShapeResolver.DefaultCornerRadiusRatio);

    public static readonly StyledProperty<double> CharacterSpacingProperty =
        AvaloniaProperty.Register<MatrixDisplay, double>(nameof(CharacterSpacing), 8);

    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<MatrixDisplay, Thickness>(nameof(Padding));

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<MatrixDisplay, HorizontalAlignment>(nameof(HorizontalContentAlignment), HorizontalAlignment.Left);

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        AvaloniaProperty.Register<MatrixDisplay, VerticalAlignment>(nameof(VerticalContentAlignment), VerticalAlignment.Top);

    public static readonly StyledProperty<MatrixOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<MatrixDisplay, MatrixOverflowMode>(nameof(OverflowMode));

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<MatrixDisplay, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<MatrixDisplay, IBrush?>(nameof(BorderBrush));

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<MatrixDisplay, Thickness>(nameof(BorderThickness));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<MatrixDisplay, CornerRadius>(nameof(CornerRadius));

    public static readonly StyledProperty<IBrush?> ActiveBrushProperty =
        AvaloniaProperty.Register<MatrixDisplay, IBrush?>(nameof(ActiveBrush));

    public static readonly StyledProperty<IBrush?> InactiveBrushProperty =
        AvaloniaProperty.Register<MatrixDisplay, IBrush?>(nameof(InactiveBrush));

    public static readonly StyledProperty<IBrush?> GlowBrushProperty =
        AvaloniaProperty.Register<MatrixDisplay, IBrush?>(nameof(GlowBrush));

    public static readonly StyledProperty<double> GlowOpacityProperty =
        AvaloniaProperty.Register<MatrixDisplay, double>(nameof(GlowOpacity), LedGlowValueSanitizer.DefaultOpacity);

    public static readonly StyledProperty<double> GlowRadiusProperty =
        AvaloniaProperty.Register<MatrixDisplay, double>(nameof(GlowRadius), LedGlowValueSanitizer.DefaultRadius);

    public static readonly StyledProperty<bool> ShowInactiveDotsProperty =
        AvaloniaProperty.Register<MatrixDisplay, bool>(nameof(ShowInactiveDots), true);

    public static readonly StyledProperty<bool> IsMarqueeEnabledProperty =
        AvaloniaProperty.Register<MatrixDisplay, bool>(nameof(IsMarqueeEnabled));

    public static readonly StyledProperty<double> MarqueeSpeedProperty =
        AvaloniaProperty.Register<MatrixDisplay, double>(nameof(MarqueeSpeed), LedMarqueeValueSanitizer.DefaultSpeed);

    public static readonly StyledProperty<TimeSpan> MarqueeRepeatDelayProperty =
        AvaloniaProperty.Register<MatrixDisplay, TimeSpan>(
            nameof(MarqueeRepeatDelay),
            LedMarqueeValueSanitizer.DefaultRepeatDelay);

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double DotSize
    {
        get => GetValue(DotSizeProperty);
        set => SetValue(DotSizeProperty, value);
    }

    public double DotSpacing
    {
        get => GetValue(DotSpacingProperty);
        set => SetValue(DotSpacingProperty, value);
    }

    public MatrixDotShape DotShape
    {
        get => GetValue(DotShapeProperty);
        set => SetValue(DotShapeProperty, value);
    }

    public double DotCornerRadiusRatio
    {
        get => GetValue(DotCornerRadiusRatioProperty);
        set => SetValue(DotCornerRadiusRatioProperty, value);
    }

    public double CharacterSpacing
    {
        get => GetValue(CharacterSpacingProperty);
        set => SetValue(CharacterSpacingProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public MatrixOverflowMode OverflowMode
    {
        get => GetValue(OverflowModeProperty);
        set => SetValue(OverflowModeProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public IBrush? ActiveBrush
    {
        get => GetValue(ActiveBrushProperty);
        set => SetValue(ActiveBrushProperty, value);
    }

    public IBrush? InactiveBrush
    {
        get => GetValue(InactiveBrushProperty);
        set => SetValue(InactiveBrushProperty, value);
    }

    public IBrush? GlowBrush
    {
        get => GetValue(GlowBrushProperty);
        set => SetValue(GlowBrushProperty, value);
    }

    public double GlowOpacity
    {
        get => GetValue(GlowOpacityProperty);
        set => SetValue(GlowOpacityProperty, value);
    }

    public double GlowRadius
    {
        get => GetValue(GlowRadiusProperty);
        set => SetValue(GlowRadiusProperty, value);
    }

    public bool ShowInactiveDots
    {
        get => GetValue(ShowInactiveDotsProperty);
        set => SetValue(ShowInactiveDotsProperty, value);
    }

    public bool IsMarqueeEnabled
    {
        get => GetValue(IsMarqueeEnabledProperty);
        set => SetValue(IsMarqueeEnabledProperty, value);
    }

    public double MarqueeSpeed
    {
        get => GetValue(MarqueeSpeedProperty);
        set => SetValue(MarqueeSpeedProperty, value);
    }

    public TimeSpan MarqueeRepeatDelay
    {
        get => GetValue(MarqueeRepeatDelayProperty);
        set => SetValue(MarqueeRepeatDelayProperty, value);
    }

    #endregion

    #region 内部属性定义

    internal int LayoutCacheVersion { get; private set; }

    internal int GeometryBuildCount { get; private set; }

    internal int GeometryCacheCount => _geometryCache.Count;

    internal IEnumerable<MatrixGlyphGeometry> GeometryCacheValues => _geometryCache.Values;

    internal int BorderGeometryBuildCount { get; private set; }

    internal Geometry? BorderGeometryCache => _borderGeometryCache;

    internal int GlowEffectBuildCount => _glowRenderer?.EffectBuildCount ?? 0;

    internal int GlowEffectScopeCount => _glowRenderer?.EffectScopeCount ?? 0;

    internal static readonly StyledProperty<double> MarqueeProgressProperty =
        AvaloniaProperty.Register<MatrixDisplay, double>("MarqueeProgress");

    internal double MarqueeProgress
    {
        get => GetValue(MarqueeProgressProperty);
        set => SetValue(MarqueeProgressProperty, value);
    }

    internal bool IsMarqueeAnimationRunning => _marqueeController?.IsRunning == true;

    internal object? MarqueeController => _marqueeController;

    internal int MarqueeVisibilitySubscriptionCount => _visibilityAncestors.Count;

    #endregion

    private bool _hasLayoutCache;
    private LedGlowRenderer? _glowRenderer;
    private LedMarqueeController? _marqueeController;
    private Size _arrangedSize;
    private bool _isAttachedToVisualTree;
    private readonly List<Visual> _visibilityAncestors = new();
    private MatrixLayoutCacheKey _layoutCacheKey;
    private MatrixDisplayLayout? _layoutCache;
    private readonly Dictionary<MatrixGlyphGeometryCacheKey, MatrixGlyphGeometry> _geometryCache = new();
    private bool _hasBorderGeometryCache;
    private MatrixPanelBorderGeometryCacheKey _borderGeometryCacheKey;
    private Geometry? _borderGeometryCache;
    private IBrush? _borderPenBrush;
    private double _borderPenThickness = double.NaN;
    private Pen? _borderPen;

    static MatrixDisplay()
    {
        AffectsMeasure<MatrixDisplay>(
            TextProperty,
            DotSizeProperty,
            DotSpacingProperty,
            CharacterSpacingProperty,
            PaddingProperty,
            BorderThicknessProperty);
        AffectsRender<MatrixDisplay>(
            HorizontalContentAlignmentProperty,
            VerticalContentAlignmentProperty,
            OverflowModeProperty,
            DotShapeProperty,
            DotCornerRadiusRatioProperty,
            BackgroundProperty,
            BorderBrushProperty,
            BorderThicknessProperty,
            CornerRadiusProperty,
            ActiveBrushProperty,
            InactiveBrushProperty,
            GlowBrushProperty,
            GlowOpacityProperty,
            GlowRadiusProperty,
            ShowInactiveDotsProperty,
            IsMarqueeEnabledProperty,
            MarqueeSpeedProperty,
            MarqueeRepeatDelayProperty,
            MarqueeProgressProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredSize = GetLayout().DesiredSize;
        var borderThickness = GetEffectiveBorderThickness();
        return new Size(
            desiredSize.Width + borderThickness.Left + borderThickness.Right,
            desiredSize.Height + borderThickness.Top + borderThickness.Bottom);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new MatrixDisplayAutomationPeer(this);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var arrangedSize = base.ArrangeOverride(finalSize);
        _arrangedSize = arrangedSize;
        UpdateMarqueeAnimation();
        return arrangedSize;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _isAttachedToVisualTree = true;
        UpdateAncestorVisibilitySubscriptions();
        UpdateMarqueeAnimation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttachedToVisualTree = false;
        UnsubscribeFromAncestorVisibility();
        ReleaseMarqueeController();
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        RenderBackground(context);
        RenderContent(context);
        RenderBorder(context);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            ClearLayoutCache();
            if (ControlAutomationPeer.FromElement(this) is MatrixDisplayAutomationPeer automationPeer)
            {
                automationPeer.NotifyTextChanged(change.GetOldValue<string?>(), change.GetNewValue<string?>());
            }
        }
        else if (change.Property == DotSizeProperty
                 || change.Property == DotSpacingProperty
                 || change.Property == CharacterSpacingProperty
                 || change.Property == PaddingProperty)
        {
            ClearLayoutCache();
        }

        if (change.Property == DotSizeProperty || change.Property == DotSpacingProperty)
        {
            _geometryCache.Clear();
        }
        else if (change.Property == DotShapeProperty)
        {
            _geometryCache.Clear();
        }
        else if (change.Property == DotCornerRadiusRatioProperty
                 && MatrixDotShapeResolver.CoerceShape(DotShape) == MatrixDotShape.RoundedSquare)
        {
            _geometryCache.Clear();
        }

        if (change.Property == BorderThicknessProperty || change.Property == CornerRadiusProperty)
        {
            ClearBorderRenderCache();
        }
        else if (change.Property == BorderBrushProperty)
        {
            ClearBorderPenCache();
            if (BorderBrush is null)
            {
                ClearBorderGeometryCache();
            }
        }

        if (change.Property == GlowBrushProperty && GlowBrush is null)
        {
            _glowRenderer = null;
        }

        if (change.Property == IsMarqueeEnabledProperty)
        {
            UpdateAncestorVisibilitySubscriptions();
        }

        if (change.Property == TextProperty
            || change.Property == DotSizeProperty
            || change.Property == DotSpacingProperty
            || change.Property == CharacterSpacingProperty
            || change.Property == PaddingProperty
            || change.Property == BorderThicknessProperty
            || change.Property == IsMarqueeEnabledProperty
            || change.Property == MarqueeSpeedProperty
            || change.Property == MarqueeRepeatDelayProperty
            || change.Property == IsVisibleProperty)
        {
            UpdateMarqueeAnimation();
        }
    }

    private void RenderContent(DrawingContext context)
    {
        var activeBrush = ActiveBrush;
        if (activeBrush is null)
        {
            return;
        }

        var layout = GetLayout();
        var options = GetLayoutOptions();
        var contentViewport = GetContentViewport();
        var glow = GetGlowRenderOptions();
        if (IsEffectiveMarqueeEnabled() && layout.Slots.Count > 0)
        {
            RenderMarqueeContent(context, layout, options, contentViewport, activeBrush, glow);
            return;
        }

        var scale = OverflowMode == MatrixOverflowMode.ScaleDown
            ? LedDisplayLayoutMath.CalculateScaleDown(layout.DesiredSize, contentViewport.Size)
            : 1;
        if (scale <= 0 || contentViewport.Width <= 0 || contentViewport.Height <= 0)
        {
            return;
        }

        var alignmentOffset = LedDisplayLayoutMath.CalculateAlignmentOffset(
            layout.DesiredSize,
            contentViewport.Size,
            scale,
            HorizontalContentAlignment,
            VerticalContentAlignment);
        var offset = new Vector(
            contentViewport.X + alignmentOffset.X,
            contentViewport.Y + alignmentOffset.Y);
        var visibleBounds = CalculateVisibleBounds(contentViewport, scale, offset);
        var effectiveGlowRadius = glow.EffectiveRadius;
        if (effectiveGlowRadius > 0)
        {
            visibleBounds = visibleBounds.Inflate(effectiveGlowRadius);
        }
        using (context.PushClip(contentViewport))
        using (PushLayoutTransform(context, scale, offset))
        {
            RenderVisibleGlyphs(context, layout, visibleBounds, options, activeBrush, glow);
        }
    }

    private void RenderMarqueeContent(
        DrawingContext context,
        MatrixDisplayLayout layout,
        MatrixLayoutOptions options,
        Rect contentViewport,
        IBrush activeBrush,
        MatrixGlowRenderOptions glow)
    {
        if (contentViewport.Width <= 0 || contentViewport.Height <= 0)
        {
            return;
        }

        var verticalOffset = LedDisplayLayoutMath.CalculateAlignmentOffset(
            layout.DesiredSize,
            contentViewport.Size,
            1,
            HorizontalAlignment.Left,
            VerticalContentAlignment).Y;
        var plan = LeftThroughMarqueeMotion.Calculate(new MarqueeMotionContext(
            contentViewport.Width,
            layout.DesiredSize.Width,
            MarqueeProgress));

        using (context.PushClip(contentViewport))
        {
            for (var i = 0; i < plan.PlacementCount; i++)
            {
                var offset = new Vector(
                    contentViewport.X + plan.GetX(i),
                    contentViewport.Y + verticalOffset);
                var visibleBounds = CalculateVisibleBounds(contentViewport, 1, offset);
                var effectiveGlowRadius = glow.EffectiveRadius;
                if (effectiveGlowRadius > 0)
                {
                    visibleBounds = visibleBounds.Inflate(effectiveGlowRadius);
                }

                using (PushLayoutTransform(context, 1, offset))
                {
                    RenderVisibleGlyphs(context, layout, visibleBounds, options, activeBrush, glow);
                }
            }
        }
    }

    private void UpdateMarqueeAnimation()
    {
        if (!_isAttachedToVisualTree
            || !IsEffectivelyVisible
            || !IsEffectiveMarqueeEnabled()
            || string.IsNullOrEmpty(Text))
        {
            ReleaseMarqueeController();
            return;
        }

        var size = _arrangedSize.Width > 0 && _arrangedSize.Height > 0 ? _arrangedSize : Bounds.Size;
        var viewport = GetContentViewport(size);
        var layout = GetLayout();
        if (viewport.Width <= 0 || viewport.Height <= 0 || layout.Slots.Count == 0)
        {
            ReleaseMarqueeController();
            return;
        }

        _marqueeController ??= new LedMarqueeController(this, MarqueeProgressProperty);
        _marqueeController.Update(viewport.Width, layout.DesiredSize.Width, MarqueeSpeed, MarqueeRepeatDelay);
        if (!_marqueeController.IsRunning)
        {
            ReleaseMarqueeController();
        }
    }

    private void ReleaseMarqueeController()
    {
        _marqueeController?.Dispose();
        _marqueeController = null;
    }

    private bool IsEffectiveMarqueeEnabled()
    {
        return IsMarqueeEnabled && LedMarqueeValueSanitizer.CoerceSpeed(MarqueeSpeed) > 0;
    }

    private void UpdateAncestorVisibilitySubscriptions()
    {
        if (_isAttachedToVisualTree && IsMarqueeEnabled)
        {
            SubscribeToAncestorVisibility();
        }
        else
        {
            UnsubscribeFromAncestorVisibility();
        }
    }

    private void SubscribeToAncestorVisibility()
    {
        UnsubscribeFromAncestorVisibility();
        foreach (var ancestor in this.GetVisualAncestors())
        {
            ancestor.PropertyChanged += HandleAncestorPropertyChanged;
            _visibilityAncestors.Add(ancestor);
        }
    }

    private void UnsubscribeFromAncestorVisibility()
    {
        foreach (var ancestor in _visibilityAncestors)
        {
            ancestor.PropertyChanged -= HandleAncestorPropertyChanged;
        }

        _visibilityAncestors.Clear();
    }

    private void HandleAncestorPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty)
        {
            UpdateMarqueeAnimation();
        }
    }

    private MatrixDisplayLayout GetLayout()
    {
        var key = new MatrixLayoutCacheKey(Text, GetLayoutOptions());
        if (_hasLayoutCache && _layoutCacheKey == key && _layoutCache is not null)
        {
            return _layoutCache;
        }

        var layout = MatrixLayoutEngine.Calculate(key.Text, key.Options);
        _layoutCacheKey = key;
        _layoutCache    = layout;
        _hasLayoutCache = true;
        LayoutCacheVersion++;
        return layout;
    }

    private MatrixLayoutOptions GetLayoutOptions()
    {
        return new MatrixLayoutOptions(
            MatrixValueSanitizer.CoerceAtLeast(DotSize, 1),
            MatrixValueSanitizer.CoerceNonNegative(DotSpacing),
            MatrixValueSanitizer.CoerceNonNegative(CharacterSpacing),
            MatrixValueSanitizer.CoerceThickness(Padding));
    }

    private void RenderBackground(DrawingContext context)
    {
        var background = Background;
        if (background is null)
        {
            return;
        }

        context.DrawRectangle(
            background,
            null,
            new RoundedRect(
                new Rect(0, 0, Bounds.Width, Bounds.Height),
                MatrixValueSanitizer.CoerceCornerRadius(CornerRadius)));
    }

    private void RenderBorder(DrawingContext context)
    {
        var borderBrush = BorderBrush;
        var borderThickness = GetEffectiveBorderThickness();
        if (borderBrush is null
            || Bounds.Width <= 0
            || Bounds.Height <= 0
            || !HasVisibleBorder(borderThickness))
        {
            return;
        }

        var cornerRadius = MatrixValueSanitizer.CoerceCornerRadius(CornerRadius);
        if (borderThickness.IsUniform
            && borderThickness.Top * 2 < Bounds.Width
            && borderThickness.Top * 2 < Bounds.Height)
        {
            ClearBorderGeometryCache();
            RenderUniformBorder(context, borderBrush, borderThickness.Top, cornerRadius);
            return;
        }

        ClearBorderPenCache();
        var geometry = GetBorderGeometry(borderThickness, cornerRadius);
        context.DrawGeometry(borderBrush, null, geometry);
    }

    private void RenderUniformBorder(
        DrawingContext context,
        IBrush borderBrush,
        double borderThickness,
        CornerRadius cornerRadius)
    {
        if (!ReferenceEquals(_borderPenBrush, borderBrush) || _borderPenThickness != borderThickness)
        {
            _borderPenBrush = borderBrush;
            _borderPenThickness = borderThickness;
            _borderPen = new Pen(borderBrush, borderThickness);
        }

        var halfThickness = borderThickness / 2;
        var rect = new Rect(Bounds.Size).Deflate(halfThickness);
        var centerRadius = new CornerRadius(
            Math.Max(0, cornerRadius.TopLeft - halfThickness),
            Math.Max(0, cornerRadius.TopRight - halfThickness),
            Math.Max(0, cornerRadius.BottomRight - halfThickness),
            Math.Max(0, cornerRadius.BottomLeft - halfThickness));
        context.DrawRectangle(null, _borderPen, new RoundedRect(rect, centerRadius));
    }

    private Geometry GetBorderGeometry(Thickness borderThickness, CornerRadius cornerRadius)
    {
        var key = new MatrixPanelBorderGeometryCacheKey(Bounds.Size, borderThickness, cornerRadius);
        if (_hasBorderGeometryCache && _borderGeometryCacheKey == key && _borderGeometryCache is not null)
        {
            return _borderGeometryCache;
        }

        _borderGeometryCache = MatrixPanelBorderGeometryFactory.Create(
            Bounds.Size,
            borderThickness,
            cornerRadius);
        _borderGeometryCacheKey = key;
        _hasBorderGeometryCache = true;
        BorderGeometryBuildCount++;
        return _borderGeometryCache;
    }

    private static IDisposable PushLayoutTransform(DrawingContext context, double scale, Vector offset)
    {
        return context.PushTransform(AvaloniaMatrix.CreateScale(scale, scale) * AvaloniaMatrix.CreateTranslation(offset.X, offset.Y));
    }

    private static Rect CalculateVisibleBounds(Rect viewport, double scale, Vector offset)
    {
        return new Rect(
            (viewport.Left - offset.X) / scale,
            (viewport.Top - offset.Y) / scale,
            viewport.Width / scale,
            viewport.Height / scale);
    }

    private MatrixGlowRenderOptions GetGlowRenderOptions()
    {
        return new MatrixGlowRenderOptions(
            GlowBrush,
            LedGlowValueSanitizer.CoerceOpacity(GlowOpacity),
            LedGlowValueSanitizer.CoerceRadius(GlowRadius));
    }

    private LedGlowRenderScope PushGlow(
        DrawingContext context,
        Rect activeBounds,
        in MatrixGlowRenderOptions glow)
    {
        if (!glow.IsActive)
        {
            return default;
        }

        _glowRenderer ??= new LedGlowRenderer();
        return _glowRenderer.Push(context, glow.Brush, glow.Opacity, glow.Radius, activeBounds);
    }

    private void RenderVisibleGlyphs(
        DrawingContext context,
        MatrixDisplayLayout layout,
        Rect visibleBounds,
        MatrixLayoutOptions options,
        IBrush activeBrush,
        in MatrixGlowRenderOptions glow)
    {
        if (layout.Slots.Count == 0)
        {
            return;
        }

        var firstSlot = layout.Slots[0];
        if (firstSlot.Origin.Y >= visibleBounds.Bottom
            || firstSlot.Origin.Y + layout.GlyphSize.Height <= visibleBounds.Top)
        {
            return;
        }

        var firstVisibleIndex = FindFirstVisibleSlot(layout, visibleBounds.Left);
        var lastVisibleIndex = FindLastVisibleSlot(layout, visibleBounds.Right, firstVisibleIndex);
        RenderInactiveGlyphs(context, layout, options, firstVisibleIndex, lastVisibleIndex);

        if (TryCalculateActiveBounds(layout, options, firstVisibleIndex, lastVisibleIndex, out var activeBounds))
        {
            using (var glowScope = PushGlow(context, activeBounds, glow))
            {
                if (glowScope.IsActive)
                {
                    RenderActiveGlyphs(context, layout, options, firstVisibleIndex, lastVisibleIndex, glow.Brush!);
                }
            }
        }

        RenderActiveGlyphs(context, layout, options, firstVisibleIndex, lastVisibleIndex, activeBrush);
    }

    private readonly record struct MatrixGlowRenderOptions(IBrush? Brush, double Opacity, double Radius)
    {
        public bool IsActive => Brush is not null && Opacity > 0 && Radius > 0;

        public double EffectiveRadius => IsActive ? Radius : 0;
    }

    private static int FindLastVisibleSlot(MatrixDisplayLayout layout, double visibleRight, int firstVisibleIndex)
    {
        var index = firstVisibleIndex;
        while (index < layout.Slots.Count && layout.Slots[index].Origin.X < visibleRight)
        {
            index++;
        }

        return index;
    }

    private void RenderInactiveGlyphs(
        DrawingContext context,
        MatrixDisplayLayout layout,
        MatrixLayoutOptions options,
        int start,
        int end)
    {
        var inactiveBrush = InactiveBrush;
        if (!ShowInactiveDots || inactiveBrush is null)
        {
            return;
        }

        for (var i = start; i < end; i++)
        {
            var slot = layout.Slots[i];
            var geometry = GetGlyphGeometry(slot.Pattern.Glyph, options);
            if (geometry.InactiveDotCount == 0)
            {
                continue;
            }

            using (context.PushTransform(AvaloniaMatrix.CreateTranslation(slot.Origin.X, slot.Origin.Y)))
            {
                context.DrawGeometry(inactiveBrush, null, geometry.InactiveGeometry);
            }
        }
    }

    private void RenderActiveGlyphs(
        DrawingContext context,
        MatrixDisplayLayout layout,
        MatrixLayoutOptions options,
        int start,
        int end,
        IBrush brush)
    {
        for (var i = start; i < end; i++)
        {
            var slot = layout.Slots[i];
            var geometry = GetGlyphGeometry(slot.Pattern.Glyph, options);
            if (geometry.ActiveDotCount == 0)
            {
                continue;
            }

            using (context.PushTransform(AvaloniaMatrix.CreateTranslation(slot.Origin.X, slot.Origin.Y)))
            {
                context.DrawGeometry(brush, null, geometry.ActiveGeometry);
            }
        }
    }

    private bool TryCalculateActiveBounds(
        MatrixDisplayLayout layout,
        MatrixLayoutOptions options,
        int start,
        int end,
        out Rect bounds)
    {
        bounds = default;
        var hasBounds = false;
        for (var i = start; i < end; i++)
        {
            var slot = layout.Slots[i];
            var geometry = GetGlyphGeometry(slot.Pattern.Glyph, options);
            if (geometry.ActiveDotCount == 0)
            {
                continue;
            }

            var translated = geometry.ActiveGeometry.Bounds.Translate(new Vector(slot.Origin.X, slot.Origin.Y));
            bounds = hasBounds ? bounds.Union(translated) : translated;
            hasBounds = true;
        }

        return hasBounds;
    }

    private static int FindFirstVisibleSlot(MatrixDisplayLayout layout, double visibleLeft)
    {
        var low  = 0;
        var high = layout.Slots.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (layout.Slots[middle].Origin.X + layout.GlyphSize.Width <= visibleLeft)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private MatrixGlyphGeometry GetGlyphGeometry(MatrixGlyph glyph, MatrixLayoutOptions options)
    {
        var dotShape = MatrixDotShapeResolver.CoerceShape(DotShape);
        var dotCornerRadiusRatio = MatrixDotShapeResolver.GetEffectiveCornerRadiusRatio(
            dotShape,
            DotCornerRadiusRatio);
        var key = new MatrixGlyphGeometryCacheKey(
            glyph.Bits,
            options.DotSize,
            options.DotSpacing,
            dotShape,
            dotCornerRadiusRatio);
        if (_geometryCache.TryGetValue(key, out var geometry))
        {
            return geometry;
        }

        geometry = MatrixGlyphGeometryFactory.Create(
            glyph,
            options.DotSize,
            options.DotSpacing,
            dotShape,
            dotCornerRadiusRatio);
        _geometryCache.Add(key, geometry);
        GeometryBuildCount++;
        return geometry;
    }

    private void ClearLayoutCache()
    {
        _hasLayoutCache = false;
        _layoutCache    = null;
    }

    private Thickness GetEffectiveBorderThickness()
    {
        return MatrixValueSanitizer.CoerceThickness(BorderThickness);
    }

    private Rect GetContentViewport()
    {
        return GetContentViewport(Bounds.Size);
    }

    private Rect GetContentViewport(Size size)
    {
        var borderThickness = GetEffectiveBorderThickness();
        return new Rect(
            borderThickness.Left,
            borderThickness.Top,
            Math.Max(0, size.Width - borderThickness.Left - borderThickness.Right),
            Math.Max(0, size.Height - borderThickness.Top - borderThickness.Bottom));
    }

    private static bool HasVisibleBorder(Thickness thickness)
    {
        return thickness.Left > 0
               || thickness.Top > 0
               || thickness.Right > 0
               || thickness.Bottom > 0;
    }

    private void ClearBorderRenderCache()
    {
        ClearBorderGeometryCache();
        ClearBorderPenCache();
    }

    private void ClearBorderPenCache()
    {
        _borderPen = null;
        _borderPenBrush = null;
        _borderPenThickness = double.NaN;
    }

    private void ClearBorderGeometryCache()
    {
        _hasBorderGeometryCache = false;
        _borderGeometryCache = null;
    }

    private readonly record struct MatrixLayoutCacheKey(
        string? Text,
        MatrixLayoutOptions Options);
}
