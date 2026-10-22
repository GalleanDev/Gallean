using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class ModalFrameDecorator : Decorator
{
    private const double OutlineThickness = 1;
    private const double BandThickness = 2;
    private const double FrameThickness = (2 * OutlineThickness) + BandThickness;
    private const double CenterInset = OutlineThickness + (BandThickness / 2.0);
    private const double BottomCornerRadius = 10;
    private const double SpikeReach = 1.5;
    private const double SpikeBase = 8;

    private static readonly double SpikeOutset = SpikeReach + GetSpikeReach() - CenterInset;

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ModalFrameDecorator, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<ModalFrameDecorator, IBrush?>(nameof(BorderBrush));

    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        AvaloniaProperty.Register<ModalFrameDecorator, IBrush?>(nameof(OutlineBrush));

    static ModalFrameDecorator()
    {
        AffectsRender<ModalFrameDecorator>(BackgroundProperty, BorderBrushProperty, OutlineBrushProperty);

        Thickness padding = new(FrameThickness + SpikeOutset, FrameThickness + SpikeOutset, FrameThickness + SpikeOutset, FrameThickness);
        PaddingProperty.OverrideDefaultValue<ModalFrameDecorator>(padding);

        ClipToBoundsProperty.OverrideDefaultValue<ModalFrameDecorator>(false);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public IBrush? OutlineBrush
    {
        get => GetValue(OutlineBrushProperty);
        set => SetValue(OutlineBrushProperty, value);
    }

    private static Geometry FrameLine(Size size)
    {
        double left = SpikeOutset + CenterInset;
        double top = SpikeOutset + CenterInset;
        double right = size.Width - SpikeOutset - CenterInset;
        double bottom = size.Height - CenterInset;
        double radius = BottomCornerRadius - CenterInset;
        Size arc = new(radius, radius);

        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(left, top + SpikeBase));
            context.LineTo(new Point(left - SpikeReach, top - SpikeReach));
            context.LineTo(new Point(left + SpikeBase, top));
            context.LineTo(new Point(right - SpikeBase, top));
            context.LineTo(new Point(right + SpikeReach, top - SpikeReach));
            context.LineTo(new Point(right, top + SpikeBase));
            context.LineTo(new Point(right, bottom - radius));
            context.ArcTo(new Point(right - radius, bottom), arc, 0, false, SweepDirection.Clockwise);
            context.LineTo(new Point(left + radius, bottom));
            context.ArcTo(new Point(left, bottom - radius), arc, 0, false, SweepDirection.Clockwise);
            context.EndFigure(true);
        }

        return geometry;
    }

    public override void Render(DrawingContext context)
    {
        Geometry line = FrameLine(Bounds.Size);
        context.DrawGeometry(Background, null, line);

        IBrush? outline = OutlineBrush;
        if (outline is not null)
        {
            context.DrawGeometry(null, new Pen(outline, FrameThickness, lineJoin: PenLineJoin.Miter), line);
        }

        IBrush? border = BorderBrush;
        if (border is not null)
        {
            context.DrawGeometry(null, new Pen(border, BandThickness, lineJoin: PenLineJoin.Miter), line);
        }
    }

    private static double GetSpikeReach()
    {
        double edgeAngle = Math.Atan2(SpikeBase + SpikeReach, SpikeReach);
        double halfAngle = edgeAngle - (Math.PI / 4.0);
        double length = FrameThickness / 2.0 / Math.Sin(halfAngle);

        return length / Math.Sqrt(2.0);
    }
}
