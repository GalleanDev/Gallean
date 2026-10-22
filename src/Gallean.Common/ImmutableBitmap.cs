using System.Collections;

namespace Gallean.Common;

public sealed partial record ImmutableBitmap
{
    private readonly Lazy<int> hashCode;

    private ImmutableBitmap(int width, int height, BitArray data)
    {
        hashCode = new Lazy<int>(ComputeHashCode);
        Width = width;
        Height = height;
        Data = new BitArray(data);
    }

    public int Width { get; }
    public int Height { get; }
    private BitArray Data { get; }

    public bool Equals(ImmutableBitmap? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Width != other.Width || Height != other.Height)
        {
            return false;
        }

        for (int index = 0; index < Data.Length; index++)
        {
            if (Data[index] != other.Data[index])
            {
                return false;
            }
        }

        return true;
    }

    public static ImmutableBitmap Create(int width, int height, bool initialValue = false)
    {
        BitArray data = new(GetBitCount(width, height), initialValue);

        return new ImmutableBitmap(width, height, data);
    }

    public bool Get(int x, int y)
    {
        int index = y * Width + x;

        return Data[index];
    }

    public byte[] ToBits()
    {
        byte[] bits = new byte[GetByteCount(Data.Length)];

        Data.CopyTo(bits, 0);

        return bits;
    }

    public Builder ToBuilder()
    {
        return new Builder(this);
    }

    public override int GetHashCode()
    {
        return hashCode.Value;
    }

    private static int GetBitCount(int width, int height)
    {
        return checked(width * height);
    }

    private static int GetByteCount(int bitCount)
    {
        return (bitCount + 7) / 8;
    }

    private int ComputeHashCode()
    {
        HashCode hash = new();

        hash.Add(Width);
        hash.Add(Height);

        for (int index = 0; index < Data.Length; index++)
        {
            hash.Add(Data[index]);
        }

        return hash.ToHashCode();
    }
}
