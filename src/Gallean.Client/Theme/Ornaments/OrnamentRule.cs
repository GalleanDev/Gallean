using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class OrnamentRule : Control
{
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<OrnamentRule, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<IBrush?> LineProperty =
        AvaloniaProperty.Register<OrnamentRule, IBrush?>(nameof(Line));

    public static readonly StyledProperty<IBrush?> PatternProperty =
        AvaloniaProperty.Register<OrnamentRule, IBrush?>(nameof(Pattern));

    public static readonly StyledProperty<OrnamentRuleType> TypeProperty =
        AvaloniaProperty.Register<OrnamentRule, OrnamentRuleType>(nameof(Type));

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<OrnamentRule, double>(nameof(Size), 4);

    static OrnamentRule()
    {
        AffectsRender<OrnamentRule>(StrokeProperty, LineProperty, PatternProperty, TypeProperty, SizeProperty);
        AffectsMeasure<OrnamentRule>(TypeProperty, SizeProperty);
        ClipToBoundsProperty.OverrideDefaultValue<OrnamentRule>(false);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public IBrush? Line
    {
        get => GetValue(LineProperty);
        set => SetValue(LineProperty, value);
    }

    public IBrush? Pattern
    {
        get => GetValue(PatternProperty);
        set => SetValue(PatternProperty, value);
    }

    public OrnamentRuleType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    private double RuleOffset => Type switch
    {
        OrnamentRuleType.Plain => 0.5,
        OrnamentRuleType.Node => Size + 0.5,
        OrnamentRuleType.Drop => Size + 0.5,
        OrnamentRuleType.Center => Size * 3.0,
        OrnamentRuleType.Peak => Size * 1.6,
        OrnamentRuleType.Scroll => Size
    };

    private double TotalHeight => Type switch
    {
        OrnamentRuleType.Plain => 1,
        OrnamentRuleType.Node => (Size * 2) + 1,
        OrnamentRuleType.Drop => (Size * 4.4) + 1,
        OrnamentRuleType.Center => Size * 9,
        OrnamentRuleType.Peak => Size * 3.2,
        OrnamentRuleType.Scroll => Size * 8
    };

    private static void DrawOrnament(DrawingContext context, IBrush brush, Geometry shape, Matrix placement)
    {
        using (context.PushTransform(placement))
        {
            context.DrawGeometry(brush, null, shape);
        }
    }

    private void DrawScroll(DrawingContext context, IBrush brush, Point center)
    {
        double period = Size * 9;
        double height = Size * 2.1;
        double crest = 0.78;
        double end = center.X - (Size * 20.5);
        double top = center.Y + (Size * 0.75);

        Pen hairline = new(brush, Size * 0.4);

        int periods = (int)Math.Ceiling(end / period) + 1;
        double start = end - ((periods + crest) * period);
        Geometry wave = OrnamentShape.Wave(periods + crest, period, height * 0.4, crest);
        Geometry swash = OrnamentShape.Swash(period, height, Size * 0.8);

        foreach (Matrix side in new[] { Matrix.Identity, OrnamentPlacement.MirrorHorizontally(center.X) })
        {
            using (context.PushTransform(Matrix.CreateTranslation(start, top + (height * 0.6)) * side))
            {
                context.DrawGeometry(null, hairline, wave);
            }

            for (int index = 0; index < periods; index++)
            {
                using (context.PushTransform(Matrix.CreateTranslation(start + (index * period), top) * side))
                {
                    context.DrawGeometry(brush, null, swash);
                }
            }
        }
    }

    private void DrawComposition(DrawingContext context, IBrush brush, Point center)
    {
        Geometry finial = OrnamentDecoration.CreateGlyph(IconMoniker.OrnamentFinial);
        Geometry curl = OrnamentDecoration.CreateGlyph(IconMoniker.OrnamentCurl);
        Matrix placement = OrnamentPlacement.At(center, Size);
        Matrix finialPlacement = Matrix.CreateRotation(Math.PI / 2.0) * OrnamentPlacement.At(center, Size * 16.6);
        Matrix scrollPlacement = Matrix.CreateRotation(Math.PI) * OrnamentPlacement.At(new Point(center.X + (Size * 27), center.Y + (Size * 2)), Size * 27);

        switch (Type)
        {
            case OrnamentRuleType.Plain:
                break;
            case OrnamentRuleType.Node:
                DrawOrnament(context, brush, OrnamentDecoration.Node, placement);
                break;
            case OrnamentRuleType.Drop:
                DrawOrnament(context, brush, OrnamentDecoration.Drop, placement);
                break;
            case OrnamentRuleType.Center:
                DrawOrnament(context, brush, finial, finialPlacement);
                break;
            case OrnamentRuleType.Peak:
                DrawOrnament(context, brush, OrnamentShape.Diamond(), placement);
                break;
            case OrnamentRuleType.Scroll:
                DrawOrnament(context, brush, curl, scrollPlacement);
                DrawOrnament(context, brush, curl, scrollPlacement * OrnamentPlacement.MirrorHorizontally(center.X));
                break;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;

        return new Size(width, TotalHeight);
    }

    public override void Render(DrawingContext context)
    {
        IBrush? stroke = Stroke;

        if (stroke is null)
        {
            return;
        }

        double y = Math.Round(RuleOffset) + 0.5;
        Point center = new(Bounds.Width / 2.0, y);

        Pen line = new(Line ?? stroke);

        if (Type == OrnamentRuleType.Peak)
        {
            double rise = Size * 1.1;
            Pen bright = new(Line ?? stroke, 1.5);
            Point left = new(0, y + rise);
            Point right = new(Bounds.Width, y + rise);
            Point peak = new(center.X, y);
            context.DrawLine(bright, left, peak);
            context.DrawLine(bright, peak, right);
        }
        else if (Type == OrnamentRuleType.Scroll)
        {
            IBrush? pattern = Pattern;
            context.FillRectangle(Line ?? stroke, new Rect(0, y - 0.5, Bounds.Width, 2));

            if (pattern is not null)
            {
                DrawScroll(context, pattern, center);
            }
        }
        else
        {
            context.DrawLine(line, new Point(0, y), new Point(Bounds.Width, y));
        }

        DrawComposition(context, stroke, center);
    }
}
