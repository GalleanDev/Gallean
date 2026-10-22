using NAudio.Wave;
using NVorbis;

namespace Gallean.Client;

internal sealed class VorbisSampleProvider : ISampleProvider, IDisposable
{
    private readonly VorbisReader reader;
    private long remaining;

    public VorbisSampleProvider(Stream data)
    {
        reader = new VorbisReader(data, closeOnDispose: false);
        remaining = reader.TotalSamples * reader.Channels;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(reader.SampleRate, reader.Channels);
    }

    public WaveFormat WaveFormat { get; }
    public TimeSpan Time => reader.TimePosition;
    public bool IsFinished => remaining == 0;

    public long GetFrameCount()
    {
        return reader.TotalSamples;
    }

    public void Seek(TimeSpan time)
    {
        reader.TimePosition = time;
        remaining = (reader.TotalSamples - reader.SamplePosition) * reader.Channels;
    }

    public int Read(Span<float> buffer)
    {
        int count = (int)Math.Min(buffer.Length, remaining);
        int read = reader.ReadSamples(buffer[..count]);

        remaining -= read;

        return read;
    }

    public void Dispose()
    {
        reader.Dispose();
    }
}
