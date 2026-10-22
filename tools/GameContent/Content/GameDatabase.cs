using Gallean.Engine.Contracts;

namespace GameContent;

internal sealed class GameDatabase(GlobalDatabase globalDatabase, CampaignDatabase campaignDatabase)
{
    public IReadOnlyDictionary<UnitDefinitionId, UnitDefinitionId> BaseUnits { get; } = BuildBaseUnits(globalDatabase);
    public IReadOnlyDictionary<ScenarioId, string> VictoryMusic { get; } = BuildVictoryMusic(campaignDatabase);
    public IReadOnlyDictionary<ScenarioId, int> BriefingVoices { get; } = BuildBriefingVoices(campaignDatabase);
    public IReadOnlyDictionary<ScenarioId, string> BriefingVideos { get; } = BuildBriefingVideos(campaignDatabase);

    private static IReadOnlyDictionary<UnitDefinitionId, UnitDefinitionId> BuildBaseUnits(GlobalDatabase globalDatabase)
    {
        Dictionary<UnitDefinitionId, UnitDefinitionId> map = new();

        foreach (Db.Unit unit in globalDatabase.Units.Values)
        {
            if (unit.BaseUnit is { } baseUnit && !baseUnit.Equals(unit.UnitId))
            {
                map[new UnitDefinitionId(unit.UnitId.Value)] = new UnitDefinitionId(baseUnit.Value);
            }
        }

        return map;
    }

    private static IReadOnlyDictionary<ScenarioId, string> BuildVictoryMusic(CampaignDatabase campaignDatabase)
    {
        Dictionary<ScenarioId, string> map = new();

        foreach (Db.CampaignScenario scenario in campaignDatabase.CampaignScenarios)
        {
            map[new ScenarioId(scenario.ScenarioId.Value)] = scenario.WinAudio;
        }

        return map;
    }

    private static IReadOnlyDictionary<ScenarioId, string> BuildBriefingVideos(CampaignDatabase campaignDatabase)
    {
        Dictionary<ScenarioId, string> map = new();

        foreach (Db.CampaignScenario scenario in campaignDatabase.CampaignScenarios)
        {
            map[new ScenarioId(scenario.ScenarioId.Value)] = scenario.BriefingVideo;
        }

        return map;
    }

    private static IReadOnlyDictionary<ScenarioId, int> BuildBriefingVoices(CampaignDatabase campaignDatabase)
    {
        Dictionary<ScenarioId, int> map = new();

        foreach (Db.CampaignScenario scenario in campaignDatabase.CampaignScenarios)
        {
            Db.Campaign campaign = campaignDatabase.Campaigns[scenario.CampaignId];

            if (campaign.PlayRace == Db.RaceType.Elf && scenario.ScenarioNumber is { } number)
            {
                map[new ScenarioId(scenario.ScenarioId.Value)] = number;
            }
        }

        return map;
    }
}
