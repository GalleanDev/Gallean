namespace Gallean.Engine;

internal sealed class DuelThiefActionResolver(WorldThiefDuelAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        if (target is not ThiefStackTarget stackTarget)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        (WorldUnit thiefLeader, GridPosition _, UnitDefinition _) = thief.GetLeader(context.Definitions);

        if (!thiefLeader.IsAlive)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefLeaderRequired);
        }

        WorldStack defenderStack = stackTarget.Stack;
        (WorldUnit defenderLeader, GridPosition _, UnitDefinition _) = defenderStack.GetLeader(context.Definitions);

        if (!defenderLeader.IsAlive)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        BattleModifiers modifiers = WorldCommandHelpers.CreateModifiers(state, thief.OwnerId, defenderStack.OwnerId, context);
        BattleState battleState = WorldCommandHelpers.CreateDuelBattleState(thief, defenderStack, modifiers, context);

        return AttemptResult.Success(
            WorldThiefActionType.Duel,
            defenderStack.OwnerId,
            [new WorldStackBattleStartedEvent(thief.Id, defenderStack.Id, defenderStack.Position, battleState, IsDuel: true)]);
    }
}
