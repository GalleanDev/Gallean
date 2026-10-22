using Gallean.Engine;

namespace Gallean.Client;

internal sealed record GameScenario(
    string Id,
    string Name,
    string? Description,
    string? Objective,
    int SuggestedLevel,
    int MapSize,
    int CityCount,
    string? Creator,
    IReadOnlyList<ScenarioRace> PlayableRaces);
