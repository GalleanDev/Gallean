using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class PlateLightRenderOperation(
    Rect bounds,
    PlateChromeShapeType shape,
    double thickness,
    double tipLength,
    double cornerRadius,
    IReadOnlyList<PlateChrome.Lighting.Light> lights,
    double ambient) : ICustomDrawOperation
{
    private const int LightCount = 4;

    private static readonly Lazy<PlateLightShaderProgram> Program = new(() => new PlateLightShaderProgram());

    public Rect Bounds => bounds;

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
        using SKShader shader = BuildShader(lease.CurrentOpacity);
        using SKPaint paint = new() { Shader = shader, BlendMode = SKBlendMode.Overlay };

        lease.SkCanvas.DrawRect(bounds.ToSKRect(), paint);
    }

    private static SKPoint3 ToRadiance(PlateChrome.Lighting.Light light)
    {
        float scale = (float)(light.Intensity / 255.0);

        return new SKPoint3(light.Color.R * scale, light.Color.G * scale, light.Color.B * scale);
    }

    private static float ShapeIndex(PlateChromeShapeType shape)
    {
        return shape switch
        {
            PlateChromeShapeType.Spiked => 0,
            PlateChromeShapeType.Round => 1,
            PlateChromeShapeType.Pill => 2,
            PlateChromeShapeType.Square => 3,
            PlateChromeShapeType.Octagon => 1,
            PlateChromeShapeType.Panel => 3
        };
    }

    private SKShader BuildShader(double opacity)
    {
        SKColorF[] positions = new SKColorF[LightCount];
        SKPoint3[] radiances = new SKPoint3[LightCount];

        for (int index = 0; index < Math.Min(lights.Count, LightCount); index++)
        {
            PlateChrome.Lighting.Light light = lights[index];

            positions[index] = new SKColorF((float)light.Position.X, (float)light.Position.Y, (float)light.Height, (float)light.Radius);
            radiances[index] = ToRadiance(light);
        }

        using PlateLightShaderProgram.Editor editor = Program.Value.Edit();

        editor.SetSize(new SKPoint((float)bounds.Width, (float)bounds.Height));
        editor.SetThickness((float)thickness);
        editor.SetTipLength((float)tipLength);
        editor.SetCornerRadius((float)cornerRadius);
        editor.SetShape(ShapeIndex(shape));
        editor.SetAmbient((float)ambient);
        editor.SetOpacity((float)opacity);
        editor.SetLightPositions(positions);
        editor.SetLightRadiances(radiances);

        return editor.Build();
    }
}
