using System.Numerics;

namespace Packer;

internal sealed class WaterTintFilter : ISpriteFilter
{
    private const float MinimumChroma = 0.001f;

    private static readonly Vector3 LumaWeights = new(0.2126f, 0.7152f, 0.0722f);
    private static readonly Vector3 SheetColor = ToVector(29, 73, 77);
    private static readonly Vector3 ShoreColor = ToVector(36, 91, 98);

    private readonly Vector3 sheetDirection;
    private readonly Vector3 shoreDirection;
    private readonly float chromaGain;
    private readonly float lumaExponent;

    public WaterTintFilter()
    {
        (float sheetLuma, Vector3 sheetChroma) = Split(SheetColor);
        (float shoreLuma, Vector3 shoreChroma) = Split(ShoreColor);

        float sheetLength = sheetChroma.Length();
        float shoreLength = shoreChroma.Length();

        sheetDirection = sheetChroma / sheetLength;
        shoreDirection = shoreChroma / shoreLength;
        chromaGain = shoreLength / sheetLength;
        lumaExponent = MathF.Log(shoreLuma) / MathF.Log(sheetLuma);
    }

    public Guid Type { get; } = new("e46d70f8-1484-4e07-8e6b-94ba9597ada2");

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return true;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        List<AssetSprite> result = [];

        foreach (AssetSprite frame in frames)
        {
            result.Add(Process(frame));
        }

        return result;
    }

    private AssetSprite Process(AssetSprite sprite)
    {
        byte[] pixels = sprite.Pixels.ToArray();

        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            if (pixels[offset + 3] > 0)
            {
                Vector3 tinted = Apply(ToVector(pixels[offset], pixels[offset + 1], pixels[offset + 2]));

                pixels[offset] = ToByte(tinted.X);
                pixels[offset + 1] = ToByte(tinted.Y);
                pixels[offset + 2] = ToByte(tinted.Z);
            }
        }

        return sprite with { Pixels = pixels };
    }

    private Vector3 Apply(Vector3 color)
    {
        (float luma, Vector3 chroma) = Split(color);

        float length = chroma.Length();

        if (length > MinimumChroma)
        {
            float alignment = Math.Max(Vector3.Dot(chroma, sheetDirection) / length, 0);
            float weight = alignment * alignment;
            Vector3 target = shoreDirection * (length * chromaGain);
            Vector3 tintedChroma = Vector3.Lerp(chroma, target, weight);
            float tintedLuma = float.Lerp(luma, MathF.Pow(luma, lumaExponent), weight);

            return new Vector3(tintedLuma) + tintedChroma;
        }
        else
        {
            return color;
        }
    }

    private static (float Luma, Vector3 Chroma) Split(Vector3 color)
    {
        float luma = Vector3.Dot(color, LumaWeights);

        return (luma, color - new Vector3(luma));
    }

    private static Vector3 ToVector(byte red, byte green, byte blue)
    {
        return new Vector3((float)red / byte.MaxValue, (float)green / byte.MaxValue, (float)blue / byte.MaxValue);
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp(MathF.Round(value * byte.MaxValue), byte.MinValue, byte.MaxValue);
    }
}
