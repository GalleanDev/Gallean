using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal sealed record BattleStartCommand : IBattleCommand<Empty>
{
    public BattleCommandResult Execute(BattleState state, IEngineContext context)
    {
        ImmutableArray<BattleUnitTurn> openingTurnOrder = BattleTurnOrderCalculator.GetOpeningRoundOrder(state, context);

        if (!openingTurnOrder.IsEmpty)
        {
            return BattleCommandResult.Success(
                Empty.Value,
                [
                    new BattleRoundStartedEvent(BattleRound.Opening(state.RoundNumber), openingTurnOrder),
                    new BattleUnitTurnStartedEvent(openingTurnOrder[0])
                ]);
        }

        ImmutableArray<BattleUnitTurn> normalTurnOrder = BattleTurnOrderCalculator.GetNormalRoundOrder(state, context);

        if (normalTurnOrder.IsEmpty)
        {
            return BattleCommandResult.Success(Empty.Value, []);
        }

        return BattleCommandResult.Success(
            Empty.Value,
            [
                new BattleRoundStartedEvent(BattleRound.Normal(state.RoundNumber), normalTurnOrder),
                new BattleUnitTurnStartedEvent(normalTurnOrder[0])
            ]);
    }
}
