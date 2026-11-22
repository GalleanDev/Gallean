using NAudio.Wave;

namespace Gallean.Client.Tests;

public sealed class CubicVolumeSampleProviderTests
{
    private sealed class TestSampleProvider(float sample) : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels);

        public int Read(Span<float> buffer)
        {
            buffer.Fill(sample);

            return buffer.Length;
        }
    }

    [Test]
    public void HalfVolumeTest()
    {
        CubicVolumeSampleProvider provider = new(new TestSampleProvider(0.8f), 0.5f);
        float[] samples = new float[4];

        int read = provider.Read(samples);

        Assert.That(read, Is.EqualTo(4));
        Assert.That(samples, Is.EqualTo([0.1f, 0.1f, 0.1f, 0.1f]));
    }

    [Test]
    public void FullVolumeTest()
    {
        CubicVolumeSampleProvider provider = new(new TestSampleProvider(0.8f), 1.0f);
        float[] samples = new float[4];

        int read = provider.Read(samples);

        Assert.That(read, Is.EqualTo(4));
        Assert.That(samples, Is.EqualTo([0.8f, 0.8f, 0.8f, 0.8f]));
    }

    [Test]
    public void ZeroVolumeTest()
    {
        CubicVolumeSampleProvider provider = new(new TestSampleProvider(0.8f), 0.0f);
        float[] samples = new float[4];

        int read = provider.Read(samples);

        Assert.That(read, Is.EqualTo(4));
        Assert.That(samples, Is.EqualTo([0.0f, 0.0f, 0.0f, 0.0f]));
    }

    [Test]
    public void ChangeVolumeTest()
    {
        CubicVolumeSampleProvider provider = new(new TestSampleProvider(0.8f), 1.0f);
        float[] samples = new float[4];

        provider.Volume = 0.5f;
        int read = provider.Read(samples);

        Assert.That(provider.Volume, Is.EqualTo(0.5f));
        Assert.That(read, Is.EqualTo(4));
        Assert.That(samples, Is.EqualTo([0.1f, 0.1f, 0.1f, 0.1f]));
    }
}
