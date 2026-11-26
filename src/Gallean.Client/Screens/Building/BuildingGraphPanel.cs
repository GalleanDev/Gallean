using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class BuildingGraphPanel : Panel
{
    static BuildingGraphPanel()
    {
        AffectsParentArrange<BuildingGraphPanel>(XProperty, YProperty);
        AffectsMeasure<BuildingGraphPanel>(UnitSizeProperty, ExtentXProperty, ExtentYProperty);
    }

    [GeneratedStyledProperty]
    public partial double UnitSize { get; set; }

    [GeneratedStyledProperty]
    public partial double ExtentX { get; set; }

    [GeneratedStyledProperty]
    public partial double ExtentY { get; set; }

    [GeneratedAttachedProperty]
    public static partial double GetX(Control control);

    [GeneratedAttachedProperty]
    public static partial double GetY(Control control);

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (Control child in Children)
        {
            child.Measure(Size.Infinity);
        }

        return new Size(2.0 * ExtentX * UnitSize, 2.0 * ExtentY * UnitSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double centerX = finalSize.Width / 2.0;
        double centerY = finalSize.Height / 2.0;

        foreach (Control child in Children)
        {
            Size size = child.DesiredSize;
            double x = centerX + GetX(child) * UnitSize - size.Width / 2.0;
            double y = centerY + GetY(child) * UnitSize - size.Height / 2.0;

            child.Arrange(new Rect(x, y, size.Width, size.Height));
        }

        return finalSize;
    }
}
