using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TriggerScenarioEventsRule(IEngineContext context) : WorldEventRule(context)
    {
        private static EventRuleResult<WorldEvent> Check(ScenarioEventTrigger trigger)
        {
            return EventRuleResult<WorldEvent>.Append(new WorldScenarioCheckEvent(trigger, []));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldFortificationCapturedEvent ev, WorldState state)
        {
            return Check(new ScenarioEventFortificationCapturedTrigger(ev.FortificationId, ev.CapturingStackId));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldRuinLootedEvent ev, WorldState state)
        {
            return Check(new ScenarioEventStackMovedTrigger(ev.StackId));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldStackDestroyedEvent ev, WorldState state)
        {
            return Check(new ScenarioEventStackDestroyedTrigger(ev.Destroyer));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartedEvent ev, WorldState state)
        {
            return Check(new ScenarioEventTurnStartedTrigger(ev.PlayerId));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldSiteVisitedEvent ev, WorldState state)
        {
            return Check(new ScenarioEventStackMovedTrigger(ev.StackId));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldBagPickedUpEvent ev, WorldState state)
        {
            return Check(new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(ev.StackId)));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldItemPurchasedEvent ev, WorldState state)
        {
            return Check(new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(ev.StackId)));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldMerchantItemStolenEvent ev, WorldState state)
        {
            return Check(new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Stack(ev.StackId)));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldInventoryItemTransferredEvent ev, WorldState state)
        {
            return Check(new ScenarioEventItemGainedTrigger(ev.Target));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldDiplomacyItemTradedEvent ev, WorldState state)
        {
            WorldPlan plan = WorldPlan.For(state);
            WorldCapital? capital = plan.GetCapital(ev.BuyerId);

            if (capital is null)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return Check(new ScenarioEventItemGainedTrigger(new WorldUnitGroup.Fortification(capital.Id)));
        }

        public override EventRuleResult<WorldEvent> Visit(WorldScenarioCheckEvent ev, WorldState state)
        {
            ImmutableArray<WorldEvent> cascadeEvents = ScenarioEventCalculator.CascadeEvents(
                Context.Scenario,
                Context.Random,
                Context.Globals,
                state,
                ev.Trigger,
                ev.TriggeredEvents);

            return EventRuleResult<WorldEvent>.Consume([.. cascadeEvents]);
        }
    }
}
