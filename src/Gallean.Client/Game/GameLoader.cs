using Gallean.Common;
using Gallean.Engine;
using Gallean.Entities;
using Gallean.Entities.GameData;

namespace Gallean.Client;

internal sealed class GameLoader(IRandomProvider random)
{
    private delegate Scenario ScenarioLoadFunc(
        ScenarioDatabase scenarioDatabase,
        IGameDefinitionProvider definitions,
        GlobalVariables globals);

    public const string TutorialScenarioId = "S253SC0000";

    private readonly Lazy<CampaignDatabase> campaignDatabase = new(() => CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath));
    private readonly Lazy<GlobalDatabase> globalDatabase = new(() => GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath));
    private readonly Lazy<GlobalDatabaseIndex> globalDatabaseIndex = new(() => GlobalDatabaseLoader.LoadIndex(RiseOfTheElves.GlobalDatabasePath));
    private readonly Lazy<ScenarioDatabaseIndex> scenarioDatabaseIndex = new(() => ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath));

    public (IReadOnlyList<GameCampaign> Campaigns, IGameTextProvider Texts, IGameDefinitionProvider Definitions) LoadCampaigns()
    {
        IReadOnlyList<ScenarioCampaign> campaigns = ScenarioLoader.EnumerateCampaigns(
            globalDatabaseIndex.Value,
            scenarioDatabaseIndex.Value,
            campaignDatabase.Value);
        IGameTextProvider texts = new GameTextProvider(globalDatabaseIndex.Value, scenarioDatabaseIndex.Value, []);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDatabase.Value);

        List<GameCampaign> result = [];

        foreach (ScenarioCampaign campaign in campaigns)
        {
            List<GameScenario> scenarios = [];

            foreach (ScenarioMetadata scenario in campaign.Scenarios)
            {
                scenarios.Add(new GameScenario(
                    scenario.Id.Value,
                    texts.GetScenarioText(scenario.InfoTexts.Name),
                    GetOptionalText(texts, scenario.InfoTexts.Description),
                    texts.GetScenarioText(scenario.InfoTexts.Objective),
                    scenario.SuggestedLevel,
                    scenario.MapSize,
                    scenario.CityCount,
                    GetOptionalText(texts, scenario.InfoTexts.Creator),
                    scenario.PlayableRaces));
            }

            result.Add(new GameCampaign(
                campaign.Id.Value,
                texts.GetGlobalText(campaign.NameTextId).Text,
                campaign.PlayRace,
                campaign.IsSkirmish,
                scenarios));
        }

        return (result, texts, definitions);
    }

    private static string? GetOptionalText(IGameTextProvider texts, ScenarioTextId? id)
    {
        if (id is not null)
        {
            return texts.GetScenarioText(id.Value);
        }
        else
        {
            return null;
        }
    }

    public (GameSession Session, IGameTextProvider Texts) LoadScenario(string scenarioId, WorldState? worldState)
    {
        return LoadInternal(
            scenarioId,
            (scenarioDatabase, definitions, globals) => ScenarioLoader.Load(
                globalDatabase.Value,
                scenarioDatabase,
                campaignDatabase.Value,
                new ScenarioId(scenarioId),
                definitions,
                globals),
            worldState);
    }

    public (GameSession Session, IGameTextProvider Texts) CreateScenario(string scenarioId, ScenarioConfiguration configuration)
    {
        return LoadInternal(
            scenarioId,
            (scenarioDatabase, definitions, globals) => ScenarioLoader.Create(
                globalDatabase.Value,
                scenarioDatabase,
                campaignDatabase.Value,
                new ScenarioId(scenarioId),
                definitions,
                globals,
                configuration,
                random),
            worldState: null);
    }

    private (GameSession Session, IGameTextProvider Texts) LoadInternal(
        string scenarioId,
        ScenarioLoadFunc loadFunc,
        WorldState? worldState)
    {
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDatabase.Value);
        ScenarioDatabase scenarioDatabase = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath, scenarioId);
        IGameTextProvider texts = new GameTextProvider(globalDatabase.Value, scenarioDatabase, scenarioDatabase.TextScenarios);
        GlobalVariables globals = new();
        Scenario scenario = loadFunc(scenarioDatabase, definitions, globals);

        UnitStatsProvider unitStats = new(definitions, globals, scenario.Provider.Definition);
        EngineContext engineContext = new(random, definitions, unitStats, scenario.Provider, globals);
        GameSession session = new(engineContext, worldState ?? scenario.State);

        return (session, texts);
    }
}
