using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed class ArcPanel : Panel
{
    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<ArcPanel, double>(nameof(Radius), 200);

    public static readonly StyledProperty<double> ItemAngleProperty =
        AvaloniaProperty.Register<ArcPanel, double>(nameof(ItemAngle), 15);

    public static readonly StyledProperty<double> SpreadProperty =
        AvaloniaProperty.Register<ArcPanel, double>(nameof(Spread), 1);

    private Size slot;

    static ArcPanel()
    {
        AffectsMeasure<ArcPanel>(RadiusProperty, ItemAngleProperty);
        AffectsArrange<ArcPanel>(SpreadProperty);
    }

    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public double ItemAngle
    {
        get => GetValue(ItemAngleProperty);
        set => SetValue(ItemAngleProperty, value);
    }

    public double Spread
    {
        get => GetValue(SpreadProperty);
        set => SetValue(SpreadProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0;
        double height = 0;

        foreach (Control child in Children)
        {
            child.Measure(Size.Infinity);

            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        slot = new Size(width, height);

        double reach = Math.Max(Children.Count - 1, 0) / 2.0;
        Point end = GetOffset(reach, spread: 1);

        return new Size((2 * end.X) + slot.Width, end.Y + slot.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double spread = Spread;
        double left = (finalSize.Width - slot.Width) / 2.0;
        double middle = (Children.Count - 1) / 2.0;

        for (int index = 0; index < Children.Count; index++)
        {
            Point offset = GetOffset(index - middle, spread);

            Children[index].Arrange(new Rect(new Point(left + offset.X, offset.Y), slot));
        }

        return finalSize;
    }

    private Point GetOffset(double position, double spread)
    {
        double radius = Radius;
        double angle = position * ItemAngle * spread * Math.PI / 180.0;

        return new Point(radius * Math.Sin(angle), radius * (1 - Math.Cos(angle)));
    }
}
