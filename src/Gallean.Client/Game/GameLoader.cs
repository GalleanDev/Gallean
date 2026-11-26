using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;
using Gallean.Entities.GameData;

namespace Gallean.Client;

internal sealed class GameLoader(IRandomProvider random)
{
    public const string TutorialScenarioId = "S253SC0000";

    private readonly ScenarioLibrary library = new(random, CreateLibraryConfiguration());

    public (IReadOnlyList<GameCampaign> Campaigns, IGameTextProvider Texts, IGameDefinitionProvider Definitions) LoadCampaigns(Language language)
    {
        (IReadOnlyList<ScenarioCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = library.LoadCampaigns(language);

        List<GameCampaign> result = [];

        foreach (ScenarioCampaign campaign in campaigns)
        {
            List<GameScenario> scenarios = [];

            foreach (ScenarioMetadata scenario in campaign.Scenarios)
            {
                scenarios.Add(CreateScenario(customScenarioId: null, campaign, scenario, texts));
            }

            result.Add(new GameCampaign(
                CustomScenarioId: null,
                campaign.Id.Value,
                texts.GetGlobalText(campaign.NameTextId).Text,
                campaign.PlayRace,
                campaign.IsSkirmish,
                scenarios));
        }

        return (result, texts, definitions);
    }

    public GameScenario LoadCustomScenario(CustomScenarioFile customScenario, Language language)
    {
        using MemoryStream stream = new(customScenario.Data, writable: false);

        ScenarioLibraryFile file = library.OpenScenarioFile(stream);
        (ScenarioCampaign campaign, IGameTextProvider texts) = library.LoadCampaign(file, language);
        ScenarioMetadata scenario = campaign.Scenarios.Single();

        return CreateScenario(customScenario.Id, campaign, scenario, texts);
    }

    public GameCampaign LoadCustomCampaign(CustomScenarioFile customScenario, Language language)
    {
        using MemoryStream stream = new(customScenario.Data, writable: false);

        ScenarioLibraryFile file = library.OpenCampaignFile(stream);
        (ScenarioCampaign campaign, IGameTextProvider texts) = library.LoadCampaign(file, language);

        List<GameScenario> scenarios = [];

        foreach (ScenarioMetadata scenario in campaign.Scenarios)
        {
            scenarios.Add(CreateScenario(customScenario.Id, campaign, scenario, texts));
        }

        return new GameCampaign(customScenario.Id, campaign.Id.Value, file.Name, campaign.PlayRace, campaign.IsSkirmish, scenarios);
    }

    private static ScenarioLibraryConfiguration CreateLibraryConfiguration()
    {
        Dictionary<Language, ScenarioLibraryLanguageConfiguration> languages = [];

        foreach (Language language in Enum.GetValues<Language>())
        {
            languages.Add(language, new ScenarioLibraryLanguageConfiguration(
                RiseOfTheElves.GetLanguageGlobalDatabasePath(language.Code),
                RiseOfTheElves.GetLanguageScenarioDatabasePath(language.Code)));
        }

        return new ScenarioLibraryConfiguration(
            RiseOfTheElves.GlobalDatabasePath,
            RiseOfTheElves.ScenarioDatabasePath,
            RiseOfTheElves.CampaignDatabasePath,
            languages.ToImmutableDictionary());
    }

    private static GameScenario CreateScenario(
        CustomScenarioId? customScenarioId,
        ScenarioCampaign campaign,
        ScenarioMetadata scenario,
        IGameTextProvider texts)
    {
        return new GameScenario(
            customScenarioId,
            scenario.Id.Value,
            texts.GetScenarioText(scenario.InfoTexts.Name),
            GetOptionalText(texts, scenario.InfoTexts.Description),
            texts.GetScenarioText(scenario.InfoTexts.Objective),
            scenario.SuggestedLevel,
            scenario.MapSize,
            scenario.CityCount,
            GetOptionalText(texts, scenario.InfoTexts.Creator),
            campaign.IsSkirmish,
            campaign.CanImportLeader,
            scenario.PlayableRaces);
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

    public (GameSession Session, IGameTextProvider Texts) LoadScenario(
        string scenarioId,
        CustomScenarioFile? customScenario,
        WorldState? worldState,
        Language language)
    {
        ScenarioLibraryFile? file = OpenFile(customScenario);

        return library.LoadScenario(new ScenarioId(scenarioId), file, worldState, language);
    }

    public (GameSession Session, IGameTextProvider Texts) CreateScenario(
        string scenarioId,
        CustomScenarioFile? customScenario,
        ScenarioConfiguration configuration,
        Language language)
    {
        ScenarioLibraryFile? file = OpenFile(customScenario);

        return library.CreateScenario(new ScenarioId(scenarioId), file, configuration, language);
    }

    public IGameTextProvider LoadTexts(string scenarioId, CustomScenarioFile? customScenario, Language language)
    {
        ScenarioLibraryFile? file = OpenFile(customScenario);

        return library.LoadTexts(new ScenarioId(scenarioId), file, language);
    }

    private ScenarioLibraryFile? OpenFile(CustomScenarioFile? customScenario)
    {
        if (customScenario is not null)
        {
            using MemoryStream stream = new(customScenario.Data, writable: false);

            return customScenario.Type switch
            {
                CustomScenarioType.Scenario => library.OpenScenarioFile(stream),
                CustomScenarioType.Campaign => library.OpenCampaignFile(stream)
            };
        }
        else
        {
            return null;
        }
    }
}
