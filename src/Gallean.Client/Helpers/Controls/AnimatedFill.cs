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

    public static readonly StyledProperty<IReadOnlyList<AnimationProperty>> AnimationProperty =
        AvaloniaProperty.Register<AnimatedFill, IReadOnlyList<AnimationProperty>>(nameof(Animation), []);

    private readonly AnimationPropertyPlayer player = new();

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

    public IReadOnlyList<AnimationProperty> Animation
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
            player.Start(this, CurrentProperty, Animation);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        player.Start(this, CurrentProperty, Animation);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        player.Stop();
    }
}
