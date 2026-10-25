using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public abstract partial record AbstractWorldMoveStackCommand(WorldStackId StackId, WorldPosition TargetPosition) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Map.IsValidPosition(TargetPosition))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOutOfBounds);
        }

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, context.Definitions);

        if (!WorldMovementCalculator.IsPassable(state.Map, TargetPosition, profile))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetUnpassable);
        }

        WorldPlan plan = WorldPlan.For(state);
        WorldFortification? currentFortification = plan.GetFortificationAt(stack.Position);

        if (currentFortification is not null && FortificationCalculator.IsRioting(currentFortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        if (plan.GetResourceNodeAt(TargetPosition) is not null)
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetUnpassable);
        }

        WorldFortification? targetFortification = plan.GetFortificationAt(TargetPosition);

        if (targetFortification is not null &&
            FortificationCalculator.GetOwner(targetFortification) == stack.OwnerId &&
            FortificationCalculator.IsRioting(targetFortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        WorldStack? targetStack = plan.GetStackAt(TargetPosition);

        if (targetStack is not null && !DiplomacyCalculator.IsHostile(state, stack.OwnerId, targetStack.OwnerId))
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOccupied);
        }

        StackStats stackStats = context.UnitStats.GetWorldStackStats(stack);
        WorldMovementLimit limit = new(stack.MovementPoints, stackStats.MaxMovementPoints, TurnLimit: 1);

        ImmutableBitmap? fogOfWar = GetFogOfWar(state, stack);
        WorldPath? path = plan.GetPath(stack.Position, TargetPosition, fogOfWar, profile, limit);
        if (path is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.PathNotFound);
        }

        PathTraversalCalculator.TraversalInfo traversal = PathTraversalCalculator.Calculate(state, stack, path, profile, context.Definitions);
        ImmutableArray<WorldPosition> approach = traversal.Path.Positions;

        if (approach.IsEmpty)
        {
            if (traversal.Encounter is null)
            {
                return WorldCommandResult.Fail(WorldCommandError.InsufficientMovement);
            }

            return traversal.Encounter.Accept(EncounterVisitor.Instance, new EncounterVisitor.Context(state, stack, context));
        }

        WorldFortification? enteredFortification = plan.GetFortificationAt(approach[^1]);

        if (enteredFortification is not null &&
            FortificationCalculator.GetOwner(enteredFortification) is { } enteredOwner &&
            DiplomacyCalculator.IsHostile(state, stack.OwnerId, enteredOwner) &&
            !FortificationCalculator.CanCapture(stack, context.Definitions))
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationCaptureForbidden);
        }

        WorldPosition? encounterPosition;

        if (traversal.Encounter is PathTraversalCalculator.Encounter.Battle { CanBegin: false })
        {
            encounterPosition = null;
        }
        else
        {
            encounterPosition = traversal.Encounter?.Position;
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [WorldCommandHelpers.CreateMoveStepEvent(stack, plan, profile, approach, encounterPosition)]);
    }

    private protected abstract ImmutableBitmap? GetFogOfWar(WorldState state, WorldStack stack);
}
