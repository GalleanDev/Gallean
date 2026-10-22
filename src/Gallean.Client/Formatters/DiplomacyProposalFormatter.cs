using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal static class DiplomacyProposalFormatter
{
    private sealed record OfferContext(
        IGameDefinitionProvider Definitions,
        IGameTextProvider Texts,
        string RaceName);

    private sealed record AnswerContext(string RaceName, bool Accepted);

    private sealed class OfferFormatter :
        IWorldDiplomacyProposalActionVisitor<OfferContext, string>,
        IStaticInstance<OfferFormatter>
    {
        public string Visit(WorldDiplomacyTradeItemAction action, OfferContext context)
        {
            ItemDefinition item = context.Definitions.GetItem(action.ItemId);
            string itemName = context.Texts.GetGlobalText(item.NameTextId).Text;

            return Tr("The {0} offers you {1} for {2} gold.", context.RaceName, itemName, action.Price);
        }

        public string Visit(WorldDiplomacyTradeSpellAction action, OfferContext context)
        {
            WorldSpellDefinition spell = context.Definitions.GetSpell(action.SpellId);
            string spellName = context.Texts.GetGlobalText(spell.NameTextId).Text;

            return Tr("The {0} offers you the spell {1} for {2} gold.", context.RaceName, spellName, action.Price);
        }

        public string Visit(WorldDiplomacyProposeAllianceAction action, OfferContext context)
        {
            return Tr("The {0} offers to ally with you.", context.RaceName);
        }

        public string Visit(WorldDiplomacyExchangeMapAction action, OfferContext context)
        {
            return Tr("The {0} offers to exchange map information with you.", context.RaceName);
        }
    }

    private sealed class AnswerFormatter :
        IWorldDiplomacyProposalActionVisitor<AnswerContext, string>,
        IStaticInstance<AnswerFormatter>
    {
        public string Visit(WorldDiplomacyTradeItemAction action, AnswerContext context)
        {
            return FormatTradeAnswer(action, context);
        }

        public string Visit(WorldDiplomacyTradeSpellAction action, AnswerContext context)
        {
            return FormatTradeAnswer(action, context);
        }

        public string Visit(WorldDiplomacyProposeAllianceAction action, AnswerContext context)
        {
            if (context.Accepted)
            {
                return Tr("The {0} is now your ally", context.RaceName);
            }
            else
            {
                return Tr("The {0} refuses the alliance", context.RaceName);
            }
        }

        public string Visit(WorldDiplomacyExchangeMapAction action, AnswerContext context)
        {
            if (context.Accepted)
            {
                return Tr("Map information shared with the {0}", context.RaceName);
            }
            else
            {
                return Tr("The {0} refuses to share map information", context.RaceName);
            }
        }

        private static string FormatTradeAnswer(WorldDiplomacyTradeAction action, AnswerContext context)
        {
            if (context.Accepted)
            {
                return Tr("The {0} paid {1} gold", context.RaceName, action.Price);
            }
            else
            {
                return Tr("The {0} declined your offer", context.RaceName);
            }
        }
    }

    public static string FormatOffer(
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        WorldState state,
        WorldDiplomacyProposal proposal)
    {
        string raceName = WorldPlayerFormatter.FormatRaceName(definitions, texts, state.Players[proposal.PlayerId]);

        return proposal.Action.AcceptProposal(OfferFormatter.Instance, new OfferContext(definitions, texts, raceName));
    }

    public static string FormatAnswer(
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        WorldState state,
        WorldDiplomacyProposal proposal,
        bool accepted)
    {
        string raceName = WorldPlayerFormatter.FormatRaceName(definitions, texts, state.Players[proposal.TargetPlayerId]);

        return proposal.Action.AcceptProposal(AnswerFormatter.Instance, new AnswerContext(raceName, accepted));
    }
}
