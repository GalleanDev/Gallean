using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class ArcTrack : Control
{
    private const double ShadowBlur = 6;
    private const double ShadowReach = 18;

    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<ArcTrack, double>(nameof(Radius), 200);

    public static readonly StyledProperty<double> ItemAngleProperty =
        AvaloniaProperty.Register<ArcTrack, double>(nameof(ItemAngle), 15);

    public static readonly StyledProperty<double> SpreadProperty =
        AvaloniaProperty.Register<ArcTrack, double>(nameof(Spread), 1);

    public static readonly StyledProperty<int> ItemCountProperty =
        AvaloniaProperty.Register<ArcTrack, int>(nameof(ItemCount));

    public static readonly StyledProperty<double> PaddingProperty =
        AvaloniaProperty.Register<ArcTrack, double>(nameof(Padding));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ArcTrack, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<ArcTrack, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<IBrush?> ShadowProperty =
        AvaloniaProperty.Register<ArcTrack, IBrush?>(nameof(Shadow));

    static ArcTrack()
    {
        AffectsRender<ArcTrack>(RadiusProperty, ItemAngleProperty, SpreadProperty, ItemCountProperty, PaddingProperty, FillProperty, StrokeProperty, ShadowProperty);
        ClipToBoundsProperty.OverrideDefaultValue<ArcTrack>(false);
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

    public int ItemCount
    {
        get => GetValue(ItemCountProperty);
        set => SetValue(ItemCountProperty, value);
    }

    public double Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public IBrush? Shadow
    {
        get => GetValue(ShadowProperty);
        set => SetValue(ShadowProperty, value);
    }

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
