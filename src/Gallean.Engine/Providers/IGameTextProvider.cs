using System.Collections.Immutable;

namespace Gallean.Engine;

public interface IGameTextProvider
{
    ImmutableArray<ScenarioName> Names { get; }

    GlobalText GetGlobalText(GlobalTextId id);
    string GetScenarioText(ScenarioTextId id);
    string GetLeaderName(LeaderNameId id);
    string GetUnitName(UnitName name);
}
