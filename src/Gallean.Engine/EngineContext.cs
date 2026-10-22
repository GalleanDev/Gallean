using Gallean.Common;

namespace Gallean.Engine;

public sealed record EngineContext(
    IRandomProvider Random,
    IGameDefinitionProvider Definitions,
    IUnitStatsProvider UnitStats,
    IGameScenarioProvider Scenario,
    GlobalVariables Globals) : IEngineContext;
