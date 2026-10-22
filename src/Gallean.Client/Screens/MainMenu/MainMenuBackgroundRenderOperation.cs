using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class MainMenuBackgroundRenderOperation(
    Rect destination,
    Sprite sprite,
    SKImage normal,
    SKImage depth,
    SKImage albedo,
    MainMenuBackgroundLighting lighting,
    TimeSpan time) : ICustomDrawOperation
{
    private const int LightCount = 16;
    private const double VerticalSwayPhase = 0.7;
    private const double SecondFlickerRatio = 1.575;
    private const double SecondFlickerPhase = 2.0;

    private static readonly Lazy<MainMenuBackgroundShaderProgram> Program = new(() => new MainMenuBackgroundShaderProgram());

    public Rect Bounds => destination;

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
        using SKShader shader = BuildShader();
        using SKPaint paint = new() { Shader = shader, Color = new SKColor(byte.MaxValue, byte.MaxValue, byte.MaxValue, ToAlpha(lease.CurrentOpacity)) };

        lease.SkCanvas.DrawRect(destination.ToSKRect(), paint);
    }

    private static byte ToAlpha(double opacity)
    {
        return (byte)Math.Clamp(Math.Round(opacity * byte.MaxValue), byte.MinValue, byte.MaxValue);
    }

    private static Point GetPosition(MainMenuBackgroundLight light, double seconds)
    {
        double phase = (2.0 * Math.PI * seconds / light.SwayPeriod.TotalSeconds) + light.SwayPhase;

        return new Point(
            light.Center.X + (light.Sway.X * Math.Sin(phase)),
            light.Center.Y + (light.Sway.Y * Math.Sin((2.0 * phase) + VerticalSwayPhase)));
    }

    private static double GetPower(MainMenuBackgroundLight light, double seconds)
    {
        double phase = (2.0 * Math.PI * light.FlickerRate * seconds) + light.SwayPhase;
        double flicker = (0.6 * Math.Sin(phase)) + (0.4 * Math.Sin((phase * SecondFlickerRatio) + SecondFlickerPhase));

        return light.Power * (1 + (light.Flicker * flicker));
    }

    private SKShader BuildImageShader(SKImage image)
    {
        SKMatrix imageToDestination = SKMatrix.CreateScaleTranslation(
            (float)(destination.Width / image.Width),
            (float)(destination.Height / image.Height),
            (float)destination.X,
            (float)destination.Y);

        return image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear, imageToDestination);
    }

    private SKShader BuildShader()
    {
        double scaleX = destination.Width / sprite.Metadata.Width;
        double scaleY = destination.Height / sprite.Metadata.Height;
        double scale = (scaleX + scaleY) / 2.0;
        double seconds = time.TotalSeconds;
        SKColorF[] positions = new SKColorF[LightCount];
        SKColorF[] radiances = new SKColorF[LightCount];

        for (int index = 0; index < Math.Min(lighting.Lights.Count, LightCount); index++)
        {
            MainMenuBackgroundLight light = lighting.Lights[index];
            Point position = GetPosition(light, seconds);
            float power = (float)(GetPower(light, seconds) / byte.MaxValue);

            positions[index] = new SKColorF(
                (float)(destination.X + (position.X * scaleX)),
                (float)(destination.Y + (position.Y * scaleY)),
                (float)(light.Height * scale),
                (float)(light.Radius * scale));

            radiances[index] = new SKColorF(
                light.Color.R * power,
                light.Color.G * power,
                light.Color.B * power,
                (float)(light.CoreRadius * scale));
        }

        using SKShader source = BuildImageShader(sprite.Image);
        using SKShader normals = BuildImageShader(normal);
        using SKShader heightField = BuildImageShader(depth);
        using SKShader surface = BuildImageShader(albedo);
        using MainMenuBackgroundShaderProgram.Editor editor = Program.Value.Edit();

        editor.SetSource(source);
        editor.SetNormals(normals);
        editor.SetHeightField(heightField);
        editor.SetAlbedo(surface);
        editor.SetLightPositions(positions);
        editor.SetLightRadiances(radiances);
        editor.SetDepthScale((float)(lighting.DepthScale * scale));

        return editor.Build();
    }
}
