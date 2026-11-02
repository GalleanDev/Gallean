using System.Text;
using System.Text.Json;

namespace Gallean.Client.Texts.Tests;

public sealed class TextCatalogReaderTests
{
    private static TextCatalog Read(string json)
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));

        return TextCatalogReader.Read(stream);
    }

    [Test]
    public void ReadTest()
    {
        TextCatalog catalog = Read("{\"formatVersion\": 1, \"translations\": {\"Close\": \"Закрыть\", \"Every {0} turns\": \"Каждые {0} ходов\"}}");

        Assert.That(catalog.Translations, Is.EquivalentTo(new Dictionary<string, string>
        {
            ["Close"] = "Закрыть",
            ["Every {0} turns"] = "Каждые {0} ходов"
        }));
        Assert.That(catalog.Translate("Close"), Is.EqualTo("Закрыть"));
        Assert.That(catalog.Translate("Every {0} turns"), Is.EqualTo("Каждые {0} ходов"));
        Assert.That(catalog.Translate("Settings"), Is.EqualTo("Settings"));
    }

    [Test]
    public void EmptyTest()
    {
        TextCatalog catalog = Read("{\"formatVersion\": 1, \"translations\": {}}");

        Assert.That(catalog.Translations, Is.Empty);
        Assert.That(catalog.Translate("Close"), Is.EqualTo("Close"));
    }

    [Test]
    public void DuplicateTranslationTest()
    {
        TextCatalog catalog = Read("{\"formatVersion\": 1, \"translations\": {\"Close\": \"Zu\", \"Close\": \"Schliessen\"}}");

        Assert.That(catalog.Translate("Close"), Is.EqualTo("Schliessen"));
    }

    [Test]
    public void UnsupportedFormatVersionTest()
    {
        Assert.That(
            () => Read("{\"formatVersion\": 2, \"translations\": {}}"),
            Throws.TypeOf<InvalidDataException>().With.Message.EqualTo("Unsupported text catalog format '2'."));
    }

    [Test]
    public void NotObjectTest()
    {
        Assert.That(() => Read("[]"), Throws.InstanceOf<JsonException>());
    }

    [Test]
    public void MissingFormatVersionTest()
    {
        Assert.That(() => Read("{\"translations\": {}}"), Throws.InstanceOf<JsonException>());
    }

    [Test]
    public void MissingTranslationsTest()
    {
        Assert.That(() => Read("{\"formatVersion\": 1}"), Throws.InstanceOf<JsonException>());
    }

    [Test]
    public void NullTranslationsTest()
    {
        Assert.That(() => Read("{\"formatVersion\": 1, \"translations\": null}"), Throws.InstanceOf<JsonException>());
    }

    [Test]
    public void NonStringTranslationTest()
    {
        Assert.That(() => Read("{\"formatVersion\": 1, \"translations\": {\"Close\": 5}}"), Throws.InstanceOf<JsonException>());
    }

    [Test]
    public void InvalidJsonTest()
    {
        Assert.That(() => Read("not json"), Throws.InstanceOf<JsonException>());
    }
}
