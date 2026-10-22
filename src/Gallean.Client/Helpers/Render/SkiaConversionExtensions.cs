using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace Gallean.Client;

internal static class SkiaConversionExtensions
{
    extension(Color color)
    {
        public SKColorF ToSKColorF()
        {
            return new SKColorF(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
        }
    }

    extension(Vector3D value)
    {
        public SKPoint3 ToSKPoint3()
        {
            return new SKPoint3((float)value.X, (float)value.Y, (float)value.Z);
        }
    }
}
