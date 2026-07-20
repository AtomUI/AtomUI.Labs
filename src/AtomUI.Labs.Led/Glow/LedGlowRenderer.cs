using Avalonia;
using Avalonia.Media;

namespace AtomUI.Labs.Led.Glow;

internal sealed class LedGlowRenderer
{
    private BlurEffect? _effect;

    public int EffectBuildCount { get; private set; }

    public int EffectScopeCount { get; private set; }

    public LedGlowRenderScope Push(
        DrawingContext context,
        IBrush? brush,
        double opacity,
        double radius,
        Rect sourceBounds)
    {
        var effectiveOpacity = LedGlowValueSanitizer.CoerceOpacity(opacity);
        var effectiveRadius = LedGlowValueSanitizer.CoerceRadius(radius);
        if (brush is null
            || effectiveOpacity <= 0
            || effectiveRadius <= 0
            || !IsUsable(sourceBounds))
        {
            return default;
        }

        if (_effect is null)
        {
            _effect = new BlurEffect();
            EffectBuildCount++;
        }

        _effect.Radius = effectiveRadius;
        EffectScopeCount++;
        return new LedGlowRenderScope(context, _effect, effectiveOpacity, sourceBounds);
    }

    private static bool IsUsable(Rect bounds)
    {
        return double.IsFinite(bounds.X)
               && double.IsFinite(bounds.Y)
               && double.IsFinite(bounds.Width)
               && double.IsFinite(bounds.Height)
               && bounds.Width > 0
               && bounds.Height > 0;
    }
}

internal readonly ref struct LedGlowRenderScope
{
    private readonly IDisposable? _opacityScope;
    private readonly IDisposable? _effectScope;

    public LedGlowRenderScope(
        DrawingContext context,
        BlurEffect effect,
        double opacity,
        Rect sourceBounds)
    {
        _opacityScope = context.PushOpacity(opacity);
        _effectScope = context.PushEffect(effect, sourceBounds);
        IsActive = true;
    }

    public bool IsActive { get; }

    public void Dispose()
    {
        _effectScope?.Dispose();
        _opacityScope?.Dispose();
    }
}
