using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    private sealed class CaptureTerrainGrowthVisitor : IWorldFortificationVisitor<GlobalVariables, int>, IStaticInstance<CaptureTerrainGrowthVisitor>
    {
        public int Visit(WorldCapital capital, GlobalVariables globals)
        {
            return 0;
        }

        public int Visit(WorldCity city, GlobalVariables globals)
        {
            int growth = GetCityTerrainGrowth(city.DefinitionId.Level, globals);

            return globals.CityCaptureTerrainPercent / 100 * growth;
        }

        public int Visit(WorldRuin ruin, GlobalVariables globals)
        {
            return 0;
        }
    }
}
