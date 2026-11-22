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
            WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, Context.Definitions);

            if (ev.RemainingPath.IsEmpty)
            {
                if (ev.EncounterPosition is not null)
                {
                    return TryResolveEncounter(state, stack, plan, ev.EncounterPosition.Value) ?? EventRuleResult<WorldEvent>.Empty;
                }
                else if (profile.IsThief)
                {
                    return VisitThief(ev, state, stack, plan, profile);
                }
                else
                {
                    return EventRuleResult<WorldEvent>.Empty;
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

            WorldStackMovingEvent nextStep = WorldCommandHelpers.CreateMoveStepEvent(stack, plan, profile, ev.RemainingPath, ev.EncounterPosition);

            return EventRuleResult<WorldEvent>.Append(nextStep);
        }

        private EventRuleResult<WorldEvent> VisitThief(
            WorldStackMovedEvent ev,
            WorldState state,
            WorldStack stack,
            WorldPlan plan,
            WorldMovementCalculator.MovementProfile profile)
        {
            WorldStack? sourceStack = PathTraversalCalculator.FindInterceptingStack(state, plan, stack, profile, ev.SourcePosition, Context.Definitions);
            WorldStack? targetStack = PathTraversalCalculator.FindInterceptingStack(state, plan, stack, profile, ev.TargetPosition, Context.Definitions);

            if (sourceStack is not null && targetStack is not null)
            {
                StackStats stackStats = Context.UnitStats.GetWorldStackStats(stack);
                int actionCost = WorldMovementCalculator.GetActionCost(stackStats, Context.Globals);
                int movementPoints = Math.Max(0, stack.MovementPoints - actionCost);

                return EventRuleResult<WorldEvent>.Append(new WorldStackMovementConsumedEvent(stack.Id, movementPoints));
            }
            else
            {
                return EventRuleResult<WorldEvent>.Empty;
            }
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

            WorldSite? site = plan.GetSiteAt(position);

            if (site is not null)
            {
                return EventRuleResult<WorldEvent>.Append([.. WorldCommandHelpers.CreateSiteVisitEvents(stack, site, Context)]);
            }

            return null;
        }
    }
}
