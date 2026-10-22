using System.Collections.Immutable;

namespace Gallean.Engine;

internal readonly record struct ScenarioEventConditionContext(
    WorldState State,
    IGameScenarioProvider Scenario,
    GlobalVariables Globals,
    WorldPlayerId TriggererId,
    WorldStack? TriggererStack,
    ImmutableArray<WorldRace> AffectedRaces,
    ScenarioEventTrigger Trigger);
