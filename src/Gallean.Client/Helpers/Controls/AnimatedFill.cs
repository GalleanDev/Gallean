using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class AnimatedFill : Control
{
    public static readonly StyledProperty<double> CurrentProperty =
        AvaloniaProperty.Register<AnimatedFill, double>(nameof(Current));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<AnimatedFill, double>(nameof(Maximum));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<AnimatedFill, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<AnimationRange> AnimationProperty =
        AvaloniaProperty.Register<AnimatedFill, AnimationRange>(nameof(Animation), AnimationRange.None);

    private readonly AnimationPropertyPlayer currentPlayer = new();
    private readonly AnimationPropertyPlayer maximumPlayer = new();

    static AnimatedFill()
    {
        AffectsRender<AnimatedFill>(CurrentProperty, MaximumProperty, FillProperty);
    }

    public AnimatedFill()
    {
        IsHitTestVisible = false;
    }

    public double Current
    {
        get => GetValue(CurrentProperty);
        set => SetValue(CurrentProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public AnimationRange Animation
    {
        get => GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        IBrush? fill = Fill;

        if (fill is null)
        {
            return;
        }

        double top = Bounds.Height * GetRatio();

        if (top < Bounds.Height)
        {
            context.FillRectangle(fill, new Rect(0, top, Bounds.Width, Bounds.Height - top));
        }
    }

    private double GetRatio()
    {
        if (Maximum > 0)
        {
            return Math.Clamp(Current / Maximum, 0, 1);
        }
        else
        {
            return 1;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == AnimationProperty && IsLoaded)
        {
            StartAnimation();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        StartAnimation();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        currentPlayer.Stop();
        maximumPlayer.Stop();
    }

    private void StartAnimation()
    {
        currentPlayer.Start(this, CurrentProperty, Animation.Current);
        maximumPlayer.Start(this, MaximumProperty, Animation.Maximum);
    }
}
