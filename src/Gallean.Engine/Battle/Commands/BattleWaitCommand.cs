using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleWaitCommand(BattleUnitId UnitId) : IBattleCommand<Empty>
{
    public BattleCommandResult Execute(BattleState state, IEngineContext context)
    {
        if (UnitId != state.CurrentUnitId)
        {
            return BattleCommandResult.Fail(BattleCommandError.NotUnitTurn);
        }

        if (!IsAvailableInTurn(state.CurrentUnitTurn.Type))
        {
            return BattleCommandResult.Fail(BattleCommandError.ActionNotAvailable);
        }

        if (state.WaitingQueue.Any(turn => turn.UnitId == UnitId))
        {
            return BattleCommandResult.Fail(BattleCommandError.UnitAlreadyWaited);
        }

        return BattleCommandResult.Success(
            Empty.Value,
            [new BattleUnitWaitedEvent(UnitId)]);
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
}
