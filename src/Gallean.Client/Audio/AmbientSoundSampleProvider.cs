using NAudio.Wave;

namespace Gallean.Client;

internal sealed class AmbientSoundSampleProvider : ISampleProvider, IAmbientAudioPlayback
{
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(250);

    private readonly MusicQueueSampleProvider source;
    private readonly float gainPerFrame;
    private volatile float targetLeftGain;
    private volatile float targetRightGain;
    private float leftGain;
    private float rightGain;

    public AmbientSoundSampleProvider(MusicQueueSampleProvider source, float volume, float pan)
    {
        this.source = source;

        gainPerFrame = 1.0f / (float)(source.WaveFormat.SampleRate * FadeDuration.TotalSeconds);

        SetLevel(volume, pan);
    }

    public WaveFormat WaveFormat => source.WaveFormat;

    public void SetLevel(float volume, float pan)
    {
        targetLeftGain = volume * MathF.Sqrt(1 - pan);
        targetRightGain = volume * MathF.Sqrt(pan);
    }

    public void Stop()
    {
        targetLeftGain = 0;
        targetRightGain = 0;
    }

    public int Read(Span<float> buffer)
    {
        float targetLeft = targetLeftGain;
        float targetRight = targetRightGain;

        int count = source.Read(buffer);
        int frames = count / SoundDecoder.MixerChannels;

        for (int frame = 0; frame < frames; frame++)
        {
            leftGain = AdjustGain(leftGain, targetLeft);
            rightGain = AdjustGain(rightGain, targetRight);

            int index = frame * SoundDecoder.MixerChannels;

            buffer[index] *= leftGain;
            buffer[index + 1] *= rightGain;
        }

        if (targetLeft == 0 && targetRight == 0 && leftGain == 0 && rightGain == 0 ||
            count < buffer.Length)
        {
            source.Dispose();
        }

        return count;
    }

    private float AdjustGain(float gain, float target)
    {
        if (gain < target)
        {
            return Math.Min(gain + gainPerFrame, target);
        }
        else
        {
            return Math.Max(gain - gainPerFrame, target);
        }
    }
}
