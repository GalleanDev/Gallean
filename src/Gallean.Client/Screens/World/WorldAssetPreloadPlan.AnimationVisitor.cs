using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal static partial class WorldAssetPreloadPlan
{
    private sealed class AnimationVisitor(List<AssetPreloadItem> required, List<AssetPreloadItem> optional)
        : IWorldAnimationVisitor<Empty, Empty>, ISpellCastLayerVisitor<Empty, Empty>
    {
        public Empty Visit(StackMove animation, Empty context)
        {
            optional.Add(new AssetPreloadItem.Sound(animation.SoundKey));
            required.Add(new AssetPreloadItem.Sprite(animation.BannerKey));

            foreach (StackMoveStep step in animation.Steps)
            {
                required.Add(new AssetPreloadItem.Animation(step.AssetKey));
                AddStackLayers(step.ShadowKey, step.AuraKey);
            }

            return Empty.Value;
        }

        public Empty Visit(Effect animation, Empty context)
        {
            if (animation.SoundKey is not null)
            {
                optional.Add(new AssetPreloadItem.Sound(animation.SoundKey));
            }

            required.Add(new AssetPreloadItem.Animation(animation.AssetKey));

            return Empty.Value;
        }

        public Empty Visit(SpellCast animation, Empty context)
        {
            optional.Add(new AssetPreloadItem.Sound(animation.SoundKey));

            foreach (SpellCastLayer layer in animation.Layers)
            {
                layer.Accept(this, context);
            }

            return Empty.Value;
        }

        public Empty Visit(SpellCastLayer.Effect layer, Empty context)
        {
            required.Add(new AssetPreloadItem.Animation(layer.AssetKey));

            return Empty.Value;
        }

        public Empty Visit(SpellCastLayer.Summon layer, Empty context)
        {
            required.Add(new AssetPreloadItem.Animation(layer.Stack.AssetKey));
            required.Add(new AssetPreloadItem.Sprite(layer.Stack.BannerKey));
            AddStackLayers(layer.Stack.ShadowKey, layer.Stack.AuraKey);

            return Empty.Value;
        }

        public Empty Visit(Pause animation, Empty context)
        {
            return Empty.Value;
        }

        public Empty Visit(StackBattle animation, Empty context)
        {
            required.Add(new AssetPreloadItem.Animation(animation.AssetKey));

            return Empty.Value;
        }

        public Empty Visit(BattleTransition animation, Empty context)
        {
            return Empty.Value;
        }

        private void AddStackLayers(AssetAnimationKey? shadowKey, AssetAnimationKey? auraKey)
        {
            if (shadowKey is not null)
            {
                required.Add(new AssetPreloadItem.Animation(shadowKey));
            }

            if (auraKey is not null)
            {
                required.Add(new AssetPreloadItem.Animation(auraKey));
            }
        }
    }
}
