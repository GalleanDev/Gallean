namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartedGenerateResourcesRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartedEvent ev, WorldState state)
        {
            WorldPlayer player = state.Players[ev.PlayerId];
            WorldResourceSet income = PlayerIncomeCalculator.GetIncome(player, state, Context);

            return EventRuleResult<WorldEvent>.Append(new WorldTurnResourcesGeneratedEvent(ev.PlayerId, income));
        }
    }
}
