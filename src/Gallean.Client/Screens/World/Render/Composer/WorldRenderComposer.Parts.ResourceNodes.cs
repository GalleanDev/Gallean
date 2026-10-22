using System.Collections.Immutable;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class ResourceNodes
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> ResourceNodes,
            WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input)
        {
            List<MapObject> objects = [];

            foreach (WorldResourceNode node in input.ResourceNodes.Values)
            {
                if (IsObjectVisible(input.Visibility, node.Position))
                {
                    objects.Add(new ResourceNode(
                        node.Position,
                        GetDepth(node.Position, Layer.Crystals),
                        new AssetAnimationKey.Crystal(node.ResourceType)));
                }
            }

            return objects;
        }
    }
}
