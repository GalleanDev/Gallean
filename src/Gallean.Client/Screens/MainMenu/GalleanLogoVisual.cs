using Avalonia;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class GalleanLogoVisual : AbstractAnimatedVisual
{
    private const double PlateLightGain = 2.0;
    private const double PlateLightSpecular = 0.12;
    private const double TorchX = -0.04;
    private const double TorchY = 0.4;
    private const double TorchHeight = 0.3;
    private const double TorchRadius = 0.9;
    private const double TorchIntensity = 1.6;
    private const double MoonX = 1.1;
    private const double MoonY = -0.5;
    private const double MoonHeight = 0.7;
    private const double MoonRadius = 3.0;
    private const double MoonIntensity = 2.4;
    private const double MoonSpecular = 0.12;
    private const double ShineStartX = -0.2;
    private const double ShineEndX = 1.2;
    private const double ShineY = -0.15;
    private const double ShineHeight = 0.04;
    private const double ShineRadius = 0.45;
    private const double ShineIntensity = 3.8;
    private const double ShineSpecular = 0.22;

    private static readonly TimeSpan ShinePeriod = TimeSpan.FromSeconds(11.0);
    private static readonly TimeSpan ShineDuration = TimeSpan.FromSeconds(3.5);

    private readonly AnimationClock clock = new();
    private TimeSpan time;

    protected override void OnFrame(TimeSpan frameTime)
    {
        time += clock.Tick();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return Stretch.Uniform.CalculateSize(availableSize, LogoSize());
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return Stretch.Uniform.CalculateSize(finalSize, LogoSize());
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (Bounds is { Width: > 0, Height: > 0 })
        {
            Rect destination = new(Bounds.Size);

            context.Custom(new GalleanLogoRenderOperation(destination, GalleanLogoLayers.Default, ComposeLights(destination.Size)));
        }
    }

    private static Size LogoSize()
    {
        GalleanLogoLayers layers = GalleanLogoLayers.Default;

        return new Size(layers.Color.Width, layers.Color.Height);
    }

    private List<GalleanLogoLight> ComposeLights(Size size)
    {
        PlateChrome.Lighting? lighting = PlateChrome.GetLighting(this);

        List<GalleanLogoLight> lights = [];

        if (lighting is not null)
        {
            foreach (PlateChrome.Lighting.Light light in lighting.Lights)
            {
                Point? position = lighting.Space.TranslatePoint(light.Position, this);

                if (position is not null)
                {
                    lights.Add(new GalleanLogoLight(
                        position.Value,
                        light.Height,
                        light.Radius,
                        light.Color,
                        light.Intensity * PlateLightGain,
                        PlateLightSpecular));
                }
            }
        }
        else
        {
            lights.Add(new GalleanLogoLight(
                new Point(TorchX * size.Width, TorchY * size.Height),
                TorchHeight * size.Width,
                TorchRadius * size.Width,
                Client.Theme.Plate.KeyLightColor,
                TorchIntensity * PlateLightGain,
                PlateLightSpecular));
        }

        lights.Add(new GalleanLogoLight(
            new Point(MoonX * size.Width, MoonY * size.Height),
            MoonHeight * size.Width,
            MoonRadius * size.Width,
            Client.Theme.Logo.MoonColor,
            MoonIntensity,
            MoonSpecular));

        double shineProgress = (time.Ticks % ShinePeriod.Ticks) / (double)ShineDuration.Ticks;

        if (shineProgress < 1)
        {
            lights.Add(new GalleanLogoLight(
                new Point((ShineStartX + ((ShineEndX - ShineStartX) * shineProgress)) * size.Width, ShineY * size.Height),
                ShineHeight * size.Width,
                ShineRadius * size.Width,
                Client.Theme.Logo.ShineColor,
                ShineIntensity * Math.Sin(Math.PI * shineProgress),
                ShineSpecular));
        }

        return lights;
    }
}
