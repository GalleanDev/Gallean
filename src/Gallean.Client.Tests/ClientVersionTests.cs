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

    [Test]
    public void CompareTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(new ClientVersion("0.14.0", null).CompareTo(new ClientVersion("0.13.0", "2d40300")), Is.GreaterThan(0));
            Assert.That(new ClientVersion("0.13.10", null).CompareTo(new ClientVersion("0.13.9", null)), Is.GreaterThan(0));
            Assert.That(new ClientVersion("0.13.0", null).CompareTo(new ClientVersion("0.13.0", "2d40300")), Is.EqualTo(0));
            Assert.That(new ClientVersion("0.12.1", null).CompareTo(new ClientVersion("0.13.0", null)), Is.LessThan(0));
            Assert.That(new ClientVersion("latest", null).CompareTo(new ClientVersion("0.13.0", null)), Is.LessThan(0));
            Assert.That(new ClientVersion("0.14.0", null) > new ClientVersion("0.13.0", null), Is.True);
            Assert.That(new ClientVersion("0.13.0", null) < new ClientVersion("0.14.0", null), Is.True);
            Assert.That(new ClientVersion("0.13.0", null) > new ClientVersion("0.13.0", null), Is.False);
        }
    }
}
