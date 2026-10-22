using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private sealed class SpellCastLayerRenderItemVisitor(WorldPosition position, int frameIndex)
        : ISpellCastLayerVisitor<Empty, AnimationRenderItem?>
    {
        public AnimationRenderItem? Visit(SpellCastLayer.Effect layer, Empty context)
        {
            return new AnimationRenderItem.Effect(layer.AssetKey, layer.FrameCount, position, frameIndex, layer.Depth);
        }

        public AnimationRenderItem? Visit(SpellCastLayer.Summon layer, Empty context)
        {
            if (frameIndex >= layer.RevealFrame)
            {
                Stack stack = layer.Stack;

                return new AnimationRenderItem.StackObject(
                    WorldTransform.ToWorldPoint(stack.Position),
                    stack.AssetKey,
                    stack.ShadowKey,
                    stack.AuraKey,
                    stack.BannerKey,
                    stack.Opacity,
                    false,
                    null,
                    stack.Depth);
            }
            else
            {
                return null;
            }
        }
    }
}
