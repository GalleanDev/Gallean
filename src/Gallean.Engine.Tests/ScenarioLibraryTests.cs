using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Entities.GameData;

namespace Gallean.Engine.Tests;

public sealed class ScenarioLibraryTests
{
    private ScenarioLibrary library = null!;

    [SetUp]
    public void SetUp()
    {
        Dictionary<Language, ScenarioLibraryLanguageConfiguration> languages = [];

        foreach (Language language in Enum.GetValues<Language>())
        {
            languages.Add(language, new ScenarioLibraryLanguageConfiguration(
                RiseOfTheElves.GetLanguageGlobalDatabasePath(language.Code),
                RiseOfTheElves.GetLanguageScenarioDatabasePath(language.Code)));
        }

        library = new ScenarioLibrary(
            new RandomProvider(0),
            new ScenarioLibraryConfiguration(
                RiseOfTheElves.GlobalDatabasePath,
                RiseOfTheElves.ScenarioDatabasePath,
                RiseOfTheElves.CampaignDatabasePath,
                languages.ToImmutableDictionary()));
    }

    private static IReadOnlyList<string> FormatCampaign(ScenarioCampaign campaign, IGameTextProvider texts)
    {
        List<string> rows = [$"{campaign.Id} | {campaign.PlayRace.Race} | {campaign.IsSkirmish} | {campaign.CanImportLeader}"];

        foreach (ScenarioMetadata scenario in campaign.Scenarios)
        {
            string name = texts.GetScenarioText(scenario.InfoTexts.Name);

            rows.Add($"{scenario.Id} | {name} | {scenario.SuggestedLevel} | {scenario.MapSize} | {scenario.CityCount} | {scenario.Difficulty}");
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
    public void LoadTextsRussianTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S095SC0000"), file: null, Language.Russian);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Империя"));
        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0006")).Text, Is.EqualTo("Эльфы"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Крепость Хазан"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095LO0000:NAME_TXT:0")), Is.EqualTo("Дракон-страж"));
    }

    [Test]
    public void LoadTutorialTextsRussianTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S253SC0000"), file: null, Language.Russian);

        Assert.That(texts.GetScenarioText(new ScenarioTextId("S253FT0003:NAME_TXT:0")), Is.EqualTo("Неферис"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S253LO0000:NAME_TXT:0")), Is.EqualTo("Грабеж"));
    }

    [Test]
    public void LoadTextsCzechTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S095SC0000"), file: null, Language.Czech);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Říše lidí"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Chazanův Týn"));
    }

    [Test]
    public void LoadTextsFrenchTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S095SC0000"), file: null, Language.French);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0001")).Text, Is.EqualTo("Clans des Nains"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Castel Kazan"));
    }

    [Test]
    public void LoadTextsGermanTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S095SC0000"), file: null, Language.German);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Imperium"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Khazan Feste"));
    }

    [Test]
    public void LoadTextsItalianTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S095SC0000"), file: null, Language.Italian);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Umani"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Rocca di Khazan"));
    }

    [Test]
    public void LoadTextsPolishTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S095SC0000"), file: null, Language.Polish);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0001")).Text, Is.EqualTo("Górskie Klany"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Warownia Khazan"));
    }

    [Test]
    public void LoadTextsSpanishTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S095SC0000"), file: null, Language.Spanish);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Humanos"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S095FT0003:NAME_TXT:0")), Is.EqualTo("Torreón de Khazan"));
    }

    [Test]
    public void LoadTextsEnglishTest()
    {
        IGameTextProvider texts = library.LoadTexts(new ScenarioId("S253SC0000"), file: null, Language.English);

        Assert.That(texts.GetGlobalText(new GlobalTextId("x000tg0000")).Text, Is.EqualTo("Empire"));
        Assert.That(texts.GetScenarioText(new ScenarioTextId("S253FT0003:NAME_TXT:0")), Is.EqualTo("Nepheris"));
    }

    [Test]
    public void OpenScenarioFileTest()
    {
        using FileStream stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterCrossing.sg"));

        ScenarioLibraryFile file = library.OpenScenarioFile(stream);
        (ScenarioCampaign campaign, IGameTextProvider texts) = library.LoadCampaign(file, Language.English);
        (GameSession session, _) = library.LoadScenario(new ScenarioId("S900SC0000"), file, worldState: null, Language.English);

        Assert.That(file.Name, Is.EqualTo("Greywater Crossing"));
        Assert.That(FormatCampaign(campaign, texts), Is.EqualTo([
            "C000CC0001 | Neutral | True | True",
            "S900SC0000 | Greywater Crossing | 2 | 48 | 3 | Average"
        ]));
        Assert.That(session.EngineContext.Scenario.Definition.NextScenarioId, Is.Null);
        Assert.That(FormatPlayers(session.World.State), Is.EqualTo([
            "1 Neutral ai",
            "2 Dwarf human",
            "3 Undead ai"
        ]));
    }

    [Test]
    public void OpenCampaignFileTest()
    {
        using FileStream stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterChronicle.csg"));

        ScenarioLibraryFile file = library.OpenCampaignFile(stream);
        (ScenarioCampaign campaign, IGameTextProvider texts) = library.LoadCampaign(file, Language.English);
        (GameSession session, _) = library.LoadScenario(new ScenarioId("S900SC0000"), file, worldState: null, Language.English);

        Assert.That(file.Name, Is.EqualTo("The Greywater Chronicle"));
        Assert.That(FormatCampaign(campaign, texts), Is.EqualTo([
            "C000CC0900 | Undead | False | True",
            "S900SC0000 | Greywater Crossing | 2 | 48 | 3 | Average",
            "S901SC0000 | Greywater Siege | 2 | 48 | 3 | Average"
        ]));
        Assert.That(session.EngineContext.Scenario.Definition.NextScenarioId, Is.EqualTo(new ScenarioId("S901SC0000")));
        Assert.That(FormatPlayers(session.World.State), Is.EqualTo([
            "1 Neutral ai",
            "2 Dwarf ai",
            "3 Undead human"
        ]));
    }

    [Test]
    public void OpenInvalidCampaignFileTest()
    {
        using FileStream stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestScenarios", "GreywaterCrossing.sg"));

        Assert.That(
            () => library.OpenCampaignFile(stream),
            Throws.TypeOf<InvalidDataException>().With.Message.EqualTo("Archive does not start with 'MQDB'."));
    }
}
