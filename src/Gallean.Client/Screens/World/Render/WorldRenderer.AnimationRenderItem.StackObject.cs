using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private abstract partial record AnimationRenderItem
    {
        public sealed record StackObject(
            Point Center,
            AssetAnimationKey AssetKey,
            AssetAnimationKey? ShadowKey,
            AssetAnimationKey? AuraKey,
            AssetSpriteKey BannerKey,
            double Opacity,
            bool IsObjective,
            Color? Highlight,
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
                if (IsObjective)
                {
                    DrawStackObjectiveMarker(canvas, Center, visible);
                }

                DrawStackObject(canvas, assets, Center, AssetKey, ShadowKey, AuraKey, BannerKey, Opacity, loopFrameIndex, visible);

                if (Highlight is not null)
                {
                    Rect body = GetStackBodyBounds(assets, Center, AssetKey, BannerKey);

                    DrawHighlight(
                        canvas,
                        highlightShaderProgram,
                        Highlight.Value,
                        body.Inflate(HighlightGlowRadius),
                        (bodyCanvas, paint) =>
                        {
                            DrawStackBody(
                                bodyCanvas,
                                paint,
                                assets,
                                Center,
                                AssetKey,
                                BannerKey,
                                loopFrameIndex,
                                visible);
                        });
                }
            }

            public override void RenderGhost(SKCanvas canvas, IAssetProvider assets, int loopFrameIndex, Rect visible)
            {
                DrawStackGhost(canvas, assets, Center, AssetKey, BannerKey, Opacity, loopFrameIndex, visible);
            }
        }
    }
}
