using System.Numerics;
using Avalonia.Media;

namespace Gallean.Client;

internal static class MedallionMetal
{
    private const float Ambient = 0.3f;
    private const float Diffuse = 0.7f;
    private const float Specular = 0.5f;
    private const float Shininess = 22.0f;

    public static Vector3 Light { get; } = Vector3.Normalize(new Vector3(-0.5f, -0.6f, 0.62f));

    public static NoiseOctave[] Octaves { get; } =
    [
        new(5, 5, 0.07f),
        new(11, 11, 0.05f),
        new(23, 23, 0.035f),
        new(47, 47, 0.025f)
    ];

    public static Color Shade(Vector3 normal, float relief, float grain)
    {
        Vector3 half = Vector3.Normalize(Light + Vector3.UnitZ);

        float lambert = Math.Max(Vector3.Dot(normal, Light), 0);
        float specular = MathF.Pow(Math.Max(Vector3.Dot(normal, half), 0), Shininess) * Specular;
        float light = Math.Clamp(Ambient + (Diffuse * lambert) + relief, 0, 1);

        Color dark = Theme.Medallion.GoldDarkColor;
        Color middle = Theme.Medallion.GoldColor;
        Color bright = Theme.Medallion.GoldLightColor;
        Color shine = Theme.Medallion.ShineColor;

        float red = TextureHelpers.Ramp(dark.R, middle.R, bright.R, light);
        float green = TextureHelpers.Ramp(dark.G, middle.G, bright.G, light);
        float blue = TextureHelpers.Ramp(dark.B, middle.B, bright.B, light);

        red = TextureHelpers.Lerp(red, shine.R, specular);
        green = TextureHelpers.Lerp(green, shine.G, specular);
        blue = TextureHelpers.Lerp(blue, shine.B, specular);

        return TextureHelpers.ToColor(red + grain, green + grain, blue + grain);
    }
}
