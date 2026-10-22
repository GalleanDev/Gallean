namespace Gallean.Engine.Tests;

public sealed class SixbitTextDecoderTests
{
    [Test]
    public void ZeroesTest()
    {
        Assert.That(SixbitTextDecoder.Decode("!!!!!!!!"), Is.EqualTo(new byte[6]));
    }

    [Test]
    public void SingleHighBitTest()
    {
        Assert.That(SixbitTextDecoder.Decode("A!!!"), Is.EqualTo(new byte[] { 0x80, 0x00, 0x00 }));
    }

    [Test]
    public void SingleLowBitTest()
    {
        Assert.That(SixbitTextDecoder.Decode("!1!!"), Is.EqualTo(new byte[] { 0x01, 0x00, 0x00 }));
    }

    [Test]
    public void ByteCountTest()
    {
        Assert.That(SixbitTextDecoder.Decode(new string('!', 171)), Is.EqualTo(new byte[128]));
    }

    [Test]
    public void ByteOrderTest()
    {
        Assert.That(SixbitTextDecoder.Decode("%D27"), Is.EqualTo(new byte[] { 0x12, 0x34, 0x56 }));
    }

    [Test]
    public void EmptyTest()
    {
        Assert.That(SixbitTextDecoder.Decode(string.Empty), Is.Empty);
    }

    [Test]
    public void UnsupportedCharacterTest()
    {
        Assert.That(() => SixbitTextDecoder.Decode("!!a!"), Throws.InstanceOf<InvalidDataException>());
    }
}
