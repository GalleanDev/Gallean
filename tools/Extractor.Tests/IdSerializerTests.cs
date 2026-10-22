namespace Extractor.Tests;

public sealed class IdSerializerTests
{
    [Test]
    public void DeserializeTest()
    {
        Assert.That(IdSerializer.Deserialize(196613u), Is.EqualTo(new Db.Id("G000RR0005")));
    }

    [Test]
    public void SerializeTest()
    {
        Assert.That(IdSerializer.Serialize(new Db.Id("G000RR0005")), Is.EqualTo(196613u));
    }

    [Test]
    public void RoundTripTest()
    {
        IReadOnlyList<Db.Id> ids =
        [
            new("G000RR0005"),
            new("G000TA0001"),
            new("G000UU8040"),
            new("G160TG0013"),
            new("C000CC0001"),
            new("S205SC0000"),
            new("S205KC001C"),
            new("E255SV9999")
        ];

        Assert.That(ids.Select(id => IdSerializer.Deserialize(IdSerializer.Serialize(id))), Is.EqualTo(ids));
    }

    [Test]
    public void DeserializeUnknownTypeTest()
    {
        Assert.That(() => IdSerializer.Deserialize(0u), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void SerializeUnknownTypeTest()
    {
        Assert.That(() => IdSerializer.Serialize(new Db.Id("G000ZZ0001")), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void SerializeUnknownCategoryTest()
    {
        Assert.That(() => IdSerializer.Serialize(new Db.Id("X005TA0742")), Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void SerializeShortIdTest()
    {
        Assert.That(() => IdSerializer.Serialize(new Db.Id("G000RR")), Throws.TypeOf<InvalidDataException>());
    }
}
