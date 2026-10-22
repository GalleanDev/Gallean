namespace Gallean.Engine;

internal sealed class PoisonThiefActionResolver(WorldThiefPoisonAction action) : AbstractThiefActionResolver(action.TargetPosition)
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
            WorldStack stack = stackTarget.Stack;

            if (!CanPoison(stack.Units))
            {
                return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
            }

            UnitGrid<WorldUnit> poisoned = Poison(stack.Units, context.Globals.PoisonStackDamagePercent);

            return AttemptResult.Success(
                WorldThiefActionType.PoisonStack,
                stack.OwnerId,
                [new WorldStackPoisonedEvent(stack.Id, poisoned)]);
        }
        else if (target is ThiefFortificationTarget fortificationTarget)
        {
            WorldFortification fortification = fortificationTarget.Fortification;

            if (!CanPoison(fortification.Garrison))
            {
                return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
            }

            UnitGrid<WorldUnit> poisoned = Poison(fortification.Garrison, context.Globals.PoisonCityDamagePercent);

            return AttemptResult.Success(
                WorldThiefActionType.PoisonCity,
                fortificationTarget.Owner,
                [new WorldFortificationPoisonedEvent(fortification.Id, poisoned)]);
        }
        else
        {
            return AttemptResult.Fail(WorldCommandError.ThiefTargetInvalid);
        }
    }

    private static bool CanPoison(UnitGrid<WorldUnit> units)
    {
        foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
        {
            if (unit.Hitpoints > 1)
            {
                return true;
            }
        }

        return false;
    }

    private static UnitGrid<WorldUnit> Poison(UnitGrid<WorldUnit> units, int damagePercent)
    {
        UnitGrid<WorldUnit>.Builder builder = new();

        foreach ((WorldUnit unit, GridPosition position) in units.GetUnits())
        {
            WorldUnit result = unit;

            if (unit.IsAlive)
            {
                int damage = unit.Hitpoints * damagePercent / 100;
                result = unit with { Hitpoints = unit.Hitpoints - damage };
            }

            builder.Place(result, position, units.IsLargeAt(position));
        }

        return builder.Build();
    }
}
