using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class VideoVisual
{
    private sealed class RenderOperation(Rect destination, Rect source, FrameBuffer frameBuffer) : ICustomDrawOperation
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
            using SKPaint paint = new();

            byte alpha = (byte)Math.Clamp(Math.Round(lease.CurrentOpacity * byte.MaxValue), byte.MinValue, byte.MaxValue);
            paint.Color = new SKColor(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);

            frameBuffer.Draw(lease.SkCanvas, source.ToSKRect(), destination.ToSKRect(), paint);
        }
    }
}
