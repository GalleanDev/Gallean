namespace Gallean.Engine;

public sealed record ScenarioMetadata(
    ScenarioId Id,
    ScenarioInfoTexts InfoTexts,
    int SuggestedLevel,
    int MapSize,
    int CityCount,
    WorldDifficulty Difficulty,
    IReadOnlyList<ScenarioRace> PlayableRaces);
