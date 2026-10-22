namespace GameContent.Tests;

public sealed class StreamExtensionsTests
{
    [Test]
    public void ReadInt16LittleEndianTest()
    {
        byte[] data = [0x01, 0x00];
        using MemoryStream stream = new(data);

        short result = stream.ReadInt16LittleEndian();

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void ReadInt16LittleEndianNegativeTest()
    {
        byte[] data = [0xff, 0xff];
        using MemoryStream stream = new(data);

        short result = stream.ReadInt16LittleEndian();

        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void ReadInt32LittleEndianTest()
    {
        byte[] data = [0x2a, 0x00, 0x00, 0x00];
        using MemoryStream stream = new(data);

        int result = stream.ReadInt32LittleEndian();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ReadInt32LittleEndianLargeValueTest()
    {
        byte[] data = [0x00, 0x01, 0x00, 0x00];
        using MemoryStream stream = new(data);

        int result = stream.ReadInt32LittleEndian();

        Assert.That(result, Is.EqualTo(256));
    }

    [Test]
    public void ReadInt64LittleEndianTest()
    {
        byte[] data = [0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        using MemoryStream stream = new(data);

        long result = stream.ReadInt64LittleEndian();

        Assert.That(result, Is.EqualTo(7L));
    }

    [Test]
    public void SkipTest()
    {
        byte[] data = [0, 1, 2, 3, 4, 5];
        using MemoryStream stream = new(data);

        stream.Skip(4);

        Assert.That(stream.Position, Is.EqualTo(4));

        int result = stream.ReadInt16LittleEndian();

        Assert.That(result, Is.EqualTo(0x0504));
    }

    [Test]
    public void ReadAsciiStringFixedLengthTest()
    {
        byte[] data = "Hello\0\0\0"u8.ToArray();
        using MemoryStream stream = new(data);

        string result = stream.ReadAsciiString(8);

        Assert.That(result, Is.EqualTo("Hello"));
    }

    [Test]
    public void ReadAsciiStringFixedLengthNoNullTest()
    {
        byte[] data = "MQDB"u8.ToArray();
        using MemoryStream stream = new(data);

        string result = stream.ReadAsciiString(4);

        Assert.That(result, Is.EqualTo("MQDB"));
    }

    [Test]
    public void ReadAsciiStringFixedLengthEmptyTest()
    {
        byte[] data = [0x00, 0x00, 0x00];
        using MemoryStream stream = new(data);

        string result = stream.ReadAsciiString(3);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ReadAsciiStringNullTerminatedTest()
    {
        byte[] data = [.. "Test"u8.ToArray(), 0x00, 0xff];
        using MemoryStream stream = new(data);

        string result = stream.ReadAsciiString();

        Assert.That(result, Is.EqualTo("Test"));
        Assert.That(stream.Position, Is.EqualTo(5));
    }

    [Test]
    public void ReadAsciiStringNullTerminatedEmptyTest()
    {
        byte[] data = [0x00, 0x41];
        using MemoryStream stream = new(data);

        string result = stream.ReadAsciiString();

        Assert.That(result, Is.EqualTo(string.Empty));
    }
}
