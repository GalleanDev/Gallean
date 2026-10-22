using System.Buffers.Binary;
using NAudio.Wave;

namespace GameContent;

internal static class BinkAudioExtractor
{
    private const int HeaderLength = 44;
    private const int TrackInfoLength = 12;
    private const int FrameCountOffset = 8;
    private const int TrackCountOffset = 40;
    private const uint KeyFrameFlag = 1;
    private const int StereoFlag = 0x2000;
    private const int DctFlag = 0x1000;

    public static byte[]? ExtractWave(Stream stream)
    {
        byte[] header = new byte[HeaderLength];
        stream.Position = 0;
        stream.ReadExactly(header);

        if (!header.AsSpan(0, 3).SequenceEqual("BIK"u8) || header[3] is < (byte)'c' or > (byte)'i')
        {
            throw new InvalidDataException("Unexpected Bink file signature.");
        }

        int frameCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(FrameCountOffset)));
        int trackCount = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(TrackCountOffset)));

        if (trackCount <= 0)
        {
            return null;
        }
        else if (trackCount > 1)
        {
            throw new InvalidDataException("Unexpected Bink audio track count.");
        }

        byte[] tracks = new byte[TrackInfoLength];
        stream.ReadExactly(tracks);

        int sampleRate = BinaryPrimitives.ReadUInt16LittleEndian(tracks.AsSpan(sizeof(uint)));
        int flags = BinaryPrimitives.ReadUInt16LittleEndian(tracks.AsSpan(sizeof(uint) + sizeof(ushort)));

        if ((flags & DctFlag) != 0)
        {
            throw new InvalidDataException("Unexpected Bink audio flags.");
        }

        int channels = (flags & StereoFlag) != 0 ? 2 : 1;
        byte[] index = new byte[(frameCount + 1) * sizeof(uint)];
        stream.ReadExactly(index);

        BinkAudioDecoder decoder = new(sampleRate, channels);
        MemoryStream output = new();

        using (WaveFileWriter writer = new(output, new WaveFormat(sampleRate, 16, channels)))
        {
            for (int frame = 0; frame < frameCount; frame++)
            {
                long frameStart = BinaryPrimitives.ReadUInt32LittleEndian(index.AsSpan(frame * sizeof(uint))) & ~KeyFrameFlag;
                long frameEnd = BinaryPrimitives.ReadUInt32LittleEndian(index.AsSpan((frame + 1) * sizeof(uint))) & ~KeyFrameFlag;
                byte[] packet = ReadAudioPacket(stream, frameStart, frameEnd);

                if (packet.Length >= sizeof(uint))
                {
                    WriteSamples(writer, decoder.DecodePacket(packet));
                }
            }
        }

        return output.ToArray();
    }

    private static byte[] ReadAudioPacket(Stream stream, long frameStart, long frameEnd)
    {
        Span<byte> sizeBytes = stackalloc byte[sizeof(uint)];
        stream.Position = frameStart;
        stream.ReadExactly(sizeBytes);

        int size = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(sizeBytes));

        if (sizeof(uint) + size > frameEnd - frameStart)
        {
            throw new InvalidDataException("Unexpected Bink audio packet.");
        }

        byte[] packet = new byte[size];
        stream.ReadExactly(packet);

        return packet;
    }

    private static void WriteSamples(WaveFileWriter writer, float[] samples)
    {
        short[] pcm = new short[samples.Length];

        for (int index = 0; index < samples.Length; index++)
        {
            pcm[index] = (short)Math.Clamp(MathF.Round(samples[index] * 32768), short.MinValue, short.MaxValue);
        }

        writer.WriteSamples(pcm, 0, pcm.Length);
    }
}
