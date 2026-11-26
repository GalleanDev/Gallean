using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class PopupLayoutTransformControl : LayoutTransformControl
{
    public PopupLayoutTransformControl()
    {
        UpdateLayoutTransform();
    }

    [GeneratedStyledProperty]
    public partial Transform? Transform { get; set; }

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
            double factor = UiScaleDecorator.GetFactor(this);

            LayoutTransform = new ScaleTransform(factor, factor);
        }
    }
}
