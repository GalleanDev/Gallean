using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class EndBattleRule(IEngineContext context) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleUnitTurnEndingEvent ev, BattleState state)
        {
            return CheckBattleEnd(state);
        }

        public override EventRuleResult<BattleEvent> Visit(BattleUnitUnsummonedEvent ev, BattleState state)
        {
            return CheckBattleEnd(state);
        }

        public override EventRuleResult<BattleEvent> Visit(BattleAutoResolveStepEvent ev, BattleState state)
        {
            if (state.CurrentRound.Type is BattleRoundType.Recovery)
            {
                return EventRuleResult<BattleEvent>.Empty;
            }

            return CheckBattleEnd(state);
        }

        private EventRuleResult<BattleEvent> CheckBattleEnd(BattleState state)
        {
            if (state.Outcome is not null)
            {
                return EventRuleResult<BattleEvent>.Empty;
            }

            foreach (BattleUnitId unitId in state.GetAllUnitIds())
            {
                BattleUnit unit = state.GetUnit(unitId);

                if (unit.SummonerId is not null && !unit.IsAlive)
                {
                    return EventRuleResult<BattleEvent>.Append(new BattleUnitUnsummonedEvent(unitId));
                }
            }

            IReadOnlyList<BattleUnitId> orphanedSummons = SummonCalculator.GetOrphanedSummons(state);

            if (orphanedSummons.Count > 0)
            {
                return EventRuleResult<BattleEvent>.Append(new BattleUnitUnsummonedEvent(orphanedSummons[0]));
            }

            BattleOutcome? outcome = BattleOutcomeCalculator.GetOutcome(state);

            if (outcome is null || HasRecoveryTurnLeft(state))
            {
                return EventRuleResult<BattleEvent>.Empty;
            }

            foreach (BattleUnitId unitId in state.GetAllUnitIds())
            {
                BattleUnit unit = state.GetUnit(unitId);

                if (unit.SummonerId is not null)
                {
                    return EventRuleResult<BattleEvent>.Append(new BattleUnitUnsummonedEvent(unitId));
                }
            }

            if (state.CurrentRound.Type is not BattleRoundType.Recovery)
            {
                ImmutableArray<BattleUnitTurn> order = BattleTurnOrderCalculator.GetRecoveryRoundOrder(state, Context, outcome.WinningSide);

                if (!order.IsEmpty)
                {
                    return EventRuleResult<BattleEvent>.Append(
                        new BattleRoundEndedEvent(),
                        new BattleRoundStartedEvent(BattleRound.Recovery(state.RoundNumber), order));
                }
            }

            return EventRuleResult<BattleEvent>.Append(
            [
                new BattleRoundEndedEvent(),
                .. BattleTransformationCalculator.GetRestoreEvents(state, Context),
                new BattleEndedEvent(outcome.WinningSide)
            ]);
        }

        private static bool HasRecoveryTurnLeft(BattleState state)
        {
            return state.CurrentRound.Type switch
            {
                BattleRoundType.Opening => false,
                BattleRoundType.Normal => false,
                BattleRoundType.Recovery => BattleTurnOrderCalculator.FindNextTurn(state) is not null
            };
        }
    }
}
