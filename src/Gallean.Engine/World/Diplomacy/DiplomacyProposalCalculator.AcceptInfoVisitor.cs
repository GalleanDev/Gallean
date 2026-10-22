using Gallean.Common;

namespace Gallean.Engine;

internal static partial class DiplomacyProposalCalculator
{
    private sealed class AcceptInfoVisitor :
        IWorldDiplomacyProposalActionVisitor<ProposalContext, AcceptInfo>,
        IStaticInstance<AcceptInfoVisitor>
    {
        public AcceptInfo Visit(WorldDiplomacyTradeItemAction action, ProposalContext context)
        {
            SendInfo sendInfo = SendInfoVisitor.Instance.Visit(action, context);

            if (sendInfo.Error is not null)
            {
                return new AcceptInfo(0, sendInfo.Error);
            }

            return GetTradeInfo(action, sendInfo.TradeValue, context);
        }

        public AcceptInfo Visit(WorldDiplomacyTradeSpellAction action, ProposalContext context)
        {
            SendInfo sendInfo = SendInfoVisitor.Instance.Visit(action, context);

            if (sendInfo.Error is not null)
            {
                return new AcceptInfo(0, sendInfo.Error);
            }

            WorldSpellDefinition spell = context.Context.Definitions.GetSpell(action.SpellId);

            return GetTradeInfo(action, sendInfo.TradeValue * (spell.Level + 1), context);
        }

        public AcceptInfo Visit(WorldDiplomacyProposeAllianceAction action, ProposalContext context)
        {
            SendInfo sendInfo = SendInfoVisitor.Instance.Visit(action, context);

            if (sendInfo.Error is not null)
            {
                return new AcceptInfo(0, sendInfo.Error);
            }

            GlobalVariables globals = context.Context.Globals;
            WorldPlayer player = context.State.Players[context.Proposal.PlayerId];
            WorldRelation relation = DiplomacyCalculator.GetRelation(context.State, player.Id, context.Proposal.TargetPlayerId);
            WorldMapTerrain terrain = WorldHelpers.GetNativeTerrain(player.Race);
            int landPercentage = TerrainTransformCalculator.GetTransformedPercentage(context.State.Map, terrain);
            int chance = Math.Max(
                globals.DiplomacyAllianceMinChance,
                relation.Current - globals.DiplomacyAllianceChancePenalty - landPercentage);

            return new AcceptInfo(chance, Error: null);
        }

        public AcceptInfo Visit(WorldDiplomacyExchangeMapAction action, ProposalContext context)
        {
            SendInfo sendInfo = SendInfoVisitor.Instance.Visit(action, context);

            if (sendInfo.Error is not null)
            {
                return new AcceptInfo(0, sendInfo.Error);
            }

            return new AcceptInfo(context.Context.Globals.DiplomacyExchangeMapChance, Error: null);
        }

        private static AcceptInfo GetTradeInfo(WorldDiplomacyTradeAction action, int value, ProposalContext context)
        {
            GlobalVariables globals = context.Context.Globals;
            WorldPlayer targetPlayer = context.State.Players[context.Proposal.TargetPlayerId];

            if (targetPlayer.Resources.Gold < action.Price)
            {
                return new AcceptInfo(0, WorldCommandError.InsufficientResources);
            }

            if (action.Price <= 0)
            {
                return new AcceptInfo(ChanceMax, Error: null);
            }
            else if (action.Price >= value)
            {
                return new AcceptInfo(globals.DiplomacyFairTradeChance * value / action.Price, Error: null);
            }
            else
            {
                return new AcceptInfo(ChanceMax - globals.DiplomacyFairTradeChance * action.Price / value, Error: null);
            }
        }
    }
}
