namespace Extractor.Tests;

public sealed class SixbitTextEncoderTests
{
    [Test]
    public void ZeroesTest()
    {
        Assert.That(SixbitTextEncoder.Encode(new byte[6]), Is.EqualTo("!!!!!!!!"));
    }

    [Test]
    public void SingleHighBitTest()
    {
        Assert.That(SixbitTextEncoder.Encode([0x80, 0x00, 0x00]), Is.EqualTo("A!!!"));
    }

    [Test]
    public void SingleLowBitTest()
    {
        Assert.That(SixbitTextEncoder.Encode([0x01, 0x00, 0x00]), Is.EqualTo("!1!!"));
    }

    [Test]
    public void CharacterCountTest()
    {
        Assert.That(SixbitTextEncoder.Encode(new byte[128]), Has.Length.EqualTo(171));
    }

    [Test]
    public void ByteOrderTest()
    {
        Assert.That(SixbitTextEncoder.Encode([0x12, 0x34, 0x56]), Is.EqualTo("%D27"));
    }

    [Test]
    public void EmptyTest()
    {
        Assert.That(SixbitTextEncoder.Encode([]), Is.EqualTo(string.Empty));
    }
}
