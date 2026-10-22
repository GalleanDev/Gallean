using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ThiefCalculator
{
    private sealed class ReachVisitor : IThiefTargetVisitor<WorldStack, bool>, IStaticInstance<ReachVisitor>
    {
        public bool Visit(ThiefStackTarget target, WorldStack thief)
        {
            return thief.Position.IsAdjacentTo(target.Stack.Position);
        }

        public bool Visit(ThiefFortificationTarget target, WorldStack thief)
        {
            return thief.Position.IsAdjacentTo(target.Fortification.Entrance);
        }

        public bool Visit(ThiefMerchantSiteTarget target, WorldStack thief)
        {
            return thief.Position.IsAdjacentTo(target.Site.Entrance);
        }

        public bool Visit(ThiefMageSiteTarget target, WorldStack thief)
        {
            return thief.Position.IsAdjacentTo(target.Site.Entrance);
        }

        public bool Visit(ThiefRuinTarget target, WorldStack thief)
        {
            return thief.Position.IsAdjacentTo(target.Ruin.Entrance);
        }
    }
}
