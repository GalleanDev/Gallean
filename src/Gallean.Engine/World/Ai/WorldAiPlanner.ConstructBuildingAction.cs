namespace Gallean.Engine;

internal sealed partial class WorldAiPlanner
{
    private sealed record ConstructBuildingAction(WorldAiContext Context, int Priority) : AbstractEconomyAction(Context, Priority)
    {
        private const int MaxBuildingsPerTurn = 1;

        public override WorldAiActionPlan? Plan(WorldState state, WorldPlayerId playerId, WorldAiMemory memory)
        {
            if (memory.Turn.BuildingsConstructed >= MaxBuildingsPerTurn)
            {
                return null;
            }

            WorldPlayer player = state.Players[playerId];

            foreach (WorldPlayerBuildingDefinitionId buildingId in player.Buildings.Available.OrderBy(GetBuildingOrder))
            {
                WorldConstructBuildingCommand command = new(playerId, buildingId);

                if (!command.Execute(state, Context.Engine).Failed)
                {
                    WorldPlayerBuildingDefinition building = GetBuildingDefinition(buildingId);
                    int score = UtilityScore.InverseRatio(building.Cost.Gold, player.Resources.Gold).Score;

                    return MakePlan(score, command, memory);
                }
            }

            return null;
        }

        private WorldPlayerBuildingDefinition GetBuildingDefinition(WorldPlayerBuildingDefinitionId id)
        {
            return Context.Engine.Definitions.GetPlayerBuilding(id);
        }

        private int GetBuildingOrder(WorldPlayerBuildingDefinitionId id)
        {
            WorldPlayerBuildingDefinition building = GetBuildingDefinition(id);

            return building.Category switch
            {
                WorldPlayerBuildingCategory.Heal => 0,
                WorldPlayerBuildingCategory.Guild => 1,
                WorldPlayerBuildingCategory.Unit => 2,
                WorldPlayerBuildingCategory.Spell => 3
            };
        }
    }
}
