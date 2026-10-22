using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    private sealed class TerrainGrowthVisitor : IWorldFortificationVisitor<GlobalVariables, int>, IStaticInstance<TerrainGrowthVisitor>
    {
        public int Visit(WorldCapital capital, GlobalVariables globals)
        {
            return globals.CapitalTerrainTilesPerTurn;
        }

        public int Visit(WorldCity city, GlobalVariables globals)
        {
            return GetCityTerrainGrowth(city.DefinitionId.Level, globals);
        }

        public int Visit(WorldRuin ruin, GlobalVariables globals)
        {
            return 0;
        }
    }
}
