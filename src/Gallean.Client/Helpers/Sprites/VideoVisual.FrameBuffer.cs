using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class VideoVisual
{
    private sealed class FrameBuffer
    {
        private readonly Lock locker = new();
        private SKImage? image;

        public void Present(SKImage frame)
        {
            lock (locker)
            {
                image?.Dispose();
                image = frame;
            }
        }

        public void Clear()
        {
            lock (locker)
            {
                image?.Dispose();
                image = null;
            }
        }

        public void Draw(SKCanvas canvas, SKRect source, SKRect destination, SKPaint paint)
        {
            lock (locker)
            {
                if (image is not null)
                {
                    canvas.DrawImage(image, source, destination, RenderSamplingOptions.Linear, paint);
                }
            }
        }
    }
}
