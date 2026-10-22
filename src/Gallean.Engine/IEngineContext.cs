using Gallean.Common;

namespace Gallean.Engine;

public interface IEngineContext
{
    IRandomProvider Random { get; }
    IGameDefinitionProvider Definitions { get; }
    IUnitStatsProvider UnitStats { get; }
    IGameScenarioProvider Scenario { get; }
    GlobalVariables Globals { get; }
}
