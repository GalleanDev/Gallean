using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class UiScaleDecorator : Decorator
{
    public static readonly AttachedProperty<double> FactorProperty =
        AvaloniaProperty.RegisterAttached<UiScaleDecorator, Visual, double>("Factor", 1.0, inherits: true);

    public static readonly StyledProperty<double> MinContentWidthProperty =
        AvaloniaProperty.Register<UiScaleDecorator, double>(nameof(MinContentWidth));

    public static readonly StyledProperty<double> MinContentHeightProperty =
        AvaloniaProperty.Register<UiScaleDecorator, double>(nameof(MinContentHeight));

    private readonly ScaleTransform transform = new();
    private TopLevel? topLevel;

    static UiScaleDecorator()
    {
        FactorProperty.Changed.AddClassHandler<Visual>(OnCacheScaleChanged);
        CacheModeProperty.Changed.AddClassHandler<Visual>(OnCacheScaleChanged);
    }

    public double MinContentWidth
    {
        get => GetValue(MinContentWidthProperty);
        set => SetValue(MinContentWidthProperty, value);
    }

    public double MinContentHeight
    {
        get => GetValue(MinContentHeightProperty);
        set => SetValue(MinContentHeightProperty, value);
    }

    public static double GetRenderScaling(Visual element, TopLevel topLevel)
    {
        return topLevel.RenderScaling * GetScalingFactor(element);
    }

    public static double GetLayoutScaling(Layoutable element)
    {
        return LayoutHelper.GetLayoutScale(element) * GetScalingFactor(element);
    }

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

    public static double GetScalingFactor(Visual element)
    {
        return element.GetValue(FactorProperty);
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
            transform.ScaleX = GetScalingFactor(this);
            transform.ScaleY = GetScalingFactor(this);
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

        SetValue(FactorProperty, factor);

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
        Child?.Arrange(new Rect(finalSize / GetScalingFactor(this)));

        return finalSize;
    }

    private static void OnCacheScaleChanged(Visual visual, AvaloniaPropertyChangedEventArgs e)
    {
        if (visual.CacheMode is BitmapCache cache)
        {
            cache.RenderAtScale = GetScalingFactor(visual);
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
