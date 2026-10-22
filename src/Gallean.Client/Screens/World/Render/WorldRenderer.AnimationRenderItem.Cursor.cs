using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private abstract partial record AnimationRenderItem
    {
        public sealed record Cursor(
            WorldRenderState.Cursor Value,
            TimeSpan LoopTime) : AnimationRenderItem(Value.Depth)
        {
            public override void Render(
                SKCanvas canvas,
                SKPaint atlasPaint,
                WorldHighlightShaderProgram highlightShaderProgram,
                IAssetProvider assets,
                int loopFrameIndex,
                Rect visible)
            {
                DrawCursor(canvas, Value, WorldTransform.ToWorldPoint(Value.Position), LoopTime, visible);
            }

            public override void RenderGhost(SKCanvas canvas, IAssetProvider assets, int loopFrameIndex, Rect visible)
            {
            }
        }
    }
}
