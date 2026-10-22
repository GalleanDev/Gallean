using Avalonia;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;

namespace Gallean.Client;

internal static partial class AvaloniaExtensions
{
    private sealed class DirectRenderContext(IPlatformRenderInterfaceContext inner) : IPlatformRenderInterfaceContext
    {
        public bool IsLost => inner.IsLost;
        public IReadOnlyDictionary<Type, object> PublicFeatures => inner.PublicFeatures;
        public PixelSize? MaxOffscreenRenderTargetPixelSize => inner.MaxOffscreenRenderTargetPixelSize;

        public IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) => new DirectRenderTarget(inner.CreateRenderTarget(surfaces));
        public IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, Vector scaling, bool enableTextAntialiasing) => inner.CreateOffscreenRenderTarget(pixelSize, scaling, enableTextAntialiasing);
        public bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) => inner.IsReadyToCreateRenderTarget(surfaces);
        public object? TryGetFeature(Type featureType) => inner.TryGetFeature(featureType);
        public void Dispose() => inner.Dispose();
    }
}
