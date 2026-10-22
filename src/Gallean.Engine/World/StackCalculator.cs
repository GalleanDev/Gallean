namespace Gallean.Engine;

internal static class StackCalculator
{
    public static int GetTotalLeadership(UnitGrid<WorldUnit> units, IGameDefinitionProvider definitions)
    {
        foreach ((WorldUnit unit, _) in units.GetUnits())
        {
            UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                return GetLeaderLeadership(unit, definition, definitions);
            }
        }

        return 0;
    }

    public static int GetOccupiedLeadership(UnitGrid<WorldUnit> units, IGameDefinitionProvider definitions)
    {
        int result = 0;

        foreach ((WorldUnit unit, _) in units.GetUnits())
        {
            result += GetUnitLeadership(definitions.GetUnit(unit.DefinitionId));
        }

        return result;
    }

    private static int GetLeaderLeadership(WorldUnit leader, UnitDefinition definition, IGameDefinitionProvider definitions)
    {
        int leadership = UnitStatsCalculator.GetLeadership(definition, leader.Modifiers, definitions);

        if (definition.IsLarge)
        {
            return Math.Max(leadership, GridPosition.LineCount);
        }
        else
        {
            return leadership;
        }
    }

    private static int GetUnitLeadership(UnitDefinition definition)
    {
        if (definition.IsLarge)
        {
            return GridPosition.LineCount;
        }
        else
        {
            return 1;
        }
    }
}
