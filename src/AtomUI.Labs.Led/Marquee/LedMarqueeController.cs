using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AtomUI.Labs.Led.Marquee;

internal sealed class LedMarqueeController : IDisposable
{
    private static readonly TimeSpan MaximumCycleDuration = TimeSpan.FromDays(365);
    private readonly Control _owner;
    private readonly StyledProperty<double> _progressProperty;
    private MarqueePlaybackKey _playbackKey;
    private Style? _animationStyle;
    private bool _hasPlaybackKey;

    public LedMarqueeController(Control owner, StyledProperty<double> progressProperty)
    {
        _owner = owner;
        _progressProperty = progressProperty;
    }

    public bool IsRunning => _animationStyle is not null;

    public void Update(double viewportWidth, double contentWidth, double speed, TimeSpan repeatDelay)
    {
        var effectiveSpeed = LedMarqueeValueSanitizer.CoerceSpeed(speed);
        if (!double.IsFinite(viewportWidth)
            || !double.IsFinite(contentWidth)
            || viewportWidth <= 0
            || contentWidth <= 0
            || effectiveSpeed <= 0)
        {
            Stop();
            return;
        }

        var effectiveDelay = LedMarqueeValueSanitizer.CoerceRepeatDelay(repeatDelay);
        var key = new MarqueePlaybackKey(viewportWidth, contentWidth, effectiveSpeed, effectiveDelay);
        if (_hasPlaybackKey && _playbackKey == key && _animationStyle is not null)
        {
            return;
        }

        Stop();
        _playbackKey = key;
        _hasPlaybackKey = true;

        var movementSeconds = (viewportWidth + contentWidth) / effectiveSpeed;
        var totalSeconds = Math.Min(
            movementSeconds + effectiveDelay.TotalSeconds,
            MaximumCycleDuration.TotalSeconds);
        if (!double.IsFinite(totalSeconds) || totalSeconds <= 0)
        {
            return;
        }

        var movementCue = Math.Clamp(movementSeconds / totalSeconds, 0, 1);
        _animationStyle = new Style
        {
            Animations =
            {
                new Animation
                {
                    Duration = TimeSpan.FromSeconds(totalSeconds),
                    IterationCount = IterationCount.Infinite,
                    FillMode = FillMode.Both,
                    Children =
                    {
                        CreateKeyFrame(0, 0),
                        CreateKeyFrame(movementCue, 1),
                        CreateKeyFrame(1, 1)
                    }
                }
            }
        };
        _owner.Styles.Add(_animationStyle);
    }

    public void Stop()
    {
        if (_animationStyle is not null)
        {
            _owner.Styles.Remove(_animationStyle);
            _animationStyle = null;
        }

        _owner.SetCurrentValue(_progressProperty, 0);
    }

    public void Dispose()
    {
        Stop();
    }

    private KeyFrame CreateKeyFrame(double cue, double progress)
    {
        return new KeyFrame
        {
            Cue = new Cue(cue),
            Setters = { new Setter(_progressProperty, progress) }
        };
    }

    private readonly record struct MarqueePlaybackKey(
        double ViewportWidth,
        double ContentWidth,
        double Speed,
        TimeSpan RepeatDelay);
}
