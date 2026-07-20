namespace AtomUI.Labs.Led;

internal static class LedCharacterNormalizer
{
    public static char NormalizeAscii(char character)
    {
        return character is >= 'a' and <= 'z'
            ? (char)(character - ('a' - 'A'))
            : character;
    }
}
