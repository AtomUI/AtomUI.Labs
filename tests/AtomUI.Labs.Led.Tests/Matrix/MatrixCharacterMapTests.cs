using AtomUI.Labs.Led.Matrix.Character;
using System.Text;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Matrix;

public class MatrixCharacterMapTests
{
    [Theory]
    [InlineData('A', 'A')]
    [InlineData('z', 'Z')]
    [InlineData('7', '7')]
    [InlineData('+', '+')]
    [InlineData(' ', ' ')]
    public void GetPattern_ShouldNormalizeAndMapSupportedCharacters(char input, char expected)
    {
        var pattern = MatrixCharacterMap.GetPattern(input);

        pattern.Character.ShouldBe(expected);
        if (expected == ' ')
        {
            pattern.Glyph.Bits.ShouldBe(0UL);
        }
        else
        {
            pattern.Glyph.Bits.ShouldNotBe(0UL);
        }
    }

    [Theory]
    [InlineData('中')]
    [InlineData('@')]
    [InlineData('\n')]
    public void GetPattern_ShouldFallbackUnsupportedCharactersToQuestionMark(char input)
    {
        var pattern = MatrixCharacterMap.GetPattern(input);

        pattern.Character.ShouldBe('?');
        MatrixFiveBySevenGlyphMap.TryGetGlyph('?', out var fallbackGlyph).ShouldBeTrue();
        pattern.Glyph.ShouldBe(fallbackGlyph);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("labs 2026", "LABS 2026")]
    [InlineData("A中@Z", "A??Z")]
    [InlineData("A😀Z", "A?Z")]
    public void GetDisplayText_ShouldMatchRenderedCharacterSemantics(string? text, string expected)
    {
        MatrixCharacterMap.GetDisplayText(text).ShouldBe(expected);
    }

    [Fact]
    public void GetDisplayText_ShouldFallbackUnpairedSurrogateOnce()
    {
        var text = new string(new[] { 'A', '\uD800', 'Z' });

        MatrixCharacterMap.GetDisplayText(text).ShouldBe("A?Z");
    }

    [Fact]
    public void GetPattern_ShouldFallbackSupplementaryRuneOnce()
    {
        var pattern = MatrixCharacterMap.GetPattern(new Rune(0x1F600));

        pattern.Character.ShouldBe('?');
    }

    [Fact]
    public void GetPatternCount_ShouldCountUnicodeScalarsInsteadOfUtf16CodeUnits()
    {
        MatrixCharacterMap.GetPatternCount("A😀Z").ShouldBe(3);
        MatrixCharacterMap.GetPatternCount("A\uD800Z").ShouldBe(3);
    }

    [Fact]
    public void GetDisplayText_ShouldReuseOriginalStringWhenNoNormalizationIsNeeded()
    {
        var text = new string("MATRIX 2026".ToCharArray());

        MatrixCharacterMap.GetDisplayText(text).ShouldBeSameAs(text);
    }
}
