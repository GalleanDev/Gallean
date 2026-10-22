using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class AutoResolveStepRule(IEngineContext context) : BattleEventRule(context)
    {
        private const int RetreatRound = 15;
        private const int DefendRound = 30;
        private const int LimitRound = 45;

        private readonly BattleAiPlanner ai = new(context, canUseItems: false);

        public override EventRuleResult<BattleEvent> Visit(BattleAutoResolveStepEvent ev, BattleState state)
        {
            if (state.Outcome is not null)
            {
                return EventRuleResult<BattleEvent>.Consume();
            }

            if (state.CurrentRound.Type is not BattleRoundType.Recovery &&
                BattleOutcomeCalculator.GetOutcome(state) is not null)
            {
                return EventRuleResult<BattleEvent>.Consume(new BattleAutoResolveStepEvent(ev.StartRoundNumber));
            }

            int elapsedRounds = state.RoundNumber - ev.StartRoundNumber;
            if (elapsedRounds > LimitRound)
            {
                throw new InvalidOperationException("Battle auto-resolve failed to complete.");
            }

            BattleUnit currentUnit = state.CurrentUnit;
            IBattleCommand<Empty> command;

            if (elapsedRounds > RetreatRound && currentUnit.Side == BattleSide.Attacker)
            {
                command = new BattleRetreatCommand(currentUnit.Id);
            }
            else if (elapsedRounds > DefendRound && currentUnit.Side == BattleSide.Defender)
            {
                command = new BattleDefendCommand(currentUnit.Id);
            }
            else
            {
                command = ai.DecideAction(state).Command;
            }

            BattleCommandResult commandResult = command.Execute(state, Context);

            IReadOnlyList<BattleEvent> newEvents;
            if (!commandResult.Failed)
            {
                newEvents = commandResult.Events;
            }
            else
            {
                newEvents = new BattleSkipCommand(currentUnit.Id).Execute(state, Context).Events;
            }

            return EventRuleResult<BattleEvent>.Consume(
            [
                .. newEvents,
                new BattleAutoResolveStepEvent(ev.StartRoundNumber)
            ]);
        }
    }
}
