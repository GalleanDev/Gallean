using System.Buffers.Binary;
using ImageMagick;

namespace GameContent;

internal sealed record IndexedPng(int Width, int Height, byte[] Palette, byte[] Indexes)
{
    private const int SignatureLength = 8;
    private const int ChunkHeaderLength = 8;
    private const int ChunkChecksumLength = 4;

    private static readonly uint PaletteChunkType = BinaryPrimitives.ReadUInt32BigEndian("PLTE"u8);
    private static readonly uint[] ChecksumTable = BuildChecksumTable();

    public static IndexedPng Read(byte[] content)
    {
        int paletteOffset = FindPaletteOffset(content);
        int paletteLength = BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(paletteOffset - ChunkHeaderLength));
        byte[] palette = [.. content.AsSpan(paletteOffset, paletteLength)];

        for (int index = 0; index < paletteLength / 3; index++)
        {
            content[paletteOffset + index * 3] = (byte)index;
            content[paletteOffset + index * 3 + 1] = (byte)index;
            content[paletteOffset + index * 3 + 2] = (byte)index;
        }

        int checksumOffset = paletteOffset + paletteLength;
        uint originalChecksum = BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(checksumOffset));
        BinaryPrimitives.WriteUInt32BigEndian(content.AsSpan(checksumOffset), ComputeChecksum(content.AsSpan(paletteOffset - 4, paletteLength + 4)));

        byte[] indexes;
        int width;
        int height;

        try
        {
            using MagickImage image = new(content);
            image.Alpha(AlphaAction.Set);

            using IUnsafePixelCollection<byte> collection = image.GetPixelsUnsafe();
            byte[] pixels = collection.ToByteArray(PixelMapping.RGBA) ?? [];

            width = (int)image.Width;
            height = (int)image.Height;
            indexes = new byte[width * height];

            for (int i = 0; i < indexes.Length; i++)
            {
                indexes[i] = pixels[i * 4];
            }
        }
        finally
        {
            palette.CopyTo(content.AsSpan(paletteOffset));
            BinaryPrimitives.WriteUInt32BigEndian(content.AsSpan(checksumOffset), originalChecksum);
        }

        return new IndexedPng(width, height, palette, indexes);
    }

    private static int FindPaletteOffset(byte[] content)
    {
        int offset = SignatureLength;

        while (offset + ChunkHeaderLength <= content.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(offset));
            uint type = BinaryPrimitives.ReadUInt32BigEndian(content.AsSpan(offset + 4));

            if (type == PaletteChunkType)
            {
                return offset + ChunkHeaderLength;
            }

            offset += ChunkHeaderLength + length + ChunkChecksumLength;
        }

        throw new ResourceException("Image is not a palette PNG.");
    }

    private static uint ComputeChecksum(ReadOnlySpan<byte> data)
    {
        uint checksum = uint.MaxValue;

        foreach (byte value in data)
        {
            checksum = ChecksumTable[(checksum ^ value) & 0xff] ^ (checksum >> 8);
        }

        return checksum ^ uint.MaxValue;
    }

    private static uint[] BuildChecksumTable()
    {
        uint[] table = new uint[256];

        for (uint index = 0; index < table.Length; index++)
        {
            uint value = index;

            for (int bit = 0; bit < 8; bit++)
            {
                if ((value & 1) != 0)
                {
                    value = 0xedb88320 ^ (value >> 1);
                }
                else
                {
                    value >>= 1;
                }
            }

            table[index] = value;
        }

        return table;
    }
}
