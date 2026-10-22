using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class AdvanceTurnRule(IEngineContext context) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleUnitDiedEvent ev, BattleState state)
        {
            if (ev.UnitId == state.CurrentUnitId)
            {
                return EventRuleResult<BattleEvent>.Append(new BattleUnitTurnEndingEvent(ev.UnitId));
            }
            else
            {
                return EventRuleResult<BattleEvent>.Empty;
            }
        }

        public override EventRuleResult<BattleEvent> Visit(BattleUnitTurnEndedEvent ev, BattleState state)
        {
            if (state.Outcome is not null)
            {
                return EventRuleResult<BattleEvent>.Consume();
            }

            BattleUnitTurn? nextUnit = BattleTurnOrderCalculator.FindNextTurn(state);

            if (nextUnit is not null)
            {
                return EventRuleResult<BattleEvent>.Append(new BattleUnitTurnStartedEvent(nextUnit.Value));
            }

            ImmutableArray<BattleUnitTurn> turnOrder = BattleTurnOrderCalculator.GetNormalRoundOrder(state, Context);

            bool attackerAlive = turnOrder.Any(turn => state.GetUnit(turn.UnitId).Side == BattleSide.Attacker);
            bool defenderAlive = turnOrder.Any(turn => state.GetUnit(turn.UnitId).Side == BattleSide.Defender);

            if (attackerAlive && defenderAlive)
            {
                int roundNumber = state.RoundNumber;

                if (state.CurrentRound.Type is not BattleRoundType.Opening)
                {
                    roundNumber += 1;
                }

                return EventRuleResult<BattleEvent>.Append(
                    new BattleRoundEndedEvent(),
                    new BattleRoundStartedEvent(BattleRound.Normal(roundNumber), turnOrder),
                    new BattleUnitTurnStartedEvent(turnOrder[0]));
            }
            else
            {
                return EventRuleResult<BattleEvent>.Consume();
            }
        }

        public override EventRuleResult<BattleEvent> Visit(BattleUnitWaitedEvent ev, BattleState state)
        {
            if (state.Outcome is not null)
            {
                return EventRuleResult<BattleEvent>.Empty;
            }

            BattleUnitTurn? nextTurn = BattleTurnOrderCalculator.FindNextTurn(state);

            if (nextTurn is not null)
            {
                return EventRuleResult<BattleEvent>.Append(new BattleUnitTurnStartedEvent(nextTurn.Value));
            }
            else
            {
                return EventRuleResult<BattleEvent>.Empty;
            }
        }
    }
}
