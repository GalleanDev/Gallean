namespace Gallean.Engine;

internal sealed class AssassinateThiefActionResolver(WorldThiefAssassinateAction action) : AbstractThiefActionResolver(action.TargetPosition)
{
    protected override AttemptResult AttemptAction(
        WorldState state,
        IEngineContext context,
        WorldStack thief,
        WorldPlayerId player,
        ThiefTarget target)
    {
        if (target is ThiefStackTarget stackTarget)
        {
            return AttemptStack(stackTarget, thief, context);
        }
        else if (target is ThiefFortificationTarget fortificationTarget)
        {
            return AttemptFortification(state, fortificationTarget, thief, context);
        }
        else
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }
    }

    private static AttemptResult AttemptStack(ThiefStackTarget stackTarget, WorldStack thief, IEngineContext context)
    {
        WorldStack stack = stackTarget.Stack;

        WorldUnit? victim = GetVictim(stack.Units, context);

        if (victim is null)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        int aliveCount = 0;

        foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
        {
            if (unit.IsAlive)
            {
                aliveCount++;
            }
        }

        WorldEvent outcome;

        if (aliveCount == 1)
        {
            outcome = new WorldStackDestroyedEvent(stack.Id, new WorldUnitGroup.Stack(thief.Id), thief.OwnerId);
        }
        else
        {
            outcome = new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(stack.Id), victim.Id, 0, thief.OwnerId);
        }

        return AttemptResult.Success(WorldThiefActionType.Assassinate, stack.OwnerId, [outcome]);
    }

    private static AttemptResult AttemptFortification(
        WorldState state,
        ThiefFortificationTarget fortificationTarget,
        WorldStack thief,
        IEngineContext context)
    {
        if (fortificationTarget.Fortification is not WorldCity city)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        WorldPlayer owner = state.Players[fortificationTarget.Owner];

        if (!WorldHelpers.IsRacePlayable(owner.Race))
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        WorldUnit? victim = GetVictim(city.Garrison, context);

        if (victim is null)
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }

        return AttemptResult.Success(
            WorldThiefActionType.Assassinate,
            fortificationTarget.Owner,
            [new WorldUnitDamagedEvent(new WorldUnitGroup.Fortification(city.Id), victim.Id, 0, thief.OwnerId)]);
    }

    private static WorldUnit? GetVictim(UnitGrid<WorldUnit> units, IEngineContext context)
    {
        WorldUnit? soldier = null;
        int soldierExperienceKilled = 0;
        WorldUnit? leader = null;

        foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
        {
            if (!unit.IsAlive)
            {
                continue;
            }

            UnitDefinition definition = context.Definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                leader ??= unit;
            }
            else
            {
                UnitStats stats = context.UnitStats.GetWorldUnitStats(unit);

                if (soldier is null || stats.ExperienceKilled < soldierExperienceKilled)
                {
                    soldier = unit;
                    soldierExperienceKilled = stats.ExperienceKilled;
                }
            }
        }

        return soldier ?? leader;
    }
}
