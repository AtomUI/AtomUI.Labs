using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Layout;
using AtomUI.Labs.Led;
using AtomUI.Labs.Led.Glow;
using AtomUI.Labs.Led.Segment.Character;
using AtomUI.Labs.Led.Segment.Layout;
using AtomUI.Labs.Led.Segment.Rendering;
using Avalonia.Media;
using AvaloniaMatrix = Avalonia.Matrix;

namespace AtomUI.Labs.Led.Segment;

public class SegmentDisplay : Control
{
    #region 公共属性定义

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<SegmentDisplay, string?>(nameof(Text));

    public static readonly StyledProperty<double> CharacterHeightProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(CharacterHeight), 72);

    public static readonly StyledProperty<double> CharacterAspectRatioProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(CharacterAspectRatio), 0.58);

    public static readonly StyledProperty<double> CharacterSpacingProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(CharacterSpacing), 8);

    public static readonly StyledProperty<double> SegmentThicknessProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(SegmentThickness), 8);

    public static readonly StyledProperty<double> SegmentGapProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(SegmentGap), 2);

    public static readonly StyledProperty<double> SegmentBevelRatioProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(SegmentBevelRatio), 0.5);

    public static readonly StyledProperty<double> DotScaleProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(DotScale), 0.72);

    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<SegmentDisplay, Thickness>(nameof(Padding));

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<SegmentDisplay, HorizontalAlignment>(nameof(HorizontalContentAlignment), HorizontalAlignment.Left);

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        AvaloniaProperty.Register<SegmentDisplay, VerticalAlignment>(nameof(VerticalContentAlignment), VerticalAlignment.Top);

    public static readonly StyledProperty<SegmentOverflowMode> OverflowModeProperty =
        AvaloniaProperty.Register<SegmentDisplay, SegmentOverflowMode>(nameof(OverflowMode));

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<SegmentDisplay, IBrush?>(nameof(Background));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<SegmentDisplay, CornerRadius>(nameof(CornerRadius));

    public static readonly StyledProperty<IBrush?> ActiveBrushProperty =
        AvaloniaProperty.Register<SegmentDisplay, IBrush?>(nameof(ActiveBrush));

    public static readonly StyledProperty<IBrush?> InactiveBrushProperty =
        AvaloniaProperty.Register<SegmentDisplay, IBrush?>(nameof(InactiveBrush));

    public static readonly StyledProperty<IBrush?> GlowBrushProperty =
        AvaloniaProperty.Register<SegmentDisplay, IBrush?>(nameof(GlowBrush));

    public static readonly StyledProperty<double> GlowOpacityProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(GlowOpacity), LedGlowValueSanitizer.DefaultOpacity);

    public static readonly StyledProperty<double> GlowRadiusProperty =
        AvaloniaProperty.Register<SegmentDisplay, double>(nameof(GlowRadius), LedGlowValueSanitizer.DefaultRadius);

    public static readonly StyledProperty<bool> ShowInactiveSegmentsProperty =
        AvaloniaProperty.Register<SegmentDisplay, bool>(nameof(ShowInactiveSegments), true);

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double CharacterHeight
    {
        get => GetValue(CharacterHeightProperty);
        set => SetValue(CharacterHeightProperty, value);
    }

    public double CharacterAspectRatio
    {
        get => GetValue(CharacterAspectRatioProperty);
        set => SetValue(CharacterAspectRatioProperty, value);
    }

    public double CharacterSpacing
    {
        get => GetValue(CharacterSpacingProperty);
        set => SetValue(CharacterSpacingProperty, value);
    }

    public double SegmentThickness
    {
        get => GetValue(SegmentThicknessProperty);
        set => SetValue(SegmentThicknessProperty, value);
    }

    public double SegmentGap
    {
        get => GetValue(SegmentGapProperty);
        set => SetValue(SegmentGapProperty, value);
    }

    public double SegmentBevelRatio
    {
        get => GetValue(SegmentBevelRatioProperty);
        set => SetValue(SegmentBevelRatioProperty, value);
    }

    public double DotScale
    {
        get => GetValue(DotScaleProperty);
        set => SetValue(DotScaleProperty, value);
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

    public SegmentOverflowMode OverflowMode
    {
        get => GetValue(OverflowModeProperty);
        set => SetValue(OverflowModeProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
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

    public bool ShowInactiveSegments
    {
        get => GetValue(ShowInactiveSegmentsProperty);
        set => SetValue(ShowInactiveSegmentsProperty, value);
    }

    #endregion

    #region 内部属性定义

    internal int LayoutCacheVersion { get; private set; }

    internal int GeometryCacheVersion { get; private set; }

    internal int VisibleGeometryBuildCount { get; private set; }

    internal Geometry? VisibleActiveGeometry => _visibleGeometryCache?.ActiveGeometry;

    internal Geometry? VisibleInactiveGeometry => _visibleInactiveGeometryCache;

    internal int GlowEffectBuildCount => _glowRenderer?.EffectBuildCount ?? 0;

    internal int GlowEffectScopeCount => _glowRenderer?.EffectScopeCount ?? 0;

    #endregion

    private bool _hasLayoutCache;
    private LedGlowRenderer? _glowRenderer;
    private SegmentLayoutCacheKey _layoutCacheKey;
    private SegmentDisplayLayout? _layoutCache;

    private bool _hasGeometryCache;
    private SegmentGeometryOptions _geometryCacheOptions;
    private IReadOnlyList<SegmentPreparedSlot>? _geometryCache;

    private bool _hasVisibleGeometryCache;
    private SegmentVisibleGeometryCacheKey _visibleGeometryCacheKey;
    private SegmentVisibleGeometry? _visibleGeometryCache;
    private bool _hasVisibleInactiveGeometryCache;
    private SegmentVisibleInactiveGeometryCacheKey _visibleInactiveGeometryCacheKey;
    private Geometry? _visibleInactiveGeometryCache;

    static SegmentDisplay()
    {
        AffectsMeasure<SegmentDisplay>(
            TextProperty,
            CharacterHeightProperty,
            CharacterAspectRatioProperty,
            CharacterSpacingProperty,
            PaddingProperty);
        AffectsRender<SegmentDisplay>(
            SegmentThicknessProperty,
            SegmentGapProperty,
            SegmentBevelRatioProperty,
            DotScaleProperty,
            HorizontalContentAlignmentProperty,
            VerticalContentAlignmentProperty,
            OverflowModeProperty,
            BackgroundProperty,
            CornerRadiusProperty,
            ActiveBrushProperty,
            InactiveBrushProperty,
            GlowBrushProperty,
            GlowOpacityProperty,
            GlowRadiusProperty,
            ShowInactiveSegmentsProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = GetLayout();
        return layout.DesiredSize;
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new SegmentDisplayAutomationPeer(this);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        RenderBackground(context);

        var activeBrush = ActiveBrush;
        if (activeBrush is null)
        {
            return;
        }

        var layout        = GetPreparedLayout();
        var preparedSlots = GetPreparedSlots(layout);
        var scale = OverflowMode == SegmentOverflowMode.ScaleDown
            ? LedDisplayLayoutMath.CalculateScaleDown(layout.DesiredSize, Bounds.Size)
            : 1;
        if (scale <= 0)
        {
            return;
        }

        var offset = LedDisplayLayoutMath.CalculateAlignmentOffset(
            layout.DesiredSize,
            Bounds.Size,
            scale,
            HorizontalContentAlignment,
            VerticalContentAlignment);
        var visibleBounds = new Rect(
            -offset.X / scale,
            -offset.Y / scale,
            Bounds.Width / scale,
            Bounds.Height / scale);
        var glowBrush = GlowBrush;
        var glowOpacity = LedGlowValueSanitizer.CoerceOpacity(GlowOpacity);
        var glowRadius = LedGlowValueSanitizer.CoerceRadius(GlowRadius);
        var hasGlow = glowBrush is not null && glowOpacity > 0 && glowRadius > 0;
        if (hasGlow)
        {
            visibleBounds = visibleBounds.Inflate(glowRadius);
        }

        var firstVisibleIndex = FindFirstVisibleSlot(layout, visibleBounds.Left);
        var lastVisibleIndex = FindLastVisibleSlot(layout, visibleBounds.Right, firstVisibleIndex);
        using (context.PushClip(new Rect(Bounds.Size)))
        using (context.PushTransform(
                   AvaloniaMatrix.CreateScale(scale, scale)
                   * AvaloniaMatrix.CreateTranslation(offset.X, offset.Y)))
        {
            var visibleGeometry = GetVisibleGeometry(
                layout,
                preparedSlots,
                firstVisibleIndex,
                lastVisibleIndex);
            if (ShowInactiveSegments
                && InactiveBrush is { } inactiveBrush
                && visibleGeometry.InactiveGeometry is { } inactiveGeometry)
            {
                context.DrawGeometry(inactiveBrush, null, inactiveGeometry);
            }

            if (visibleGeometry.ActiveGeometry is { } activeGeometry)
            {
                using (var glowScope = PushGlow(
                           context,
                           activeGeometry.Bounds,
                           glowBrush,
                           glowOpacity,
                           glowRadius))
                {
                    if (glowScope.IsActive)
                    {
                        context.DrawGeometry(glowBrush, null, activeGeometry);
                    }
                }

                context.DrawGeometry(activeBrush, null, activeGeometry);
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            ClearLayoutCache();
            if (ControlAutomationPeer.FromElement(this) is SegmentDisplayAutomationPeer automationPeer)
            {
                automationPeer.NotifyTextChanged(change.GetOldValue<string?>(), change.GetNewValue<string?>());
            }
        }
        else if (change.Property == CharacterHeightProperty
            || change.Property == CharacterAspectRatioProperty
            || change.Property == CharacterSpacingProperty
            || change.Property == PaddingProperty)
        {
            ClearLayoutCache();
            ClearGeometryCache();
        }
        else if (change.Property == SegmentThicknessProperty
                 || change.Property == SegmentGapProperty
                 || change.Property == SegmentBevelRatioProperty
                 || change.Property == DotScaleProperty)
        {
            ClearGeometryCache();
        }

        if (change.Property == GlowBrushProperty && GlowBrush is null)
        {
            _glowRenderer = null;
        }
    }

    private SegmentDisplayLayout GetLayout()
    {
        var key = new SegmentLayoutCacheKey(Text, GetLayoutOptions());
        if (_hasLayoutCache && _layoutCacheKey == key && _layoutCache is not null)
        {
            return _layoutCache;
        }

        var layout = SegmentLayoutEngine.Calculate(key.Text, key.Options);
        _layoutCacheKey = key;
        _layoutCache    = layout;
        _hasLayoutCache = true;
        LayoutCacheVersion++;
        return layout;
    }

    private SegmentDisplayLayout GetPreparedLayout()
    {
        return GetLayout();
    }

    private IReadOnlyList<SegmentPreparedSlot> GetPreparedSlots(SegmentDisplayLayout layout)
    {
        var geometryOptions = GetGeometryOptions();
        if (_hasGeometryCache
            && _geometryCacheOptions == geometryOptions
            && _geometryCache is not null
            && GeometryCacheMatchesLayout(_geometryCache, layout))
        {
            return _geometryCache;
        }

        var preparedSlots = new SegmentPreparedSlot[layout.Slots.Count];
        for (var i = 0; i < layout.Slots.Count; i++)
        {
            var slot = layout.Slots[i];
            var geometrySet = slot.Pattern.Kind == SegmentCharacterKind.Segments
                ? SegmentGeometryFactory.Create(slot.Bounds, geometryOptions)
                : null;
            var symbolGeometries = slot.Pattern.Kind switch
            {
                SegmentCharacterKind.Colon => SegmentGeometryFactory.CreateColon(slot.Bounds, geometryOptions),
                SegmentCharacterKind.Dot   => new[] { SegmentGeometryFactory.CreateDot(slot.Bounds, geometryOptions, false) },
                _                          => null
            };
            preparedSlots[i] = new SegmentPreparedSlot(slot.Pattern.Kind, slot.Bounds, geometrySet, symbolGeometries);
        }

        _geometryCacheOptions = geometryOptions;
        _geometryCache        = preparedSlots;
        _hasGeometryCache     = true;
        GeometryCacheVersion++;
        return preparedSlots;
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
                SegmentValueSanitizer.CoerceCornerRadius(CornerRadius)));
    }

    private SegmentLayoutOptions GetLayoutOptions()
    {
        return new SegmentLayoutOptions(
            SegmentValueSanitizer.CoerceNonNegative(CharacterHeight),
            SegmentValueSanitizer.CoerceAtLeast(CharacterAspectRatio, 0.1),
            SegmentValueSanitizer.CoerceNonNegative(CharacterSpacing),
            SegmentValueSanitizer.CoerceThickness(Padding));
    }

    private SegmentGeometryOptions GetGeometryOptions()
    {
        return new SegmentGeometryOptions(
            SegmentValueSanitizer.CoerceAtLeast(SegmentThickness, 1),
            SegmentValueSanitizer.CoerceNonNegative(SegmentGap),
            SegmentValueSanitizer.CoerceRange(SegmentBevelRatio, 0, 1),
            SegmentValueSanitizer.CoerceRange(DotScale, 0, 1));
    }

    private void ClearLayoutCache()
    {
        _hasLayoutCache = false;
        _layoutCache    = null;
    }

    private void ClearGeometryCache()
    {
        _hasGeometryCache = false;
        _geometryCache    = null;
        ClearVisibleGeometryCache();
    }

    private static bool GeometryCacheMatchesLayout(
        IReadOnlyList<SegmentPreparedSlot> preparedSlots,
        SegmentDisplayLayout layout)
    {
        if (preparedSlots.Count != layout.Slots.Count)
        {
            return false;
        }

        for (var i = 0; i < preparedSlots.Count; i++)
        {
            var preparedSlot = preparedSlots[i];
            var layoutSlot   = layout.Slots[i];
            if (preparedSlot.Kind != layoutSlot.Pattern.Kind
                || preparedSlot.Bounds != layoutSlot.Bounds)
            {
                return false;
            }
        }

        return true;
    }

    private SegmentVisibleGeometry GetVisibleGeometry(
        SegmentDisplayLayout layout,
        IReadOnlyList<SegmentPreparedSlot> preparedSlots,
        int start,
        int end)
    {
        var key = new SegmentVisibleGeometryCacheKey(
            LayoutCacheVersion,
            GeometryCacheVersion,
            start,
            end);
        if (_hasVisibleGeometryCache
            && _visibleGeometryCacheKey == key
            && _visibleGeometryCache is not null)
        {
            return _visibleGeometryCache;
        }

        var active = new GeometryGroup { FillRule = FillRule.NonZero };
        var inactive = GetVisibleInactiveGeometry(preparedSlots, start, end);
        for (var i = start; i < end; i++)
        {
            var pattern = layout.Slots[i].Pattern;
            var slot = preparedSlots[i];
            if (pattern.Kind == SegmentCharacterKind.Segments && slot.GeometrySet is not null)
            {
                foreach (var item in slot.GeometrySet.Items)
                {
                    if ((pattern.Parts & item.Part) != 0)
                    {
                        active.Children.Add(item.Geometry);
                    }
                }
            }
            else if (slot.SymbolGeometries is not null)
            {
                foreach (var geometry in slot.SymbolGeometries)
                {
                    active.Children.Add(geometry);
                }
            }
        }

        _visibleGeometryCache = new SegmentVisibleGeometry(
            active.Children.Count > 0 ? active : null,
            inactive);
        _visibleGeometryCacheKey = key;
        _hasVisibleGeometryCache = true;
        VisibleGeometryBuildCount++;
        return _visibleGeometryCache;
    }

    private Geometry? GetVisibleInactiveGeometry(
        IReadOnlyList<SegmentPreparedSlot> preparedSlots,
        int start,
        int end)
    {
        var key = new SegmentVisibleInactiveGeometryCacheKey(
            GeometryCacheVersion,
            start,
            end);
        if (_hasVisibleInactiveGeometryCache && _visibleInactiveGeometryCacheKey == key)
        {
            return _visibleInactiveGeometryCache;
        }

        var inactive = new GeometryGroup { FillRule = FillRule.NonZero };
        for (var i = start; i < end; i++)
        {
            var geometrySet = preparedSlots[i].GeometrySet;
            if (geometrySet is null)
            {
                continue;
            }

            foreach (var item in geometrySet.Items)
            {
                inactive.Children.Add(item.Geometry);
            }
        }

        _visibleInactiveGeometryCache = inactive.Children.Count > 0 ? inactive : null;
        _visibleInactiveGeometryCacheKey = key;
        _hasVisibleInactiveGeometryCache = true;
        return _visibleInactiveGeometryCache;
    }

    private void ClearVisibleGeometryCache()
    {
        _hasVisibleGeometryCache = false;
        _visibleGeometryCache = null;
        _hasVisibleInactiveGeometryCache = false;
        _visibleInactiveGeometryCache = null;
    }

    private LedGlowRenderScope PushGlow(
        DrawingContext context,
        Rect activeBounds,
        IBrush? glowBrush,
        double opacity,
        double radius)
    {
        if (glowBrush is null || opacity <= 0 || radius <= 0)
        {
            return default;
        }

        _glowRenderer ??= new LedGlowRenderer();
        return _glowRenderer.Push(context, glowBrush, opacity, radius, activeBounds);
    }

    private static int FindFirstVisibleSlot(SegmentDisplayLayout layout, double visibleLeft)
    {
        var low = 0;
        var high = layout.Slots.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (layout.Slots[middle].Bounds.Right <= visibleLeft)
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

    private static int FindLastVisibleSlot(
        SegmentDisplayLayout layout,
        double visibleRight,
        int firstVisibleIndex)
    {
        var index = firstVisibleIndex;
        while (index < layout.Slots.Count && layout.Slots[index].Bounds.Left < visibleRight)
        {
            index++;
        }

        return index;
    }

    private readonly record struct SegmentLayoutCacheKey(
        string? Text,
        SegmentLayoutOptions Options);

    private readonly record struct SegmentPreparedSlot(
        SegmentCharacterKind Kind,
        Rect Bounds,
        SegmentGeometrySet? GeometrySet,
        IReadOnlyList<Geometry>? SymbolGeometries);

    private readonly record struct SegmentVisibleGeometryCacheKey(
        int LayoutVersion,
        int GeometryVersion,
        int FirstVisibleIndex,
        int LastVisibleIndex);

    private readonly record struct SegmentVisibleInactiveGeometryCacheKey(
        int GeometryVersion,
        int FirstVisibleIndex,
        int LastVisibleIndex);
}
