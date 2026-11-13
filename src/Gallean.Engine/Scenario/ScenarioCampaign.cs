namespace Gallean.Engine;

public sealed record ScenarioCampaign(
    ScenarioCampaignId Id,
    GlobalTextId NameTextId,
    ScenarioRace PlayRace,
    bool IsSkirmish,
    bool CanImportLeader,
    IReadOnlyList<ScenarioMetadata> Scenarios);
