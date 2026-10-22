namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class DiplomacyProposalSentAiAnswerRule(IEngineContext context) : WorldEventRule(context)
    {
        private const int ChanceMax = 100;

        public override EventRuleResult<WorldEvent> Visit(WorldDiplomacyProposalSentEvent ev, WorldState state)
        {
            WorldPlayer targetPlayer = state.Players[ev.Proposal.TargetPlayerId];

            if (!targetPlayer.IsAi)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            DiplomacyProposalCalculator.AcceptInfo info = DiplomacyProposalCalculator.GetAcceptInfo(ev.Proposal, state, Context);
            bool accepted = Context.Random.Next(ChanceMax) < info.Chance;

            return EventRuleResult<WorldEvent>.Append(new WorldDiplomacyProposalAnsweredEvent(ev.Proposal, accepted));
        }
    }
}
