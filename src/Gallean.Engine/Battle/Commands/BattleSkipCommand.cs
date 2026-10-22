using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleSkipCommand(BattleUnitId UnitId) : IBattleCommand<Empty>
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

        return BattleCommandResult.Success(
            Empty.Value,
            [new BattleUnitTurnEndingEvent(UnitId)]);
    }

    private static bool IsAvailableInTurn(BattleUnitTurnType type)
    {
        return type switch
        {
            BattleUnitTurnType.Normal => true,
            BattleUnitTurnType.SecondAttack => true,
            BattleUnitTurnType.GrantedAttack => true,
            BattleUnitTurnType.Resumed => true,
            BattleUnitTurnType.Doppelganger => true
        };
    }
}
