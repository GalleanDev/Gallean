using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    private sealed class VisionRadiusVisitor : IWorldFortificationVisitor<IEngineContext, int>, IStaticInstance<VisionRadiusVisitor>
    {
        public int Visit(WorldCapital capital, IEngineContext context)
        {
            return context.Globals.CapitalVisionRadius;
        }

        public int Visit(WorldCity city, IEngineContext context)
        {
            return context.Definitions.GetCity(city.DefinitionId).VisionRadius;
        }

        public int Visit(WorldRuin ruin, IEngineContext context)
        {
            return 0;
        }
    }
}
