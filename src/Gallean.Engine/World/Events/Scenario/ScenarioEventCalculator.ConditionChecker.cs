namespace Gallean.Engine;

internal static partial class ScenarioEventCalculator
{
    private static partial class ConditionChecker
    {
        public static bool Check(ScenarioEventCondition condition, ScenarioEventConditionContext context)
        {
            return condition switch
            {
                ScenarioEventFrequencyCondition frequency => CheckFrequency(frequency, context),
                ScenarioEventEnteringZoneCondition enteringZone => CheckEnteredZone(enteringZone, context),
                ScenarioEventEnteringCityCondition enteringCity => CheckEnteredCity(enteringCity, context),
                ScenarioEventOwningCityCondition owningCity => CheckFortificationOwned(owningCity, context),
                ScenarioEventDestroyingStackCondition destroyingStack => ScenarioStackReferenceCalculator.IsDestroyed(destroyingStack.Stack, context.AffectedRaces, context.State),
                ScenarioEventOwningItemCondition owningItem => CheckItemOwned(owningItem, context),
                ScenarioEventLeaderOwningItemCondition leaderOwningItem => AnyReferencedStackWithItem(leaderOwningItem.Stack, leaderOwningItem.Item, context),
                ScenarioEventDiplomacyRelationsCondition diplomacyRelations => CheckRelationLevel(diplomacyRelations, context),
                ScenarioEventAllianceCondition alliance => CheckAlliance(alliance, context),
                ScenarioEventLootingRuinCondition lootingRuin => CheckRuinLooted(lootingRuin, context),
                ScenarioEventTransformingLandCondition transformingLand => CheckLandTransformed(transformingLand, context),
                ScenarioEventVisitingSiteCondition visitingSite => CheckSiteVisited(visitingSite, context),
                ScenarioEventStackToLocationCondition stackToLocation => CheckStackAtLocation(stackToLocation, context),
                ScenarioEventStackToCityCondition stackToCity => AnyReferencedStackAt(stackToCity.Stack, stackToCity.Fortification, context),
                ScenarioEventItemToLocationCondition itemToLocation => CheckItemAtLocation(itemToLocation, context),
                ScenarioEventStackExistenceCondition stackExistence => ScenarioStackReferenceCalculator.CheckExistence(stackExistence.Stack, stackExistence.MustExist, context.State),
                ScenarioEventVariableInRangeCondition variableInRange => ScenarioVariableCalculator.IsInRange(variableInRange, context.State, context.Scenario)
            };
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
