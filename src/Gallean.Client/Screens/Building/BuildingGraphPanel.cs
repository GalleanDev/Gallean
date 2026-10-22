using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed class BuildingGraphPanel : Panel
{
    public static readonly AttachedProperty<double> XProperty =
        AvaloniaProperty.RegisterAttached<BuildingGraphPanel, Control, double>("X");

    public static readonly AttachedProperty<double> YProperty =
        AvaloniaProperty.RegisterAttached<BuildingGraphPanel, Control, double>("Y");

    public static readonly StyledProperty<double> UnitSizeProperty =
        AvaloniaProperty.Register<BuildingGraphPanel, double>(nameof(UnitSize));

    public static readonly StyledProperty<double> ExtentXProperty =
        AvaloniaProperty.Register<BuildingGraphPanel, double>(nameof(ExtentX));

    public static readonly StyledProperty<double> ExtentYProperty =
        AvaloniaProperty.Register<BuildingGraphPanel, double>(nameof(ExtentY));

    static BuildingGraphPanel()
    {
        AffectsParentArrange<BuildingGraphPanel>(XProperty, YProperty);
        AffectsMeasure<BuildingGraphPanel>(UnitSizeProperty, ExtentXProperty, ExtentYProperty);
    }

    public double UnitSize
    {
        get => GetValue(UnitSizeProperty);
        set => SetValue(UnitSizeProperty, value);
    }

    public double ExtentX
    {
        get => GetValue(ExtentXProperty);
        set => SetValue(ExtentXProperty, value);
    }

    public double ExtentY
    {
        get => GetValue(ExtentYProperty);
        set => SetValue(ExtentYProperty, value);
    }

    public static double GetX(Control control)
    {
        return control.GetValue(XProperty);
    }

    public static void SetX(Control control, double value)
    {
        control.SetValue(XProperty, value);
    }

    public static double GetY(Control control)
    {
        return control.GetValue(YProperty);
    }

    public static void SetY(Control control, double value)
    {
        control.SetValue(YProperty, value);
    }

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
