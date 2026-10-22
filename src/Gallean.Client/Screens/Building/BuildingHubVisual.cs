using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class BuildingHubVisual : Control
{
    private const double TextureOpacity = 0.55;
    private const double DropShadowBlur = 8.0;
    private const double DropShadowReach = 48.0;

    private static readonly Vector DropShadowOffset = new(0.0, 3.0);
    private static readonly Vector BevelOffset = new(-1.0, -1.3);
    private static readonly Vector InnerShadowOffset = new(1.1, 1.5);

    public static readonly StyledProperty<BuildingGraphViewModel?> GraphProperty =
        AvaloniaProperty.Register<BuildingHubVisual, BuildingGraphViewModel?>(nameof(Graph));

    public static readonly StyledProperty<double> UnitSizeProperty =
        AvaloniaProperty.Register<BuildingHubVisual, double>(nameof(UnitSize));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<BuildingHubVisual, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> TextureProperty =
        AvaloniaProperty.Register<BuildingHubVisual, IBrush?>(nameof(Texture));

    public static readonly StyledProperty<IBrush?> ShadowProperty =
        AvaloniaProperty.Register<BuildingHubVisual, IBrush?>(nameof(Shadow));

    public static readonly StyledProperty<IBrush?> BevelProperty =
        AvaloniaProperty.Register<BuildingHubVisual, IBrush?>(nameof(Bevel));

    static BuildingHubVisual()
    {
        AffectsRender<BuildingHubVisual>(GraphProperty, UnitSizeProperty, FillProperty, TextureProperty, ShadowProperty, BevelProperty);
    }

    public BuildingHubVisual()
    {
        IsHitTestVisible = false;
    }

    public BuildingGraphViewModel? Graph
    {
        get => GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    public double UnitSize
    {
        get => GetValue(UnitSizeProperty);
        set => SetValue(UnitSizeProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Texture
    {
        get => GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    public IBrush? Shadow
    {
        get => GetValue(ShadowProperty);
        set => SetValue(ShadowProperty, value);
    }

    public IBrush? Bevel
    {
        get => GetValue(BevelProperty);
        set => SetValue(BevelProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        BuildingGraphViewModel? graph = Graph;

        if (graph is not null && graph.HubSize > 0)
        {
            Point origin = new(Bounds.Width / 2.0, Bounds.Height / 2.0);
            Geometry hub = OrnamentDecoration.CreateGlyph(IconMoniker.BuildingHub);
            double size = graph.HubSize * UnitSize;
            Matrix placement = OrnamentPlacement.At(origin, size);

            DrawDropShadow(context, hub, placement);

            using (context.PushTransform(placement))
            {
                if (Fill is not null)
                {
                    context.DrawGeometry(Fill, null, hub);
                }

                if (Texture is not null)
                {
                    using (context.PushOpacity(TextureOpacity))
                    {
                        context.DrawGeometry(Texture, null, hub);
                    }
                }

                DrawInsets(context, hub, 1024.0 / size);
            }
        }
    }

    private void DrawDropShadow(DrawingContext context, Geometry hub, Matrix placement)
    {
        IBrush? shadow = Shadow;

        if (shadow is not null)
        {
            Rect reach = hub.Bounds.TransformToAABB(placement).Inflate(DropShadowReach);

            using (context.PushEffect(new ImmutableBlurEffect(DropShadowBlur), reach))
            using (context.PushTransform(placement * Matrix.CreateTranslation(DropShadowOffset)))
            {
                context.DrawGeometry(shadow, null, hub);
            }
        }
    }

    private void DrawInsets(DrawingContext context, Geometry hub, double unitsPerPixel)
    {
        using (context.PushGeometryClip(hub))
        {
            DrawInset(context, Bevel, hub, BevelOffset * unitsPerPixel);
            DrawInset(context, Shadow, hub, InnerShadowOffset * unitsPerPixel);
        }
    }

    private static void DrawInset(DrawingContext context, IBrush? brush, Geometry hub, Vector offset)
    {
        if (brush is not null)
        {
            using (context.PushTransform(Matrix.CreateTranslation(offset)))
            {
                context.DrawGeometry(brush, null, hub);
            }
        }
    }
}
