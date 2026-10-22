using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldHealUnitCommand(
    WorldUnitGroup Group,
    WorldUnitId UnitId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldHealCalculator.HealInfo heal = WorldHealCalculator.GetHealInfo(state, Group, UnitId, context);

        if (heal.Error is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldUnitHealedEvent(Group, UnitId, heal.Hitpoints, heal.Cost)]);
    }
}
