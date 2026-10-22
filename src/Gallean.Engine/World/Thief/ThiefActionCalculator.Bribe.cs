namespace Gallean.Engine;

internal static partial class ThiefCalculator
{
    public static WorldResourceSet GetCityBribeCost(WorldCity city, WorldState state, IEngineContext context)
    {
        WorldPlan plan = WorldPlan.For(state);
        WorldStack? stack = plan.GetStackAt(city.Entrance);
        int cost = GetUnitsBribeCost(city.Garrison, context);

        if (stack is not null)
        {
            cost += GetUnitsBribeCost(stack.Units, context);
        }

        return new WorldResourceSet(cost * context.Globals.BribeCostMultiplier * city.DefinitionId.Level, 0, 0, 0, 0, 0);
    }

    private static int GetUnitsBribeCost(UnitGrid<WorldUnit> units, IEngineContext context)
    {
        int cost = 0;

        foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
        {
            UnitStats stats = context.UnitStats.GetWorldUnitStats(unit);

            cost += stats.ExperienceKilled;
        }

        return cost;
    }
}
