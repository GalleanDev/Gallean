namespace Gallean.Engine;

internal static class GarrisonCalculator
{
    public static int GetTotalCapacity(WorldFortification fortification, GlobalVariables globals)
    {
        return fortification switch
        {
            WorldCapital => globals.CapitalCityLevel,
            WorldCity city => city.DefinitionId.Level,
            WorldRuin => globals.RuinGarrisonSize
        };
    }

    public static int GetOccupiedCapacity(UnitGrid<WorldUnit> units, IGameDefinitionProvider definitions)
    {
        int result = 0;

        foreach ((WorldUnit unit, _) in units.GetUnits())
        {
            result += GetUnitCapacity(definitions.GetUnit(unit.DefinitionId));
        }

        return result;
    }

    private static int GetUnitCapacity(UnitDefinition definition)
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
