using Gallean.Common;

namespace Gallean.Engine;

internal static class GarrisonCalculator
{
    private sealed class CapacityVisitor : IWorldFortificationVisitor<GlobalVariables, int>, IStaticInstance<CapacityVisitor>
    {
        public int Visit(WorldCapital capital, GlobalVariables globals) => globals.CapitalCityLevel;
        public int Visit(WorldCity city, GlobalVariables globals) => city.DefinitionId.Level;
        public int Visit(WorldRuin ruin, GlobalVariables globals) => globals.RuinGarrisonSize;
    }

    public static int GetTotalCapacity(WorldFortification fortification, GlobalVariables globals)
    {
        return fortification.Accept(CapacityVisitor.Instance, globals);
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
