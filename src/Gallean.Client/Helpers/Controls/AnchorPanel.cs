using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class AnchorPanel : Panel
{
    static AnchorPanel()
    {
        AffectsArrange<AnchorPanel>(AnchorProperty);
    }

    [GeneratedStyledProperty]
    public partial RelativePoint Anchor { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (Control child in Children)
        {
            child.Measure(Size.Infinity);
        }

        return default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Point anchor = Anchor.ToPixels(finalSize);
        double scale = UiScaleDecorator.GetRenderScaling(this);

        foreach (Control child in Children)
        {
            Size size = child.DesiredSize;
            double left = Math.Round((anchor.X - (size.Width / 2.0)) * scale) / scale;
            double top = Math.Round((anchor.Y - size.Height) * scale) / scale;

            child.Arrange(new Rect(left, top, size.Width, size.Height));
        }

        return finalSize;
    }
}
