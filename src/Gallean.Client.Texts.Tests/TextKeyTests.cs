using System.ComponentModel;

namespace Gallean.Client.Texts.Tests;

[NonParallelizable]
public sealed class TextKeyTests
{
    [TearDown]
    public void TearDown()
    {
        new TextCatalog([]).MakeCurrent();
    }

    [Test]
    public void SourceTextTest()
    {
        TextKey key = TextKey.For("Close");

        Assert.That(key.Text, Is.EqualTo("Close"));
        Assert.That(key.Translation, Is.EqualTo("Close"));
    }

    [Test]
    public void InternTest()
    {
        TextKey key = TextKey.For("Close");

        Assert.That(TextKey.For("Close"), Is.SameAs(key));
        Assert.That(TextKey.For("Cancel"), Is.Not.SameAs(key));
    }

    [Test]
    public void TranslationChangedTest()
    {
        TextKey key = TextKey.For("Close");
        List<(string? Property, string Translation)> changes = [];
        key.PropertyChanged += (_, e) => changes.Add((e.PropertyName, key.Translation));

        new TextCatalog([new KeyValuePair<string, string>("Close", "Schliessen")]).MakeCurrent();
        new TextCatalog([]).MakeCurrent();

        Assert.That(changes, Is.EqualTo([("Translation", "Schliessen"), ("Translation", "Close")]));
    }

    [Test]
    public void UnsubscribeTest()
    {
        TextKey key = TextKey.For("Close");
        List<string?> changes = [];
        PropertyChangedEventHandler handler = (_, e) => changes.Add(e.PropertyName);
        key.PropertyChanged += handler;
        key.PropertyChanged -= handler;

        new TextCatalog([new KeyValuePair<string, string>("Close", "Schliessen")]).MakeCurrent();

        Assert.That(changes, Is.Empty);
    }
}
