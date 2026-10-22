using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleUseItemCommand(BattleUnitId UnitId, LeaderEquipmentSlot Slot, BattleAttackTarget Target) : IBattleCommand<Empty>
{
    public BattleCommandResult Execute(BattleState state, IEngineContext context)
    {
        if (UnitId != state.CurrentUnitId)
        {
            return BattleCommandResult.Fail(BattleCommandError.NotUnitTurn);
        }

        BattleItemCalculator.ItemInfo info = BattleItemCalculator.GetItemInfo(state, context, UnitId, Slot, Target);

        if (info.Failed)
        {
            return BattleCommandResult.Fail(info.Error.Value);
        }

        return BattleCommandResult.Success(Empty.Value, [.. info.Events, new BattleUnitTurnEndingEvent(UnitId)]);
    }
}
