using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldCreateRodCommand(
    WorldStackId StackId,
    WorldPosition TargetPosition) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId currentPlayer = state.TurnInfo.CurrentPlayerId;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        (WorldUnit leader, _, _) = stack.GetLeader(context.Definitions);
        LeaderAbility leaderAbilities = context.UnitStats.GetWorldUnitAbilities(leader);

        if (!leaderAbilities.HasFlag(LeaderAbility.Rod))
        {
            return WorldCommandResult.Fail(WorldCommandError.RodLeaderRequired);
        }

        if (!leader.IsAlive)
        {
            return WorldCommandResult.Fail(WorldCommandError.RodLeaderDead);
        }

        if (stack.MovementPoints <= 0)
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientMovement);
        }

        WorldPlan plan = WorldPlan.For(state);

        if (plan.GetFortificationAt(stack.Position) is WorldCity or WorldCapital)
        {
            return WorldCommandResult.Fail(WorldCommandError.RodInvalidLocation);
        }

        WorldPlayer player = state.CurrentPlayer;

        if (!ResourceSetCalculator.CanAfford(player.Resources, context.Globals.RodCost))
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientResources);
        }

        if (!state.Map.IsValidPosition(TargetPosition))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOutOfBounds);
        }

        if (stack.Position.GetDistance(TargetPosition) > context.Globals.RodPlantRange)
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetTooFar);
        }

        foreach (WorldRod rod in state.Rods.Values)
        {
            if (rod.Position.GetDistance(TargetPosition) <= context.Globals.RodSpacingRange)
            {
                return WorldCommandResult.Fail(WorldCommandError.RodInvalidLocation);
            }
        }

        WorldMapCell cell = state.Map.GetCellAt(TargetPosition);

        if (cell.Terrain == WorldMapTerrain.Water || cell.Feature == WorldMapFeature.Mountain)
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetUnpassable);
        }

        if (plan.HasStructureAt(TargetPosition) ||
            plan.GetStackAt(TargetPosition) is not null ||
            plan.GetBagAt(TargetPosition) is not null)
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOccupied);
        }

        WorldPlayer? landOwner = FindLandOwner(state, cell.Terrain);

        if (landOwner is not null && landOwner.Id != currentPlayer)
        {
            WorldRelation relation = DiplomacyCalculator.GetRelation(state, currentPlayer, landOwner.Id);

            if (relation.Alliance)
            {
                return WorldCommandResult.Fail(WorldCommandError.RodInvalidLocation);
            }
        }

        WorldRodId rodId = GenerateRodId(state);

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldRodCreatedEvent(rodId, currentPlayer, TargetPosition), new WorldStackMovementConsumedEvent(StackId, 0)]);
    }

    private static WorldPlayer? FindLandOwner(WorldState state, WorldMapTerrain terrain)
    {
        foreach (WorldPlayer player in state.Players.Values)
        {
            if (WorldHelpers.IsRacePlayable(player.Race) && WorldHelpers.GetNativeTerrain(player.Race) == terrain)
            {
                return player;
            }
        }

        return null;
    }

    private static WorldRodId GenerateRodId(WorldState state)
    {
        int index = 1;

        while (state.Rods.ContainsKey(new WorldRodId($"Rod{index:D4}")))
        {
            index++;
        }

        return new WorldRodId($"Rod{index:D4}");
    }
}
