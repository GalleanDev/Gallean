using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class IconVisual : Control
{
    public static readonly StyledProperty<IconMoniker> MonikerProperty =
        AvaloniaProperty.Register<IconVisual, IconMoniker>(nameof(Moniker));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<IconVisual>();

    public static readonly StyledProperty<IBrush?> OutlineProperty =
        AvaloniaProperty.Register<IconVisual, IBrush?>(nameof(Outline));

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        AvaloniaProperty.Register<IconVisual, double>(nameof(OutlineThickness));

    static IconVisual()
    {
        AffectsRender<IconVisual>(MonikerProperty, ForegroundProperty, OutlineProperty, OutlineThicknessProperty);
    }

    public IconMoniker Moniker
    {
        get => GetValue(MonikerProperty);
        set => SetValue(MonikerProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
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

    public override void Render(DrawingContext context)
    {
        IconGeometry icon = Icons.Get(Moniker);
        double scale = Math.Min(Bounds.Width, Bounds.Height) / icon.ViewBox;
        if (scale > 0)
        {
            double drawnSize = icon.ViewBox * scale;
            double offsetX = (Bounds.Width - drawnSize) / 2.0;
            double offsetY = (Bounds.Height - drawnSize) / 2.0;

            using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(offsetX, offsetY)))
            {
                if (Outline is not null && OutlineThickness > 0)
                {
                    Pen pen = new(Outline, OutlineThickness * 2 / scale, lineJoin: PenLineJoin.Round);

                    foreach (IconLayer layer in icon.Layers)
                    {
                        context.DrawGeometry(null, pen, layer.Geometry);
                    }
                }

                foreach (IconLayer layer in icon.Layers)
                {
                    IBrush? brush = layer.Brush ?? Foreground;

                    if (brush is not null)
                    {
                        context.DrawGeometry(brush, null, layer.Geometry);
                    }
                }
            }
        }
    }
}
