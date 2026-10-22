using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Gallean.Client;

internal static partial class AvaloniaExtensions
{
    private sealed class DirectRenderTarget(IRenderTarget inner) : IRenderTarget
    {
        public RenderTargetProperties Properties => new()
        {
            IsSuitableForDirectRendering = true,
            RetainsPreviousFrameContents = true
        };

        public PlatformRenderTargetState PlatformRenderTargetState => inner.PlatformRenderTargetState;

        public IDrawingContextImpl CreateDrawingContext(IRenderTarget.RenderTargetSceneInfo sceneInfo, out RenderTargetDrawingContextProperties properties)
        {
            IDrawingContextImpl context = inner.CreateDrawingContext(sceneInfo, out properties);

            if (sceneInfo.TransparencyLevel == CompositionTransparencyLevel.None && context is IDrawingContextImplWithEffects effects)
            {
                return new DirectRenderDrawingContext(effects);
            }
            else
            {
                return context;
            }
        }

        public void Dispose() => inner.Dispose();
    }
}
