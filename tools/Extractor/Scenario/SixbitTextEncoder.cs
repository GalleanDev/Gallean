namespace Extractor;

internal static class SixbitTextEncoder
{
    private const int BitsPerByte = 8;
    private const int BitsPerCharacter = 6;
    private const int CharacterMask = (1 << BitsPerCharacter) - 1;
    private const char FirstCharacter = '!';

    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        char[] chars = new char[(bytes.Length * BitsPerByte + BitsPerCharacter - 1) / BitsPerCharacter];
        int index = 0;
        int bits = 0;
        int bitsCount = 0;

        foreach (byte value in bytes)
        {
            bits = (bits << BitsPerByte) | value;
            bitsCount += BitsPerByte;

            while (bitsCount >= BitsPerCharacter)
            {
                bitsCount -= BitsPerCharacter;
                chars[index] = ToCharacter(bits >> bitsCount);
                index++;
            }
        }

        if (bitsCount > 0)
        {
            chars[index] = ToCharacter(bits << (BitsPerCharacter - bitsCount));
        }

        return new string(chars);
    }

    private static char ToCharacter(int value)
    {
        return (char)(FirstCharacter + (value & CharacterMask));
    }
}
