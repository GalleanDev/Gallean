using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace Gallean.Client;

internal sealed partial class VideoVisual
{
    private sealed class RenderOperation(
        Rect destination,
        Rect source,
        FrameBuffer frameBuffer,
        int frameIndex,
        double grain) : ICustomDrawOperation
    {
        public Rect Bounds => destination;

        public bool HitTest(Point point)
        {
            return false;
        }

        public bool Equals(ICustomDrawOperation? other)
        {
            return ReferenceEquals(this, other);
        }

        public void Dispose()
        {
        }

        public void Render(ImmediateDrawingContext context)
        {
            using ISkiaSharpApiLease lease = context.GetSkiaApiLease();

            byte alpha = (byte)Math.Clamp(Math.Round(lease.CurrentOpacity * byte.MaxValue), byte.MinValue, byte.MaxValue);

            frameBuffer.Draw(lease.SkCanvas, source.ToSKRect(), destination.ToSKRect(), alpha, frameIndex, (float)grain);
        }
    }
}
