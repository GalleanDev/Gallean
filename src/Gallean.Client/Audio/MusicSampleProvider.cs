using NAudio.Wave;

namespace Gallean.Client;

internal sealed class MusicSampleProvider(MusicQueueSampleProvider source) : ISampleProvider, IMusicPlayback
{
    private static readonly TimeSpan FadeDuration = TimeSpan.FromSeconds(1.0);

    private readonly float gainPerFrame = 1.0f / (float)(source.WaveFormat.SampleRate * FadeDuration.TotalSeconds);
    private volatile float targetGain = 1;
    private float gain;

    public WaveFormat WaveFormat => source.WaveFormat;
    public MusicPosition Position => source.Position;

    public void Stop()
    {
        targetGain = 0;
    }

    public int Read(Span<float> buffer)
    {
        float target = targetGain;

        int count = source.Read(buffer);
        int frames = count / SoundDecoder.MixerChannels;

        for (int frame = 0; frame < frames; frame++)
        {
            gain = AdjustGain(gain, target);

            int index = frame * SoundDecoder.MixerChannels;
            buffer[index] *= gain;
            buffer[index + 1] *= gain;
        }

        if (target == 0 && gain == 0 || count < buffer.Length)
        {
            source.Dispose();
        }

        return count;
    }

    private float AdjustGain(float current, float target)
    {
        if (current < target)
        {
            return Math.Min(current + gainPerFrame, target);
        }
        else
        {
            return Math.Max(current - gainPerFrame, target);
        }
    }
}
