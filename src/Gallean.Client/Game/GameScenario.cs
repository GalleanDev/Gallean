using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record GameScenario(
    CustomScenarioId? CustomScenarioId,
    string Id,
    string Name,
    string? Description,
    string? Objective,
    int SuggestedLevel,
    int MapSize,
    int CityCount,
    string? Creator,
    bool IsSkirmish,
    bool CanImportLeader,
    IReadOnlyList<ScenarioRace> PlayableRaces);
