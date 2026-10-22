using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class PointerTiltDecorator : Decorator
{
    private const double FollowRate = 5.0;
    private const double SettleDistance = 0.001;

    public static readonly StyledProperty<double> AngleXProperty =
        AvaloniaProperty.Register<PointerTiltDecorator, double>(nameof(AngleX));

    public static readonly StyledProperty<double> AngleYProperty =
        AvaloniaProperty.Register<PointerTiltDecorator, double>(nameof(AngleY));

    public static readonly StyledProperty<double> DepthProperty =
        AvaloniaProperty.Register<PointerTiltDecorator, double>(nameof(Depth));

    public static readonly StyledProperty<double> TiltProperty =
        AvaloniaProperty.Register<PointerTiltDecorator, double>(nameof(Tilt));

    private readonly Rotate3DTransform transform = new();

    private TopLevel? topLevel;
    private Vector target;
    private Vector current;
    private TimeSpan? lastFrame;
    private bool frameRequested;

    public PointerTiltDecorator()
    {
        RenderTransform = transform;
    }

    public double AngleX
    {
        get => GetValue(AngleXProperty);
        set => SetValue(AngleXProperty, value);
    }

    public double AngleY
    {
        get => GetValue(AngleYProperty);
        set => SetValue(AngleYProperty, value);
    }

    public double Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    public double Tilt
    {
        get => GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == AngleXProperty ||
            e.Property == AngleYProperty ||
            e.Property == DepthProperty ||
            e.Property == TiltProperty)
        {
            UpdateTransform();
        }
        else if (e.Property == ScreenPanel.IsTopmostProperty)
        {
            lastFrame = null;
            RequestFrame();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        topLevel = TopLevel.GetTopLevel(this);
        topLevel?.AddHandler(PointerMovedEvent, OnTopLevelPointerMoved, RoutingStrategies.Tunnel, true);
        UpdateTransform();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        topLevel?.RemoveHandler(PointerMovedEvent, OnTopLevelPointerMoved);
        topLevel = null;
        lastFrame = null;
    }

    private void OnTopLevelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (topLevel?.Bounds is { Width: > 0, Height: > 0 })
        {
            Point position = e.GetPosition(topLevel);
            double halfWidth = topLevel.Bounds.Width / 2.0;
            double halfHeight = topLevel.Bounds.Height / 2.0;

            target = new Vector(
                Math.Clamp((position.X - halfWidth) / halfWidth, -1, 1),
                Math.Clamp((position.Y - halfHeight) / halfHeight, -1, 1));

            RequestFrame();
        }
    }

    private void RequestFrame()
    {
        if (!frameRequested && topLevel is not null && ScreenPanel.GetIsTopmost(this))
        {
            frameRequested = true;
            topLevel.RequestAnimationFrame(OnFrame);
        }
    }

    private void OnFrame(TimeSpan time)
    {
        frameRequested = false;

        if (topLevel is not null && ScreenPanel.GetIsTopmost(this))
        {
            TimeSpan elapsed = TimeSpan.Zero;
            if (lastFrame is not null)
            {
                elapsed = time - lastFrame.Value;
            }

            double blend = 1 - Math.Exp(-FollowRate * elapsed.TotalSeconds);
            current += (target - current) * blend;

            Vector remaining = target - current;
            if (remaining.Length > SettleDistance)
            {
                lastFrame = time;
                RequestFrame();
            }
            else
            {
                current = target;
                lastFrame = null;
            }

            UpdateTransform();
        }
    }

    private void UpdateTransform()
    {
        transform.AngleX = AngleX - (current.Y * Tilt);
        transform.AngleY = AngleY - (current.X * Tilt);
        transform.Depth = Depth;
    }
}
