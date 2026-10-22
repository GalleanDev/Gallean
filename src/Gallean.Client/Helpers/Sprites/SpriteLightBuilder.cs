using System.Runtime.InteropServices;
using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal static class SpriteLightBuilder
{
    private readonly record struct Sample(Point Position, Vector3D Color, double Luminance);

    private readonly record struct LightGroup(IReadOnlyList<int> Clusters, SpriteLight Light);

    private const int MaxLights = 3;
    private const int MaxSamples = 16 * 1024;
    private const int Iterations = 8;
    private const double LuminanceThreshold = 0.01;
    private const double RatioThreshold = 0.12;
    private const double MergeDistance = 1.5;

    private static readonly Lazy<float[]> LinearChannels = new(BuildLinearChannels);

    public static IReadOnlyList<IReadOnlyList<SpriteLight>> Build(IReadOnlyList<AssetSprite<SKImage>> frames)
    {
        Sample[][] frameSamples = [.. frames.Select(CollectSamples)];
        Sample[] samples = [.. frameSamples.SelectMany(frame => frame)];

        if (samples.Length > 0)
        {
            Point[] centers = Cluster(samples, SeedCenters(samples));
            int[] clusters = Assign(samples, centers);
            IReadOnlyList<IReadOnlyList<int>> groups = GroupClusters(BuildGroups(samples, centers, clusters));
            List<IReadOnlyList<SpriteLight>> lights = new(frames.Count);

            for (int index = 0; index < frames.Count; index++)
            {
                lights.Add(BuildFrameLights(frames[index].Metadata, frameSamples[index], centers, groups));
            }

            return lights;
        }
        else
        {
            return [.. frames.Select(_ => (IReadOnlyList<SpriteLight>)[])];
        }
    }

    private static IReadOnlyList<SpriteLight> BuildFrameLights(
        AssetSpriteMetadata metadata,
        ReadOnlySpan<Sample> samples,
        Point[] centers,
        IReadOnlyList<IReadOnlyList<int>> groups)
    {
        Point[] frameCenters = Cluster(samples, centers);
        int[] clusters = Assign(samples, frameCenters);
        IReadOnlyList<SpriteLight?> clusterLights = BuildClusterLights(samples, frameCenters, clusters);
        Vector offset = new(metadata.OffsetX, metadata.OffsetY);
        List<SpriteLight> lights = [];

        foreach (IReadOnlyList<int> group in groups)
        {
            SpriteLight? light = null;

            foreach (int cluster in group)
            {
                SpriteLight? current = clusterLights[cluster];

                if (current is null)
                {
                    continue;
                }

                if (light is null)
                {
                    light = current;
                }
                else
                {
                    light = Merge(light, current);
                }
            }

            if (light is not null)
            {
                lights.Add(light with { Position = light.Position - offset });
            }
        }

        return lights;
    }

    private static int GetStride(AssetSpriteMetadata metadata)
    {
        return Math.Max((int)Math.Ceiling(Math.Sqrt(metadata.PixelWidth * (double)metadata.PixelHeight / MaxSamples)), 1);
    }

    private static Sample[] CollectSamples(AssetSprite<SKImage> sprite)
    {
        AssetSpriteMetadata metadata = sprite.Metadata;

        if (!metadata.Additive)
        {
            return [];
        }

        int width = metadata.PixelWidth;
        int height = metadata.PixelHeight;
        int stride = GetStride(metadata);
        double scaleX = metadata.Width / (double)width;
        double scaleY = metadata.Height / (double)height;
        float[] linear = LinearChannels.Value;
        using SKPixmap pixmap = sprite.Pixels.PeekPixels();
        ReadOnlySpan<byte> pixels = pixmap.GetPixelSpan();
        List<Sample> samples = [];

        for (int y = stride / 2; y < height; y += stride)
        {
            for (int x = stride / 2; x < width; x += stride)
            {
                int offset = (y * width + x) * 4;
                Vector3D color = new(linear[pixels[offset + 2]], linear[pixels[offset + 1]], linear[pixels[offset]]);
                double luminance = GetLuminance(color);

                if (luminance > LuminanceThreshold)
                {
                    Point position = new(metadata.OffsetX + (x + 0.5) * scaleX, metadata.OffsetY + (y + 0.5) * scaleY);

                    samples.Add(new Sample(position, color, luminance));
                }
            }
        }

        return [.. samples];
    }

    private static Point[] Cluster(ReadOnlySpan<Sample> samples, Point[] seeds)
    {
        Point[] centers = seeds;

        for (int iteration = 0; iteration < Iterations; iteration++)
        {
            int[] assignment = Assign(samples, centers);

            centers = UpdateCenters(samples, centers, assignment);
        }

        return centers;
    }

    private static Point[] SeedCenters(ReadOnlySpan<Sample> samples)
    {
        List<Point> centers = [GetCentroid(samples)];

        while (centers.Count < MaxLights)
        {
            double bestScore = 0;
            Point best = new();

            foreach (Sample sample in samples)
            {
                double score = sample.Luminance * GetNearestDistanceSquared(sample.Position, CollectionsMarshal.AsSpan(centers));

                if (score > bestScore)
                {
                    bestScore = score;
                    best = sample.Position;
                }
            }

            if (bestScore <= 0)
            {
                break;
            }

            centers.Add(best);
        }

        return [.. centers];
    }

    private static Point GetCentroid(ReadOnlySpan<Sample> samples)
    {
        double total = 0;
        double x = 0;
        double y = 0;

        foreach (Sample sample in samples)
        {
            total += sample.Luminance;
            x += sample.Position.X * sample.Luminance;
            y += sample.Position.Y * sample.Luminance;
        }

        return new Point(x / total, y / total);
    }

    private static double GetNearestDistanceSquared(Point position, ReadOnlySpan<Point> centers)
    {
        double nearest = double.MaxValue;

        foreach (Point center in centers)
        {
            nearest = Math.Min(nearest, GetDistanceSquared(position, center));
        }

        return nearest;
    }

    private static double GetDistanceSquared(Point first, Point second)
    {
        double dx = first.X - second.X;
        double dy = first.Y - second.Y;

        return dx * dx + dy * dy;
    }

    private static int[] Assign(ReadOnlySpan<Sample> samples, ReadOnlySpan<Point> centers)
    {
        int[] clusters = new int[samples.Length];

        for (int index = 0; index < samples.Length; index++)
        {
            double nearest = double.MaxValue;

            for (int center = 0; center < centers.Length; center++)
            {
                double distance = GetDistanceSquared(samples[index].Position, centers[center]);

                if (distance < nearest)
                {
                    nearest = distance;
                    clusters[index] = center;
                }
            }
        }

        return clusters;
    }

    private static Point[] UpdateCenters(ReadOnlySpan<Sample> samples, ReadOnlySpan<Point> centers, ReadOnlySpan<int> clusters)
    {
        double[] totals = new double[centers.Length];
        double[] xs = new double[centers.Length];
        double[] ys = new double[centers.Length];

        for (int index = 0; index < samples.Length; index++)
        {
            Sample sample = samples[index];
            int cluster = clusters[index];

            totals[cluster] += sample.Luminance;
            xs[cluster] += sample.Position.X * sample.Luminance;
            ys[cluster] += sample.Position.Y * sample.Luminance;
        }

        Point[] updated = new Point[centers.Length];

        for (int index = 0; index < centers.Length; index++)
        {
            if (totals[index] > 0)
            {
                updated[index] = new Point(xs[index] / totals[index], ys[index] / totals[index]);
            }
            else
            {
                updated[index] = centers[index];
            }
        }

        return updated;
    }

    private static List<SpriteLight?> BuildClusterLights(
        ReadOnlySpan<Sample> samples,
        ReadOnlySpan<Point> centers,
        ReadOnlySpan<int> clusters)
    {
        double[] totals = new double[centers.Length];
        double[] spreads = new double[centers.Length];
        double[] counts = new double[centers.Length];
        Vector3D[] colors = new Vector3D[centers.Length];

        for (int index = 0; index < samples.Length; index++)
        {
            Sample sample = samples[index];
            int cluster = clusters[index];

            totals[cluster] += sample.Luminance;
            spreads[cluster] += sample.Luminance * GetDistanceSquared(sample.Position, centers[cluster]);
            counts[cluster] += 1;
            colors[cluster] += sample.Color;
        }

        List<SpriteLight?> lights = [];

        for (int index = 0; index < centers.Length; index++)
        {
            if (totals[index] > 0)
            {
                double radius = Math.Sqrt(spreads[index] / totals[index]);
                Vector3D radiance = colors[index] * (Math.PI / counts[index]);

                lights.Add(new SpriteLight(centers[index], radius, radiance, GetLuminance(radiance)));
            }
            else
            {
                lights.Add(null);
            }
        }

        return lights;
    }

    private static List<LightGroup> BuildGroups(
        ReadOnlySpan<Sample> samples,
        ReadOnlySpan<Point> centers,
        ReadOnlySpan<int> clusters)
    {
        double[] totals = new double[centers.Length];
        double totalLuminance = 0;

        for (int index = 0; index < samples.Length; index++)
        {
            totals[clusters[index]] += samples[index].Luminance;
            totalLuminance += samples[index].Luminance;
        }

        List<SpriteLight?> clusterLights = BuildClusterLights(samples, centers, clusters);
        List<LightGroup> lights = [];

        for (int index = 0; index < centers.Length; index++)
        {
            SpriteLight? light = clusterLights[index];

            if (light is not null && totals[index] >= totalLuminance * RatioThreshold)
            {
                lights.Add(new LightGroup([index], light));
            }
        }

        return lights;
    }

    private static IReadOnlyList<IReadOnlyList<int>> GroupClusters(IReadOnlyList<LightGroup> lights)
    {
        List<LightGroup> merged = [];

        foreach (LightGroup light in lights)
        {
            LightGroup current = light;

            while (true)
            {
                int index = merged.FindIndex(existing => IsOverlapping(existing.Light, current.Light));
                if (index < 0)
                {
                    break;
                }

                current = new LightGroup([.. merged[index].Clusters, .. current.Clusters], Merge(merged[index].Light, current.Light));
                merged.RemoveAt(index);
            }

            merged.Add(current);
        }

        return [.. merged.Select(group => group.Clusters)];
    }

    private static bool IsOverlapping(SpriteLight first, SpriteLight second)
    {
        Vector offset = first.Position - second.Position;

        return offset.Length < (first.Radius + second.Radius) * MergeDistance;
    }

    private static SpriteLight Merge(SpriteLight first, SpriteLight second)
    {
        double share = second.Power / Math.Max(first.Power + second.Power, 1e-9);
        Vector offset = first.Position - second.Position;
        Point position = first.Position + (second.Position - first.Position) * share;
        double radius = Math.Max(first.Radius, second.Radius) + offset.Length * 0.5;
        Vector3D radiance = first.Radiance + second.Radiance;

        return new SpriteLight(position, radius, radiance, GetLuminance(radiance));
    }

    private static float[] BuildLinearChannels()
    {
        float[] channels = new float[256];

        for (int index = 0; index < channels.Length; index++)
        {
            channels[index] = MathF.Pow(index / 255.0f, 2.2f);
        }

        return channels;
    }

    private static double GetLuminance(Vector3D color)
    {
        return 0.2126 * color.X + 0.7152 * color.Y + 0.0722 * color.Z;
    }
}
