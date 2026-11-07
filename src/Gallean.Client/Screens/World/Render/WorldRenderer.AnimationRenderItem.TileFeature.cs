using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private abstract partial record AnimationRenderItem
    {
        public sealed record TileFeature(
            AssetSpriteKey AssetKey,
            WorldPosition Position,
            double Opacity,
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
                using SKPaint paint = MakeOpacityPaint(Opacity);
                using AssetCache<Sprite>.Lease lease = assets.GetRequiredSprite(AssetKey);

                DrawSprite(canvas, paint, lease.Value, WorldTransform.ToWorldPoint(Position), visible);
            }

            public override void RenderGhost(SKCanvas canvas, IAssetProvider assets, int loopFrameIndex, Rect visible)
            {
            }
        }
    }
}
