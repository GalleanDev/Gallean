namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class BattleEndedRevealRuinRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
        {
            WorldFortification fortification = state.Fortifications[ev.DefenderFortificationId];
            IReadOnlyList<WorldEvent> events = fortification switch
            {
                WorldCapital => [],
                WorldCity => [],
                WorldRuin ruin => RevealRuin(ev, state, ruin)
            };

            return new EventRuleResult<WorldEvent>(false, events);
        }

        private static IReadOnlyList<WorldEvent> RevealRuin(WorldFortificationBattleEndedEvent ev, WorldState state, WorldRuin ruin)
        {
            WorldPlayerId ownerId = state.Stacks[ev.AttackerStackId].OwnerId;

            if (!SpyCalculator.SpiesOnRuin(state, ownerId, ruin.Id))
            {
                return [new WorldRuinSpiedEvent(ownerId, ruin.Id)];
            }
            else
            {
                return [];
            }
        }
    }
}
