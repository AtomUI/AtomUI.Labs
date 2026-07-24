using AtomUI.Labs.Controls.Led.Matrix.Character;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.Led.Tests.Matrix;

public class MatrixGlyphMapTests
{
    [Fact]
    public void SupportedCharacters_ShouldContainExactlyFortyFiveDistinctCharacters()
    {
        MatrixFiveBySevenGlyphMap.SupportedCharacters.Length.ShouldBe(45);
        MatrixFiveBySevenGlyphMap.SupportedCharacters.Distinct().Count().ShouldBe(45);
    }

    [Fact]
    public void TryGetGlyph_ShouldReturnEverySupportedGlyph()
    {
        foreach (var character in MatrixFiveBySevenGlyphMap.SupportedCharacters)
        {
            MatrixFiveBySevenGlyphMap.TryGetGlyph(character, out var glyph).ShouldBeTrue();
            if (character == ' ')
            {
                glyph.Bits.ShouldBe(0UL);
            }
            else
            {
                glyph.Bits.ShouldNotBe(0UL);
            }
        }
    }

    [Fact]
    public void SupportedGlyphs_ShouldNotSetBitsOutsideFiveBySevenBounds()
    {
        foreach (var character in MatrixFiveBySevenGlyphMap.SupportedCharacters)
        {
            MatrixFiveBySevenGlyphMap.TryGetGlyph(character, out var glyph).ShouldBeTrue();
            (glyph.Bits >> (MatrixGlyph.Width * MatrixGlyph.Height)).ShouldBe(0UL);
        }
    }

    [Theory]
    [InlineData('a')]
    [InlineData('中')]
    [InlineData('@')]
    public void TryGetGlyph_ShouldRejectUnsupportedCharacters(char character)
    {
        MatrixFiveBySevenGlyphMap.TryGetGlyph(character, out var glyph).ShouldBeFalse();
        glyph.ShouldBe(default);
    }

    [Fact]
    public void GlyphA_ShouldUseDocumentedRowMajorOrientation()
    {
        MatrixFiveBySevenGlyphMap.TryGetGlyph('A', out var glyph).ShouldBeTrue();
        var expectedRows = new[]
        {
            "01110",
            "10001",
            "10001",
            "11111",
            "10001",
            "10001",
            "10001"
        };

        for (var row = 0; row < MatrixGlyph.Height; row++)
        {
            for (var column = 0; column < MatrixGlyph.Width; column++)
            {
                glyph.IsActive(row, column).ShouldBe(expectedRows[row][column] == '1');
            }
        }
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(7, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 5)]
    public void IsActive_ShouldReturnFalseOutsideGlyphBounds(int row, int column)
    {
        MatrixFiveBySevenGlyphMap.TryGetGlyph('8', out var glyph).ShouldBeTrue();

        glyph.IsActive(row, column).ShouldBeFalse();
    }
}
