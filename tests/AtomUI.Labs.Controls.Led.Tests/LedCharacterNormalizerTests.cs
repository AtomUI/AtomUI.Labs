using AtomUI.Labs.Controls.Led;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests;

public class LedCharacterNormalizerTests
{
    [Theory]
    [InlineData('a', 'A')]
    [InlineData('z', 'Z')]
    [InlineData('m', 'M')]
    [InlineData('A', 'A')]
    [InlineData('0', '0')]
    [InlineData(':', ':')]
    [InlineData(' ', ' ')]
    public void NormalizeAscii_ShouldOnlyUppercaseAsciiLowercaseLetters(char input, char expected)
    {
        LedCharacterNormalizer.NormalizeAscii(input).ShouldBe(expected);
    }
}
