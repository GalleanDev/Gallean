namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackDestroyedEvent ev, WorldState state)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldStack stack = state.Stacks[ev.StackId];
        WorldMapCell cell = state.Map.GetCellAt(stack.Position);
        (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(Context.Definitions);

        if (plan.GetFortificationAt(stack.Position) is null &&
            leaderDefinition.Category is not (UnitCategory.Summon or UnitCategory.Illusion) &&
            leaderDefinition.LeaderCategory != UnitLeaderCategory.Thief &&
            cell.Terrain != WorldMapTerrain.Water &&
            cell.Feature != WorldMapFeature.Forest)
        {
            WorldTomb tomb = CreateTomb(state, stack, ev);

            state = state with { Tombs = state.Tombs.Add(tomb.Id, tomb) };
        }

        return WorldCommandHelpers.RemoveStack(state, ev.StackId, ev.Destroyer, ev.DestroyerPlayerId, Context.Globals);
    }

    private WorldTomb CreateTomb(WorldState state, WorldStack stack, WorldStackDestroyedEvent ev)
    {
        (WorldUnit leader, _, _) = stack.GetLeader(Context.Definitions);

        WorldUnit? destroyerLeader = GetDestroyerLeader(state, ev.Destroyer);

        return new WorldTomb(
            WorldCommandHelpers.GenerateTombId(state),
            stack.Position,
            stack.OwnerId,
            leader.DefinitionId,
            leader.Name,
            ev.DestroyerPlayerId,
            destroyerLeader?.DefinitionId,
            destroyerLeader?.Name,
            state.TurnInfo.TurnNumber);
    }

    private WorldUnit? GetDestroyerLeader(WorldState state, WorldUnitGroup? unitGroup)
    {
        if (unitGroup is WorldUnitGroup.Stack data)
        {
            WorldStack stack = state.Stacks[data.StackId];
            (WorldUnit leader, _, _) = stack.GetLeader(Context.Definitions);

            return leader;
        }
        else
        {
            return null;
        }
    }
}
