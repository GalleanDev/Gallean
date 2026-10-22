using System.Collections.Immutable;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Rods
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldRodId, WorldRod> Rods,
            PlayerRaces PlayerRaces,
            WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input)
        {
            List<MapObject> objects = [];

            foreach (WorldRod rod in input.Rods.Values)
            {
                if (IsObjectVisible(input.Visibility, rod.Position))
                {
                    objects.Add(new Rod(
                        rod.Position,
                        GetDepth(rod.Position, Layer.Rods),
                        new AssetSpriteKey.Rod(input.PlayerRaces[rod.OwnerId])));
                }
            }

            return objects;
        }
    }
}
