using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class GalleanLogoRenderOperation(Rect destination, GalleanLogoLayers layers, IReadOnlyList<GalleanLogoLight> lights) : ICustomDrawOperation
{
    private const int LightCount = 8;
    private const double GrazingStrength = 0.09;
    private const double ShadowOffsetX = 0.004;
    private const double ShadowOffsetY = 0.012;
    private const double ShadowBlur = 0.012;
    private const double ShadowExtent = 0.05;

    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);
    private static readonly Lazy<GalleanLogoShaderProgram> Program = new(() => new GalleanLogoShaderProgram());

    public Rect Bounds => destination.Inflate(destination.Width * ShadowExtent);

    public bool HitTest(Point point)
    {
        return false;
    }

    public bool Equals(ICustomDrawOperation? other)
    {
        return ReferenceEquals(this, other);
    }

    public void Dispose()
    {
    }

    public void Render(ImmediateDrawingContext context)
    {
        using ISkiaSharpApiLease lease = context.GetSkiaApiLease();
        SKColor opacity = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, ToAlpha(lease.CurrentOpacity));
        SKRect rect = destination.ToSKRect();
        float width = (float)destination.Width;
        Color shadowColor = Theme.Logo.ShadowColor;

        using (SKImageFilter shadowFilter = SKImageFilter.CreateDropShadowOnly(
            (float)(ShadowOffsetX * width),
            (float)(ShadowOffsetY * width),
            (float)(ShadowBlur * width),
            (float)(ShadowBlur * width),
            new SKColor(shadowColor.R, shadowColor.G, shadowColor.B, shadowColor.A)))
        using (SKPaint shadow = new() { ImageFilter = shadowFilter, Color = opacity })
        {
            lease.SkCanvas.DrawImage(layers.Color, rect, Sampling, shadow);
        }

        using SKShader shader = BuildShader();
        using SKPaint paint = new() { Shader = shader, Color = opacity };

        lease.SkCanvas.DrawRect(rect, paint);
    }

    private static byte ToAlpha(double opacity)
    {
        return (byte)Math.Clamp(Math.Round(opacity * byte.MaxValue), byte.MinValue, byte.MaxValue);
    }

    private static float ToUnit(byte channel)
    {
        return channel / (float)byte.MaxValue;
    }

    private SKShader BuildImageShader(SKImage image)
    {
        SKMatrix imageToDestination = SKMatrix.CreateScaleTranslation(
            (float)(destination.Width / image.Width),
            (float)(destination.Height / image.Height),
            (float)destination.X,
            (float)destination.Y);

        return image.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, Sampling, imageToDestination);
    }

    private SKShader BuildShader()
    {
        SKColorF[] positions = new SKColorF[LightCount];
        SKColorF[] radiances = new SKColorF[LightCount];

        for (int index = 0; index < Math.Min(lights.Count, LightCount); index++)
        {
            GalleanLogoLight light = lights[index];
            float intensity = (float)light.Intensity;

            positions[index] = new SKColorF(
                (float)light.Position.X,
                (float)light.Position.Y,
                (float)light.Height,
                (float)light.Radius);

            radiances[index] = new SKColorF(
                ToUnit(light.Color.R) * intensity,
                ToUnit(light.Color.G) * intensity,
                ToUnit(light.Color.B) * intensity,
                (float)light.Specular);
        }

        Color ambient = Theme.Logo.AmbientColor;
        Color grazing = Theme.Logo.GrazingColor;

        using SKShader color = BuildImageShader(layers.Color);
        using SKShader normals = BuildImageShader(layers.Normal);
        using GalleanLogoShaderProgram.Editor editor = Program.Value.Edit();

        editor.SetColor(color);
        editor.SetNormals(normals);
        editor.SetLightPositions(positions);
        editor.SetLightRadiances(radiances);
        editor.SetAmbient(new SKPoint3(ToUnit(ambient.R), ToUnit(ambient.G), ToUnit(ambient.B)));
        editor.SetGrazing(new SKColorF(ToUnit(grazing.R), ToUnit(grazing.G), ToUnit(grazing.B), (float)GrazingStrength));

        return editor.Build();
    }
}
