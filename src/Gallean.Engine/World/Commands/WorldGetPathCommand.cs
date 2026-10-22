using Gallean.Common;
using CommandResult = Gallean.Engine.ICommandResult<Gallean.Engine.WorldCommandError, Gallean.Engine.WorldEvent, Gallean.Engine.WorldGetPathCommand.Result>;

namespace Gallean.Engine;

public sealed partial record WorldGetPathCommand(WorldStackId StackId, WorldPosition TargetPosition) : IWorldCommand<WorldGetPathCommand.Result>
{
    public sealed record Result(WorldPath Path, WorldPath Remainder, WorldPathEncounter? Encounter);

    public CommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return CommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Map.IsValidPosition(TargetPosition))
        {
            return CommandResult.Fail(WorldCommandError.TargetOutOfBounds);
        }

        WorldPlan plan = WorldPlan.For(state);
        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, context.Definitions);
        StackStats stackStats = context.UnitStats.GetWorldStackStats(stack);
        WorldMovementLimit limit = new(stack.MovementPoints, stackStats.MaxMovementPoints, TurnLimit: int.MaxValue);

        WorldPlayer player = state.Players[stack.OwnerId];
        WorldPath? path = plan.GetPath(stack.Position, TargetPosition, player.FogOfWar, profile, limit);
        if (path is null)
        {
            return CommandResult.Fail(WorldCommandError.PathNotFound);
        }

        PathTraversalCalculator.TraversalInfo traversal = PathTraversalCalculator.Calculate(state, stack, path, profile, context.Definitions);
        WorldPathEncounter? encounter;

        if (traversal.Encounter is null)
        {
            encounter = GetTargetEncounter(state, stack, plan, profile, context);
        }
        else
        {
            encounter = traversal.Encounter.Accept(EncounterVisitor.Instance, new EncounterVisitor.Context(state, stack, profile, context));
        }

        return CommandResult.Success(new Result(traversal.Path, traversal.Remainder, encounter), []);
    }

    private WorldPathEncounter? GetTargetEncounter(
        WorldState state,
        WorldStack stack,
        WorldPlan plan,
        WorldMovementCalculator.MovementProfile profile,
        IEngineContext context)
    {
        WorldSite? site = plan.GetSiteAt(TargetPosition);

        if (profile.IsThief && ThiefCalculator.ResolveTarget(state, context, stack.OwnerId, TargetPosition) is not null)
        {
            return new WorldPathEncounter.Thief(TargetPosition);
        }
        else if (site is not null)
        {
            return new WorldPathEncounter.Site(site.Id);
        }
        else
        {
            return null;
        }
    }
}
