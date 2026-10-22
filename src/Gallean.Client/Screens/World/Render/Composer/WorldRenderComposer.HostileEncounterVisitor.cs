using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private sealed class HostileEncounterVisitor : IWorldPathEncounterVisitor<Empty, WorldPosition?>
    {
        public static readonly HostileEncounterVisitor Instance = new();

        public WorldPosition? Visit(WorldPathEncounter.Battle encounter, Empty context)
        {
            return encounter.TargetPosition;
        }

        public WorldPosition? Visit(WorldPathEncounter.Thief encounter, Empty context)
        {
            return encounter.TargetPosition;
        }

        public WorldPosition? Visit(WorldPathEncounter.Bag encounter, Empty context)
        {
            return null;
        }

        public WorldPosition? Visit(WorldPathEncounter.Site encounter, Empty context)
        {
            return null;
        }
    }
}
