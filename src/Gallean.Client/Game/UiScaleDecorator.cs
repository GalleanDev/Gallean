using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed partial class UiScaleDecorator : Decorator
{
    private readonly ScaleTransform transform = new();
    private TopLevel? topLevel;

    static UiScaleDecorator()
    {
        FactorProperty.Changed.AddClassHandler<Visual>(OnCacheScaleChanged);
        CacheModeProperty.Changed.AddClassHandler<Visual>(OnCacheScaleChanged);
    }

    [GeneratedStyledProperty]
    public partial double MinContentWidth { get; set; }

    [GeneratedStyledProperty]
    public partial double MinContentHeight { get; set; }

    [GeneratedAttachedProperty(DefaultValue = 1.0, Inherits = true)]
    public static partial double GetRenderScaling(Visual element);

    [GeneratedAttachedProperty(DefaultValue = 1.0, Inherits = true)]
    public static partial double GetFactor(Visual element);

    public static double GetViewportScaling(double deviceHeight)
    {
        if (double.IsFinite(deviceHeight))
        {
            return Math.Max(deviceHeight / 1080, 1);
        }
        else
        {
            return 1.0;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ChildProperty)
        {
            if (e.OldValue is Control oldChild)
            {
                oldChild.RenderTransform = null;
            }

            if (e.NewValue is Control newChild)
            {
                newChild.RenderTransformOrigin = RelativePoint.TopLeft;
                newChild.RenderTransform = transform;
            }
        }
        else if (e.Property == FactorProperty)
        {
            transform.ScaleX = GetFactor(this);
            transform.ScaleY = GetFactor(this);
        }
        else if (e.Property == SettingsScope.SettingsProperty ||
                 e.Property == MinContentWidthProperty ||
                 e.Property == MinContentHeightProperty)
        {
            InvalidateMeasure();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is not null)
        {
            topLevel.ScalingChanged += OnScalingChanged;
        }

        InvalidateMeasure();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (topLevel is not null)
        {
            topLevel.ScalingChanged -= OnScalingChanged;
            topLevel = null;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double factor = GetContentScalingFactor(availableSize);
        double renderScaling = topLevel is null ? 1.0 : topLevel.RenderScaling;

        SetValue(FactorProperty, factor);
        SetValue(RenderScalingProperty, factor * renderScaling);

        if (Child is not null)
        {
            Child.Measure(availableSize / factor);

            return Child.DesiredSize * factor;
        }
        else
        {
            return default;
        }
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize / GetFactor(this)));

        return finalSize;
    }

    private static void OnCacheScaleChanged(Visual visual, AvaloniaPropertyChangedEventArgs e)
    {
        if (visual.CacheMode is BitmapCache cache)
        {
            cache.RenderAtScale = GetFactor(visual);
        }
    }

    private double GetContentScalingFactor(Size availableSize)
    {
        double actualFactor = GetActualScalingFactor(availableSize);
        double fitFactor = Math.Min(availableSize.Width / MinContentWidth, availableSize.Height / MinContentHeight);

        return Math.Min(actualFactor, Math.Max(fitFactor, 1));
    }

    private double GetActualScalingFactor(Size availableSize)
    {
        GameSettings settings = SettingsScope.GetSettings(this);

        if (topLevel is not null)
        {
            double renderScaling = topLevel.RenderScaling;
            double viewportScaling = GetViewportScaling(availableSize.Height * renderScaling);

            return viewportScaling * settings.UiSizeFactor / renderScaling;
        }
        else
        {
            return 1.0;
        }
    }

    private void OnScalingChanged(object? sender, EventArgs e)
    {
        InvalidateMeasure();
    }
}
