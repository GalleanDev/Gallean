namespace Gallean.Client.Tests;

public sealed class ClientVersionFormatterTests
{
    [Test]
    public void RevisionTest()
    {
        ClientVersion version = new("0.1.0", "2d403009d94320d045b06c4c97671638a00444b2");

        Assert.That(ClientVersionFormatter.Format(version), Is.EqualTo("0.1.0+2d403009d"));
    }

    [Test]
    public void ShortRevisionTest()
    {
        ClientVersion version = new("0.1.0-rc1", "2d40300");

        Assert.That(ClientVersionFormatter.Format(version), Is.EqualTo("0.1.0-rc1+2d40300"));
    }

    [Test]
    public void NoRevisionTest()
    {
        ClientVersion version = new("0.1.0", null);

        Assert.That(ClientVersionFormatter.Format(version), Is.EqualTo("0.1.0"));
    }
}
