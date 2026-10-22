using System.Text.Json;

namespace Gallean.Entities.Tests;

public sealed class DbResourceSetTests
{
    [Test]
    public void ParseTest()
    {
        Db.ResourceSet resources = Db.ResourceSet.Parse("g0150:r0010:y0025:e0030:w0045:b0099");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resources.Gold, Is.EqualTo(150));
            Assert.That(resources.InfernalMana, Is.EqualTo(10));
            Assert.That(resources.LifeMana, Is.EqualTo(25));
            Assert.That(resources.DeathMana, Is.EqualTo(30));
            Assert.That(resources.RuneMana, Is.EqualTo(45));
        }
    }

    [Test]
    public void ParseCaseInsensitiveTest()
    {
        Db.ResourceSet resources = Db.ResourceSet.Parse("G0150:R0010:Y0025:E0030:W0045:B0099");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resources.Gold, Is.EqualTo(150));
            Assert.That(resources.InfernalMana, Is.EqualTo(10));
            Assert.That(resources.LifeMana, Is.EqualTo(25));
            Assert.That(resources.DeathMana, Is.EqualTo(30));
            Assert.That(resources.RuneMana, Is.EqualTo(45));
            Assert.That(resources.GroveMana, Is.EqualTo(99));
        }
    }

    [Test]
    public void EmptyStringTest()
    {
        Db.ResourceSet resources = Db.ResourceSet.Parse("");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resources.Gold, Is.Zero);
            Assert.That(resources.InfernalMana, Is.Zero);
            Assert.That(resources.LifeMana, Is.Zero);
            Assert.That(resources.DeathMana, Is.Zero);
            Assert.That(resources.RuneMana, Is.Zero);
            Assert.That(resources.GroveMana, Is.Zero);
            Assert.That(resources.IsEmpty, Is.True);
        }
    }

    [Test]
    public void MissingResourceTypeTest()
    {
        Db.ResourceSet resources = Db.ResourceSet.Parse("g0150:e0030");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resources.Gold, Is.EqualTo(150));
            Assert.That(resources.DeathMana, Is.EqualTo(30));
            Assert.That(resources.LifeMana, Is.Zero);
        }
    }

    [Test]
    public void ValuePropertyTest()
    {
        Db.ResourceSet resources = Db.ResourceSet.Parse("g0150:e0030");
        Assert.That(resources.Value, Is.EqualTo("g0150:r0000:y0000:e0030:w0000:b0000"));
    }

    [Test]
    public void ToStringTest()
    {
        Db.ResourceSet resources = Db.ResourceSet.Parse("g0150:e0030");
        string str = resources.ToString();
        Assert.That(str, Is.EqualTo("g0150:r0000:y0000:e0030:w0000:b0000"));
    }

    [Test]
    public void JsonSerializationTest()
    {
        Db.ResourceSet resources = Db.ResourceSet.Parse("g0150:e0030");
        string json = JsonSerializer.Serialize(resources);
        Assert.That(json, Is.EqualTo("\"g0150:r0000:y0000:e0030:w0000:b0000\""));
    }

    [Test]
    public void JsonDeserializationTest()
    {
        string json = "\"g0150:e0030\"";
        Db.ResourceSet resources = JsonSerializer.Deserialize<Db.ResourceSet>(json);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resources.Value, Is.EqualTo("g0150:r0000:y0000:e0030:w0000:b0000"));
            Assert.That(resources.Gold, Is.EqualTo(150));
        }
    }
}
