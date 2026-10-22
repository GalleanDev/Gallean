using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace Gallean.Client;

internal static class MedallionTexture
{
    private enum State
    {
        Dark,
        DarkHover,
        Lit,
        LitHover
    }

    private const int Scale = 2;
    private const int Seed = 5303;
    private const float EdgeFeather = 1.0f * Scale;
    private const float HaloWidth = 6.0f * Scale;
    private const float GrainAmplitude = 1.6f;
    private const float Curvature = 1.2f;

    public const double Size = 180;
    private const int Pixels = (int)Size * Scale;
    private const float Center = Pixels / 2.0f;
    private const float OuterRadius = 84.0f * Scale;
    private const float BorderRadius = 80.5f * Scale;
    private const float OrnamentRadius = 72.0f * Scale;
    private const float BevelThickness = 1.2f * Scale;
    private const float InsetRadius = 58.5f * Scale;
    private const float FaceRadius = 56.0f * Scale;
    public const double MetalThickness = (OuterRadius - InsetRadius) / Scale;
    private const int SlotCount = 36;
    private const float SlotShare = 0.3f;
    private const float SlotMargin = 2.6f * Scale;
    private const int ToothCount = 60;
    private const float ToothDepth = 5.0f * Scale;

    private static readonly Lazy<WriteableBitmap> RingTexture = new(() => Create(State.Dark, faceOnly: false));
    private static readonly Lazy<WriteableBitmap> FaceDarkHoverTexture = new(() => Create(State.DarkHover, faceOnly: true));
    private static readonly Lazy<WriteableBitmap> FaceLitTexture = new(() => Create(State.Lit, faceOnly: true));
    private static readonly Lazy<WriteableBitmap> FaceLitHoverTexture = new(() => Create(State.LitHover, faceOnly: true));

    public static Thickness MetalMargin { get; } = new((Size / 2.0) - (OuterRadius / Scale));

    public static WriteableBitmap Ring => RingTexture.Value;
    public static WriteableBitmap FaceDarkHover => FaceDarkHoverTexture.Value;
    public static WriteableBitmap FaceLit => FaceLitTexture.Value;
    public static WriteableBitmap FaceLitHover => FaceLitHoverTexture.Value;

    private static WriteableBitmap Create(State state, bool faceOnly)
    {
        TextureBuffer buffer = new(Pixels, Pixels);

        for (int y = 0; y < Pixels; y++)
        {
            for (int x = 0; x < Pixels; x++)
            {
                int index = (y * Pixels) + x;
                float dx = x + 0.5f - Center;
                float dy = y + 0.5f - Center;
                float distance = MathF.Sqrt((dx * dx) + (dy * dy));
                float angle = MathF.Atan2(dx, -dy);

                TexturePixel pixel = default;

                if (!faceOnly)
                {
                    pixel = pixel.Over(Theme.Medallion.HaloColor, Halo(distance, OuterRadius));
                }

                Color face = Face(distance, state, x, y);
                pixel = pixel.Over(face, Disc(distance, FaceRadius));

                if (!faceOnly)
                {
                    pixel = pixel.Over(Theme.Medallion.InsetColor, Band(distance, FaceRadius, InsetRadius));

                    Color markings = Gold(distance, angle, InsetRadius, OrnamentRadius, x, y);
                    pixel = pixel.Over(markings, Band(distance, InsetRadius, OrnamentRadius));

                    Color slot = Theme.Medallion.SlotColor;
                    float slotGrain = TextureNoise.Grain(x, y, Seed + 3, GrainAmplitude);
                    pixel = pixel.Over(TextureHelpers.ToColor(slot.R + slotGrain, slot.G + slotGrain, slot.B + slotGrain), SlotCoverage(distance, angle));

                    Color borderBackdrop = Theme.Medallion.BorderBackdropColor;
                    float borderBackdropGrain = TextureNoise.Grain(x, y, Seed + 4, GrainAmplitude);
                    pixel = pixel.Over(TextureHelpers.ToColor(borderBackdrop.R + borderBackdropGrain, borderBackdrop.G + borderBackdropGrain, borderBackdrop.B + borderBackdropGrain), Band(distance, OrnamentRadius, BorderRadius));

                    Color bevel = Gold(distance, angle, OrnamentRadius, OrnamentRadius + BevelThickness, x, y);
                    pixel = pixel.Over(bevel, Band(distance, OrnamentRadius, OrnamentRadius + BevelThickness));

                    Color tooth = Gold(distance, angle, BorderRadius - ToothDepth, BorderRadius, x, y);
                    pixel = pixel.Over(tooth, ToothCoverage(distance, angle));

                    Color border = Gold(distance, angle, BorderRadius, OuterRadius, x, y);
                    pixel = pixel.Over(border, Band(distance, BorderRadius, OuterRadius));
                }

                buffer.Set(index, pixel);
            }
        }

        return buffer.ToPremultipliedBitmap();
    }

    private static Color Face(float distance, State state, int x, int y)
    {
        Color center = ResolveFaceCenter(state);
        Color edge = ResolveFaceEdge(state);
        Color glow = ResolveFaceGlow(state);

        float t = Math.Clamp(distance / FaceRadius, 0, 1);
        float depth = t * t * t;
        float glowLight = MathF.Pow(Math.Clamp((t - 0.68f) / 0.32f, 0, 1), 2.2f);
        float grain = TextureNoise.Grain(x, y, Seed + 5, GrainAmplitude) * 0.5f;

        float red = TextureHelpers.Lerp(center.R, edge.R, depth);
        float green = TextureHelpers.Lerp(center.G, edge.G, depth);
        float blue = TextureHelpers.Lerp(center.B, edge.B, depth);

        red = TextureHelpers.Lerp(red, glow.R, glowLight);
        green = TextureHelpers.Lerp(green, glow.G, glowLight);
        blue = TextureHelpers.Lerp(blue, glow.B, glowLight);

        return TextureHelpers.ToColor(red + grain, green + grain, blue + grain);
    }

    private static Color Gold(float distance, float angle, float inner, float outer, int x, int y)
    {
        float t = Math.Clamp((distance - inner) / (outer - inner), 0, 1);
        float tilt = (t - 0.5f) * 2 * Curvature;
        float radialX = MathF.Sin(angle);
        float radialY = -MathF.Cos(angle);
        Vector3 normal = new(radialX * MathF.Sin(tilt), radialY * MathF.Sin(tilt), MathF.Cos(tilt));
        float relief = TextureNoise.Fractal(x, y, MedallionMetal.Octaves, Pixels, Pixels, Seed + 10);

        return MedallionMetal.Shade(normal, relief, TextureNoise.Grain(x, y, Seed + 1, GrainAmplitude));
    }

    private static float SlotCoverage(float distance, float angle)
    {
        float phase = angle / (2 * MathF.PI) * SlotCount;
        float pitch = 2 * MathF.PI * distance / SlotCount;
        float along = MathF.Abs(phase - MathF.Round(phase)) * pitch;
        float across = Math.Clamp(((SlotShare * pitch / 2) - along) / EdgeFeather + 0.5f, 0, 1);
        float radial = Band(distance, InsetRadius + SlotMargin, OrnamentRadius - SlotMargin);

        return across * radial;
    }

    private static float ToothCoverage(float distance, float angle)
    {
        float phase = angle / (2 * MathF.PI) * ToothCount;
        float tooth = 1 - MathF.Abs((2 * (phase - MathF.Floor(phase))) - 1);
        float inner = BorderRadius - (ToothDepth * tooth);

        return Band(distance, inner, BorderRadius);
    }

    private static float Halo(float distance, float outer)
    {
        if (distance >= outer - EdgeFeather && distance < outer + HaloWidth)
        {
            float falloff = 1 - Math.Clamp((distance - outer) / HaloWidth, 0, 1);

            return Theme.Medallion.HaloColor.A / 255.0f * falloff * falloff;
        }
        else
        {
            return 0;
        }
    }

    private static Color ResolveFaceCenter(State state)
    {
        return state switch
        {
            State.Dark => Theme.Medallion.FaceColor,
            State.DarkHover => Theme.Medallion.FaceColor,
            State.Lit => Theme.Medallion.FaceLitColor,
            State.LitHover => Theme.Medallion.FaceLitHoverColor
        };
    }

    private static Color ResolveFaceEdge(State state)
    {
        return state switch
        {
            State.Dark => Theme.Medallion.FaceEdgeColor,
            State.DarkHover => Theme.Medallion.FaceHoverEdgeColor,
            State.Lit => Theme.Medallion.FaceLitEdgeColor,
            State.LitHover => Theme.Medallion.FaceLitHoverEdgeColor
        };
    }

    private static Color ResolveFaceGlow(State state)
    {
        return state switch
        {
            State.Dark => Theme.Medallion.FaceGlowColor,
            State.DarkHover => Theme.Medallion.FaceHoverGlowColor,
            State.Lit => Theme.Medallion.FaceLitGlowColor,
            State.LitHover => Theme.Medallion.FaceLitHoverGlowColor
        };
    }

    private static float Disc(float distance, float radius)
    {
        return Math.Clamp(((radius - distance) / EdgeFeather) + 0.5f, 0, 1);
    }

    private static float Band(float distance, float inner, float outer)
    {
        return Math.Clamp((Math.Min(distance - inner, outer - distance) / EdgeFeather) + 0.5f, 0, 1);
    }
}
