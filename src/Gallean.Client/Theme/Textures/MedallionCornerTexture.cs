using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Gallean.Client;

internal static class MedallionCornerTexture
{
    private readonly record struct Feature(float Coverage, float Height);

    private sealed record Layout(Size Size, Matrix Inverse, Point Center, Point RightRhombus, Point BottomRhombus);

    private const int Scale = 2;
    private const int Seed = 7717;
    private const float GrainAmplitude = 1.6f;
    private const float Step = 0.5f;
    private const int NoisePeriod = 512;

    private const float RingRadius = 84.0f;
    private const float Tuck = 6.0f;
    private const float ChannelWidth = 7.0f;
    private const float CradleWidth = 2.0f;
    private const float CradleHeight = 1.0f;
    private const float RhombusOffset = 1.0f;
    private const float RhombusHalf = 7.0f;
    private const float FrameWidth = 1.0f;
    private const float PyramidHalf = 3.4f;
    private const float FrameHeight = 0.9f;
    private const float PyramidHeight = 3.0f;
    private const float ArmRootHalf = 3.0f;
    private const float ArmFootHalf = 12.0f;
    private const float ArmFlareExponent = 3.0f;
    private const float ArmHeight = 2.0f;
    private const float NotchHalf = 3.5f;
    private const float NotchExponent = 2.2f;
    private const float DiagonalReach = 30.0f;
    private const float DiagonalHalf = 0.75f;
    private const float DiagonalHeight = 0.8f;
    private const float CornerInset = 11.0f;
    private const float CornerRhombusHalf = 6.2f;
    private const float CornerPyramidHalf = 3.1f;
    private const float SocketBevel = 1.4f;
    private const float SocketDepth = 1.0f;
    private const float HaloWidth = 3.0f;
    private const float HaloStrength = 0.75f;
    private const float ShadowReach = 2.2f;
    private const float ShadowStrength = 0.6f;
    private const float StoneRelief = 0.9f;

    private static readonly Vector2 ShadowDirection = Vector2.Normalize(new Vector2(-0.5f, -0.6f));

    private static readonly NoiseOctave[] StoneOctaves =
    [
        new(9, 9, 0.16f),
        new(23, 23, 0.12f),
        new(53, 53, 0.1f),
        new(127, 127, 0.09f),
        new(256, 256, 0.07f)
    ];

    private static readonly Func<Layout, Point, Feature>[] BackLayers =
    [
        Cradle,
        Diagonal,
        RightArm,
        BottomArm,
        CornerRhombusFrame,
        CornerRhombusPyramid
    ];

    private static readonly Func<Layout, Point, Feature>[] FrontLayers =
    [
        RightRhombusFrame,
        RightRhombusPyramid,
        BottomRhombusFrame,
        BottomRhombusPyramid
    ];

    private static readonly Func<Layout, Point, Feature>[] MetalLayers = [.. BackLayers, .. FrontLayers];

    public static double RhombusRadius => RingRadius + RhombusOffset;

    public static WriteableBitmap Create(MedallionCornerLayer layer, Size size, Matrix transform, Point center, Point rightRhombus, Point bottomRhombus)
    {
        Layout layout = new(size, transform.Invert(), center, rightRhombus, bottomRhombus);
        int width = (int)Math.Ceiling(size.Width * Scale);
        int height = (int)Math.Ceiling(size.Height * Scale);
        TextureBuffer buffer = new(width, height);
        Func<Layout, Point, Feature>[] metals = layer switch
        {
            MedallionCornerLayer.Back => BackLayers,
            MedallionCornerLayer.Front => FrontLayers
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                Point point = new((x + 0.5) / Scale, (y + 0.5) / Scale);

                TexturePixel pixel = default;
                float plate = layer == MedallionCornerLayer.Back ? Plate(layout, point) : 0;

                if (plate > 0)
                {
                    float shadow = 1 - (ShadowStrength * MetalCoverage(layout, new Point(point.X + (ShadowDirection.X * ShadowReach), point.Y + (ShadowDirection.Y * ShadowReach))));
                    Color stone = Stone(x, y, shadow);
                    pixel = pixel.Over(stone, plate);

                    float channel = Coverage((RingRadius + ChannelWidth) - Radius(layout, point)) * plate;
                    pixel = pixel.Over(Dark(x, y, shadow), channel);
                }

                foreach ((Point rhombus, float half) in Sockets(layer, layout))
                {
                    float distance = Diamond(point, rhombus);
                    float halo = 1 - Math.Clamp((distance - half) / HaloWidth, 0, 1);
                    pixel = pixel.Over(Colors.Black, HaloStrength * halo * halo);

                    Feature socket = Socket(point, rhombus, half);
                    float light = 0.55f + (0.9f * Math.Max(Vector3.Dot(SocketNormal(point, rhombus, half), MedallionMetal.Light), 0));
                    pixel = pixel.Over(Dark(x, y, light), socket.Coverage);
                }

                foreach (Func<Layout, Point, Feature> metal in metals)
                {
                    Feature feature = metal(layout, point);

                    if (feature.Coverage > 0)
                    {
                        Vector3 normal = Normal(metal, layout, point);
                        float relief = TextureNoise.Fractal(x, y, MedallionMetal.Octaves, NoisePeriod, NoisePeriod, Seed + 10);
                        pixel = pixel.Over(MedallionMetal.Shade(normal, relief, TextureNoise.Grain(x, y, Seed + 1, GrainAmplitude)), feature.Coverage);
                    }
                }

                buffer.Set(index, pixel);
            }
        }

        return buffer.ToPremultipliedBitmap();
    }

    private static (Point Center, float Half)[] Sockets(MedallionCornerLayer layer, Layout layout)
    {
        return layer switch
        {
            MedallionCornerLayer.Back => [(CornerRhombus(layout), CornerRhombusHalf)],
            MedallionCornerLayer.Front => [(layout.RightRhombus, RhombusHalf), (layout.BottomRhombus, RhombusHalf)]
        };
    }

    private static Point CornerRhombus(Layout layout)
    {
        return new Point(layout.Size.Width - CornerInset, layout.Size.Height - CornerInset);
    }

    private static float Radius(Layout layout, Point point)
    {
        Point local = layout.Inverse.Transform(point);
        double dx = local.X - layout.Center.X;
        double dy = local.Y - layout.Center.Y;

        return (float)Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static float Diamond(Point point, Point center)
    {
        return (float)(Math.Abs(point.X - center.X) + Math.Abs(point.Y - center.Y));
    }

    private static float Coverage(float distance)
    {
        return Math.Clamp((distance * Scale) + 0.5f, 0, 1);
    }

    private static float Dome(float distance, float half, float height)
    {
        float u = Math.Clamp(distance / half, -1, 1);

        return height * MathF.Sqrt(1 - (u * u));
    }

    private static float Plate(Layout layout, Point point)
    {
        float left = (float)(point.X - layout.BottomRhombus.X);
        float top = (float)(point.Y - layout.RightRhombus.Y);

        return Coverage(Math.Min(left, top)) * Coverage(Radius(layout, point) - (RingRadius - Tuck));
    }

    private static float MetalCoverage(Layout layout, Point point)
    {
        float coverage = 0;

        foreach (Func<Layout, Point, Feature> metal in MetalLayers)
        {
            coverage = Math.Max(coverage, metal(layout, point).Coverage);
        }

        return coverage;
    }

    private static Feature Cradle(Layout layout, Point point)
    {
        float middle = RingRadius + ChannelWidth + (CradleWidth / 2.0f);
        float offset = Radius(layout, point) - middle;
        float half = CradleWidth / 2.0f;
        float coverage = Coverage(half - Math.Abs(offset)) * Plate(layout, point);

        return new Feature(coverage, Dome(offset, half, CradleHeight));
    }

    private static Feature Diagonal(Layout layout, Point point)
    {
        if (Plate(layout, point) > 0)
        {
            float along = (float)((layout.Size.Width - point.X) + (layout.Size.Height - point.Y)) / MathF.Sqrt(2);
            float offset = along - (DiagonalReach / MathF.Sqrt(2));

            return new Feature(Coverage(DiagonalHalf - Math.Abs(offset)), Dome(offset, DiagonalHalf, DiagonalHeight));
        }
        else
        {
            return default;
        }
    }

    private static Feature Arm(float along, float length, float across)
    {
        if (along >= -RhombusHalf && length > 0)
        {
            float t = Math.Clamp(along / length, 0, 1);
            float half = ArmRootHalf + ((ArmFootHalf - ArmRootHalf) * MathF.Pow(t, ArmFlareExponent));
            float notch = NotchHalf * MathF.Pow(t, NotchExponent);
            float prong = (half - notch) / 2.0f;
            float offset = Math.Abs(across) - (notch + prong);

            return new Feature(Coverage(prong - Math.Abs(offset)), Dome(offset, prong, ArmHeight));
        }
        else
        {
            return default;
        }
    }

    private static Feature RightArm(Layout layout, Point point)
    {
        double start = layout.RightRhombus.X + RhombusHalf;

        return Arm((float)(point.X - start), (float)(layout.Size.Width - start), (float)(point.Y - layout.RightRhombus.Y));
    }

    private static Feature BottomArm(Layout layout, Point point)
    {
        double start = layout.BottomRhombus.Y + RhombusHalf;

        return Arm((float)(point.Y - start), (float)(layout.Size.Height - start), (float)(point.X - layout.BottomRhombus.X));
    }

    private static Feature Frame(Point point, Point center, float half)
    {
        float distance = Diamond(point, center);
        float width = FrameWidth / 2.0f;
        float offset = distance - (half - width);

        return new Feature(Coverage(width - Math.Abs(offset)), Dome(offset, width, FrameHeight));
    }

    private static Feature Socket(Point point, Point center, float half)
    {
        float inside = half - Diamond(point, center);

        return new Feature(Coverage(inside), -SocketDepth * Math.Clamp(inside / SocketBevel, 0, 1));
    }

    private static Vector3 SocketNormal(Point point, Point center, float half)
    {
        float left = Socket(new Point(point.X - Step, point.Y), center, half).Height;
        float right = Socket(new Point(point.X + Step, point.Y), center, half).Height;
        float up = Socket(new Point(point.X, point.Y - Step), center, half).Height;
        float down = Socket(new Point(point.X, point.Y + Step), center, half).Height;

        return Vector3.Normalize(new Vector3(-(right - left) / (2 * Step), -(down - up) / (2 * Step), 1));
    }

    private static Feature Pyramid(Point point, Point center, float core)
    {
        float distance = Diamond(point, center);

        return new Feature(Coverage(core - distance), PyramidHeight * Math.Max(0, 1 - (distance / core)));
    }

    private static Feature RightRhombusFrame(Layout layout, Point point)
    {
        return Frame(point, layout.RightRhombus, RhombusHalf);
    }

    private static Feature RightRhombusPyramid(Layout layout, Point point)
    {
        return Pyramid(point, layout.RightRhombus, PyramidHalf);
    }

    private static Feature BottomRhombusFrame(Layout layout, Point point)
    {
        return Frame(point, layout.BottomRhombus, RhombusHalf);
    }

    private static Feature BottomRhombusPyramid(Layout layout, Point point)
    {
        return Pyramid(point, layout.BottomRhombus, PyramidHalf);
    }

    private static Feature CornerRhombusFrame(Layout layout, Point point)
    {
        return Frame(point, CornerRhombus(layout), CornerRhombusHalf);
    }

    private static Feature CornerRhombusPyramid(Layout layout, Point point)
    {
        return Pyramid(point, CornerRhombus(layout), CornerPyramidHalf);
    }

    private static Vector3 Normal(Func<Layout, Point, Feature> metal, Layout layout, Point point)
    {
        float left = metal(layout, new Point(point.X - Step, point.Y)).Height;
        float right = metal(layout, new Point(point.X + Step, point.Y)).Height;
        float up = metal(layout, new Point(point.X, point.Y - Step)).Height;
        float down = metal(layout, new Point(point.X, point.Y + Step)).Height;

        return Vector3.Normalize(new Vector3(-(right - left) / (2 * Step), -(down - up) / (2 * Step), 1));
    }

    private static Color Stone(int x, int y, float shadow)
    {
        float relief = TextureNoise.Fractal(x, y, StoneOctaves, NoisePeriod, NoisePeriod, Seed + 30);
        float slopeX = TextureNoise.Fractal(x + 1, y, StoneOctaves, NoisePeriod, NoisePeriod, Seed + 30) - relief;
        float slopeY = TextureNoise.Fractal(x, y + 1, StoneOctaves, NoisePeriod, NoisePeriod, Seed + 30) - relief;
        Vector3 normal = Vector3.Normalize(new Vector3(-slopeX * StoneRelief * Scale, -slopeY * StoneRelief * Scale, 1));
        float lambert = Math.Max(Vector3.Dot(normal, MedallionMetal.Light), 0);
        float light = Math.Clamp((0.5f + (0.5f * lambert) + (relief * 1.4f)) * shadow, 0, 1.5f);
        float grain = TextureNoise.Grain(x, y, Seed + 2, GrainAmplitude) * 1.5f;
        Color stone = Theme.Medallion.StoneColor;

        return TextureHelpers.ToColor((stone.R * light) + grain, (stone.G * light) + grain, (stone.B * light) + grain);
    }

    private static Color Dark(int x, int y, float shadow)
    {
        Color channel = Theme.Medallion.ChannelColor;
        float light = (1 + (TextureNoise.Fractal(x, y, StoneOctaves, NoisePeriod, NoisePeriod, Seed + 40) * 1.2f)) * shadow;
        float grain = TextureNoise.Grain(x, y, Seed + 3, GrainAmplitude);

        return TextureHelpers.ToColor((channel.R * light) + grain, (channel.G * light) + grain, (channel.B * light) + grain);
    }
}
