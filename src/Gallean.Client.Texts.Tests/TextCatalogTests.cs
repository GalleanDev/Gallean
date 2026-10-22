namespace Gallean.Client.Texts.Tests;

[NonParallelizable]
public sealed class TextCatalogTests
{
    [TearDown]
    public void TearDown()
    {
        new TextCatalog([]).Activate();
    }

    [Test]
    public void SourceTextTest()
    {
        Assert.That(TextCatalog.Tr("Sound and music"), Is.EqualTo("Sound and music"));
    }

    [Test]
    public void FormatTest()
    {
        Assert.That(TextCatalog.Tr("Move: {0} / {1}", 3, 5), Is.EqualTo("Move: 3 / 5"));
    }

    [Test]
    public void TranslationTest()
    {
        new TextCatalog([new KeyValuePair<string, string>("Close", "Schliessen")]).Activate();

        Assert.That(TextCatalog.Tr("Close"), Is.EqualTo("Schliessen"));
    }

    [Test]
    public void PartialTranslationTest()
    {
        new TextCatalog([new KeyValuePair<string, string>("Close", "Schliessen")]).Activate();

        Assert.That(TextCatalog.Tr("Settings"), Is.EqualTo("Settings"));
    }

    [Test]
    public void TranslatedFormatTest()
    {
        new TextCatalog([new KeyValuePair<string, string>("Every {0} turns", "Alle {0} Runden")]).Activate();

        Assert.That(TextCatalog.Tr("Every {0} turns", 5), Is.EqualTo("Alle 5 Runden"));
    }

    [Test]
    public void SpansTest()
    {
        TextSpans text = TextCatalog.TrSpans("Move: {0:*} / {1:*}", 3, 5);

        Assert.That(text.Spans, Is.EqualTo([
            new TextSpan("Move: ", TextStyle.None),
            new TextSpan("3", TextStyle.Highlight),
            new TextSpan(" / ", TextStyle.None),
            new TextSpan("5", TextStyle.Highlight)
        ]));
    }

    [Test]
    public void SpansWithoutArgumentsTest()
    {
        TextSpans text = TextCatalog.TrSpans("Plundered");

        Assert.That(text.Spans, Is.EqualTo([new TextSpan("Plundered", TextStyle.None)]));
    }

    [Test]
    public void SpansStylesTest()
    {
        TextSpans text = TextCatalog.TrSpans("{0:b} and {1:i} and {2:*bi}", "bold", "italic", "all");

        Assert.That(text.Spans, Is.EqualTo([
            new TextSpan("bold", TextStyle.Bold),
            new TextSpan(" and ", TextStyle.None),
            new TextSpan("italic", TextStyle.Italic),
            new TextSpan(" and ", TextStyle.None),
            new TextSpan("all", TextStyle.Highlight | TextStyle.Bold | TextStyle.Italic)
        ]));
    }

    [Test]
    public void SpansUnknownStyleTest()
    {
        Assert.Throws<FormatException>(() => TextCatalog.TrSpans("{0:D3}", 7));
    }

    [Test]
    public void SpansRepeatedArgumentTest()
    {
        TextSpans text = TextCatalog.TrSpans("{0:*} or {0} or {0:*}", 7);

        Assert.That(text.Spans, Is.EqualTo([
            new TextSpan("7", TextStyle.Highlight),
            new TextSpan(" or 7 or ", TextStyle.None),
            new TextSpan("7", TextStyle.Highlight)
        ]));
    }

    [Test]
    public void SpansEscapeTest()
    {
        TextSpans text = TextCatalog.TrSpans("{{{0:*}}} {{x}}", 1);

        Assert.That(text.Spans, Is.EqualTo([
            new TextSpan("{", TextStyle.None),
            new TextSpan("1", TextStyle.Highlight),
            new TextSpan("} {x}", TextStyle.None)
        ]));
    }

    [Test]
    public void TranslatedSpansTest()
    {
        new TextCatalog([new KeyValuePair<string, string>("Owner: {0:*}", "{0:*} gehört es")]).Activate();

        TextSpans text = TextCatalog.TrSpans("Owner: {0:*}", "Empire");

        Assert.That(text.Spans, Is.EqualTo([
            new TextSpan("Empire", TextStyle.Highlight),
            new TextSpan(" gehört es", TextStyle.None)
        ]));
    }

    [Test]
    public void SpansEqualityTest()
    {
        TextSpans expected = new([new TextSpan("Owner: ", TextStyle.None), new TextSpan("Empire", TextStyle.Highlight)]);

        Assert.That(TextCatalog.TrSpans("Owner: {0:*}", "Empire"), Is.EqualTo(expected));
    }

    [Test]
    public void SpansPlainTextTest()
    {
        TextSpans text = TextCatalog.TrSpans("Move: {0:*} / {1:*}", 3, 5);

        Assert.That(text.Text, Is.EqualTo("Move: 3 / 5"));
    }

    [Test]
    public void SpansPlainArgumentTest()
    {
        TextSpans text = TextCatalog.TrSpans("{0:*}, defeated by {1}", "Aldric", "Thug");

        Assert.That(text.Spans, Is.EqualTo([
            new TextSpan("Aldric", TextStyle.Highlight),
            new TextSpan(", defeated by Thug", TextStyle.None)
        ]));
    }
}
