using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed class PlateLightingDecorator : Decorator
{
    private const double Ambient = 0.8;
    private const double KeyLightHeight = 120;
    private const double KeyLightIntensity = 1.6;
    private const double KeyLightRadius = 260;
    private const double KeyLightSway = 3;
    private const double FillLightX = 0.9;
    private const double FillLightY = -0.2;
    private const double FillLightHeight = 600;
    private const double FillLightIntensity = 0.5;
    private const double FillLightRadius = 2000;
    private const double PointerLightHeight = 60;
    private const double PointerLightIntensity = 1.2;
    private const double PointerLightRadius = 140;
    public static readonly StyledProperty<RelativePoint> KeyLightOriginProperty =
        AvaloniaProperty.Register<PlateLightingDecorator, RelativePoint>(nameof(KeyLightOrigin), new RelativePoint(0.03, 0.3, RelativeUnit.Relative));

    private TopLevel? topLevel;
    private Point? pointer;
    private TimeSpan time;
    private bool frameRequested;

    public RelativePoint KeyLightOrigin
    {
        get => GetValue(KeyLightOriginProperty);
        set => SetValue(KeyLightOriginProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        topLevel = TopLevel.GetTopLevel(this);
        topLevel?.AddHandler(PointerMovedEvent, OnTopLevelPointerMoved, RoutingStrategies.Tunnel, true);
        topLevel?.AddHandler(PointerExitedEvent, OnTopLevelPointerExited, RoutingStrategies.Direct, true);
        RequestFrame();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        topLevel?.RemoveHandler(PointerMovedEvent, OnTopLevelPointerMoved);
        topLevel?.RemoveHandler(PointerExitedEvent, OnTopLevelPointerExited);
        topLevel = null;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        UpdateLighting();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ScreenPanel.IsTopmostProperty)
        {
            UpdateLighting();
            RequestFrame();
        }
    }

    private void OnTopLevelPointerMoved(object? sender, PointerEventArgs e)
    {
        pointer = e.GetPosition(this);
    }

    private void OnTopLevelPointerExited(object? sender, PointerEventArgs e)
    {
        pointer = null;
    }

    private void RequestFrame()
    {
        if (!frameRequested && topLevel is not null && ScreenPanel.GetIsTopmost(this))
        {
            frameRequested = true;
            topLevel.RequestAnimationFrame(OnFrame);
        }
    }

    private void OnFrame(TimeSpan frameTime)
    {
        frameRequested = false;

        if (topLevel is not null)
        {
            time = frameTime;
            UpdateLighting();
            RequestFrame();
        }
    }

    private void UpdateLighting()
    {
        if (ScreenPanel.GetIsTopmost(this))
        {
            PlateChrome.SetLighting(this, ComposeLighting());
        }
        else
        {
            PlateChrome.SetLighting(this, null);
        }
    }

    private PlateChrome.Lighting ComposeLighting()
    {
        Size size = Bounds.Size;
        Point keyLightOrigin = KeyLightOrigin.ToPixels(size);

        double flicker = Flicker(time.TotalSeconds);

        Point keyLight = new(
            keyLightOrigin.X + (KeyLightSway * Math.Sin(3.7 * time.TotalSeconds)),
            keyLightOrigin.Y + (KeyLightSway * Math.Sin((5.3 * time.TotalSeconds) + 0.9)));

        Point fillLight = new(size.Width * FillLightX, size.Height * FillLightY);

        List<PlateChrome.Lighting.Light> lights =
        [
            new(keyLight, KeyLightHeight, Client.Theme.Plate.KeyLightColor, KeyLightIntensity * flicker, KeyLightRadius),
            new(fillLight, FillLightHeight, Client.Theme.Plate.FillLightColor, FillLightIntensity, FillLightRadius)
        ];

        if (pointer is not null)
        {
            lights.Add(new PlateChrome.Lighting.Light(
                pointer.Value,
                PointerLightHeight,
                Client.Theme.Plate.PointerLightColor,
                PointerLightIntensity,
                PointerLightRadius));
        }

        return new PlateChrome.Lighting(this, lights, Ambient);
    }

    private static double Flicker(double seconds)
    {
        return 1 + (0.1 * Math.Sin(7.1 * seconds)) + (0.07 * Math.Sin((12.9 * seconds) + 1.3)) + (0.05 * Math.Sin((23.3 * seconds) + 0.7));
    }
}
