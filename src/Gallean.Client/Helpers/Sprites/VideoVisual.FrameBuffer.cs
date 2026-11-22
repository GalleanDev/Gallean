using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class VideoVisual
{
    private sealed class FrameBuffer
    {
        private static readonly Lazy<VideoGrainShaderProgram> GrainProgram = new(() => new VideoGrainShaderProgram());

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

        public void Draw(
            SKCanvas canvas,
            SKRect source,
            SKRect destination,
            byte alpha,
            float grainSeed,
            float grainStrength)
        {
            lock (locker)
            {
                if (image is not null)
                {
                    float scaleX = destination.Width / source.Width;
                    float scaleY = destination.Height / source.Height;
                    SKMatrix transform = SKMatrix.CreateScaleTranslation(
                        scaleX,
                        scaleY,
                        destination.Left - (source.Left * scaleX),
                        destination.Top - (source.Top * scaleY));

                    SKMatrix device = canvas.TotalMatrix;
                    SKPoint density = new(device.ScaleX, device.ScaleY);

                    using SKShader frame = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear, transform);
                    using SKShader shader = BuildGrainShader(frame, grainSeed, grainStrength, density);
                    using SKPaint paint = new();

                    paint.Shader = shader;
                    paint.Color = new SKColor(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);
                    canvas.DrawRect(destination, paint);
                }
            }
        }

        private static SKShader BuildGrainShader(SKShader frame, float seed, float strength, SKPoint density)
        {
            using VideoGrainShaderProgram.Editor editor = GrainProgram.Value.Edit();

            editor.SetFrame(frame);
            editor.SetSeed(seed);
            editor.SetStrength(strength);
            editor.SetDensity(density);

            return editor.Build();
        }
    }
}
