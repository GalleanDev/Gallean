using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class AnimatedNumberTextBlock : TextBlock
{
    public static readonly StyledProperty<IReadOnlyList<AnimationProperty>> AnimationProperty =
        AvaloniaProperty.Register<AnimatedNumberTextBlock, IReadOnlyList<AnimationProperty>>(nameof(Animation), []);

    private readonly AnimationPropertyPlayer player = new();

    public AnimatedNumberTextBlock()
    {
        UpdateText();
    }

    [GeneratedStyledProperty]
    public partial double Value { get; set; }

    public IReadOnlyList<AnimationProperty> Animation
    {
        get => GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ValueProperty)
        {
            UpdateText();
        }

        if (e.Property == AnimationProperty && IsLoaded)
        {
            player.Start(this, ValueProperty, Animation);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        player.Start(this, ValueProperty, Animation);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        player.Stop();
    }

    private void UpdateText()
    {
        int value = (int)Math.Round(Value);

        Text = value.ToString(CultureInfo.CurrentCulture);
    }
}
