using NAudio.Wave;
using NVorbis;

namespace Packer.Tests;

public sealed class SoundEncoderTests
{
    private static AssetSound CreateClicks(WaveFormat format, int frameCount, params int[] clicks)
    {
        float[] samples = new float[frameCount * format.Channels];

        foreach (int click in clicks)
        {
            for (int channel = 0; channel < format.Channels; channel++)
            {
                samples[(click * format.Channels) + channel] = 0.8f;
            }
        }

        MemoryStream wave = new();

        using (WaveFileWriter writer = new(wave, format))
        {
            writer.WriteSamples(samples, 0, samples.Length);
        }

        return new AssetSound(new AssetSoundMetadata(), AssetStream.FromBytes(wave.ToArray()));
    }

    private static List<string> ReadClicks(AssetSound sound)
    {
        using Stream data = sound.Data.Open();
        using VorbisReader reader = new(data, closeOnDispose: false);
        float[] samples = new float[reader.TotalSamples * reader.Channels];
        int read = reader.ReadSamples(samples, 0, samples.Length);
        List<string> rows = [$"{reader.SampleRate} Hz, {reader.Channels} channels, {reader.TotalSamples} frames"];

        for (int index = 0; index < read; index++)
        {
            if (Math.Abs(samples[index]) > 0.4f)
            {
                rows.Add($"frame {index / reader.Channels} channel {index % reader.Channels}");
            }
        }

        return rows;
    }

    [Test]
    public void StereoTest()
    {
        AssetSound sound = CreateClicks(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2), 22050, 300, 5000, 21350);

        AssetSound encoded = SoundEncoder.Encode(sound);

        Assert.That(ReadClicks(encoded), Is.EqualTo([
            "44100 Hz, 2 channels, 22050 frames",
            "frame 300 channel 0",
            "frame 300 channel 1",
            "frame 5000 channel 0",
            "frame 5000 channel 1",
            "frame 21350 channel 0",
            "frame 21350 channel 1"
        ]));
    }

    [Test]
    public void MonoTest()
    {
        AssetSound sound = CreateClicks(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1), 22050, 300, 5000, 21350);

        AssetSound encoded = SoundEncoder.Encode(sound);

        Assert.That(ReadClicks(encoded), Is.EqualTo([
            "44100 Hz, 2 channels, 22050 frames",
            "frame 300 channel 0",
            "frame 300 channel 1",
            "frame 5000 channel 0",
            "frame 5000 channel 1",
            "frame 21350 channel 0",
            "frame 21350 channel 1"
        ]));
    }

    [Test]
    public void ResampleTest()
    {
        AssetSound sound = CreateClicks(WaveFormat.CreateIeeeFloatWaveFormat(22050, 1), 11025, 2500);

        AssetSound encoded = SoundEncoder.Encode(sound);

        Assert.That(ReadClicks(encoded), Is.EqualTo([
            "44100 Hz, 2 channels, 22050 frames",
            "frame 5002 channel 0",
            "frame 5002 channel 1",
            "frame 5003 channel 0",
            "frame 5003 channel 1"
        ]));
    }

    [Test]
    public void MetadataTest()
    {
        AssetSound sound = CreateClicks(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1), 4410) with
        {
            Metadata = new AssetSoundMetadata(AnimationFrame: 7, StartTime: TimeSpan.FromMilliseconds(50.0))
        };

        AssetSound encoded = SoundEncoder.Encode(sound);

        Assert.That(encoded.Metadata, Is.EqualTo(new AssetSoundMetadata(AnimationFrame: 7, StartTime: TimeSpan.FromMilliseconds(50.0))));
    }
}
