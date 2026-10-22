using Gallean.Common;

namespace Gallean.Engine;

internal static partial class DiplomacyProposalCalculator
{
    public sealed record SendInfo(int TradeValue, WorldCommandError? Error);

    public sealed record AcceptInfo(int Chance, WorldCommandError? Error);

    private sealed record ProposalContext(
        WorldDiplomacyProposal Proposal,
        WorldState State,
        IEngineContext Context);

    private const int ChanceMax = 100;

    public static SendInfo GetSendInfo(WorldDiplomacyProposal proposal, WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.Players[proposal.PlayerId];
        WorldPlayer targetPlayer = state.Players[proposal.TargetPlayerId];

        if (targetPlayer.IncomingDiplomacyProposals.Contains(proposal))
        {
            return new SendInfo(0, WorldCommandError.DiplomacyAlreadyProposed);
        }

        if (proposal.Action is WorldDiplomacyExchangeMapAction &&
            player.TurnActions.MapExchangeProposals.Contains(targetPlayer.Id))
        {
            return new SendInfo(0, WorldCommandError.DiplomacyAlreadyProposed);
        }

        return proposal.Action.AcceptProposal(SendInfoVisitor.Instance, new ProposalContext(proposal, state, context));
    }

    public static AcceptInfo GetAcceptInfo(WorldDiplomacyProposal proposal, WorldState state, IEngineContext context)
    {
        WorldPlan plan = WorldPlan.For(state);

        if (plan.IsPlayerDefeated(proposal.PlayerId) || plan.IsPlayerDefeated(proposal.TargetPlayerId))
        {
            return new AcceptInfo(0, WorldCommandError.DiplomacyTargetInvalid);
        }

        return proposal.Action.AcceptProposal(AcceptInfoVisitor.Instance, new ProposalContext(proposal, state, context));
    }

    private static WorldItem? FindTradeItem(WorldState state, WorldDiplomacyTradeItemAction action)
    {
        if (!state.Fortifications.TryGetValue(action.SourceFortificationId, out WorldFortification? source))
        {
            return null;
        }

        foreach (WorldItem item in source.Inventory.Items)
        {
            if (item.DefinitionId == action.ItemId)
            {
                return item;
            }
        }

        return null;
    }

    public static int GetTradeValue(ItemDefinitionId itemId, IEngineContext context)
    {
        ItemDefinition item = context.Definitions.GetItem(itemId);

        return item.Value.Gold;
    }

    public static int GetTradeValue(WorldSpellDefinitionId spellId, IEngineContext context)
    {
        WorldSpellDefinition spell = context.Definitions.GetSpell(spellId);

        return spell.PurchaseCost.Gold;
    }
}
