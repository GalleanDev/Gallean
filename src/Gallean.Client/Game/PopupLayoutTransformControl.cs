using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class PopupLayoutTransformControl : LayoutTransformControl
{
    public static readonly StyledProperty<Transform?> TransformProperty =
        AvaloniaProperty.Register<PopupLayoutTransformControl, Transform?>(nameof(Transform));

    public PopupLayoutTransformControl()
    {
        UpdateLayoutTransform();
    }

    public Transform? Transform
    {
        get => GetValue(TransformProperty);
        set => SetValue(TransformProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == TransformProperty || e.Property == UiScaleDecorator.FactorProperty)
        {
            UpdateLayoutTransform();
        }
    }

    private void UpdateLayoutTransform()
    {
        Transform? transform = Transform;

        if (transform is not null)
        {
            LayoutTransform = transform;
        }
        else
        {
            double factor = UiScaleDecorator.GetScalingFactor(this);

            LayoutTransform = new ScaleTransform(factor, factor);
        }
    }
}
