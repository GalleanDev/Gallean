using Avalonia;
using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private sealed class AnimationRenderItemVisitor : IWorldAnimationVisitor<TimeSpan, IReadOnlyList<AnimationRenderItem>>
    {
        public IReadOnlyList<AnimationRenderItem> Visit(StackMove animation, TimeSpan animationTime)
        {
            (StackMoveStep from, StackMoveStep to, double offset) = ResolveStackMove(animation, animationTime);

            Point fromPoint = WorldTransform.ToWorldPoint(from.Position);
            Point toPoint = WorldTransform.ToWorldPoint(to.Position);
            Point center = new(double.Lerp(fromPoint.X, toPoint.X, offset), double.Lerp(fromPoint.Y, toPoint.Y, offset));

            StackMoveStep step = offset < 0.5 ? from : to;
            int depth = (int)Math.Floor(from.Depth + ((to.Depth - from.Depth) * offset));

            return
            [
                new AnimationRenderItem.StackObject(
                    center,
                    step.AssetKey,
                    step.ShadowKey,
                    step.AuraKey,
                    animation.BannerKey,
                    animation.Opacity,
                    animation.IsObjective,
                    animation.Highlight,
                    depth)
            ];
        }

        public IReadOnlyList<AnimationRenderItem> Visit(Effect animation, TimeSpan animationTime)
        {
            int frameIndex = AnimationRate.Sprites.GetFrameIndex(animationTime);

            return [new AnimationRenderItem.Effect(animation.AssetKey, animation.FrameCount, animation.Position, frameIndex, animation.Depth)];
        }

        public IReadOnlyList<AnimationRenderItem> Visit(SpellCast animation, TimeSpan animationTime)
        {
            List<AnimationRenderItem> items = [];
            SpellCastLayerRenderItemVisitor visitor = new(animation.Position, AnimationRate.Sprites.GetFrameIndex(animationTime));

            foreach (SpellCastLayer layer in animation.Layers)
            {
                AnimationRenderItem? item = layer.Accept(visitor, Empty.Value);

                if (item is not null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        public IReadOnlyList<AnimationRenderItem> Visit(Pause animation, TimeSpan animationTime)
        {
            return [];
        }

        public IReadOnlyList<AnimationRenderItem> Visit(StackBattle animation, TimeSpan animationTime)
        {
            int frameIndex = AnimationRate.Sprites.GetFrameIndex(animationTime).Mod(animation.FrameCount);

            return [new AnimationRenderItem.StackBattle(animation.AssetKey, animation.FrameCount, animation.Position, frameIndex, animation.Depth)];
        }

        public IReadOnlyList<AnimationRenderItem> Visit(BattleTransition animation, TimeSpan animationTime)
        {
            return [];
        }

        private static (StackMoveStep From, StackMoveStep To, double Offset) ResolveStackMove(StackMove stackMove, TimeSpan animationTime)
        {
            IReadOnlyList<StackMoveStep> steps = stackMove.Steps;

            for (int index = 0; index < steps.Count - 1; index++)
            {
                StackMoveStep from = steps[index];
                StackMoveStep to = steps[index + 1];

                if (animationTime < to.Start)
                {
                    return (from, to, animationTime.ToAnimationProgress(from.Start, to.Start - from.Start));
                }
            }

            return (steps[^1], steps[^1], 0);
        }
    }
}
