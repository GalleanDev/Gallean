using NAudio.Wave;

namespace Gallean.Client;

internal sealed class SoundSampleProvider : ISampleProvider
{
    private readonly Sound sound;
    private int position;

    private SoundSampleProvider(Sound sound, WaveFormat format)
    {
        this.sound = sound;
        WaveFormat = format;
    }

    public WaveFormat WaveFormat { get; }

    public int Read(Span<float> buffer)
    {
        int available = Math.Min(buffer.Length, sound.Samples.Length - position);

        if (available <= 0)
        {
            return 0;
        }

        ReadOnlySpan<float> source = sound.Samples.AsSpan(position, available);

        source.CopyTo(buffer);
        position += available;

        return available;
    }

    public static SoundSampleProvider Single(Sound sound, WaveFormat format)
    {
        return new SoundSampleProvider(sound, format);
    }
}
