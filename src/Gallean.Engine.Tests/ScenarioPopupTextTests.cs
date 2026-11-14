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

    [Test]
    public void QuotedSpeakerNameTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("Разбойник из \"Призрачных волков\" \n\"Нежить! Каким бы оно ни было...\"");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText("Разбойник из \"Призрачных волков\"", "Нежить! Каким бы оно ни было...")));
    }

    [Test]
    public void SpeakerOnSpeechLineTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("Ratgeber    \"Schenkt den Drohungen der Elfen keine Beachtung.\"");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText("Ratgeber", "Schenkt den Drohungen der Elfen keine Beachtung.")));
    }

    [Test]
    public void SpeakerWithColonTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("Nain : \"Enfin !\"");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText("Nain", "Enfin !")));
    }

    [Test]
    public void NarrationWithQuoteTest()
    {
        ScenarioPopupText actual = ScenarioPopupText.Parse("As Erhog vanished, she shouted: \"We will meet again!\" The Emperor was found in the rubble.");

        Assert.That(actual, Is.EqualTo(new ScenarioPopupText(Speaker: null, "As Erhog vanished, she shouted: \"We will meet again!\" The Emperor was found in the rubble.")));
    }
}
