using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private sealed partial class ConditionChecker :
        IScenarioEventConditionVisitor<ScenarioEventConditionContext, bool>,
        IStaticInstance<ConditionChecker>
    {
        public bool Visit(ScenarioEventFrequencyCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new Frequency(condition), context);
        }

        public bool Visit(ScenarioEventEnteringZoneCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new EnteringZone(condition), context);
        }

        public bool Visit(ScenarioEventEnteringCityCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new EnteringCity(condition), context);
        }

        public bool Visit(ScenarioEventOwningCityCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new OwningCity(condition), context);
        }

        public bool Visit(ScenarioEventStackToCityCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new StackToCity(condition), context);
        }

        public bool Visit(ScenarioEventStackExistenceCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new StackExistence(condition), context);
        }

        public bool Visit(ScenarioEventDestroyingStackCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new DestroyingStack(condition), context);
        }

        public bool Visit(ScenarioEventDiplomacyRelationsCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new DiplomacyRelations(condition), context);
        }

        public bool Visit(ScenarioEventAllianceCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new Alliance(condition), context);
        }

        public bool Visit(ScenarioEventLootingRuinCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new LootingRuin(condition), context);
        }

        public bool Visit(ScenarioEventOwningItemCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new OwningItem(condition), context);
        }

        public bool Visit(ScenarioEventLeaderOwningItemCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new LeaderOwningItem(condition), context);
        }

        public bool Visit(ScenarioEventTransformingLandCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new TransformingLand(condition), context);
        }

        public bool Visit(ScenarioEventVisitingSiteCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new VisitingSite(condition), context);
        }

        public bool Visit(ScenarioEventStackToLocationCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new StackToLocation(condition), context);
        }

        public bool Visit(ScenarioEventItemToLocationCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new ItemToLocation(condition), context);
        }

        public bool Visit(ScenarioEventVariableInRangeCondition condition, ScenarioEventConditionContext context)
        {
            return context.Trigger.Accept(new VariableInRange(condition), context);
        }

        private static bool IsPlayerStack(WorldStackId stackId, WorldPlayerId playerId, ScenarioEventConditionContext context)
        {
            if (!context.State.Stacks.TryGetValue(stackId, out WorldStack? stack))
            {
                return false;
            }

            return stack.OwnerId == playerId;
        }

        private static bool IsReferencedStack(WorldStackId stackId, ScenarioStackReference reference, ScenarioEventConditionContext context)
        {
            return ScenarioStackReferenceCalculator.IsInstance(reference, stackId, context.State);
        }

        private static bool IsAffectedRace(WorldPlayerId playerId, ScenarioEventConditionContext context)
        {
            if (!context.State.Players.TryGetValue(playerId, out WorldPlayer? player))
            {
                return false;
            }

            return context.AffectedRaces.Contains(player.Race);
        }

        private static bool HasItem(WorldStackId stackId, ItemDefinitionId itemId, ScenarioEventConditionContext context)
        {
            if (!context.State.Stacks.TryGetValue(stackId, out WorldStack? stack))
            {
                return false;
            }

            return Contains(stack.Inventory, itemId) || Contains(stack.Equipment, itemId);
        }

        private static bool HasItem(WorldFortificationId fortificationId, ItemDefinitionId itemId, ScenarioEventConditionContext context)
        {
            if (!context.State.Fortifications.TryGetValue(fortificationId, out WorldFortification? fortification))
            {
                return false;
            }

            return Contains(fortification.Inventory, itemId);
        }

        private static bool Contains(WorldInventory inventory, ItemDefinitionId itemId)
        {
            foreach (WorldItem item in inventory.Items)
            {
                if (item.DefinitionId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Contains(LeaderEquipment equipment, ItemDefinitionId itemId)
        {
            foreach (WorldItem item in equipment.Items.Values)
            {
                if (item.DefinitionId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStackAt(WorldStackId stackId, ScenarioLocationId locationId, ScenarioEventConditionContext context)
        {
            if (!context.State.Stacks.TryGetValue(stackId, out WorldStack? stack))
            {
                return false;
            }

            WorldPlan plan = WorldPlan.For(context.State);
            WorldFortification? fortification = plan.GetFortificationAt(stack.Position);
            ScenarioLocationDefinition location = context.Scenario.GetLocation(locationId);

            if (fortification is WorldCity)
            {
                return false;
            }
            else if (fortification is WorldCapital { CapturedById: null })
            {
                return false;
            }
            else
            {
                return ScenarioLocationCalculator.Contains(location, stack.Position);
            }
        }

        private static bool IsStackAt(WorldStackId stackId, WorldFortificationId fortificationId, ScenarioEventConditionContext context)
        {
            if (!context.State.Stacks.TryGetValue(stackId, out WorldStack? stack))
            {
                return false;
            }

            WorldPlan plan = WorldPlan.For(context.State);

            return plan.GetFortificationAt(stack.Position)?.Id == fortificationId;
        }

        private static bool IsRacePlaying(WorldRace race, ScenarioEventConditionContext context)
        {
            foreach (WorldPlayer player in context.State.Players.Values)
            {
                if (player.Race == race)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyStackAt(WorldFortificationId fortificationId, ScenarioEventConditionContext context)
        {
            foreach (WorldStack stack in context.State.Stacks.Values)
            {
                if (IsStackAt(stack.Id, fortificationId, context))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyPlayerReferencedStackAt(
            WorldPlayerId playerId,
            ScenarioStackReference reference,
            ScenarioLocationId locationId,
            ScenarioEventConditionContext context)
        {
            foreach (WorldStack stack in context.State.Stacks.Values)
            {
                if (IsPlayerStack(stack.Id, playerId, context) &&
                    IsReferencedStack(stack.Id, reference, context) &&
                    IsStackAt(stack.Id, locationId, context))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyReferencedStackAt(
            ScenarioStackReference reference,
            WorldFortificationId fortificationId,
            ScenarioEventConditionContext context)
        {
            foreach (WorldStack stack in context.State.Stacks.Values)
            {
                if (IsReferencedStack(stack.Id, reference, context) &&
                    IsStackAt(stack.Id, fortificationId, context))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyReferencedStackWithItem(
            ScenarioStackReference reference,
            ItemDefinitionId itemId,
            ScenarioEventConditionContext context)
        {
            foreach (WorldStack stack in context.State.Stacks.Values)
            {
                if (IsReferencedStack(stack.Id, reference, context) &&
                    HasItem(stack.Id, itemId, context))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyAffectedRaceStackWithItem(
            ItemDefinitionId itemId,
            ScenarioEventConditionContext context)
        {
            foreach (WorldStack stack in context.State.Stacks.Values)
            {
                if (IsAffectedRace(stack.OwnerId, context) &&
                    HasItem(stack.Id, itemId, context))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyAffectedRaceFortificationWithItem(
            ItemDefinitionId itemId,
            ScenarioEventConditionContext context)
        {
            foreach (WorldFortification fortification in context.State.Fortifications.Values)
            {
                WorldPlayerId? ownerId = FortificationCalculator.GetOwner(fortification);

                if (ownerId is not null &&
                    IsAffectedRace(ownerId.Value, context) &&
                    HasItem(fortification.Id, itemId, context))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
