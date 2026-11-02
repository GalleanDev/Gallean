using Gallean.Configuration;

namespace Gallean.Client.Tests;

public sealed class TextCatalogLoaderTests
{
    private static readonly Language[] TranslatedLanguages =
    [
        Language.Czech,
        Language.French,
        Language.German,
        Language.Italian,
        Language.Polish,
        Language.Russian,
        Language.Spanish
    ];

    [Test]
    public void EnglishTest()
    {
        TextCatalog catalog = TextCatalogLoader.Load(Language.English);

        Assert.That(catalog.Translations, Is.Empty);
        Assert.That(catalog.Translate("Close"), Is.EqualTo("Close"));
    }

    [Test]
    public void CzechTest()
    {
        TextCatalog catalog = TextCatalogLoader.Load(Language.Czech);

        Assert.That(catalog.Translate("Close"), Is.EqualTo("Zavřít"));
        Assert.That(catalog.Translate("Settings"), Is.EqualTo("Nastavení"));
        Assert.That(catalog.Translate("Every {0} turns"), Is.EqualTo("Po {0} tazích"));
    }

    [Test]
    public void FrenchTest()
    {
        TextCatalog catalog = TextCatalogLoader.Load(Language.French);

        Assert.That(catalog.Translate("Close"), Is.EqualTo("Fermer"));
        Assert.That(catalog.Translate("Settings"), Is.EqualTo("Options"));
        Assert.That(catalog.Translate("Every {0} turns"), Is.EqualTo("Tous les {0} tours"));
    }

    [Test]
    public void GermanTest()
    {
        TextCatalog catalog = TextCatalogLoader.Load(Language.German);

        Assert.That(catalog.Translate("Close"), Is.EqualTo("Schließen"));
        Assert.That(catalog.Translate("Settings"), Is.EqualTo("Optionen"));
        Assert.That(catalog.Translate("Every {0} turns"), Is.EqualTo("Alle {0} Züge"));
    }

    [Test]
    public void ItalianTest()
    {
        TextCatalog catalog = TextCatalogLoader.Load(Language.Italian);

        Assert.That(catalog.Translate("Close"), Is.EqualTo("Chiudi"));
        Assert.That(catalog.Translate("Settings"), Is.EqualTo("Opzioni"));
        Assert.That(catalog.Translate("Every {0} turns"), Is.EqualTo("Ogni {0} turni"));
    }

    [Test]
    public void PolishTest()
    {
        TextCatalog catalog = TextCatalogLoader.Load(Language.Polish);

        Assert.That(catalog.Translate("Close"), Is.EqualTo("Zamknij"));
        Assert.That(catalog.Translate("Settings"), Is.EqualTo("Opcje"));
        Assert.That(catalog.Translate("Every {0} turns"), Is.EqualTo("Co {0} tur"));
    }

    [Test]
    public void RussianTest()
    {
        TextCatalog catalog = TextCatalogLoader.Load(Language.Russian);

        Assert.That(catalog.Translate("Close"), Is.EqualTo("Закрыть"));
        Assert.That(catalog.Translate("Settings"), Is.EqualTo("Опции"));
        Assert.That(catalog.Translate("Every {0} turns"), Is.EqualTo("Каждые {0} ходов"));
    }

    [Test]
    public void SpanishTest()
    {
        TextCatalog catalog = TextCatalogLoader.Load(Language.Spanish);

        Assert.That(catalog.Translate("Close"), Is.EqualTo("Cerrar"));
        Assert.That(catalog.Translate("Settings"), Is.EqualTo("Opciones"));
        Assert.That(catalog.Translate("Every {0} turns"), Is.EqualTo("Cada {0} turnos"));
    }

    [Test]
    public void CompleteTest()
    {
        HashSet<string> texts = new(StringComparer.Ordinal);

        foreach (ClientTextEntry entry in ClientTextIndex.All)
        {
            texts.Add(entry.Text);
        }

        foreach (Language language in TranslatedLanguages)
        {
            TextCatalog catalog = TextCatalogLoader.Load(language);

            Assert.That(catalog.Translations.Keys, Is.EquivalentTo(texts));
        }
    }

    [Test]
    public void PlaceholdersTest()
    {
        foreach (Language language in TranslatedLanguages)
        {
            TextCatalog catalog = TextCatalogLoader.Load(language);

            foreach ((string text, string translation) in catalog.Translations)
            {
                Assert.That(translation, Is.Not.Empty);
                Assert.That(translation.Contains("{0}"), Is.EqualTo(text.Contains("{0}")));
                Assert.That(translation.Contains("{1}"), Is.EqualTo(text.Contains("{1}")));
                Assert.That(translation.Contains("{2}"), Is.EqualTo(text.Contains("{2}")));
            }
        }
    }
}
