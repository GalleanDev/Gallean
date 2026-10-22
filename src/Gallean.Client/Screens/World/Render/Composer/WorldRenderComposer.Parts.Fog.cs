using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Fog
    {
        public readonly record struct Input(WorldVisibility Visibility);

        public static IReadOnlyList<FogTile> Build(Input input)
        {
            WorldVisibility visibility = input.Visibility;

            if (visibility.Fog is null)
            {
                return [];
            }

            List<FogTile> fogTiles = [];

            for (int y = 0; y < visibility.Height; y++)
            {
                for (int x = 0; x < visibility.Width; x++)
                {
                    WorldPosition position = new(x, y);

                    if (!visibility.IsExplored(position))
                    {
                        fogTiles.Add(new FogTile(position));
                    }
                }
            }

            return fogTiles;
        }
    }
}
