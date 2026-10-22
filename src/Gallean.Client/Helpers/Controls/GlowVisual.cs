using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gallean.Client;

internal sealed class GlowVisual : Control
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<GlowVisual, Color>(nameof(Color));

    static GlowVisual()
    {
        AffectsRender<GlowVisual>(ColorProperty);
    }

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        Color color = Color;
        RelativeScalar radius = new(0.5, RelativeUnit.Relative);
        ImmutableRadialGradientBrush brush = new(
            [
                new ImmutableGradientStop(0.0, Color.FromArgb((byte)(color.A * 0.85), color.R, color.G, color.B)),
                new ImmutableGradientStop(0.5, Color.FromArgb((byte)(color.A * 0.35), color.R, color.G, color.B)),
                new ImmutableGradientStop(1.0, Color.FromArgb(0, color.R, color.G, color.B))
            ],
            radiusX: radius,
            radiusY: radius);

        context.DrawEllipse(brush, null, new Rect(Bounds.Size));
    }
}
