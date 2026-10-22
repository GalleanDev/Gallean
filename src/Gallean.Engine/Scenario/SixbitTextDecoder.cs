namespace Gallean.Engine;

internal static class SixbitTextDecoder
{
    private const int BitsPerByte = 8;
    private const int BitsPerCharacter = 6;
    private const int ByteMask = (1 << BitsPerByte) - 1;
    private const int CharacterMask = (1 << BitsPerCharacter) - 1;
    private const char FirstCharacter = '!';
    private const char LastCharacter = (char)(FirstCharacter + CharacterMask);

    public static byte[] Decode(string text)
    {
        byte[] bytes = new byte[text.Length * BitsPerCharacter / BitsPerByte];
        int index = 0;
        int bits = 0;
        int bitsCount = 0;

        foreach (char ch in text)
        {
            bits = (bits << BitsPerCharacter) | ToValue(ch);
            bitsCount += BitsPerCharacter;

            if (bitsCount >= BitsPerByte)
            {
                bitsCount -= BitsPerByte;
                bytes[index] = (byte)((bits >> bitsCount) & ByteMask);
                index++;
            }
        }

        return bytes;
    }

    private static int ToValue(char ch)
    {
        if (ch is < FirstCharacter or > LastCharacter)
        {
            throw new InvalidDataException($"Character '{ch}' is outside the '{FirstCharacter}'-'{LastCharacter}' range of sixbit text.");
        }

        return ch - FirstCharacter;
    }
}
