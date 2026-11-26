using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record GameCampaign(
    CustomScenarioId? CustomScenarioId,
    string Id,
    string Name,
    ScenarioRace PlayRace,
    bool IsSkirmish,
    IReadOnlyList<GameScenario> Scenarios);
