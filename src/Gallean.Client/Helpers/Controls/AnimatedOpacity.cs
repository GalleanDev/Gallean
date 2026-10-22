using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed class AnimatedOpacity : Panel
{
    public static readonly StyledProperty<IReadOnlyList<AnimationProperty>> AnimationProperty =
        AvaloniaProperty.Register<AnimatedOpacity, IReadOnlyList<AnimationProperty>>(nameof(Animation), []);

    private readonly AnimationPropertyPlayer player = new();

    public IReadOnlyList<AnimationProperty> Animation
    {
        get => GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == AnimationProperty)
        {
            if (Animation.Count > 0)
            {
                Opacity = 0;
            }
            else
            {
                Opacity = 1;
            }

            if (IsLoaded)
            {
                player.Start(this, OpacityProperty, Animation);
            }
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        player.Start(this, OpacityProperty, Animation);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        player.Stop();
    }
}
