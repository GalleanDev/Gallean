using Gallean.Common;

namespace Gallean.Engine;

public sealed record BattleDefendCommand(BattleUnitId UnitId) : IBattleCommand<Empty>
{
    public BattleCommandResult Execute(BattleState state, IEngineContext context)
    {
        if (UnitId != state.CurrentUnitId)
        {
            return BattleCommandResult.Fail(BattleCommandError.NotUnitTurn);
        }

        return BattleCommandResult.Success(
            Empty.Value,
            [new BattleUnitDefendedEvent(UnitId), new BattleUnitTurnEndingEvent(UnitId)]);
    }
}
