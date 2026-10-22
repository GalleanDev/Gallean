using Gallean.Engine;

namespace Gallean.Client;

internal sealed record GameCampaign(
    string Id,
    string Name,
    ScenarioRace PlayRace,
    bool IsSkirmish,
    IReadOnlyList<GameScenario> Scenarios);
