namespace Gallean.Engine.Tests;

public sealed class ScenarioPopupTextTests
{
    [Test]
    public void SpeakerAndSpeechTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("Spearman\n\"None can pass here.\"");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText("Spearman", "None can pass here.")));
    }

    [Test]
    public void PunctuationAfterClosingQuoteTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("Копейщик\n\"Здесь никто не пройдет\".");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText("Копейщик", "Здесь никто не пройдет.")));
    }

    [Test]
    public void QuoteInsideSpeechTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("Elder\n\"We were not allowed to speak on its \"peace council\" at all.\"");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText("Elder", "We were not allowed to speak on its \"peace council\" at all.")));
    }

    [Test]
    public void NoClosingQuoteTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("Elder\n\"You will pay for this betrayal.");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText("Elder", "You will pay for this betrayal.")));
    }

    [Test]
    public void PlainTextTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("The Emperor was found in the rubble.");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText(Speaker: null, "The Emperor was found in the rubble.")));
    }
}
