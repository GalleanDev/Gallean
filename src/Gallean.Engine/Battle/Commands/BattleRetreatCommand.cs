using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleRetreatCommand(BattleUnitId UnitId) : IBattleCommand<Empty>
{
    public BattleCommandResult Execute(BattleState state, IEngineContext context)
    {
        if (UnitId != state.CurrentUnitId)
        {
            return BattleCommandResult.Fail(BattleCommandError.NotUnitTurn);
        }

        BattleUnit unit = state.GetUnit(UnitId);
        BattleSideModifiers sideModifiers = state.Modifiers.GetModifiers(unit.Side);

        if (!sideModifiers.CanRetreat)
        {
            return BattleCommandResult.Fail(BattleCommandError.UnitRetreatNotAllowed);
        }

        if (unit.SummonerId is not null)
        {
            return BattleCommandResult.Fail(BattleCommandError.UnitRetreatNotAllowed);
        }

        if (!IsAvailableInTurn(state.CurrentUnitTurn.Type))
        {
            return BattleCommandResult.Fail(BattleCommandError.ActionNotAvailable);
        }

        if (unit.IsRetreating())
        {
            return BattleCommandResult.Fail(BattleCommandError.UnitAlreadyRetreating);
        }

        if (HasFastRetreat(unit, context))
        {
            return BattleCommandResult.Success(
                Empty.Value,
                [new BattleUnitRetreatedEvent(UnitId), new BattleUnitTurnEndingEvent(UnitId)]);
        }
        else
        {
            return BattleCommandResult.Success(
                Empty.Value,
                [new BattleUnitRetreatingEvent(UnitId), new BattleUnitTurnEndingEvent(UnitId)]);
        }
    }

    private static bool IsAvailableInTurn(BattleUnitTurnType type)
    {
        return type switch
        {
            BattleUnitTurnType.Normal => true,
            BattleUnitTurnType.SecondAttack => false,
            BattleUnitTurnType.GrantedAttack => true,
            BattleUnitTurnType.Resumed => true,
            BattleUnitTurnType.Doppelganger => false
        };
    }

    private static bool HasFastRetreat(BattleUnit unit, IEngineContext context)
    {
        foreach (UnitModifier modifier in unit.Modifiers)
        {
            if (context.Definitions.GetUnitModifier(modifier.DefinitionId).FastRetreat)
            {
                return true;
            }
        }

        return false;
    }
}
