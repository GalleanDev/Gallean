using Gallean.Common;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Gallean.Client;

internal static class SoundDecoder
{
    public sealed class SampleProvider(ISampleProvider samples, IDisposable source, VorbisSampleProvider decoder) : ISampleProvider, IDisposable
    {
        public WaveFormat WaveFormat => samples.WaveFormat;
        public TimeSpan Time => decoder.Time;
        public bool IsFinished => decoder.IsFinished;

        public long GetFrameCount()
        {
            return decoder.GetFrameCount();
        }

        public int Read(Span<float> buffer)
        {
            return samples.Read(buffer);
        }

        public void Dispose()
        {
            source.Dispose();
        }
    }

    public const int MixerChannels = 2;
    public const int MixerSampleRate = 44100;

    public static Sound DecodeToMemory(Stream data)
    {
        using VorbisSampleProvider samples = OpenSamples(data);

        float[] decoded = ReadAllSamples(samples, checked((int)(samples.GetFrameCount() * MixerChannels)));

        if (decoded.Length <= 0)
        {
            throw new InvalidDataException("Sound contains no samples.");
        }

        return new Sound(decoded);
    }

    public static SampleProvider CreateSamples(Stream data, TimeSpan startTime)
    {
        VorbisSampleProvider opened;

        try
        {
            opened = OpenSamples(data);
        }
        catch
        {
            data.Dispose();
            throw;
        }

        try
        {
            if (startTime > TimeSpan.Zero)
            {
                opened.Seek(startTime);
            }

            float[] initialSamples = new float[MixerChannels];
            int sampleCount = opened.Read(initialSamples);

            if (sampleCount <= 0)
            {
                throw new InvalidDataException("Sound contains no samples.");
            }

            ConcatenatingSampleProvider sampleProvider = new([
                SoundSampleProvider.Single(new Sound(initialSamples[..sampleCount]), opened.WaveFormat),
                opened
            ]);

            CleanupList sources = new();
            sources.Add(opened);
            sources.Add(data);

            return new SampleProvider(sampleProvider, sources, opened);
        }
        catch
        {
            opened.Dispose();
            data.Dispose();
            throw;
        }
    }

    private static VorbisSampleProvider OpenSamples(Stream data)
    {
        VorbisSampleProvider samples = new(data);
        WaveFormat format = samples.WaveFormat;

        if (format.SampleRate != MixerSampleRate || format.Channels != MixerChannels)
        {
            samples.Dispose();

            throw new InvalidDataException($"Unsupported sound format '{format.SampleRate} Hz, {format.Channels} channels'.");
        }

        return samples;
    }

    private static float[] ReadAllSamples(ISampleProvider samples, int length)
    {
        float[] decoded = new float[length];
        int position = 0;
        int read;

        do
        {
            read = samples.Read(decoded.AsSpan(position, Math.Min(16 * 1024, length - position)));
            position += read;
        } while (read > 0);

        Array.Resize(ref decoded, position);

        return decoded;
    }
}
