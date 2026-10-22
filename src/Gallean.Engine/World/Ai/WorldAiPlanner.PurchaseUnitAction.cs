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
            WorldCapital? capital = plan.GetCapital(playerId);

            if (capital is null)
            {
                return null;
            }

            WorldPlayer player = state.Players[playerId];
            WorldRaceDefinition race = Context.Engine.Definitions.GetRace(player.Race);
            List<UnitDefinitionId> composition = CollectComposition(plan, capital);
            WorldPurchaseUnitCommand? bestCommand = null;
            int bestScore = 0;

            foreach (UnitDefinitionId unitId in race.Units)
            {
                UnitDefinition unit = Context.Engine.Definitions.GetUnit(unitId);
                GridPosition? position = FindFreePosition(capital.Garrison, unit.IsLarge);

                if (position is null)
                {
                    continue;
                }

                WorldPurchaseUnitCommand command = new(capital.Id, unitId, position);
                WorldCommandResult commandResult = command.Execute(state, Context.Engine);

                if (!commandResult.Failed)
                {
                    int score = ScorePurchase(unit, player, composition);

                    if (score > bestScore)
                    {
                        bestCommand = command;
                        bestScore = score;
                    }
                }
            }

            if (bestCommand is null)
            {
                return null;
            }

            return MakePlan(bestScore, bestCommand, memory);
        }

        private static int ScorePurchase(UnitDefinition unit, WorldPlayer player, List<UnitDefinitionId> composition)
        {
            List<UtilityScore.Consideration> considerations =
            [
                Considerations.Affordability(unit, player),
                Considerations.ReachPreference(unit),
                Considerations.Repetition(unit, composition)
            ];

            return UtilityScore.Combine(considerations);
        }

        private static List<UnitDefinitionId> CollectComposition(WorldPlan plan, WorldCapital capital)
        {
            List<UnitDefinitionId> result = [];

            foreach ((WorldUnit unit, _) in capital.Garrison.GetUnits())
            {
                result.Add(unit.DefinitionId);
            }

            WorldStack? stack = plan.GetStackAt(capital.Entrance);

            if (stack is not null && stack.OwnerId == capital.OwnerId)
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
