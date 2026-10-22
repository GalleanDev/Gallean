namespace Gallean.Client;

internal sealed partial class MusicQueueSampleProvider
{
    private sealed class MusicTrack(AssetSoundKey key, int index, SoundDecoder.SampleProvider samples) : IDisposable
    {
        public AssetSoundKey Key => key;
        public int Index => index;
        public TimeSpan Time => samples.Time;
        public bool IsFinished => samples.IsFinished;

        public int Read(Span<float> buffer)
        {
            return samples.Read(buffer);
        }

        public void Dispose()
        {
            samples.Dispose();
        }
    }
}
