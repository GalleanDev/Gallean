using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class IconVisual : Control
{
    static IconVisual()
    {
        AffectsRender<IconVisual>(MonikerProperty, ForegroundProperty, OutlineProperty, OutlineThicknessProperty);
    }

    [GeneratedStyledProperty]
    public partial IconMoniker Moniker { get; set; }

    [GeneratedStyledProperty(AddOwnerFrom = typeof(TextElement))]
    public partial IBrush? Foreground { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Outline { get; set; }

    [GeneratedStyledProperty]
    public partial double OutlineThickness { get; set; }

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
