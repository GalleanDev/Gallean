using Avalonia.Media;

namespace Gallean.Client;

internal static class TextureHelpers
{
    public static float Lerp(float from, float to, float amount)
    {
        return from + ((to - from) * amount);
    }

    public static float Ramp(float dark, float middle, float bright, float light)
    {
        if (light < 0.5f)
        {
            return Lerp(dark, middle, light * 2);
        }
        else
        {
            return Lerp(middle, bright, (light - 0.5f) * 2);
        }
    }

    public static float Smooth(float value)
    {
        return value * value * (3 - (2 * value));
    }

    public static byte ToByte(float value)
    {
        return (byte)Math.Clamp(value + 0.5f, byte.MinValue, byte.MaxValue);
    }

    public static Color ToColor(float red, float green, float blue)
    {
        return Color.FromRgb(ToByte(red), ToByte(green), ToByte(blue));
    }
}
