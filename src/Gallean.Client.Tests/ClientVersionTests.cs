namespace Gallean.Client.Tests;

public sealed class ClientVersionTests
{
    [Test]
    public void SerializeTest()
    {
        ClientVersion version = new("0.1.0", "2d403009d94320d045b06c4c97671638a00444b2");

        Assert.That(version.Serialize(), Is.EqualTo("0.1.0+2d403009d94320d045b06c4c97671638a00444b2"));
    }

    [Test]
    public void SerializeNoRevisionTest()
    {
        ClientVersion version = new("0.1.0", null);

        Assert.That(version.Serialize(), Is.EqualTo("0.1.0"));
    }

    [Test]
    public void DeserializeTest()
    {
        Assert.That(ClientVersion.Deserialize("0.1.0+2d403009d"), Is.EqualTo(new ClientVersion("0.1.0", "2d403009d")));
    }

    [Test]
    public void DeserializeNoRevisionTest()
    {
        Assert.That(ClientVersion.Deserialize("0.1.0"), Is.EqualTo(new ClientVersion("0.1.0", null)));
    }
}
