using Gallean.Common;
using Gallean.Configuration;
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

    private static IReadOnlyList<string> FormatScenarios(IReadOnlyList<GameScenario> scenarios)
    {
        List<string> rows = [];

        foreach (GameScenario scenario in scenarios)
        {
            rows.Add($"{scenario.CustomScenarioId} | {scenario.Id} | {scenario.Name} | {scenario.IsSkirmish} | {scenario.CanImportLeader}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatPlayers(WorldState state)
    {
        List<string> rows = [];

        foreach (WorldPlayer player in state.Players.Values)
        {
            string control = "ai";

            if (!player.IsAi)
            {
                control = "human";
            }

            rows.Add($"{player.Id} {player.Race} {control}");
        }

        return rows;
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
    public void LoadCustomScenarioTest()
    {
        CustomScenarioFile customScenario = CustomScenarioFile.Create(
            CustomScenarioType.Scenario,
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterCrossing.sg")));

        (GameSession session, IGameTextProvider texts) = new GameLoader(new RandomProvider(0)).LoadScenario("S900SC0000", customScenario, worldState: null, Language.English);
        ScenarioDefinition definition = session.EngineContext.Scenario.Definition;

        Assert.That(definition.Id, Is.EqualTo(new ScenarioId("S900SC0000")));
        Assert.That(texts.GetScenarioText(definition.InfoTexts.Name), Is.EqualTo("Greywater Crossing"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S900FT0002:NAME_TXT:0")), Is.EqualTo("Brinewatch"));
        Assert.That(session.World.State.Fortifications.Keys, Is.EqualTo([
            new WorldFortificationId("S900FT0000"),
            new WorldFortificationId("S900FT0001"),
            new WorldFortificationId("S900FT0002"),
            new WorldFortificationId("S900FT0003"),
            new WorldFortificationId("S900FT0004"),
            new WorldFortificationId("S900RU0000"),
            new WorldFortificationId("S900RU0001")
        ]));
    }

    [Test]
    public void LoadCustomCampaignTest()
    {
        CustomScenarioFile customScenario = CustomScenarioFile.Create(
            CustomScenarioType.Campaign,
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterChronicle.csg")));
        GameLoader gameLoader = new(new RandomProvider(0));

        GameCampaign campaign = gameLoader.LoadCustomCampaign(customScenario, Language.English);
        (GameSession session, _) = gameLoader.LoadScenario("S900SC0000", customScenario, worldState: null, Language.English);

        Assert.That(
            $"{campaign.CustomScenarioId} | {campaign.Id} | {campaign.Name} | {campaign.PlayRace.Race} | {campaign.IsSkirmish}",
            Is.EqualTo("c0d20b970c9606c338078e40bc9403902b08394516ce4f4c76284f93c9df3ad2 | C000CC0900 | The Greywater Chronicle | Undead | False"));
        Assert.That(FormatScenarios(campaign.Scenarios), Is.EqualTo([
            "c0d20b970c9606c338078e40bc9403902b08394516ce4f4c76284f93c9df3ad2 | S900SC0000 | Greywater Crossing | False | True",
            "c0d20b970c9606c338078e40bc9403902b08394516ce4f4c76284f93c9df3ad2 | S901SC0000 | Greywater Siege | False | True"
        ]));
        Assert.That(session.EngineContext.Scenario.Definition.NextScenarioId, Is.EqualTo(new ScenarioId("S901SC0000")));
        Assert.That(FormatPlayers(session.World.State), Is.EqualTo([
            "1 Neutral ai",
            "2 Dwarf ai",
            "3 Undead human"
        ]));
    }
}
