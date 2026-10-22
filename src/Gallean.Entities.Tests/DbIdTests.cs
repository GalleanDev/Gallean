using System.Text.Json;

namespace Gallean.Entities.Tests;

public sealed class DbIdTests
{
    [Test]
    public void ValidIdComponentsTest()
    {
        Db.Id id = new("G000UU0001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(id.Category, Is.EqualTo(Db.IdCategory.Global));
            Assert.That(id.CategoryIndex, Is.Zero);
            Assert.That(id.TypeCode, Is.EqualTo("UU"));
            Assert.That(id.Type, Is.EqualTo(Db.IdType.UnitGlobal));
            Assert.That(id.TypeIndex, Is.EqualTo(1));
        }
    }

    [Test]
    public void InvalidIdComponentsTest()
    {
        Db.Id id = new("InvalidString");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(id.Category, Is.EqualTo(Db.IdCategory.Invalid));
            Assert.That(id.CategoryIndex, Is.EqualTo(-1));
            Assert.That(id.TypeCode, Is.EqualTo("LI"));
            Assert.That(id.Type, Is.EqualTo(Db.IdType.Invalid));
            Assert.That(id.TypeIndex, Is.EqualTo(-1));
        }
    }

    [Test]
    public void ImplicitConversionFromStringTest()
    {
        Db.Id id = "G000UU0001";
        Assert.That(id.Value, Is.EqualTo("G000UU0001"));
    }

    [Test]
    public void ImplicitConversionToStringTest()
    {
        Db.Id id = new("G000UU0001");
        string str = id;
        Assert.That(str, Is.EqualTo("G000UU0001"));
    }

    [Test]
    public void JsonSerializationTest()
    {
        Db.Id id = new("G000UU0001");
        string json = JsonSerializer.Serialize(id);
        Assert.That(json, Is.EqualTo("\"G000UU0001\""));
    }

    [Test]
    public void JsonDeserializationTest()
    {
        const string json = "\"G000UU0001\"";

        Db.Id id = JsonSerializer.Deserialize<Db.Id>(json);
        Assert.That(id.Value, Is.EqualTo("G000UU0001"));
    }

    [Test]
    public void JsonDeserializationEmptyValueTest()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Db.Id>("\"G000000000\""));
    }

    [Test]
    public void JsonDeserializationNullableTest()
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new Db.IdJsonConverterFactory());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(JsonSerializer.Deserialize<Db.Id?>("\"G000UU0001\"", options), Is.EqualTo(new Db.Id("G000UU0001")));
            Assert.That(JsonSerializer.Deserialize<Db.Id?>("\"G000000000\"", options), Is.Null);
            Assert.That(JsonSerializer.Deserialize<Db.Id?>("null", options), Is.Null);
        }
    }

    [Test]
    public void JsonDeserializationNullableRecordTest()
    {
        const string json = """{ "MOD_EQUIP": "G000000000", "ATTACK_ID": "G000AA0001" }""";

        JsonSerializerOptions options = new();
        options.Converters.Add(new Db.IdJsonConverterFactory());

        Db.Item? item = JsonSerializer.Deserialize<Db.Item>(json, options);
        Assert.That(item, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.ModifierEquipment, Is.Null);
            Assert.That(item.AttackId, Is.EqualTo(new Db.Id("G000AA0001")));
        }
    }

    [Test]
    public void EqualityTest()
    {
        Db.Id upperId = new("G000UU0019");
        Db.Id lowerId = new("g000uu0019");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upperId, Is.EqualTo(lowerId));
            Assert.That(upperId.GetHashCode(), Is.EqualTo(lowerId.GetHashCode()));
        }
    }
}
