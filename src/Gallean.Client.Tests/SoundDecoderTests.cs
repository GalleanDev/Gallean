namespace Gallean.Client.Tests;

public sealed class SoundDecoderTests
{
    private sealed class TestReadStream(byte[] data) : Stream
    {
        private readonly MemoryStream inner = new(data);

        public long BytesRead { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = inner.Read(buffer, offset, count);

            BytesRead += read;

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return inner.Seek(offset, origin);
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            inner.Dispose();
            base.Dispose(disposing);
        }
    }

    [Test]
    public void DecodeTest()
    {
        using MemoryStream stream = new(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 4410));

        Sound sound = SoundDecoder.DecodeToMemory(stream);

        Assert.That(sound.Samples, Has.Length.EqualTo(8820));
    }

    [Test]
    public void StreamTest()
    {
        using TestReadStream stream = new(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 441000));

        using SoundDecoder.SampleProvider samples = SoundDecoder.CreateSamples(stream, TimeSpan.Zero);

        Assert.That(stream.BytesRead, Is.EqualTo(4816));
        Assert.That(samples.GetFrameCount(), Is.EqualTo(441000));
        Assert.That(stream.BytesRead, Is.EqualTo(4816));
    }

    [Test]
    public void StartTimeTest()
    {
        using MemoryStream stream = new(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 4410));

        using SoundDecoder.SampleProvider samples = SoundDecoder.CreateSamples(stream, TimeSpan.FromMilliseconds(5.0));
        float[] buffer = new float[16384];
        int read = samples.Read(buffer);

        Assert.That(read, Is.EqualTo(8380));
    }

    [Test]
    public void MonoTest()
    {
        using MemoryStream stream = new(TestSounds.Create(SoundDecoder.MixerSampleRate, 1, 4410));

        Assert.That(
            () => SoundDecoder.DecodeToMemory(stream),
            Throws.TypeOf<InvalidDataException>()
                .With.Message.EqualTo("Unsupported sound format '44100 Hz, 1 channels'."));
    }

    [Test]
    public void SampleRateTest()
    {
        using MemoryStream stream = new(TestSounds.Create(22050, SoundDecoder.MixerChannels, 4410));

        Assert.That(
            () => SoundDecoder.DecodeToMemory(stream),
            Throws.TypeOf<InvalidDataException>()
                .With.Message.EqualTo("Unsupported sound format '22050 Hz, 2 channels'."));
    }

    [Test]
    public void EmptyTest()
    {
        using MemoryStream stream = new(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 0));

        Assert.That(
            () => SoundDecoder.DecodeToMemory(stream),
            Throws.TypeOf<InvalidDataException>()
                .With.Message.EqualTo("Sound contains no samples."));
    }

    [Test]
    public void InvalidFormatTest()
    {
        using MemoryStream stream = new([82, 73, 70, 70, 1, 2, 3, 4]);

        Assert.That(
            () => SoundDecoder.DecodeToMemory(stream),
            Throws.TypeOf<ArgumentException>()
                .With.Message.EqualTo("Could not load the specified container! (Parameter 'containerReader')"));
    }
}
