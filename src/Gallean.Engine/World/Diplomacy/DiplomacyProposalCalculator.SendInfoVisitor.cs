using Gallean.Common;

namespace Gallean.Engine;

internal static partial class DiplomacyProposalCalculator
{
    private sealed class SendInfoVisitor :
        IWorldDiplomacyProposalActionVisitor<ProposalContext, SendInfo>,
        IStaticInstance<SendInfoVisitor>
    {
        public SendInfo Visit(WorldDiplomacyTradeItemAction action, ProposalContext context)
        {
            if (action.Price < 0)
            {
                return new SendInfo(0, WorldCommandError.DiplomacyPriceInvalid);
            }

            if (!context.State.Fortifications.TryGetValue(action.SourceFortificationId, out WorldFortification? source))
            {
                return new SendInfo(0, WorldCommandError.FortificationNotFound);
            }

            if (FortificationCalculator.GetOwner(source) != context.Proposal.PlayerId)
            {
                return new SendInfo(0, WorldCommandError.FortificationNotFound);
            }

            if (FindTradeItem(context.State, action) is null)
            {
                return new SendInfo(0, WorldCommandError.ItemNotFound);
            }

            int tradeValue = GetTradeValue(action.ItemId, context.Context);
            WorldPlan plan = WorldPlan.For(context.State);

            if (plan.GetCapital(context.Proposal.TargetPlayerId) is null)
            {
                return new SendInfo(tradeValue, WorldCommandError.CapitalNotFound);
            }

            return new SendInfo(tradeValue, Error: null);
        }

        public SendInfo Visit(WorldDiplomacyTradeSpellAction action, ProposalContext context)
        {
            if (action.Price < 0)
            {
                return new SendInfo(0, WorldCommandError.DiplomacyPriceInvalid);
            }

            WorldPlayer player = context.State.Players[context.Proposal.PlayerId];
            WorldPlayer targetPlayer = context.State.Players[context.Proposal.TargetPlayerId];

            if (!player.Spellbook.Acquired.Contains(action.SpellId))
            {
                return new SendInfo(0, WorldCommandError.SpellNotKnown);
            }

            int tradeValue = GetTradeValue(action.SpellId, context.Context);
            WorldSpellDefinition spell = context.Context.Definitions.GetSpell(action.SpellId);

            if (spell.Effect is WorldSummonStackSpellEffect)
            {
                return new SendInfo(tradeValue, WorldCommandError.DiplomacySpellNotTradable);
            }

            if (targetPlayer.Spellbook.Acquired.Contains(action.SpellId))
            {
                return new SendInfo(tradeValue, WorldCommandError.SpellAlreadyKnown);
            }

            return new SendInfo(tradeValue, Error: null);
        }

        public SendInfo Visit(WorldDiplomacyProposeAllianceAction action, ProposalContext context)
        {
            WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, context.Proposal.PlayerId, context.Proposal.TargetPlayerId);

            if (relation.Alliance)
            {
                return new SendInfo(0, WorldCommandError.DiplomacyAlreadyAllied);
            }

            return new SendInfo(0, GetPeaceError(relation, context.Context.Globals));
        }

        public SendInfo Visit(WorldDiplomacyExchangeMapAction action, ProposalContext context)
        {
            WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, context.Proposal.PlayerId, context.Proposal.TargetPlayerId);

            return new SendInfo(0, GetPeaceError(relation, context.Context.Globals));
        }

        private static WorldCommandError? GetPeaceError(WorldRelation relation, GlobalVariables globals)
        {
            WorldRelationLevel level = DiplomacyCalculator.ResolveRelationLevel(relation.Current, globals);

            if (level == WorldRelationLevel.Peace)
            {
                return null;
            }
            else
            {
                return WorldCommandError.DiplomacyPeaceRequired;
            }
        }
    }
}
