namespace GameContent.Tests;

public sealed class SubstreamTests
{
    [Test]
    public void ReadFromOffsetTest()
    {
        byte[] data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        using MemoryStream baseStream = new(data);
        using Substream substream = new(baseStream, baseOffset: 3, length: 4);

        byte[] buffer = new byte[4];
        int bytesRead = substream.Read(buffer, 0, 4);

        Assert.That(bytesRead, Is.EqualTo(4));
        Assert.That(buffer, Is.EqualTo(new byte[] { 3, 4, 5, 6 }));
    }

    [Test]
    public void ReadClampedTest()
    {
        byte[] data = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        using MemoryStream baseStream = new(data);
        using Substream substream = new(baseStream, baseOffset: 7, length: 3);

        byte[] buffer = new byte[10];
        int bytesRead = substream.Read(buffer, 0, 10);

        Assert.That(bytesRead, Is.EqualTo(3));
        Assert.That(buffer[..3], Is.EqualTo(new byte[] { 7, 8, 9 }));
    }

    [Test]
    public void LengthAndPositionTest()
    {
        byte[] data = [0, 1, 2, 3, 4];
        using MemoryStream baseStream = new(data);
        using Substream substream = new(baseStream, baseOffset: 1, length: 3);

        Assert.That(substream.Length, Is.EqualTo(3));
        Assert.That(substream.Position, Is.Zero);

        _ = substream.Read(new byte[2], 0, 2);
        Assert.That(substream.Position, Is.EqualTo(2));
    }

    [Test]
    public void SeekFromBeginTest()
    {
        byte[] data = [10, 20, 30, 40, 50];
        using MemoryStream baseStream = new(data);
        using Substream substream = new(baseStream, baseOffset: 0, length: 5);

        substream.Seek(3, SeekOrigin.Begin);

        byte[] buffer = new byte[1];
        _ = substream.Read(buffer, 0, 1);

        Assert.That(buffer[0], Is.EqualTo(40));
    }

    [Test]
    public void SeekFromEndTest()
    {
        byte[] data = [10, 20, 30, 40, 50];
        using MemoryStream baseStream = new(data);
        using Substream substream = new(baseStream, baseOffset: 0, length: 5);

        substream.Seek(-2, SeekOrigin.End);

        byte[] buffer = new byte[1];
        _ = substream.Read(buffer, 0, 1);

        Assert.That(buffer[0], Is.EqualTo(40));
    }

    [Test]
    public void ReadPastEndTest()
    {
        byte[] data = [1, 2, 3];
        using MemoryStream baseStream = new(data);
        using Substream substream = new(baseStream, baseOffset: 0, length: 3);

        _ = substream.Read(new byte[3], 0, 3);
        int bytesRead = substream.Read(new byte[1], 0, 1);

        Assert.That(bytesRead, Is.Zero);
    }

    [Test]
    public void WriteTest()
    {
        byte[] data = [1, 2, 3];
        using MemoryStream baseStream = new(data);
        using Substream substream = new(baseStream, baseOffset: 0, length: 3);

        Assert.That(substream.CanWrite, Is.False);
        Assert.Throws<NotSupportedException>(() => substream.Write([1], 0, 1));
    }
}
