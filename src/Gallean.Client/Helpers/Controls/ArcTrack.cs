using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class ArcTrack : Control
{
    private const double ShadowBlur = 6;
    private const double ShadowReach = 18;

    static ArcTrack()
    {
        AffectsRender<ArcTrack>(RadiusProperty, ItemAngleProperty, SpreadProperty, ItemCountProperty, PaddingProperty, FillProperty, StrokeProperty, ShadowProperty);
        ClipToBoundsProperty.OverrideDefaultValue<ArcTrack>(false);
    }

    [GeneratedStyledProperty(DefaultValue = 200.0)]
    public partial double Radius { get; set; }

    [GeneratedStyledProperty(DefaultValue = 15.0)]
    public partial double ItemAngle { get; set; }

    [GeneratedStyledProperty(DefaultValue = 1.0)]
    public partial double Spread { get; set; }

    [GeneratedStyledProperty]
    public partial int ItemCount { get; set; }

    [GeneratedStyledProperty]
    public partial double Padding { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Fill { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Stroke { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Shadow { get; set; }

    private static Geometry Arc(Point center, double radius, double halfAngle)
    {
        StreamGeometry geometry = new();
        Point start = new(center.X - (radius * Math.Sin(halfAngle)), center.Y - (radius * Math.Cos(halfAngle)));
        Point end = new(center.X + (radius * Math.Sin(halfAngle)), center.Y - (radius * Math.Cos(halfAngle)));

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(start, false);
            context.ArcTo(end, new Size(radius, radius), 0, halfAngle > Math.PI / 2.0, SweepDirection.Clockwise);
            context.EndFigure(false);
        }

        return geometry;
    }

    public override void Render(DrawingContext context)
    {
        double radius = Radius;
        double reach = Math.Max(ItemCount - 1, 0) / 2.0 * ItemAngle * Math.PI / 180.0;
        double slot = Bounds.Height - (radius * (1 - Math.Cos(reach)));
        double thickness = slot + (2 * Padding);
        Point center = new(Bounds.Width / 2.0, (slot / 2.0) + radius);
        Geometry arc = Arc(center, radius, reach * Spread);
        Geometry band = arc.GetWidenedGeometry(new Pen(Brushes.Black, thickness, lineCap: PenLineCap.Round));

        IBrush? shadow = Shadow;
        if (shadow is not null)
        {
            using (context.PushEffect(new ImmutableBlurEffect(ShadowBlur), new Rect(Bounds.Size).Inflate(ShadowReach)))
            {
                context.DrawGeometry(shadow, null, band);
            }
        }

        context.DrawGeometry(Fill, null, band);

        IBrush? stroke = Stroke;
        if (stroke is not null)
        {
            context.DrawGeometry(null, new Pen(stroke, 1), band);
        }
    }
}
