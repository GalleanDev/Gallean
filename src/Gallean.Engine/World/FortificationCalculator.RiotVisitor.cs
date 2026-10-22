using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    private sealed class RiotVisitor : IWorldFortificationVisitor<Empty, bool>, IStaticInstance<RiotVisitor>
    {
        public bool Visit(WorldCapital capital, Empty context)
        {
            return false;
        }

        public bool Visit(WorldCity city, Empty context)
        {
            return city.RiotTurns > 0;
        }

        public bool Visit(WorldRuin ruin, Empty context)
        {
            return false;
        }
    }
}
