using NAudio.Wave;

namespace Gallean.Client;

internal sealed partial class MusicQueueSampleProvider(IAssetProvider assets, MusicQueue queue, MusicPosition position) : ISampleProvider, IAudioPlayback, IDisposable
{
    private readonly Lock locker = new();
    private MusicTrack? currentTrack;
    private int nextTrackIndex = position.Track;
    private TimeSpan? nextTrackTime = position.Time;
    private bool disposed;

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels);

    public MusicPosition Position
    {
        get
        {
            lock (locker)
            {
                if (currentTrack is null)
                {
                    return new MusicPosition(nextTrackIndex, nextTrackTime);
                }
                else if (currentTrack.IsFinished)
                {
                    return new MusicPosition(currentTrack.Index + 1, null);
                }
                else
                {
                    return new MusicPosition(currentTrack.Index, currentTrack.Time);
                }
            }
        }
    }

    public int Read(Span<float> buffer)
    {
        lock (locker)
        {
            if (disposed)
            {
                return 0;
            }

            int totalRead = 0;

            while (totalRead < buffer.Length)
            {
                if (currentTrack is null)
                {
                    currentTrack = OpenNextTrack();
                }

                if (currentTrack is null)
                {
                    break;
                }

                MusicTrack track = currentTrack;

                int read;
                try
                {
                    read = track.Read(buffer[totalRead..]);
                }
                catch (Exception ex)
                {
                    CloseCurrentTrack();
                    throw new SoundDecodeException(track.Key, ex);
                }

                if (read > 0)
                {
                    totalRead += read;
                }
                else
                {
                    CloseCurrentTrack();
                }
            }

            return totalRead;
        }
    }

    public void Stop()
    {
        Dispose();
    }

    public void Dispose()
    {
        lock (locker)
        {
            disposed = true;
            CloseCurrentTrack();
        }
    }

    private MusicTrack? OpenNextTrack()
    {
        for (int attempt = 0; attempt < queue.Tracks.Length; attempt++)
        {
            if (nextTrackIndex == queue.Tracks.Length)
            {
                if (queue.IsLooping)
                {
                    nextTrackIndex = 0;
                }
                else
                {
                    return null;
                }
            }

            int index = nextTrackIndex;
            AssetSoundKey key = queue.Tracks[index];
            TimeSpan? time = nextTrackTime;

            nextTrackIndex++;
            nextTrackTime = null;

            AssetStream? data = assets.GetOptionalStream(key);
            AssetSoundMetadata? metadata = assets.GetOptionalSoundMetadata(key);

            if (data is not null)
            {
                try
                {
                    Stream stream = data.Open();
                    SoundDecoder.SampleProvider samples = SoundDecoder.CreateSamples(stream, time ?? metadata?.StartTime ?? TimeSpan.Zero);

                    return new MusicTrack(key, index, samples);
                }
                catch (Exception ex)
                {
                    throw new SoundDecodeException(key, ex);
                }
            }
        }

        return null;
    }

    private void CloseCurrentTrack()
    {
        currentTrack?.Dispose();
        currentTrack = null;
    }
}
