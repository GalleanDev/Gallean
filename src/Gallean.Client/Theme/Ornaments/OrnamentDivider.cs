using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class OrnamentDivider : Control
{
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<OrnamentDivider, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<double> CurlRadiusProperty =
        AvaloniaProperty.Register<OrnamentDivider, double>(nameof(CurlRadius), 5);

    static OrnamentDivider()
    {
        AffectsRender<OrnamentDivider>(StrokeProperty, CurlRadiusProperty);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double CurlRadius
    {
        get => GetValue(CurlRadiusProperty);
        set => SetValue(CurlRadiusProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double height = (CurlRadius * 2.0) + 4;
        double width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;

        return new Size(width, height);
    }

    public override void Render(DrawingContext context)
    {
        IBrush? stroke = Stroke;

        if (stroke is null)
        {
            return;
        }

        double middle = Math.Round(Bounds.Height / 2.0) + 0.5;
        double center = Bounds.Width / 2.0;
        double radius = CurlRadius;
        double gap = radius * 2.4;
        Pen pen = new(stroke);

        context.DrawLine(pen, new Point(0, middle), new Point(center - gap, middle));
        context.DrawLine(pen, new Point(center + gap, middle), new Point(Bounds.Width, middle));
        Geometry spike = OrnamentShape.Spike(gap + 2, 2.2);
        Geometry leaf = OrnamentShape.Leaf(1.4, 0.45);
        Matrix spikePlacement = Matrix.CreateTranslation(center + 3, middle);
        Matrix leafPlacement = Matrix.CreateRotation(Math.PI / 2.0) * OrnamentPlacement.At(new Point(center + (radius * 1.2), middle), radius);
        Matrix left = OrnamentPlacement.MirrorHorizontally(center);
        Matrix up = OrnamentPlacement.MirrorVertically(middle);
        Matrix[] spikePlacements = [spikePlacement, spikePlacement * left];
        Matrix[] leafPlacements = [leafPlacement, leafPlacement * left, leafPlacement * up, leafPlacement * left * up];

        using (context.PushTransform(OrnamentPlacement.At(new Point(center, middle), 3.2)))
        {
            context.DrawGeometry(stroke, null, OrnamentShape.Diamond());
        }

        foreach (Matrix placement in spikePlacements)
        {
            using (context.PushTransform(placement))
            {
                context.DrawGeometry(stroke, null, spike);
            }
        }

        foreach (Matrix placement in leafPlacements)
        {
            using (context.PushTransform(placement))
            {
                context.DrawGeometry(stroke, null, leaf);
            }
        }
    }
}
