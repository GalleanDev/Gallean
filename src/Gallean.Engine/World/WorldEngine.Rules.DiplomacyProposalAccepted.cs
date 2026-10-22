using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class DiplomacyProposalAcceptedRule(IEngineContext context) : WorldEventRule(context)
    {
        private sealed record EffectContext(WorldDiplomacyProposal Proposal, WorldState State);

        private sealed class EffectVisitor : IWorldDiplomacyProposalActionVisitor<EffectContext, IReadOnlyList<WorldEvent>>, IStaticInstance<EffectVisitor>
        {
            public IReadOnlyList<WorldEvent> Visit(WorldDiplomacyTradeItemAction action, EffectContext context)
            {
                WorldDiplomacyProposal proposal = context.Proposal;
                WorldFortification source = context.State.Fortifications[action.SourceFortificationId];
                WorldItem? item = FindItem(source.Inventory, action.ItemId);

                if (item is null)
                {
                    throw new InvalidOperationException($"Fortification '{source.Id}' has no item '{action.ItemId}'.");
                }

                return [new WorldDiplomacyItemTradedEvent(proposal.PlayerId, proposal.TargetPlayerId, action.SourceFortificationId, item, action.Price)];
            }

            public IReadOnlyList<WorldEvent> Visit(WorldDiplomacyTradeSpellAction action, EffectContext context)
            {
                WorldDiplomacyProposal proposal = context.Proposal;

                return [new WorldDiplomacySpellTradedEvent(proposal.PlayerId, proposal.TargetPlayerId, action.SpellId, action.Price)];
            }

            public IReadOnlyList<WorldEvent> Visit(WorldDiplomacyProposeAllianceAction action, EffectContext context)
            {
                WorldDiplomacyProposal proposal = context.Proposal;

                return [new WorldAllianceStartedEvent(proposal.PlayerId, proposal.TargetPlayerId, Permanent: false)];
            }

            public IReadOnlyList<WorldEvent> Visit(WorldDiplomacyExchangeMapAction action, EffectContext context)
            {
                WorldPlayer player = context.State.Players[context.Proposal.PlayerId];
                WorldPlayer targetPlayer = context.State.Players[context.Proposal.TargetPlayerId];
                ImmutableArray<WorldPosition> revealed = FogOfWarCalculator.Diff(player.FogOfWar, targetPlayer.FogOfWar);
                ImmutableArray<WorldPosition> targetRevealed = FogOfWarCalculator.Diff(targetPlayer.FogOfWar, player.FogOfWar);
                List<WorldEvent> events = [];

                if (!revealed.IsEmpty)
                {
                    events.Add(new WorldFogRevealedEvent(player.Id, revealed));
                }

                if (!targetRevealed.IsEmpty)
                {
                    events.Add(new WorldFogRevealedEvent(targetPlayer.Id, targetRevealed));
                }

                return events;
            }
        }

        public override EventRuleResult<WorldEvent> Visit(WorldDiplomacyProposalAnsweredEvent ev, WorldState state)
        {
            if (!ev.Accepted)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            IReadOnlyList<WorldEvent> events = ev.Proposal.Action.AcceptProposal(EffectVisitor.Instance, new EffectContext(ev.Proposal, state));

            return new EventRuleResult<WorldEvent>(false, events);
        }

        private static WorldItem? FindItem(WorldInventory inventory, ItemDefinitionId itemId)
        {
            foreach (WorldItem item in inventory.Items)
            {
                if (item.DefinitionId == itemId)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
