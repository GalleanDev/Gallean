using System.Security.Cryptography;

namespace Umbra;

public readonly record struct UmbraSpriteHash(ReadOnlyMemory<byte> Value)
{
    public const int Length = 12;

    public static UmbraSpriteHash Create(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> pixels)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        hash.AppendInt32(pixelWidth);
        hash.AppendInt32(pixelHeight);
        hash.AppendData(pixels);

        byte[] data = hash.GetHashAndReset();

        return new UmbraSpriteHash(data.AsMemory(0, Length));
    }

    public bool Equals(UmbraSpriteHash other)
    {
        return Value.Span.SequenceEqual(other.Value.Span);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddBytes(Value.Span);

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return Convert.ToHexStringLower(Value.Span);
    }
}
