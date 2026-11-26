namespace Gallean.Engine.Scenarios;

public static class CustomScenarioDatabaseLoader
{
    private const string DefaultBriefingVideo = "DEFAULT";
    private const string DefaultWinVideo = "DEFWIN";
    private const string DefaultLoseVideo = "DEFLOSE";
    private const string DefaultWinAudio = "GENBRIEF";

    private static readonly Db.Id SkirmishCampaignId = new("C000CC0001");

    public static (ScenarioDatabase ScenarioDatabase, CampaignDatabase CampaignDatabase) LoadScenario(
        byte[] data,
        ScenarioTextEncoding encoding,
        CampaignDatabase campaignDatabase)
    {
        using MemoryStream stream = new(data, writable: false);

        ScenarioFile file = ScenarioFileLoader.Load(stream, encoding, ScenarioSlots.GetScenarioId(ScenarioSlots.Custom.Start.Value));
        Db.ScenarioInfo scenarioInfo = file.Tables.ScenarioInfos.Single();
        Db.ScenarioInfo customScenarioInfo = scenarioInfo with { CampaignId = SkirmishCampaignId };
        Db.CampaignScenario campaignScenario = new(
            SkirmishCampaignId,
            GetNextScenarioNumber(campaignDatabase, SkirmishCampaignId),
            file.ScenarioId,
            DefaultBriefingVideo,
            DefaultWinVideo,
            DefaultLoseVideo,
            DefaultWinAudio);

        ScenarioDatabase scenarioDatabase = ScenarioFileDatabaseLoader.Load([file.Tables with { ScenarioInfos = [customScenarioInfo] }]);
        CampaignDatabase customCampaignDatabase = CampaignDatabaseLoader.Create(
            [.. campaignDatabase.Campaigns.Values],
            [.. campaignDatabase.CampaignScenarios, campaignScenario]);

        return (scenarioDatabase, customCampaignDatabase);
    }

    public static (ScenarioDatabase ScenarioDatabase, CampaignDatabase CampaignDatabase) LoadCampaign(
        CampaignFileContent campaign,
        ScenarioTextEncoding encoding,
        IGlobalDatabaseIndex globalDatabase,
        CampaignDatabase campaignDatabase)
    {
        int scenarioLimit = ScenarioSlots.Custom.End.Value - ScenarioSlots.Custom.Start.Value;

        if (campaign.Scenarios.Count > scenarioLimit)
        {
            throw new InvalidDataException("The campaign has too many scenarios.");
        }

        Db.Id campaignId = ScenarioSlots.GetCampaignId(ScenarioSlots.CampaignCustom);
        Db.RaceType playRace = GetRaceType(globalDatabase, campaign.Header.PlayRaceId);
        Db.Campaign customCampaign = new(campaignId, playRace, NameId: string.Empty, Skirmish: false, CanImport: true);

        List<ScenarioFileTables> files = [];
        List<Db.CampaignScenario> campaignScenarios = [];

        for (int i = 0; i < campaign.Scenarios.Count; i++)
        {
            CampaignFileEntry entry = campaign.Scenarios[i];

            using MemoryStream stream = new(entry.Data, writable: false);

            ScenarioFile file = ScenarioFileLoader.Load(stream, encoding, ScenarioSlots.GetScenarioId(ScenarioSlots.Custom.Start.Value + i));
            Db.ScenarioInfo scenarioInfo = file.Tables.ScenarioInfos.Single();
            Db.ScenarioInfo customScenarioInfo = scenarioInfo with { CampaignId = campaignId };
            List<Db.ScenarioPlayer> customScenarioPlayers = [];

            foreach (Db.ScenarioPlayer player in file.Tables.ScenarioPlayers)
            {
                bool isHuman = player.RaceId == campaign.Header.PlayRaceId;

                customScenarioPlayers.Add(player with { IsHuman = isHuman });
            }

            files.Add(file.Tables with
            {
                ScenarioInfos = [customScenarioInfo],
                ScenarioPlayers = customScenarioPlayers
            });
            
            campaignScenarios.Add(new Db.CampaignScenario(
                campaignId,
                i + 1,
                file.ScenarioId,
                DefaultBriefingVideo,
                DefaultWinVideo,
                DefaultLoseVideo,
                DefaultWinAudio));
        }

        ScenarioDatabase scenarioDatabase = ScenarioFileDatabaseLoader.Load(files);
        CampaignDatabase customCampaignDatabase = CampaignDatabaseLoader.Create(
            [.. campaignDatabase.Campaigns.Values, customCampaign],
            [.. campaignDatabase.CampaignScenarios, .. campaignScenarios]);

        return (scenarioDatabase, customCampaignDatabase);
    }

    private static Db.RaceType GetRaceType(IGlobalDatabaseIndex globalDatabase, Db.Id raceId)
    {
        if (globalDatabase.Races.TryGetValue(raceId, out Db.Race? race))
        {
            return race.RaceType;
        }

        throw new InvalidDataException($"Unknown campaign race '{raceId}'.");
    }

    private static int GetNextScenarioNumber(CampaignDatabase campaignDatabase, Db.Id campaignId)
    {
        int result = 1;

        foreach (Db.CampaignScenario campaignScenario in campaignDatabase.CampaignScenariosByCampaignId[campaignId])
        {
            if (campaignScenario.ScenarioNumber is not null)
            {
                result = Math.Max(result, campaignScenario.ScenarioNumber.Value + 1);
            }
        }

        return result;
    }
}
