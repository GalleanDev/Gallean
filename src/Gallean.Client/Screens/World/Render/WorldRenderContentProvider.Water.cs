using System.Runtime.InteropServices;
using Gallean.Common;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    public static class Water
    {
        private readonly record struct WaterDetail(SKColorF HeightWeights, SKColorF SlopeWeights, float FineDetail);

        private const float UnreachableDistance = 1e9f;
        private const float DiagonalStep = 1.41421356f;

        private const int SamplesPerTile = 4;
        private const int OpennessRadiusTiles = 3;
        private const float ShoreRange = 6.0f;

        private const float CoarseDetailStart = 0.08f;
        private const float CoarseDetailEnd = 0.3f;

        private const float FineDetailStart = 0.45f;
        private const float FineDetailEnd = 0.9f;

        private const float FirstOctaveScale = 4.1f;
        private const float FirstOctaveWeight = 0.5f;

        private const float SecondOctaveScale = 9.3f;
        private const float SecondOctaveWeight = 0.28f;

        private const float ThirdOctaveScale = 19.5f;
        private const float ThirdOctaveWeight = 0.15f;

        private const float FourthOctaveScale = 37.0f;
        private const float FourthOctaveWeight = 0.07f;
        private const float FourthOctaveFadeStartPixels = 1.9f;
        private const float FourthOctaveFadeEndPixels = 2.3f;

        private const int GrainSize = 256;
        private const int GrainSeed = 20260731;
        private const float GrainScale = 4;
        private const float GrainWavelengthTexels = 2;

        private static readonly Lazy<SKImage> Grain = new(BuildGrain);

        public static WaterLayer? Build(WaterTiles waterTiles, IAssetProvider assets)
        {
            IReadOnlyList<WorldPosition> tiles = waterTiles.Tiles;

            if (tiles.Count > 0)
            {
                SKImage shore = BuildShore(tiles, waterTiles.MapWidth, waterTiles.MapHeight);

                return new WaterLayer(
                    BuildTileMesh(tiles, TerrainTileBleed),
                    BuildProgram(shore),
                    BuildAlbedo(assets));
            }
            else
            {
                return null;
            }
        }

        private static SKImage BuildShore(IReadOnlyList<WorldPosition> waterTiles, int mapWidth, int mapHeight)
        {
            int width = mapWidth * SamplesPerTile;
            int height = mapHeight * SamplesPerTile;

            float[] distances = new float[width * height];

            foreach (WorldPosition tile in waterTiles)
            {
                for (int sampleY = 0; sampleY < SamplesPerTile; sampleY++)
                {
                    int row = ((tile.Y * SamplesPerTile) + sampleY) * width;

                    for (int sampleX = 0; sampleX < SamplesPerTile; sampleX++)
                    {
                        distances[row + (tile.X * SamplesPerTile) + sampleX] = UnreachableDistance;
                    }
                }
            }

            PropagateDistances(distances, width, height);

            float maxDistance = ShoreRange * SamplesPerTile;

            for (int index = 0; index < distances.Length; index++)
            {
                distances[index] = Math.Min(distances[index], maxDistance);
            }

            float[] openness = BlurDistances(distances, width, height);
            float scale = 255.0f / maxDistance;
            byte[] pixels = new byte[width * height * 4];

            for (int index = 0; index < distances.Length; index++)
            {
                int offset = index * 4;

                pixels[offset + 1] = (byte)(openness[index] * scale);
                pixels[offset + 2] = (byte)(distances[index] * scale);
                pixels[offset + 3] = byte.MaxValue;
            }

            SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);

            SKImage image = SKImage.FromPixelCopy(info, pixels);
            SkiaMemoryPressure.TrackImage(image);

            return image;
        }

        private static WaterDetail ResolveDetail(float zoom)
        {
            float coarseDetail = SmoothStep(CoarseDetailStart, CoarseDetailEnd, zoom);
            float fineDetail = SmoothStep(FineDetailStart, FineDetailEnd, zoom);
            float cellPixels = zoom * MathF.Sqrt(2.0f * WorldTransform.TileHalfWidth * WorldTransform.TileHalfHeight);
            float wavelengthPixels = GrainWavelengthTexels * cellPixels / FourthOctaveScale;
            float resolved = SmoothStep(FourthOctaveFadeStartPixels, FourthOctaveFadeEndPixels, wavelengthPixels);

            float fourthDetail = fineDetail * resolved;
            float missingVariance = (fineDetail * fineDetail) - (fourthDetail * fourthDetail);
            float heightGain = VarianceGain(FourthOctaveWeight, ThirdOctaveWeight, missingVariance);
            float slopeGain = VarianceGain(FourthOctaveWeight * FourthOctaveScale, ThirdOctaveWeight * ThirdOctaveScale, missingVariance);

            SKColorF heightWeights = new(
                coarseDetail * FirstOctaveWeight,
                coarseDetail * SecondOctaveWeight,
                coarseDetail * ThirdOctaveWeight * heightGain,
                coarseDetail * FourthOctaveWeight * fourthDetail);

            SKColorF slopeWeights = new(
                coarseDetail * FirstOctaveWeight,
                coarseDetail * SecondOctaveWeight,
                coarseDetail * ThirdOctaveWeight * slopeGain,
                coarseDetail * FourthOctaveWeight * fourthDetail);

            return new WaterDetail(heightWeights, slopeWeights, fineDetail);
        }

        private static float VarianceGain(float lost, float target, float missingVariance)
        {
            return MathF.Sqrt(1 + (lost * lost / (target * target) * missingVariance));
        }

        public static void Draw(SKCanvas canvas, WaterLayer? water, TimeSpan loopTime, double zoom, int frameIndex)
        {
            if (water is not null)
            {
                using SKShader shader = BuildShader(water, loopTime, zoom, frameIndex);
                using SKPaint paint = SkiaHelpers.MakeImagePaint();

                paint.Shader = ResolveAlbedo(water, frameIndex);
                canvas.DrawVertices(water.Mesh, SKBlendMode.Modulate, paint);

                paint.Shader = shader;
                canvas.DrawVertices(water.Mesh, SKBlendMode.Modulate, paint);
            }
        }

        private static SKShader ResolveAlbedo(WaterLayer water, int frameIndex)
        {
            return water.Albedo[frameIndex % water.Albedo.Count];
        }

        private static SKShader BuildShader(WaterLayer water, TimeSpan loopTime, double zoom, int frameIndex)
        {
            WaterDetail detail = ResolveDetail((float)zoom);

            using WaterShaderProgram.Editor editor = water.ShaderProgram.Edit();

            editor.SetAlbedo(ResolveAlbedo(water, frameIndex));
            editor.SetTime((float)loopTime.TotalSeconds);
            editor.SetFineDetail(detail.FineDetail);
            editor.SetOctaveHeightWeights(detail.HeightWeights);
            editor.SetOctaveSlopeWeights(detail.SlopeWeights);

            return editor.Build();
        }

        private static WaterShaderProgram BuildProgram(SKImage shore)
        {
            WaterShaderProgram program = new();

            using WaterShaderProgram.Editor editor = program.Edit();

            editor.SetShore(shore.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear));
            editor.SetGrain(Grain.Value.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, RenderSamplingOptions.Nearest));
            editor.SetHalfTile(new SKPoint(WorldTransform.TileHalfWidth, WorldTransform.TileHalfHeight));
            editor.SetFieldScale(SamplesPerTile);
            editor.SetShoreRange(ShoreRange);
            editor.SetGrainScale(GrainScale);
            editor.SetOctaveScales(new SKColorF(FirstOctaveScale, SecondOctaveScale, ThirdOctaveScale, FourthOctaveScale));
            editor.SetDeepColor(Theme.World.Water.DeepColor.ToSKColorF());
            editor.SetShallowColor(Theme.World.Water.ShallowColor.ToSKColorF());
            editor.SetSkyColor(Theme.World.Water.SkyColor.ToSKColorF());
            editor.SetFoamColor(Theme.World.Water.FoamColor.ToSKColorF());
            editor.SetSunColor(Theme.World.Water.SunColor.ToSKColorF());

            return program;
        }

        private static List<SKShader> BuildAlbedo(IAssetProvider assets)
        {
            using CleanupList leases = new();

            AssetCache<Animation>.Lease lease = leases.Add(assets.GetRequiredAnimation(new AssetAnimationKey.Water()));
            Animation water = lease.Value;
            int frameCount = water.Metadata.FrameCount;
            List<SKShader> result = new(frameCount);

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                Sprite frame = water[frameIndex];
                int scale = frame.Metadata.PixelScale;
                SKMatrix transform = SKMatrix.CreateScale(1.0f / scale, 1.0f / scale);
                SKImage image = BuildAlbedoImage(frame);

                result.Add(image.ToShader(
                    SKShaderTileMode.Repeat,
                    SKShaderTileMode.Repeat,
                    RenderSamplingOptions.Linear,
                    transform));
            }

            return result;
        }

        private static SKImage BuildAlbedoImage(Sprite frame)
        {
            SKImageInfo info = new(
                frame.Metadata.PixelWidth,
                frame.Metadata.PixelHeight,
                SKColorType.Bgra8888,
                SKAlphaType.Premul);

            using SKSurface surface = SKSurface.Create(info);
            SKCanvas canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.DrawImage(frame.Image, 0, 0, RenderSamplingOptions.Nearest);

            return surface.CreateRasterImage();
        }

        private static SKImage BuildGrain()
        {
            Random random = new(GrainSeed);
            byte[] values = new byte[GrainSize * GrainSize];

            random.NextBytes(values);

            byte[] pixels = new byte[GrainSize * GrainSize * 8];
            Span<Half> channels = MemoryMarshal.Cast<byte, Half>(pixels.AsSpan());

            for (int y = 0; y < GrainSize; y++)
            {
                for (int x = 0; x < GrainSize; x++)
                {
                    int index = (y * GrainSize) + x;
                    int right = (y * GrainSize) + ((x + 1) % GrainSize);
                    int below = (((y + 1) % GrainSize) * GrainSize) + x;
                    int diagonal = (((y + 1) % GrainSize) * GrainSize) + ((x + 1) % GrainSize);
                    int offset = index * 4;

                    float origin = values[index] / 255.0f;
                    float alongX = (values[right] - values[index]) / 255.0f;
                    float alongY = (values[below] - values[index]) / 255.0f;
                    float diagonalDifference = (values[diagonal] - values[right] - values[below] + values[index]) / 255.0f;
                    float alpha = EncodeCoefficient(origin);

                    channels[offset] = (Half)(EncodeCoefficient(alongX) * alpha);
                    channels[offset + 1] = (Half)(EncodeCoefficient(alongY) * alpha);
                    channels[offset + 2] = (Half)(EncodeCoefficient(diagonalDifference) * alpha);
                    channels[offset + 3] = (Half)alpha;
                }
            }

            SKImageInfo info = new(GrainSize, GrainSize, SKColorType.RgbaF16, SKAlphaType.Premul);

            SKImage image = SKImage.FromPixelCopy(info, pixels);
            SkiaMemoryPressure.TrackImage(image);

            return image;
        }

        private static float EncodeCoefficient(float coefficient)
        {
            return (coefficient / GrainScale) + 0.5f;
        }

        private static float SmoothStep(float start, float end, float value)
        {
            float position = Math.Clamp((value - start) / (end - start), 0, 1);

            return position * position * (3 - 2 * position);
        }

        private static void PropagateDistances(float[] distances, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width) + x;
                    float best = distances[index];

                    best = Math.Min(best, Sample(distances, width, height, x - 1, y) + 1);
                    best = Math.Min(best, Sample(distances, width, height, x, y - 1) + 1);
                    best = Math.Min(best, Sample(distances, width, height, x - 1, y - 1) + DiagonalStep);
                    best = Math.Min(best, Sample(distances, width, height, x + 1, y - 1) + DiagonalStep);

                    distances[index] = best;
                }
            }

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    int index = (y * width) + x;
                    float best = distances[index];

                    best = Math.Min(best, Sample(distances, width, height, x + 1, y) + 1);
                    best = Math.Min(best, Sample(distances, width, height, x, y + 1) + 1);
                    best = Math.Min(best, Sample(distances, width, height, x + 1, y + 1) + DiagonalStep);
                    best = Math.Min(best, Sample(distances, width, height, x - 1, y + 1) + DiagonalStep);

                    distances[index] = best;
                }
            }
        }

        private static float[] BlurDistances(float[] distances, int width, int height)
        {
            float[] horizontal = new float[distances.Length];
            float[] blurred = new float[distances.Length];

            BlurAxis(distances, horizontal, lines: height, length: width, lineStride: width, sampleStride: 1);
            BlurAxis(horizontal, blurred, lines: width, length: height, lineStride: 1, sampleStride: width);

            return blurred;
        }

        private static void BlurAxis(float[] source, float[] target, int lines, int length, int lineStride, int sampleStride)
        {
            const int radius = OpennessRadiusTiles * SamplesPerTile;
            const int window = (radius * 2) + 1;

            for (int line = 0; line < lines; line++)
            {
                int origin = line * lineStride;
                float sum = source[origin] * (radius + 1);

                for (int index = 1; index <= radius; index++)
                {
                    sum += source[origin + (Math.Min(index, length - 1) * sampleStride)];
                }

                for (int index = 0; index < length; index++)
                {
                    target[origin + (index * sampleStride)] = sum / window;

                    sum -= source[origin + (Math.Max(index - radius, 0) * sampleStride)];
                    sum += source[origin + (Math.Min(index + radius + 1, length - 1) * sampleStride)];
                }
            }
        }

        private static float Sample(float[] distances, int width, int height, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                return distances[(y * width) + x];
            }
            else
            {
                return UnreachableDistance;
            }
        }
    }
}
