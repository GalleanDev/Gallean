using NAudio.Wave;

namespace Gallean.Client;

internal sealed class CubicVolumeSampleProvider(ISampleProvider source, float volume) : ISampleProvider
{
    private volatile float volume = volume;

    public WaveFormat WaveFormat => source.WaveFormat;

    public float Volume
    {
        get => volume;
        set => volume = value;
    }

    public int Read(Span<float> buffer)
    {
        float position = volume;
        float gain = position * position * position;

        int count = source.Read(buffer);

        for (int index = 0; index < count; index++)
        {
            buffer[index] *= gain;
        }

        return count;
    }
}
