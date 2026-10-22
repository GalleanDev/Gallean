using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class StackMovedStepRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldStackMovedEvent ev, WorldState state)
        {
            if (!state.Stacks.TryGetValue(ev.StackId, out WorldStack? stack))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            ScenarioEventTrigger trigger = new ScenarioEventStackMovedTrigger(ev.StackId);

            ImmutableArray<WorldEvent> cascadeEvents = ScenarioEventCalculator.CascadeEvents(
                Context.Scenario,
                Context.Random,
                Context.Globals,
                state,
                trigger,
                []);

            if (!cascadeEvents.IsEmpty)
            {
                return EventRuleResult<WorldEvent>.Append([.. cascadeEvents]);
            }

            WorldPlan plan = WorldPlan.For(state);

            if (ev.RemainingPath.IsEmpty)
            {
                if (ev.EncounterPosition is not null)
                {
                    return TryResolveEncounter(state, stack, plan, ev.EncounterPosition.Value) ?? EventRuleResult<WorldEvent>.Empty;
                }
                else
                {
                    return VisitSite(stack, plan);
                }
            }

            WorldPosition nextPosition = ev.RemainingPath[0];

            WorldStack? traverseStack = plan.GetStackAt(nextPosition);
            if (traverseStack is not null && !DiplomacyCalculator.IsHostile(state, stack.OwnerId, traverseStack.OwnerId))
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            EventRuleResult<WorldEvent>? encounter = TryResolveEncounter(state, stack, plan, nextPosition);
            if (encounter is not null)
            {
                return encounter.Value;
            }

            WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, Context.Definitions);
            WorldStackMovingEvent nextStep = WorldCommandHelpers.CreateMoveStepEvent(stack, plan, profile, ev.RemainingPath, ev.EncounterPosition);

            return EventRuleResult<WorldEvent>.Append(nextStep);
        }

        private EventRuleResult<WorldEvent> VisitSite(WorldStack stack, WorldPlan plan)
        {
            WorldSite? site = plan.GetSiteAt(stack.Position);

            if (site is not null && CanVisit(stack))
            {
                return EventRuleResult<WorldEvent>.Append(new WorldSiteVisitedEvent(stack.Id, site.Id));
            }
            else
            {
                return EventRuleResult<WorldEvent>.Empty;
            }
        }

        private bool CanVisit(WorldStack stack)
        {
            (WorldUnit leader, _, _) = stack.GetLeader(Context.Definitions);

            return leader.IsAlive && !ThiefCalculator.IsThief(stack, Context.Definitions);
        }

        private EventRuleResult<WorldEvent>? TryResolveEncounter(WorldState state, WorldStack stack, WorldPlan plan, WorldPosition position)
        {
            WorldBag? bag = plan.GetBagAt(position);

            if (bag is not null)
            {
                return EventRuleResult<WorldEvent>.Append(new WorldBagPickedUpEvent(stack.Id, bag.Id, bag.Inventory));
            }

            WorldStack? enemyStack = plan.GetStackAt(position);

            if (enemyStack is not null && DiplomacyCalculator.IsHostile(state, stack.OwnerId, enemyStack.OwnerId))
            {
                (_, _, UnitDefinition leaderDefinition) = enemyStack.GetLeader(Context.Definitions);

                List<WorldEvent> events;
                if (leaderDefinition.Category == UnitCategory.Illusion)
                {
                    events = WorldCommandHelpers.CreateDestroyIllusionEvents(stack, enemyStack, Context);
                }
                else
                {
                    events = WorldCommandHelpers.CreateBattleStartEvents(state, stack, enemyStack, null, Context);
                }

                return new EventRuleResult<WorldEvent>(false, events);
            }

            WorldFortification? enemyFortification = plan.GetFortificationAt(position);

            if (enemyFortification is not null && enemyFortification.Garrison.HasAliveUnits())
            {
                WorldPlayerId? fortificationOwner = FortificationCalculator.GetOwner(enemyFortification);

                if (fortificationOwner is null || DiplomacyCalculator.IsHostile(state, stack.OwnerId, fortificationOwner.Value))
                {
                    List<WorldEvent> battleEvents = WorldCommandHelpers.CreateBattleStartEvents(
                        state, stack, null, enemyFortification, Context);

                    return new EventRuleResult<WorldEvent>(false, battleEvents);
                }
            }

            return null;
        }
    }
}
