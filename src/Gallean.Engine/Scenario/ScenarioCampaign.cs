namespace Gallean.Engine;

public sealed record ScenarioCampaign(
    ScenarioCampaignId Id,
    GlobalTextId NameTextId,
    ScenarioRace PlayRace,
    bool IsSkirmish,
    IReadOnlyList<ScenarioMetadata> Scenarios);
