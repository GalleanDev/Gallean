using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldResurrectUnitCommand(
    WorldUnitGroup Group,
    WorldUnitId UnitId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldHealCalculator.ResurrectInfo resurrect = WorldHealCalculator.GetResurrectInfo(state, Group, UnitId, context);

        if (resurrect.Error is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldUnitResurrectedEvent(Group, UnitId, resurrect.Hitpoints, resurrect.Cost)]);
    }
}
