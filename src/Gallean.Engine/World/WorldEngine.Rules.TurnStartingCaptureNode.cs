namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartingCaptureNodeRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartingEvent ev, WorldState state)
        {
            List<WorldEvent> events = [];
            Dictionary<WorldMapTerrain, WorldPlayer> playerTerrains = [];

            foreach (WorldPlayer player in state.Players.Values)
            {
                if (WorldHelpers.IsRacePlayable(player.Race))
                {
                    WorldRaceDefinition race = Context.Definitions.GetRace(player.Race);
                    playerTerrains.TryAdd(race.NativeTerrain, player);
                }
            }

            foreach (WorldResourceNode node in state.ResourceNodes.Values)
            {
                WorldMapTerrain cellTerrain = state.Map.GetCellAt(node.Position).Terrain;

                if (playerTerrains.TryGetValue(cellTerrain, out WorldPlayer? player) && node.OwnerId != player.Id)
                {
                    events.Add(new WorldResourceNodeCapturedEvent(node.Id, player.Id));
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }
    }
}
