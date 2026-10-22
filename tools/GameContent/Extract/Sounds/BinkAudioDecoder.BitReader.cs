namespace GameContent;

internal sealed partial class BinkAudioDecoder
{
    private ref struct BitReader(ReadOnlySpan<byte> data)
    {
        private const int WindowBytes = 5;

        private readonly ReadOnlySpan<byte> data = data;
        private int position;

        public int BitsLeft => (data.Length * 8) - position;

        public int GetBits(int count)
        {
            int byteIndex = position >> 3;
            int available = Math.Min(WindowBytes, data.Length - byteIndex);
            ulong window = 0;

            for (int index = 0; index < available; index++)
            {
                window |= (ulong)data[byteIndex + index] << (8 * index);
            }

            int value = (int)((window >> (position & 7)) & ((1UL << count) - 1));
            position += count;

            return value;
        }

        public int GetBit()
        {
            return GetBits(1);
        }

        public void SkipBits(int count)
        {
            position += count;
        }

        public void Align32()
        {
            position = (position + 31) & ~31;
        }
    }
}
