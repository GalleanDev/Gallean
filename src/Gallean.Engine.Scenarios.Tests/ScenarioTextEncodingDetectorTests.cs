namespace Gallean.Engine.Scenarios.Tests;

public sealed class ScenarioTextEncodingDetectorTests
{
    [Test]
    public void RussianTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([
            "\u00d1\u00f2\u00ee\u00eb\u00e8\u00f6\u00e0 \u00c8\u00ec\u00ef\u00e5\u00f0\u00e8\u00e8"
        ]);

        Assert.That(encoding.GetString([0xd1, 0xf2, 0xee, 0xeb, 0xe8, 0xf6, 0xe0]), Is.EqualTo("Столица"));
    }

    [Test]
    public void RussianInEnglishTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([
            "The road to the capital is long and the gates are closed for the night.",
            "\u00d1\u00f2\u00ee\u00eb\u00e8\u00f6\u00e0"
        ]);

        Assert.That(encoding.GetString([0xd1, 0xf2, 0xee, 0xeb, 0xe8, 0xf6, 0xe0]), Is.EqualTo("Столица"));
    }

    [Test]
    public void PolishTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([
            "Wojownik zabi\u00b3 smoka w g\u00f3rach, \u00bfeby zdoby\u00e6 z\u00b3oto."
        ]);

        Assert.That(encoding.GetString([0x7a, 0xb3, 0x6f, 0x74, 0x6f]), Is.EqualTo("złoto"));
    }

    [Test]
    public void CzechTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([
            "V\u00e1le\u00e8n\u00edk z\u00edskal \u00f8\u00ed\u009ai."
        ]);

        Assert.That(encoding.GetString([0xf8, 0xed, 0x9a, 0x69]), Is.EqualTo("říši"));
    }

    [Test]
    public void FrenchTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([
            "Le guerrier a tu\u00e9 le dragon pr\u00e8s de la for\u00eat."
        ]);

        Assert.That(encoding.GetString([0x70, 0x72, 0xe8, 0x73]), Is.EqualTo("près"));
    }

    [Test]
    public void SpanishTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([
            "\u00bfD\u00f3nde est\u00e1 el drag\u00f3n, se\u00f1or?"
        ]);

        Assert.That(encoding.GetString([0x73, 0x65, 0xf1, 0x6f, 0x72]), Is.EqualTo("señor"));
    }

    [Test]
    public void ItalianTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([
            "La citt\u00e0 \u00e8 pi\u00f9 grande, cos\u00ec dicono."
        ]);

        Assert.That(encoding.GetString([0x63, 0x6f, 0x73, 0xec]), Is.EqualTo("così"));
    }

    [Test]
    public void GermanTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([
            "Der Krieger t\u00f6tete den Drachen f\u00fcr die Gr\u00f6\u00dfe."
        ]);

        Assert.That(encoding.GetString([0x47, 0x72, 0xf6, 0xdf, 0x65]), Is.EqualTo("Größe"));
    }

    [Test]
    public void EnglishTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect(["The warrior slew the dragon."]);

        Assert.That(encoding.GetString([0xe6]), Is.EqualTo("æ"));
    }

    [Test]
    public void NoTextTest()
    {
        ScenarioTextEncoding encoding = ScenarioTextEncodingDetector.Detect([]);

        Assert.That(encoding.GetString([0xe6]), Is.EqualTo("æ"));
    }
}
