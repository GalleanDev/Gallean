using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using OggVorbisEncoder;

namespace Packer;

internal static class SoundEncoder
{
    private const int SampleRate = 44100;
    private const int Channels = 2;
    private const float Quality = 0.5f;
    private const int PrimingFrames = 1024;
    private const int MaxMp3FrameSamples = 1152 * 2;
    private const int ReadChunkSamples = 64 * 1024;

    public static AssetSound Encode(AssetSound sound)
    {
        float[] samples = ReadSamples(sound.Data);

        return sound with { Data = AssetStream.FromBytes(EncodeVorbis(samples)) };
    }

    private static float[] ReadSamples(AssetStream data)
    {
        using Stream stream = data.Open();
        using WaveFileReader reader = new(stream);
        WaveFormatEncoding encoding = reader.WaveFormat.Encoding;

        switch (encoding)
        {
            case WaveFormatEncoding.MpegLayer3:
                using (RawSourceWaveStream decoded = DecodeMp3(reader))
                {
                    return ReadMixerSamples(decoded.ToSampleProvider());
                }

            case WaveFormatEncoding.Pcm or WaveFormatEncoding.IeeeFloat:
                return ReadMixerSamples(reader.ToSampleProvider());

            default:
                throw new InvalidDataException($"Unsupported sound encoding '{encoding}'.");
        }
    }

    private static RawSourceWaveStream DecodeMp3(WaveFileReader reader)
    {
        using Mp3FrameDecompressor decoder = new(reader.WaveFormat);
        float[] frameSamples = new float[MaxMp3FrameSamples];
        MemoryStream pcm = new();

        Mp3Frame? frame = Mp3Frame.LoadFromStream(reader, readData: true);

        while (frame is not null)
        {
            int length = decoder.DecompressFrame(frame, MemoryMarshal.AsBytes(frameSamples.AsSpan()));

            pcm.Write(MemoryMarshal.AsBytes(frameSamples.AsSpan())[..length]);
            frame = Mp3Frame.LoadFromStream(reader, readData: true);
        }

        pcm.Position = 0;

        return new RawSourceWaveStream(pcm, decoder.OutputFormat);
    }

    private static float[] ReadMixerSamples(ISampleProvider source)
    {
        ISampleProvider samples = source;

        if (samples.WaveFormat.SampleRate != SampleRate)
        {
            samples = new WdlResamplingSampleProvider(samples, SampleRate);
        }

        if (samples.WaveFormat.Channels == 1)
        {
            samples = new MonoToStereoSampleProvider(samples);
        }

        if (samples.WaveFormat.Channels != Channels)
        {
            throw new InvalidDataException($"Unsupported sound channel count '{samples.WaveFormat.Channels}'.");
        }

        List<float> all = [];
        float[] chunk = new float[ReadChunkSamples];
        int read;

        do
        {
            read = samples.Read(chunk);
            all.AddRange(chunk.AsSpan(0, read));
        } while (read > 0);

        return [.. all];
    }

    private static byte[] EncodeVorbis(float[] samples)
    {
        VorbisInfo info = VorbisInfo.InitVariableBitRate(Channels, SampleRate, Quality);
        OggStream stream = new(1);
        MemoryStream output = new();

        stream.PacketIn(HeaderPacketBuilder.BuildInfoPacket(info));
        stream.PacketIn(HeaderPacketBuilder.BuildCommentsPacket(new Comments()));
        stream.PacketIn(HeaderPacketBuilder.BuildBooksPacket(info));
        WritePages(stream, output, force: true);

        ProcessingState state = ProcessingState.Create(info);
        state.WriteData([new float[PrimingFrames], new float[PrimingFrames]], PrimingFrames);
        state.WriteData(Deinterleave(samples), samples.Length / Channels);
        state.WriteEndOfStream();

        while (state.PacketOut(out OggPacket packet))
        {
            stream.PacketIn(packet);
            WritePages(stream, output, force: false);
        }

        WritePages(stream, output, force: true);

        return output.ToArray();
    }

    private static float[][] Deinterleave(float[] samples)
    {
        int frameCount = samples.Length / Channels;
        float[][] planes = [new float[frameCount], new float[frameCount]];

        for (int frame = 0; frame < frameCount; frame++)
        {
            for (int channel = 0; channel < Channels; channel++)
            {
                planes[channel][frame] = samples[(frame * Channels) + channel];
            }
        }

        return planes;
    }

    private static void WritePages(OggStream stream, MemoryStream output, bool force)
    {
        while (stream.PageOut(out OggPage page, force))
        {
            output.Write(page.Header);
            output.Write(page.Body);
        }
    }
}
