using System.Globalization;

namespace Gallean.Client.Tests;

public sealed class LanguageExtensionsTests
{
    [Test]
    public void FromCultureTest()
    {
        Assert.That(Language.FromCulture(new CultureInfo("en-GB")), Is.EqualTo(Language.English));
        Assert.That(Language.FromCulture(new CultureInfo("ru-RU")), Is.EqualTo(Language.Russian));
        Assert.That(Language.FromCulture(new CultureInfo("de-AT")), Is.EqualTo(Language.German));
        Assert.That(Language.FromCulture(new CultureInfo("es-MX")), Is.EqualTo(Language.Spanish));
        Assert.That(Language.FromCulture(new CultureInfo("pt-BR")), Is.EqualTo(Language.English));
        Assert.That(Language.FromCulture(new CultureInfo("ja-JP")), Is.EqualTo(Language.English));
        Assert.That(Language.FromCulture(CultureInfo.InvariantCulture), Is.EqualTo(Language.English));
    }
}
