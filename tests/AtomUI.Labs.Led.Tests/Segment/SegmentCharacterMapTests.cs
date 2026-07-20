using AtomUI.Labs.Led.Segment.Character;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Led.Tests.Segment;

public class SegmentCharacterMapTests
{
    [Theory]
    [InlineData('0', (int)(SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.LowerLeft | SegmentParts.LowerRight | SegmentParts.Bottom))]
    [InlineData('1', (int)(SegmentParts.UpperRight | SegmentParts.LowerRight))]
    [InlineData('2', (int)(SegmentParts.Top | SegmentParts.UpperRight | SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.Bottom))]
    [InlineData('3', (int)(SegmentParts.Top | SegmentParts.UpperRight | SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerRight | SegmentParts.Bottom))]
    [InlineData('4', (int)(SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerRight))]
    [InlineData('5', (int)(SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerRight | SegmentParts.Bottom))]
    [InlineData('6', (int)(SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.LowerRight | SegmentParts.Bottom))]
    [InlineData('7', (int)(SegmentParts.Top | SegmentParts.UpperRight | SegmentParts.LowerRight))]
    [InlineData('8', (int)(SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerLeft | SegmentParts.LowerRight | SegmentParts.Bottom))]
    [InlineData('9', (int)(SegmentParts.Top | SegmentParts.UpperLeft | SegmentParts.UpperRight | SegmentParts.MiddleLeft | SegmentParts.MiddleRight | SegmentParts.LowerRight | SegmentParts.Bottom))]
    public void GetPattern_ShouldMapDigitsToSegmentParts(char character, int expectedPartsValue)
    {
        var pattern = SegmentCharacterMap.GetPattern(character);

        pattern.Character.ShouldBe(character);
        pattern.Kind.ShouldBe(SegmentCharacterKind.Segments);
        pattern.Parts.ShouldBe((SegmentParts)expectedPartsValue);
    }

    [Theory]
    [InlineData('A')]
    [InlineData('B')]
    [InlineData('C')]
    [InlineData('D')]
    [InlineData('E')]
    [InlineData('F')]
    [InlineData('G')]
    [InlineData('H')]
    [InlineData('I')]
    [InlineData('J')]
    [InlineData('K')]
    [InlineData('L')]
    [InlineData('M')]
    [InlineData('N')]
    [InlineData('O')]
    [InlineData('P')]
    [InlineData('Q')]
    [InlineData('R')]
    [InlineData('S')]
    [InlineData('T')]
    [InlineData('U')]
    [InlineData('V')]
    [InlineData('W')]
    [InlineData('X')]
    [InlineData('Y')]
    [InlineData('Z')]
    public void GetPattern_ShouldSupportUppercaseLetters(char character)
    {
        var pattern = SegmentCharacterMap.GetPattern(character);

        pattern.Character.ShouldBe(character);
        pattern.Kind.ShouldBe(SegmentCharacterKind.Segments);
        pattern.Parts.ShouldNotBe(SegmentParts.None);
    }

    [Theory]
    [InlineData('a', 'A')]
    [InlineData('z', 'Z')]
    public void GetPattern_ShouldNormalizeLowercaseLetters(char character, char expectedCharacter)
    {
        var pattern = SegmentCharacterMap.GetPattern(character);

        pattern.Character.ShouldBe(expectedCharacter);
        pattern.Kind.ShouldBe(SegmentCharacterKind.Segments);
        pattern.Parts.ShouldNotBe(SegmentParts.None);
    }

    [Theory]
    [InlineData('-', (int)(SegmentParts.MiddleLeft | SegmentParts.MiddleRight))]
    [InlineData('_', (int)SegmentParts.Bottom)]
    public void GetPattern_ShouldMapSupportedSymbolsToSegmentParts(char character, int expectedPartsValue)
    {
        var pattern = SegmentCharacterMap.GetPattern(character);

        pattern.Character.ShouldBe(character);
        pattern.Kind.ShouldBe(SegmentCharacterKind.Segments);
        pattern.Parts.ShouldBe((SegmentParts)expectedPartsValue);
    }

    [Fact]
    public void GetPattern_ShouldMapColonToDedicatedKind()
    {
        var pattern = SegmentCharacterMap.GetPattern(':');

        pattern.Character.ShouldBe(':');
        pattern.Kind.ShouldBe(SegmentCharacterKind.Colon);
        pattern.Parts.ShouldBe(SegmentParts.None);
    }

    [Fact]
    public void GetPattern_ShouldMapDotToDedicatedKind()
    {
        var pattern = SegmentCharacterMap.GetPattern('.');

        pattern.Character.ShouldBe('.');
        pattern.Kind.ShouldBe(SegmentCharacterKind.Dot);
        pattern.Parts.ShouldBe(SegmentParts.None);
    }

    [Theory]
    [InlineData(' ')]
    [InlineData('?')]
    [InlineData('中')]
    public void GetPattern_ShouldFallbackToEmptyForUnsupportedCharacters(char character)
    {
        var pattern = SegmentCharacterMap.GetPattern(character);

        pattern.Character.ShouldBe(' ');
        pattern.Kind.ShouldBe(SegmentCharacterKind.Empty);
        pattern.Parts.ShouldBe(SegmentParts.None);
    }
}
