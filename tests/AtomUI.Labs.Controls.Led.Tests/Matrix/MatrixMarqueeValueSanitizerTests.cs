using AtomUI.Labs.Controls.Led.Marquee;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixMarqueeValueSanitizerTests
{
    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(double.NegativeInfinity, 0)]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(48, 48)]
    [InlineData(10_001, 10_000)]
    [InlineData(double.PositiveInfinity, 10_000)]
    public void Speed_ShouldUseSafeEffectiveRange(double value, double expected)
    {
        LedMarqueeValueSanitizer.CoerceSpeed(value).ShouldBe(expected);
    }

    [Fact]
    public void RepeatDelay_ShouldUseSafeEffectiveRange()
    {
        LedMarqueeValueSanitizer.CoerceRepeatDelay(TimeSpan.FromSeconds(-1)).ShouldBe(TimeSpan.Zero);
        LedMarqueeValueSanitizer.CoerceRepeatDelay(TimeSpan.FromSeconds(5)).ShouldBe(TimeSpan.FromSeconds(5));
        LedMarqueeValueSanitizer.CoerceRepeatDelay(TimeSpan.FromHours(1)).ShouldBe(TimeSpan.FromMinutes(1));
    }
}
