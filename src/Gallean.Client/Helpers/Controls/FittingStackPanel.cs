using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class FittingStackPanel : Panel
{
    private int fittingCount;

    static FittingStackPanel()
    {
        AffectsMeasure<FittingStackPanel>(SpacingProperty);
        AffectsArrange<FittingStackPanel>(OverflowProperty);
    }

    [GeneratedStyledProperty]
    public partial double Spacing { get; set; }

    [GeneratedStyledProperty]
    public partial double Overflow { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0;
        double height = 0;
        int count = 0;
        bool isFull = false;

        foreach (Control child in Children)
        {
            child.Measure(Size.Infinity);

            double bottom = height + child.DesiredSize.Height;

            if (count > 0)
            {
                bottom += Spacing;
            }

            if (bottom > availableSize.Height)
            {
                isFull = true;
            }

            if (!isFull)
            {
                width = Math.Max(width, child.DesiredSize.Width);
                height = bottom;
                count++;
            }
        }

        fittingCount = count;

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double top = 0;

        for (int index = 0; index < Children.Count; index++)
        {
            Control child = Children[index];

            if (index == fittingCount)
            {
                top += 2 * Overflow;
            }

            child.Arrange(new Rect(0, top, finalSize.Width, child.DesiredSize.Height));
            top += child.DesiredSize.Height + Spacing;
        }

        Clip = new RectangleGeometry(new Rect(finalSize).Inflate(Overflow));

        return finalSize;
    }
}
