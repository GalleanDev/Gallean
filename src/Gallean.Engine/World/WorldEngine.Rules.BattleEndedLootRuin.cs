using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class BattleEndedLootRuinRule(IEngineContext context) : WorldEventRule(context)
    {
        private sealed record LootContext(WorldFortificationBattleEndedEvent Event, WorldState State, IEngineContext Context);

        private sealed class LootVisitor : IWorldFortificationVisitor<LootContext, IReadOnlyList<WorldEvent>>, IStaticInstance<LootVisitor>
        {
            public IReadOnlyList<WorldEvent> Visit(WorldCapital capital, LootContext context)
            {
                return [];
            }

            public IReadOnlyList<WorldEvent> Visit(WorldCity city, LootContext context)
            {
                return [];
            }

            public IReadOnlyList<WorldEvent> Visit(WorldRuin ruin, LootContext context)
            {
                WorldFortificationBattleEndedEvent ev = context.Event;

                WorldItem? item;
                if (ev.SurvivingAttackerUnits.HasAliveUnits() && ruin.Item is { } itemId)
                {
                    WorldItemId lootedItemId = WorldCommandHelpers.GenerateItemId(context.State);
                    item = WorldItemFactory.Create(lootedItemId, context.Context.Definitions.GetItem(itemId), context.Context.Globals);
                }
                else
                {
                    item = null;
                }

                return [new WorldRuinLootedEvent(ruin.Id, ev.AttackerStackId, ruin.Resources, item)];
            }
        }

        public override EventRuleResult<WorldEvent> Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
        {
            if (ev.WinningSide != BattleSide.Attacker)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldFortification fortification = state.Fortifications[ev.DefenderFortificationId];

            return new EventRuleResult<WorldEvent>(false, fortification.Accept(LootVisitor.Instance, new LootContext(ev, state, Context)));
        }
    }
}
