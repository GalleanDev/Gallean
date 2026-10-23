using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Gallean.Client;

internal class OutlinedTextBlock : TextBlock
{
    public static readonly StyledProperty<IBrush?> OutlineProperty =
        AvaloniaProperty.Register<OutlinedTextBlock, IBrush?>(nameof(Outline));

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        AvaloniaProperty.Register<OutlinedTextBlock, double>(nameof(OutlineThickness));

    private TextLayout? arrangedLayout;
    private Size arrangedSize;

    static OutlinedTextBlock()
    {
        ClipToBoundsProperty.OverrideDefaultValue<OutlinedTextBlock>(false);
        AffectsRender<OutlinedTextBlock>(OutlineProperty, OutlineThicknessProperty);
    }

    public IBrush? Outline
    {
        get => GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    public double OutlineThickness
    {
        get => GetValue(OutlineThicknessProperty);
        set => SetValue(OutlineThicknessProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!ReferenceEquals(arrangedLayout, TextLayout) || arrangedSize != finalSize)
        {
            Size size = base.ArrangeOverride(finalSize);

            arrangedLayout = TextLayout;
            arrangedSize = finalSize;

            return size;
        }
        else
        {
            return finalSize;
        }
    }

    protected override void RenderTextLayout(DrawingContext context, Point origin)
    {
        Pen? pen = CreateOutlinePen();

        if (pen is not null)
        {
            DrawOutline(context, origin, pen);
        }

        base.RenderTextLayout(context, origin);
    }

    protected Pen? CreateOutlinePen()
    {
        if (Outline is not null && OutlineThickness > 0)
        {
            return new Pen(Outline, OutlineThickness * 2, lineJoin: PenLineJoin.Round);
        }
        else
        {
            return null;
        }
    }

    private void DrawOutline(DrawingContext context, Point origin, Pen pen)
    {
        double top = origin.Y;

        foreach (TextLine line in TextLayout.TextLines)
        {
            double left = origin.X + line.Start;

            foreach (TextRun run in line.TextRuns)
            {
                if (run is ShapedTextRun shaped)
                {
                    Point runOrigin = new(left, top + line.Baseline - shaped.Baseline);

                    using (context.PushTransform(Matrix.CreateTranslation(runOrigin)))
                    {
                        context.DrawGeometry(null, pen, shaped.GlyphRun.BuildGeometry());
                    }
                }

                if (run is DrawableTextRun drawable)
                {
                    left += drawable.Size.Width;
                }
            }

            top += line.Height;
        }
    }
}
