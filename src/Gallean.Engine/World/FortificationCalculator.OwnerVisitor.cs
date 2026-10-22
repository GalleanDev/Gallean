using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    private sealed class OwnerVisitor : IWorldFortificationVisitor<Empty, WorldPlayerId?>, IStaticInstance<OwnerVisitor>
    {
        public WorldPlayerId? Visit(WorldCapital capital, Empty context)
        {
            if (capital.CapturedById is null)
            {
                return capital.OwnerId;
            }
            else
            {
                return null;
            }
        }

        public WorldPlayerId? Visit(WorldCity city, Empty context)
        {
            return city.OwnerId;
        }

        public WorldPlayerId? Visit(WorldRuin ruin, Empty context)
        {
            return null;
        }
    }
}
