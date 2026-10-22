using Gallean.Common;

namespace Gallean.Engine;

internal abstract partial class AbstractThiefActionResolver(WorldPosition targetPosition) : IWorldThiefActionResolver
{
    public WorldCommandResult Resolve(WorldState state, IEngineContext context, WorldStack thief, WorldPlayerId player)
    {
        ThiefTarget? target = ThiefCalculator.ResolveTarget(state, context, player, targetPosition);

        if (target is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        if (!ThiefCalculator.CanReachTarget(thief, target))
        {
            return WorldCommandResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        AttemptResult attempt = AttemptAction(state, context, thief, player, target);

        if (attempt.Error is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        LordDefinition lord = context.Definitions.GetLord(state.Players[player].LordId);

        if (!ThiefCalculator.IsActionAllowed(attempt.Type, lord))
        {
            return WorldCommandResult.Fail(WorldCommandError.ThiefActionNotAllowed);
        }

        WorldThiefActionDefinition definition = context.Definitions.GetThiefAction(attempt.Type);
        WorldStack? targetStack = target is ThiefStackTarget stackTarget ? stackTarget.Stack : null;
        bool succeeded;
        WorldStack? destroyer;

        if (targetStack is not null && ThiefCalculator.IsIncorruptible(targetStack, context))
        {
            succeeded = false;
            destroyer = targetStack;
        }
        else
        {
            succeeded = ThiefCalculator.RollSuccess(context.Random, definition, target.Protection);
            destroyer = null;
        }

        if (succeeded)
        {
            StackStats stackStats = context.UnitStats.GetWorldStackStats(thief);
            int actionCost = WorldMovementCalculator.GetActionCost(stackStats, context.Globals);
            int movementPoints = Math.Max(0, thief.MovementPoints - actionCost);

            return WorldCommandResult.Success(
                Empty.Value,
                [
                    new WorldThiefActionSucceededEvent(thief.Id, attempt.Type, attempt.TargetPlayerId),
                    .. attempt.Events,
                    new WorldStackMovementConsumedEvent(thief.Id, movementPoints)
                ]);
        }
        else
        {
            WorldUnitGroup? destroyerGroup = destroyer is null ? null : new WorldUnitGroup.Stack(destroyer.Id);

            return WorldCommandResult.Success(
                Empty.Value,
                [
                    new WorldThiefActionFailedEvent(thief.Id, attempt.Type, attempt.TargetPlayerId),
                    new WorldStackDestroyedEvent(thief.Id, destroyerGroup, destroyer?.OwnerId)
                ]);
        }
    }

    protected abstract AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target);
}
