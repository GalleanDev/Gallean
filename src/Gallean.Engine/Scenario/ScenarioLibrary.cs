using Gallean.Common;
using Gallean.Engine.Scenarios;

namespace Gallean.Engine;

public sealed class ScenarioLibrary(IRandomProvider random, ScenarioLibraryConfiguration configuration)
{
    private readonly Lazy<CampaignDatabase> campaignDatabase = new(() => CampaignDatabaseLoader.Load(configuration.CampaignDatabasePath));
    private readonly Lazy<GlobalDatabase> globalDatabase = new(() => GlobalDatabaseLoader.Load(configuration.GlobalDatabasePath));
    private readonly Lazy<GlobalDatabaseIndex> globalDatabaseIndex = new(() => GlobalDatabaseLoader.LoadIndex(configuration.GlobalDatabasePath));
    private readonly Lazy<ScenarioDatabaseIndex> scenarioDatabaseIndex = new(() => ScenarioDatabaseLoader.LoadIndex(configuration.ScenarioDatabasePath));

    public (IReadOnlyList<ScenarioCampaign> Campaigns, IGameTextProvider Texts, IGameDefinitionProvider Definitions) LoadCampaigns(Language language)
    {
        IReadOnlyList<ScenarioCampaign> campaigns = ScenarioLoader.EnumerateCampaigns(
            globalDatabaseIndex.Value,
            scenarioDatabaseIndex.Value,
            campaignDatabase.Value);
        IGameTextProvider texts = CreateCatalogTextProvider(scenarioDatabaseIndex.Value, language);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDatabase.Value);

        return (campaigns, texts, definitions);
    }

    public (ScenarioCampaign Campaign, IGameTextProvider Texts) LoadCampaign(ScenarioLibraryFile file, Language language)
    {
        IReadOnlyList<ScenarioCampaign> campaigns = ScenarioLoader.EnumerateCampaigns(
            globalDatabaseIndex.Value,
            file.ScenarioDatabase,
            file.CampaignDatabase);
        IGameTextProvider texts = CreateCatalogTextProvider(file.ScenarioDatabase, language);
        ScenarioCampaign campaign = campaigns.Single();

        return (campaign, texts);
    }

    public ScenarioLibraryFile OpenScenarioFile(byte[] data, Language language)
    {
        ScenarioTextEncoding encoding = ScenarioTextEncoding.FromCodePage(GetFileCodePage(language));
        (ScenarioDatabase scenarioDatabase, CampaignDatabase fileCampaignDatabase) = CustomScenarioDatabaseLoader.LoadScenario(
            data,
            encoding,
            campaignDatabase.Value);
        Db.ScenarioInfo info = scenarioDatabase.ScenarioInfos.Single();

        return new ScenarioLibraryFile(info.Name, scenarioDatabase, fileCampaignDatabase);
    }

    public ScenarioLibraryFile OpenCampaignFile(byte[] data, Language language)
    {
        ScenarioTextEncoding encoding = ScenarioTextEncoding.FromCodePage(GetFileCodePage(language));
        CampaignFileContent content = CampaignFileLoader.Read(data, encoding);
        (ScenarioDatabase scenarioDatabase, CampaignDatabase fileCampaignDatabase) = CustomScenarioDatabaseLoader.LoadCampaign(
            content,
            encoding,
            globalDatabaseIndex.Value,
            campaignDatabase.Value);
        CampaignFileHeader header = content.Header;

        return new ScenarioLibraryFile(header.Name, scenarioDatabase, fileCampaignDatabase);
    }

    public (GameSession Session, IGameTextProvider Texts) LoadScenario(
        ScenarioId scenarioId,
        ScenarioLibraryFile? file,
        WorldState? worldState,
        Language language)
    {
        (ScenarioDatabase scenarioDatabase, CampaignDatabase scenarioCampaignDatabase) = LoadDatabases(scenarioId, file);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDatabase.Value);
        GlobalVariables globals = new();
        Scenario scenario = ScenarioLoader.Load(
            globalDatabase.Value,
            scenarioDatabase,
            scenarioCampaignDatabase,
            scenarioId,
            definitions,
            globals);
        GameSession session = CreateSession(scenario, worldState ?? scenario.State, definitions, globals);
        IGameTextProvider texts = CreateTextProvider(scenarioDatabase, scenarioId, language);

        return (session, texts);
    }

    public (GameSession Session, IGameTextProvider Texts) CreateScenario(
        ScenarioId scenarioId,
        ScenarioLibraryFile? file,
        ScenarioConfiguration scenarioConfiguration,
        Language language)
    {
        (ScenarioDatabase scenarioDatabase, CampaignDatabase scenarioCampaignDatabase) = LoadDatabases(scenarioId, file);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDatabase.Value);
        GlobalVariables globals = new();
        Scenario scenario = ScenarioLoader.Create(
            globalDatabase.Value,
            scenarioDatabase,
            scenarioCampaignDatabase,
            scenarioId,
            definitions,
            globals,
            scenarioConfiguration);
        GameSession session = CreateSession(scenario, scenario.State, definitions, globals);
        IGameTextProvider texts = CreateTextProvider(scenarioDatabase, scenarioId, language);

        return (session, texts);
    }

    public IGameTextProvider LoadTexts(ScenarioId scenarioId, ScenarioLibraryFile? file, Language language)
    {
        (ScenarioDatabase scenarioDatabase, _) = LoadDatabases(scenarioId, file);

        return CreateTextProvider(scenarioDatabase, scenarioId, language);
    }

    private static int GetFileCodePage(Language language)
    {
        return language switch
        {
            Language.English => 1252,
            Language.Czech => 1250,
            Language.French => 1252,
            Language.German => 1252,
            Language.Italian => 1252,
            Language.Polish => 1250,
            Language.Russian => 1251,
            Language.Spanish => 1252
        };
    }

    private GameSession CreateSession(
        Scenario scenario,
        WorldState state,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        UnitStatsProvider unitStats = new(definitions, globals, scenario.Provider.Definition);
        EngineContext engineContext = new(random, definitions, unitStats, scenario.Provider, globals);

        return new GameSession(engineContext, state);
    }

    private (ScenarioDatabase ScenarioDatabase, CampaignDatabase CampaignDatabase) LoadDatabases(
        ScenarioId scenarioId,
        ScenarioLibraryFile? file)
    {
        if (file is not null)
        {
            return (file.ScenarioDatabase, file.CampaignDatabase);
        }
        else
        {
            ScenarioDatabase scenarioDatabase = ScenarioDatabaseLoader.Load(configuration.ScenarioDatabasePath, scenarioId.Value);

            return (scenarioDatabase, campaignDatabase.Value);
        }
    }

    private IGameTextProvider CreateCatalogTextProvider(IScenarioDatabaseIndex scenarioDatabase, Language language)
    {
        ScenarioLibraryLanguageConfiguration languageConfiguration = configuration.Languages[language];
        LanguageDatabase languageDatabase = LanguageDatabaseLoader.LoadIndex(
            languageConfiguration.GlobalDatabasePath,
            languageConfiguration.ScenarioDatabasePath);

        return new GameTextProvider(globalDatabaseIndex.Value, scenarioDatabase, [], languageDatabase);
    }

    private IGameTextProvider CreateTextProvider(ScenarioDatabase scenarioDatabase, ScenarioId scenarioId, Language language)
    {
        ScenarioLibraryLanguageConfiguration languageConfiguration = configuration.Languages[language];
        LanguageDatabase languageDatabase = LanguageDatabaseLoader.Load(
            languageConfiguration.GlobalDatabasePath,
            languageConfiguration.ScenarioDatabasePath,
            scenarioId.Value);

        return new GameTextProvider(globalDatabase.Value, scenarioDatabase, scenarioDatabase.TextScenarios, languageDatabase);
    }
}
