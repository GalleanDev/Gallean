using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private abstract partial record AnimationRenderItem
    {
        public sealed record StackBattle(
            AssetAnimationKey AssetKey,
            int FrameCount,
            WorldPosition Position,
            int FrameIndex,
            int Depth) : AnimationRenderItem(Depth)
        {
            public override void Render(
                SKCanvas canvas,
                SKPaint atlasPaint,
                WorldHighlightShaderProgram highlightShaderProgram,
                IAssetProvider assets,
                int loopFrameIndex,
                Rect visible)
            {
                DrawEffect(canvas, atlasPaint, assets, AssetKey, FrameCount, Position, FrameIndex, visible);
            }

            public override void RenderGhost(SKCanvas canvas, IAssetProvider assets, int loopFrameIndex, Rect visible)
            {
                using SKPaint paint = SkiaHelpers.MakeImagePaint();
                paint.BlendMode = SKBlendMode.DstOut;

                DrawEffect(canvas, paint, assets, AssetKey, FrameCount, Position, FrameIndex, visible);
            }
        }
    }
}
