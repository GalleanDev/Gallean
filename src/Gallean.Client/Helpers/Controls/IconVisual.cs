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

    static IconVisual()
    {
        AffectsRender<IconVisual>(MonikerProperty, ForegroundProperty);
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
