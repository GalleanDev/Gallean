using Avalonia;
using Avalonia.Skia;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;
using static Gallean.Client.WorldRenderLayout;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    public static class MountainClouds
    {
        private const int MaskPadding = 32;
        private const int MaskSolidAlpha = 128;
        private const float MaskSolidCoverage = 0.5f;
        private const float MaskPeakShare = 0.28f;
        private const int MaskDilationRadius = 6;
        private const float MaskBlurSigma = 4.0f;
        private const float MaskFadeTop = -0.15f;
        private const float MaskFadeBottom = 0.42f;

        private const float BackStrength = 1.0f;
        private const float FrontStrength = 0.45f;
        private const float FrontPhaseShift = 0.37f;

        private const float NoisePeriod = 150.0f;
        private const int NoiseSize = 256;
        private const int NoiseSeed = 20261107;
        private const int NoiseOctaveCount = 4;
        private const int NoiseCellCount = 6;

        private static readonly Lazy<SKImage> Noise = new(BuildNoise);

        public static WorldMountainCloudShaderProgram BuildProgram()
        {
            WorldMountainCloudShaderProgram program = new();

            using WorldMountainCloudShaderProgram.Editor editor = program.Edit();

            editor.SetNoise(Noise.Value.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, RenderSamplingOptions.Linear));
            editor.SetNoiseScale(NoiseSize / NoisePeriod);
            editor.SetLightColor(Theme.World.MountainCloud.LightColor.ToSKColorF());
            editor.SetShadowColor(Theme.World.MountainCloud.ShadowColor.ToSKColorF());

            return program;
        }

        public static void Draw(SKCanvas canvas, WorldMountainCloudShaderProgram program, MountainCloudSprite cloud, TimeSpan loopTime)
        {
            using SKShader shader = BuildShader(program, cloud, loopTime);
            using SKPaint paint = new() { Shader = shader };

            canvas.DrawRect(cloud.Bounds.ToSKRect(), paint);
        }

        private static SKShader BuildShader(WorldMountainCloudShaderProgram program, MountainCloudSprite cloud, TimeSpan loopTime)
        {
            using WorldMountainCloudShaderProgram.Editor editor = program.Edit();

            editor.SetMask(cloud.Mask);
            editor.SetTime((float)loopTime.TotalSeconds);
            editor.SetPhase(cloud.Phase);
            editor.SetStrength(cloud.Strength);

            return editor.Build();
        }

        public static MountainCloudSprite BuildSprite(MountainCloud cloud, Sprite sprite, SKImage mask)
        {
            Point anchor = Footprint.GetAnchor(cloud.Footprint);
            double left = anchor.X + sprite.Metadata.OffsetX - MaskPadding;
            double top = anchor.Y + sprite.Metadata.OffsetY - MaskPadding;
            Rect bounds = new(left, top, mask.Width, mask.Height);
            SKMatrix transform = SKMatrix.CreateTranslation((float)left, (float)top);
            SKShader maskShader = mask.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear, transform);

            (float strength, float phaseShift) = cloud.Layer switch
            {
                MountainCloudLayer.Back => (BackStrength, 0.0f),
                MountainCloudLayer.Front => (FrontStrength, FrontPhaseShift)
            };

            return new MountainCloudSprite(cloud.Depth, bounds, maskShader, (float)cloud.Phase + phaseShift, strength);
        }

        public static SKImage BuildMask(Sprite sprite)
        {
            int scale = sprite.Metadata.PixelScale;
            int width = (sprite.Metadata.PixelWidth / scale) + (2 * MaskPadding);
            int height = (sprite.Metadata.PixelHeight / scale) + (2 * MaskPadding);
            float[] coverage = BuildCoverage(sprite, width, height);
            (int top, int bottom) = FindExtent(coverage, width, height);
            float[] peak = FindPeak(coverage, width, height, (bottom - top) * MaskPeakShare);
            float[] dilated = Dilate(peak, width, height);
            float[] blurred = Blur(dilated, width, height);
            float center = (MaskFadeTop + MaskFadeBottom) / 2.0f;
            byte[] pixels = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                float relative = (y - top) / (float)Math.Max(bottom - top, 1);
                float fade = SmoothStep(MaskFadeTop, center, relative) * SmoothStep(MaskFadeBottom, center, relative);

                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;

                    pixels[index] = ToByte(blurred[index] * fade);
                }
            }

            SKImageInfo info = new(width, height, SKColorType.Alpha8, SKAlphaType.Premul);

            return SKImage.FromPixelCopy(info, pixels);
        }

        private static float[] BuildCoverage(Sprite sprite, int width, int height)
        {
            SKImage image = sprite.Image;
            SKImageInfo info = new(image.Width, image.Height, SKColorType.Alpha8, SKAlphaType.Premul);

            using SKBitmap bitmap = new(info);

            image.ReadPixels(bitmap.PeekPixels(), 0, 0);

            ReadOnlySpan<byte> alphas = bitmap.GetPixelSpan();
            int scale = sprite.Metadata.PixelScale;
            int spriteWidth = sprite.Metadata.PixelWidth / scale;
            int spriteHeight = sprite.Metadata.PixelHeight / scale;
            float sampleShare = 1.0f / (scale * scale);
            float[] result = new float[width * height];

            for (int y = 0; y < spriteHeight; y++)
            {
                for (int x = 0; x < spriteWidth; x++)
                {
                    float covered = 0;

                    for (int sampleY = 0; sampleY < scale; sampleY++)
                    {
                        for (int sampleX = 0; sampleX < scale; sampleX++)
                        {
                            int pixelX = (x * scale) + sampleX;
                            int pixelY = (y * scale) + sampleY;

                            if (alphas[(pixelY * image.Width) + pixelX] > MaskSolidAlpha)
                            {
                                covered += sampleShare;
                            }
                        }
                    }

                    result[((y + MaskPadding) * width) + x + MaskPadding] = covered;
                }
            }

            return result;
        }

        private static (int Top, int Bottom) FindExtent(float[] coverage, int width, int height)
        {
            int top = height;
            int bottom = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (coverage[(y * width) + x] >= MaskSolidCoverage)
                    {
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                    }
                }
            }

            return (top, bottom);
        }

        private static float[] FindPeak(float[] coverage, int width, int height, float peakHeight)
        {
            float[] peak = new float[width * height];

            for (int x = 0; x < width; x++)
            {
                int ridge = -1;

                for (int y = 0; y < height; y++)
                {
                    int index = (y * width) + x;

                    if (coverage[index] >= MaskSolidCoverage)
                    {
                        if (ridge < 0)
                        {
                            ridge = y;
                        }

                        if (y - ridge < peakHeight)
                        {
                            peak[index] = 1;
                        }
                    }
                }
            }

            return peak;
        }

        private static float[] Dilate(float[] values, int width, int height)
        {
            float[] horizontal = DilateAxis(values, width, height, 1, 0);

            return DilateAxis(horizontal, width, height, 0, 1);
        }

        private static float[] DilateAxis(float[] values, int width, int height, int stepX, int stepY)
        {
            float[] result = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float best = 0;

                    for (int offset = -MaskDilationRadius; offset <= MaskDilationRadius; offset++)
                    {
                        int sampleX = Math.Clamp(x + (offset * stepX), 0, width - 1);
                        int sampleY = Math.Clamp(y + (offset * stepY), 0, height - 1);

                        best = Math.Max(best, values[(sampleY * width) + sampleX]);
                    }

                    result[(y * width) + x] = best;
                }
            }

            return result;
        }

        private static float[] Blur(float[] values, int width, int height)
        {
            float[] kernel = BuildKernel();
            float[] horizontal = BlurAxis(values, width, height, kernel, 1, 0);

            return BlurAxis(horizontal, width, height, kernel, 0, 1);
        }

        private static float[] BuildKernel()
        {
            int radius = (int)Math.Ceiling(MaskBlurSigma * 3);
            float[] kernel = new float[(2 * radius) + 1];
            float total = 0;

            for (int offset = -radius; offset <= radius; offset++)
            {
                float weight = MathF.Exp(-(offset * offset) / (2.0f * MaskBlurSigma * MaskBlurSigma));

                kernel[offset + radius] = weight;
                total += weight;
            }

            for (int index = 0; index < kernel.Length; index++)
            {
                kernel[index] /= total;
            }

            return kernel;
        }

        private static float[] BlurAxis(float[] values, int width, int height, float[] kernel, int stepX, int stepY)
        {
            int radius = kernel.Length / 2;
            float[] result = new float[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0;

                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int sampleX = Math.Clamp(x + (offset * stepX), 0, width - 1);
                        int sampleY = Math.Clamp(y + (offset * stepY), 0, height - 1);

                        sum += values[(sampleY * width) + sampleX] * kernel[offset + radius];
                    }

                    result[(y * width) + x] = sum;
                }
            }

            return result;
        }

        private static SKImage BuildNoise()
        {
            float[] red = BuildNoiseChannel(NoiseSeed);
            float[] green = BuildNoiseChannel(NoiseSeed + 1);
            float[] blue = BuildNoiseChannel(NoiseSeed + 2);
            byte[] pixels = new byte[NoiseSize * NoiseSize * 4];

            for (int index = 0; index < NoiseSize * NoiseSize; index++)
            {
                int offset = index * 4;

                pixels[offset] = ToByte(blue[index]);
                pixels[offset + 1] = ToByte(green[index]);
                pixels[offset + 2] = ToByte(red[index]);
                pixels[offset + 3] = byte.MaxValue;
            }

            SKImageInfo info = new(NoiseSize, NoiseSize, SKColorType.Bgra8888, SKAlphaType.Opaque);
            SKImage image = SKImage.FromPixelCopy(info, pixels);
            SkiaMemoryPressure.TrackImage(image);

            return image;
        }

        private static float[] BuildNoiseChannel(int seed)
        {
            float[] channel = new float[NoiseSize * NoiseSize];
            float amplitude = 0.5f;
            int cells = NoiseCellCount;

            for (int octave = 0; octave < NoiseOctaveCount; octave++)
            {
                float[] grain = BuildGrain(seed + (octave * 97), cells);

                for (int y = 0; y < NoiseSize; y++)
                {
                    for (int x = 0; x < NoiseSize; x++)
                    {
                        float u = x * cells / (float)NoiseSize;
                        float v = y * cells / (float)NoiseSize;

                        channel[(y * NoiseSize) + x] += SampleGrain(grain, cells, u, v) * amplitude;
                    }
                }

                amplitude *= 0.5f;
                cells *= 2;
            }

            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (float value in channel)
            {
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            for (int index = 0; index < channel.Length; index++)
            {
                channel[index] = (channel[index] - min) / Math.Max(max - min, 1e-6f);
            }

            return channel;
        }

        private static float[] BuildGrain(int seed, int cells)
        {
            Random random = new(seed);
            float[] grain = new float[cells * cells];

            for (int index = 0; index < grain.Length; index++)
            {
                grain[index] = random.NextSingle();
            }

            return grain;
        }

        private static float SampleGrain(float[] grain, int cells, float u, float v)
        {
            int left = (int)MathF.Floor(u);
            int top = (int)MathF.Floor(v);
            int right = (left + 1) % cells;
            int bottom = (top + 1) % cells;
            float weightX = Smooth(u - left);
            float weightY = Smooth(v - top);
            float upper = float.Lerp(grain[(top * cells) + left], grain[(top * cells) + right], weightX);
            float lower = float.Lerp(grain[(bottom * cells) + left], grain[(bottom * cells) + right], weightX);

            return float.Lerp(upper, lower, weightY);
        }

        private static float Smooth(float fraction)
        {
            return fraction * fraction * fraction * ((fraction * ((fraction * 6) - 15)) + 10);
        }

        private static float SmoothStep(float start, float end, float value)
        {
            float position = Math.Clamp((value - start) / (end - start), 0, 1);

            return position * position * (3 - 2 * position);
        }

        private static byte ToByte(float value)
        {
            return (byte)Math.Clamp(value * byte.MaxValue, byte.MinValue, byte.MaxValue);
        }
    }
}
