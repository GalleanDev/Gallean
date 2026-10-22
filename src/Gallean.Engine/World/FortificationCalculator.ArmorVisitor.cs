using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    private sealed class ArmorVisitor : IWorldFortificationVisitor<GlobalVariables, int>, IStaticInstance<ArmorVisitor>
    {
        public int Visit(WorldCapital capital, GlobalVariables globals)
        {
            return globals.CapitalProtection;
        }

        public int Visit(WorldCity city, GlobalVariables globals)
        {
            return GetCityArmorBonus(city.DefinitionId.Level, globals);
        }

        public int Visit(WorldRuin ruin, GlobalVariables globals)
        {
            return 0;
        }
    }
}
