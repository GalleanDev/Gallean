using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ThiefCalculator
{
    private sealed class UnitGroupVisitor : IThiefTargetVisitor<Empty, WorldUnitGroup?>, IStaticInstance<UnitGroupVisitor>
    {
        public WorldUnitGroup? Visit(ThiefStackTarget target, Empty context)
        {
            return new WorldUnitGroup.Stack(target.Stack.Id);
        }

        public WorldUnitGroup? Visit(ThiefFortificationTarget target, Empty context)
        {
            return new WorldUnitGroup.Fortification(target.Fortification.Id);
        }

        public WorldUnitGroup? Visit(ThiefMerchantSiteTarget target, Empty context)
        {
            return null;
        }

        public WorldUnitGroup? Visit(ThiefMageSiteTarget target, Empty context)
        {
            return null;
        }

        public WorldUnitGroup? Visit(ThiefRuinTarget target, Empty context)
        {
            return null;
        }
    }
}
