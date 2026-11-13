namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed partial record PurchaseUnitAction(WorldAiContext Context, int Priority) : AbstractEconomyAction(Context, Priority)
    {
        private const int MaxUnitsPerTurn = 1;

        public override WorldAiActionPlan? Plan(WorldState state, WorldPlayerId playerId, WorldAiMemory memory)
        {
            if (memory.Turn.UnitsPurchased >= MaxUnitsPerTurn)
            {
                return null;
            }

            WorldPlan plan = WorldPlan.For(state);
            WorldPlayer player = state.Players[playerId];
            WorldRaceDefinition race = Context.Engine.Definitions.GetRace(player.Race);
            WorldPurchaseUnitCommand? bestCommand = null;
            int bestScore = 0;

            foreach (WorldFortification fortification in state.Fortifications.Values)
            {
                if (fortification.Owner == playerId)
                {
                    List<UnitDefinitionId> composition = CollectComposition(plan, fortification);
                    int occupied = GarrisonCalculator.GetOccupiedCapacity(fortification.Garrison, Context.Engine.Definitions);
                    int capacity = GarrisonCalculator.GetTotalCapacity(fortification, Context.Engine.Globals);
                    WorldStack? visitingStack = plan.GetStackAt(fortification.Entrance);

                    foreach (UnitDefinitionId unitId in race.Units)
                    {
                        UnitDefinition unit = Context.Engine.Definitions.GetUnit(unitId);
                        GridPosition? position = FindFreePosition(fortification.Garrison, unit.IsLarge);

                        if (position is not null)
                        {
                            WorldPurchaseUnitCommand command = new(fortification.Id, unitId, position);
                            WorldCommandResult commandResult = command.Execute(state, Context.Engine);

                            if (!commandResult.Failed)
                            {
                                int score = ScorePurchase(unit, player, composition, occupied, capacity, visitingStack);

                                if (score > bestScore)
                                {
                                    bestCommand = command;
                                    bestScore = score;
                                }
                            }
                        }
                    }
                }
            }

            if (bestCommand is null)
            {
                return null;
            }

            return MakePlan(bestScore, bestCommand, memory);
        }

        private static int ScorePurchase(
            UnitDefinition unit,
            WorldPlayer player,
            List<UnitDefinitionId> composition,
            int occupied,
            int capacity,
            WorldStack? visitingStack)
        {
            List<UtilityScore.Consideration> considerations =
            [
                Considerations.Affordability(unit, player),
                Considerations.ReachPreference(unit),
                Considerations.Repetition(unit, composition),
                Considerations.Occupancy(occupied, capacity),
                Considerations.VisitingStack(player, visitingStack)
            ];

            return UtilityScore.Combine(considerations);
        }

        private static List<UnitDefinitionId> CollectComposition(WorldPlan plan, WorldFortification fortification)
        {
            List<UnitDefinitionId> result = [];

            foreach ((WorldUnit unit, _) in fortification.Garrison.GetUnits())
            {
                result.Add(unit.DefinitionId);
            }

            WorldStack? stack = plan.GetStackAt(fortification.Entrance);

            if (stack is not null && stack.OwnerId == fortification.Owner)
            {
                foreach ((WorldUnit unit, _) in stack.Units.GetUnits())
                {
                    result.Add(unit.DefinitionId);
                }
            }

            return result;
        }

        private static GridPosition? FindFreePosition(UnitGrid<WorldUnit> garrison, bool isLarge)
        {
            if (isLarge)
            {
                for (int flank = 0; flank < GridPosition.FlankCount; flank++)
                {
                    GridPosition front = GridPosition.AtFrontLine(flank);
                    GridPosition back = GridPosition.AtBackLine(flank);

                    if (garrison[front] is null && garrison[back] is null)
                    {
                        return front;
                    }
                }
            }
            else
            {
                foreach (GridPosition position in GridPosition.All)
                {
                    if (garrison[position] is null)
                    {
                        return position;
                    }
                }
            }

            return null;
        }
    }
}
