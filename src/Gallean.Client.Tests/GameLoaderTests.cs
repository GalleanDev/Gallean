using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client.Tests;

public sealed class GameLoaderTests
{
    private static GameScenario FindScenario(IReadOnlyList<GameCampaign> campaigns, string scenarioId)
    {
        foreach (GameCampaign campaign in campaigns)
        {
            foreach (GameScenario scenario in campaign.Scenarios)
            {
                if (scenario.Id == scenarioId)
                {
                    return scenario;
                }
            }
        }

        throw new KeyNotFoundException($"Scenario '{scenarioId}' was not found.");
    }

    [Test]
    public void LoadCampaignsEnglishTest()
    {
        (IReadOnlyList<GameCampaign> campaigns, _, _) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.English);

        GameScenario tutorial = FindScenario(campaigns, GameLoader.TutorialScenarioId);

        Assert.That(tutorial.Name, Is.EqualTo("Tutorial"));
        Assert.That(tutorial.Description, Is.EqualTo("To quickly learn the basics of Disciples II, try this tutorial."));
        Assert.That(tutorial.Objective, Is.EqualTo("Capture two cities:\nNepheris and Davon Fort"));
    }

    [Test]
    public void LoadCampaignsRussianTest()
    {
        (IReadOnlyList<GameCampaign> campaigns, _, _) = new GameLoader(new RandomProvider(0)).LoadCampaigns(Language.Russian);

        GameScenario antagonists = FindScenario(campaigns, "S095SC0000");
        GameScenario tutorial = FindScenario(campaigns, GameLoader.TutorialScenarioId);

        Assert.That(antagonists.Name, Is.EqualTo("Противостояние"));
        Assert.That(antagonists.Description, Is.EqualTo("Император Демосфен решил перенести свою столицу на север, в укрепленный город Финдар. Инквизитор Филипп д'Агинкур принес клятву любой ценой защищать его."));
        Assert.That(antagonists.Objective, Is.EqualTo("Уничтожить Юбера де Лали"));
        Assert.That(tutorial.Name, Is.EqualTo("Обучение"));
        Assert.That(tutorial.Description, Is.EqualTo("С помощью этого обучения Вы можете освоить основы игры ."));
        Assert.That(tutorial.Objective, Is.EqualTo("Захватите 2 города:\nНеферис и Форт Дейв"));
    }

    [Test]
    public void LoadTextsRussianTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts("S095SC0000", Language.Russian);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Империя"));
        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0006")).Text, Is.EqualTo("Эльфы"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Крепость Хазан"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095LO0000:NAME_TXT:0")), Is.EqualTo("Дракон-страж"));
    }

    [Test]
    public void LoadTutorialTextsRussianTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts(GameLoader.TutorialScenarioId, Language.Russian);

        Assert.That(texts.GetScenarioText(new ScenarioTextId("S253FT0003:NAME_TXT:0")), Is.EqualTo("Неферис"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S253LO0000:NAME_TXT:0")), Is.EqualTo("Грабеж"));
    }

    [Test]
    public void LoadTextsCzechTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts("S095SC0000", Language.Czech);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Říše lidí"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Chazanův Týn"));
    }

    [Test]
    public void LoadTextsFrenchTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts("S095SC0000", Language.French);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0001")).Text, Is.EqualTo("Clans des Nains"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Castel Kazan"));
    }

    [Test]
    public void LoadTextsGermanTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts("S095SC0000", Language.German);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Imperium"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Khazan Feste"));
    }

    [Test]
    public void LoadTextsItalianTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts("S095SC0000", Language.Italian);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Umani"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Rocca di Khazan"));
    }

    [Test]
    public void LoadTextsPolishTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts("S095SC0000", Language.Polish);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0001")).Text, Is.EqualTo("Górskie Klany"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Warownia Khazan"));
    }

    [Test]
    public void LoadTextsSpanishTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts("S095SC0000", Language.Spanish);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Humanos"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Torreón de Khazan"));
    }

    [Test]
    public void LoadTextsEnglishTest()
    {
        IGameTextProvider texts = new GameLoader(new RandomProvider(0)).LoadTexts(GameLoader.TutorialScenarioId, Language.English);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Empire"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S253FT0003:NAME_TXT:0")), Is.EqualTo("Nepheris"));
    }
}
