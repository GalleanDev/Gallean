namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class BattleEndedLootRuinRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
        {
            if (ev.WinningSide != BattleSide.Attacker)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldFortification fortification = state.Fortifications[ev.DefenderFortificationId];
            IReadOnlyList<WorldEvent> events = fortification switch
            {
                WorldCapital => [],
                WorldCity => [],
                WorldRuin ruin => [LootRuin(ev, state, ruin)]
            };

            return new EventRuleResult<WorldEvent>(false, events);
        }

        private WorldRuinLootedEvent LootRuin(WorldFortificationBattleEndedEvent ev, WorldState state, WorldRuin ruin)
        {
            WorldItem? item;
            if (ev.SurvivingAttackerUnits.HasAliveUnits() && ruin.Item is { } itemId)
            {
                WorldItemId lootedItemId = WorldCommandHelpers.GenerateItemId(state);
                item = WorldItemFactory.Create(lootedItemId, Context.Definitions.GetItem(itemId), Context.Globals);
            }
            else
            {
                item = null;
            }

            return new WorldRuinLootedEvent(ruin.Id, ev.AttackerStackId, ruin.Resources, item);
        }
    }
}
