using System.Collections;

namespace Gallean.Common;

public sealed partial record ImmutableBitmap
{
    public sealed class Builder
    {
        private readonly BitArray data;

        public Builder(ImmutableBitmap bitmap)
        {
            Width = bitmap.Width;
            Height = bitmap.Height;
            data = new BitArray(bitmap.Data);
        }

        public Builder(int width, int height, ReadOnlySpan<byte> bits)
        {
            int bitCount = GetBitCount(width, height);

            if (bits.Length != GetByteCount(bitCount))
            {
                throw new ArgumentException("Packed bits do not match the bitmap dimensions.", nameof(bits));
            }

            Width = width;
            Height = height;
            data = new BitArray(bits.ToArray()) { Length = bitCount };
        }

        public int Width { get; }
        public int Height { get; }

        public void Set(int x, int y)
        {
            int index = y * Width + x;
            data[index] = true;
        }

        public void Clear(int x, int y)
        {
            int index = y * Width + x;
            data[index] = false;
        }

        public ImmutableBitmap ToImmutable()
        {
            return new ImmutableBitmap(Width, Height, data);
        }

        public static long GetBitsLength(int width, int height)
        {
            return ((long)width * height + 7) / 8;
        }
    }
}
