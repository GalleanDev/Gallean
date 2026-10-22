namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class BattleEndedRetreatStackRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldStackBattleEndedEvent ev, WorldState state)
        {
            WorldStack attackerStack = state.Stacks[ev.AttackerStackId];
            WorldStack defenderStack = state.Stacks[ev.DefenderStackId];

            if (ev.WinningSide == BattleSide.Attacker)
            {
                return Retreat(state, defenderStack, ev.SurvivingDefenderUnits, GetStackPosition(state, attackerStack));
            }
            else
            {
                return Retreat(state, attackerStack, ev.SurvivingAttackerUnits, GetStackPosition(state, defenderStack));
            }
        }

        public override EventRuleResult<WorldEvent> Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
        {
            if (ev.WinningSide == BattleSide.Attacker)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldStack attackerStack = state.Stacks[ev.AttackerStackId];
            WorldFortification fortification = state.Fortifications[ev.DefenderFortificationId];

            return Retreat(state, attackerStack, ev.SurvivingAttackerUnits, fortification.Footprint.TopLeft);
        }

        private static WorldPosition GetStackPosition(WorldState state, WorldStack stack)
        {
            WorldPlan plan = WorldPlan.For(state);
            WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

            if (fortification is not null)
            {
                return fortification.Footprint.TopLeft;
            }
            else
            {
                return stack.Position;
            }
        }

        private EventRuleResult<WorldEvent> Retreat(
            WorldState state,
            WorldStack stack,
            UnitGrid<WorldUnit> survivingUnits,
            WorldPosition opponentPosition)
        {
            WorldPlan plan = WorldPlan.For(state);

            if (!survivingUnits.HasAliveUnits())
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (plan.GetFortificationAt(stack.Position) is not null)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldPosition? newPosition = FindRetreatPosition(state, stack, opponentPosition);

            if (newPosition is null)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldStackDirection direction = StackStepCalculator.GetDirection(stack.Position, newPosition.Value);

            return EventRuleResult<WorldEvent>.Append(
                new WorldStackRelocatedEvent(stack.Id, stack.Position, newPosition.Value, direction));
        }

        private WorldPosition? FindRetreatPosition(WorldState state, WorldStack stack, WorldPosition position)
        {
            WorldPlan plan = WorldPlan.For(state);
            WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, Context.Definitions);
            WorldStackDirection direction = StackStepCalculator.GetDirection(position, stack.Position);

            foreach (int index in new[] { 0, -1, 1, -2, 2, -3, 3, -4 })
            {
                WorldStackDirection checkDirection = (WorldStackDirection)(((int)direction + index + 8) % 8);
                WorldPosition checkPosition = StackStepCalculator.MakeStep(stack.Position, checkDirection);

                if (plan.CanPlaceStackAt(checkPosition, profile))
                {
                    return checkPosition;
                }
            }

            return null;
        }
    }
}
