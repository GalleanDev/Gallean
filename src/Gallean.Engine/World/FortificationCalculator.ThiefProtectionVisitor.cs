using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    private sealed class ThiefProtectionVisitor : IWorldFortificationVisitor<GlobalVariables, int>, IStaticInstance<ThiefProtectionVisitor>
    {
        public int Visit(WorldCapital capital, GlobalVariables globals)
        {
            return GetCityThiefProtection(globals.CapitalCityLevel, globals);
        }

        public int Visit(WorldCity city, GlobalVariables globals)
        {
            return GetCityThiefProtection(city.DefinitionId.Level, globals);
        }

        public int Visit(WorldRuin ruin, GlobalVariables globals)
        {
            return 0;
        }
    }
}
