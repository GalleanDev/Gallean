using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private abstract partial record AnimationRenderItem(int Depth)
    {
        public abstract void Render(
            SKCanvas canvas,
            SKPaint atlasPaint,
            WorldHighlightShaderProgram highlightShaderProgram,
            IAssetProvider assets,
            int loopFrameIndex,
            Rect visible);

        public abstract void RenderGhost(SKCanvas canvas, IAssetProvider assets, int loopFrameIndex, Rect visible);
    }
}
