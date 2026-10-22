namespace Gallean.Engine;

public sealed record ScenarioDefinition(
    ScenarioId Id,
    ScenarioInfoTexts InfoTexts,
    int SuggestedLevel,
    int MaxLeaderLevel,
    int MaxCityLevel,
    ScenarioId? NextScenarioId);
