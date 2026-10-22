using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering;

namespace Gallean.Client;

internal sealed class PlateChrome : Control, ICustomHitTest
{
    public sealed record Lighting(Visual Space, IReadOnlyList<Lighting.Light> Lights, double Ambient)
    {
        public readonly record struct Light(Point Position, double Height, Color Color, double Intensity, double Radius);
    }

    private readonly record struct TexturePatch(IBrush Mask, Rect Region);

    private readonly record struct PlateGeometry(Geometry Outline, Geometry Face, TexturePatch[] TexturePatches, Geometry[] Trim, Geometry? Gap);

    private const double SpikeRatio = 0.3;
    private const double CornerRadius = 6;
    private const double TickReach = 0.11;
    private const double TickHalfHeight = 0.055;
    private const double PanelTickReach = 4;
    private const double PanelTickHalfHeight = 2.2;
    private const double GlowWidth = 6;
    private const double GlowBlur = 4;
    private const double GlowReach = 14;
    private const double GlowHaloBlur = 10;
    private const double GlowHaloReach = 32;

    public static readonly StyledProperty<PlateChromeShapeType> ShapeProperty =
        AvaloniaProperty.Register<PlateChrome, PlateChromeShapeType>(nameof(Shape), PlateChromeShapeType.Spiked);

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<PlateChrome, double>(nameof(StrokeThickness), 4);

    public static readonly StyledProperty<CornerRadius> PanelRadiusProperty =
        AvaloniaProperty.Register<PlateChrome, CornerRadius>(nameof(PanelRadius));

    public static readonly StyledProperty<IBrush?> TrimProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(Trim));

    public static readonly StyledProperty<IBrush?> OuterShadowProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(OuterShadow));

    public static readonly StyledProperty<IBrush?> GlowProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(Glow));

    public static readonly StyledProperty<IBrush?> GlowHaloProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(GlowHalo));

    public static readonly StyledProperty<IBrush?> BevelProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(Bevel));

    public static readonly StyledProperty<IBrush?> OutlineProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(Outline));

    public static readonly StyledProperty<double> InnerShadowDepthProperty =
        AvaloniaProperty.Register<PlateChrome, double>(nameof(InnerShadowDepth), 4);

    public static readonly StyledProperty<IBrush?> InnerShadowProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(InnerShadow));

    public static readonly StyledProperty<IBrush?> GlossProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(Gloss));

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> TextureProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(Texture));

    public static readonly StyledProperty<double> TrailingSectionWidthProperty =
        AvaloniaProperty.Register<PlateChrome, double>(nameof(TrailingSectionWidth));

    public static readonly StyledProperty<IBrush?> TrailingSectionOverlayProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(TrailingSectionOverlay));

    public static readonly StyledProperty<IBrush?> TrailingSectionDividerProperty =
        AvaloniaProperty.Register<PlateChrome, IBrush?>(nameof(TrailingSectionDivider));

    public static readonly AttachedProperty<int> TextureSeedProperty =
        AvaloniaProperty.RegisterAttached<PlateChrome, Control, int>("TextureSeed", 1);

    public static readonly AttachedProperty<Lighting?> LightingProperty =
        AvaloniaProperty.RegisterAttached<PlateChrome, Control, Lighting?>("Lighting", inherits: true);

    static PlateChrome()
    {
        AffectsRender<PlateChrome>(ShapeProperty, StrokeProperty, StrokeThicknessProperty, PanelRadiusProperty, TrimProperty, OuterShadowProperty, GlowProperty, GlowHaloProperty, InnerShadowProperty, BevelProperty, OutlineProperty, InnerShadowDepthProperty, GlossProperty, FillProperty, TextureProperty, TrailingSectionWidthProperty, TrailingSectionOverlayProperty, TrailingSectionDividerProperty, TextureSeedProperty, LightingProperty);
        ClipToBoundsProperty.OverrideDefaultValue<PlateChrome>(false);
    }

    public PlateChromeShapeType Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public CornerRadius PanelRadius
    {
        get => GetValue(PanelRadiusProperty);
        set => SetValue(PanelRadiusProperty, value);
    }

    public IBrush? Trim
    {
        get => GetValue(TrimProperty);
        set => SetValue(TrimProperty, value);
    }

    public IBrush? OuterShadow
    {
        get => GetValue(OuterShadowProperty);
        set => SetValue(OuterShadowProperty, value);
    }

    public IBrush? Glow
    {
        get => GetValue(GlowProperty);
        set => SetValue(GlowProperty, value);
    }

    public IBrush? GlowHalo
    {
        get => GetValue(GlowHaloProperty);
        set => SetValue(GlowHaloProperty, value);
    }

    public IBrush? Bevel
    {
        get => GetValue(BevelProperty);
        set => SetValue(BevelProperty, value);
    }

    public IBrush? Outline
    {
        get => GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    public double InnerShadowDepth
    {
        get => GetValue(InnerShadowDepthProperty);
        set => SetValue(InnerShadowDepthProperty, value);
    }

    public IBrush? InnerShadow
    {
        get => GetValue(InnerShadowProperty);
        set => SetValue(InnerShadowProperty, value);
    }

    public IBrush? Gloss
    {
        get => GetValue(GlossProperty);
        set => SetValue(GlossProperty, value);
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

    public double TrailingSectionWidth
    {
        get => GetValue(TrailingSectionWidthProperty);
        set => SetValue(TrailingSectionWidthProperty, value);
    }

    public IBrush? TrailingSectionOverlay
    {
        get => GetValue(TrailingSectionOverlayProperty);
        set => SetValue(TrailingSectionOverlayProperty, value);
    }

    public IBrush? TrailingSectionDivider
    {
        get => GetValue(TrailingSectionDividerProperty);
        set => SetValue(TrailingSectionDividerProperty, value);
    }

    public static int GetTextureSeed(Control control)
    {
        return control.GetValue(TextureSeedProperty);
    }

    public static void SetTextureSeed(Control control, int value)
    {
        control.SetValue(TextureSeedProperty, value);
    }

    public static Lighting? GetLighting(Control control)
    {
        return control.GetValue(LightingProperty);
    }

    public static void SetLighting(Control control, Lighting? value)
    {
        control.SetValue(LightingProperty, value);
    }

    public bool HitTest(Point point)
    {
        return new Rect(Bounds.Size).Contains(point);
    }

    private static IBrush MakeEndMask(bool fromLeft)
    {
        LinearGradientBrush mask = new()
        {
            StartPoint = new RelativePoint(fromLeft ? 0.0 : 1.0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(fromLeft ? 1.0 : 0.0, 0.5, RelativeUnit.Relative)
        };

        mask.GradientStops.Add(new GradientStop(Colors.Black, 0.0));
        mask.GradientStops.Add(new GradientStop(Colors.Black, 0.35));
        mask.GradientStops.Add(new GradientStop(Colors.Transparent, 1.0));

        return mask;
    }

    private static IBrush MakeEdgeMask()
    {
        RadialGradientBrush mask = new()
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative)
        };

        mask.GradientStops.Add(new GradientStop(Colors.Transparent, 0.0));
        mask.GradientStops.Add(new GradientStop(Colors.Transparent, 0.35));
        mask.GradientStops.Add(new GradientStop(Colors.Black, 1.0));

        return mask;
    }

    private static Geometry MakeRoundedPlate(Rect rect, double spike, double radius)
    {
        StreamGeometry geometry = new();

        double middle = rect.Top + (rect.Height / 2.0);
        double slope = (rect.Height / 2.0) / spike;
        double along = radius / Math.Sqrt(1 + (slope * slope));
        double rise = along * slope;

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(rect.Left + spike + radius, rect.Top), true);
            context.LineTo(new Point(rect.Right - spike - radius, rect.Top));
            context.QuadraticBezierTo(new Point(rect.Right - spike, rect.Top), new Point(rect.Right - spike + along, rect.Top + rise));
            context.LineTo(new Point(rect.Right - along, middle - rise));
            context.QuadraticBezierTo(new Point(rect.Right, middle), new Point(rect.Right - along, middle + rise));
            context.LineTo(new Point(rect.Right - spike + along, rect.Bottom - rise));
            context.QuadraticBezierTo(new Point(rect.Right - spike, rect.Bottom), new Point(rect.Right - spike - radius, rect.Bottom));
            context.LineTo(new Point(rect.Left + spike + radius, rect.Bottom));
            context.QuadraticBezierTo(new Point(rect.Left + spike, rect.Bottom), new Point(rect.Left + spike - along, rect.Bottom - rise));
            context.LineTo(new Point(rect.Left + along, middle + rise));
            context.QuadraticBezierTo(new Point(rect.Left, middle), new Point(rect.Left + along, middle - rise));
            context.LineTo(new Point(rect.Left + spike - along, rect.Top + rise));
            context.QuadraticBezierTo(new Point(rect.Left + spike, rect.Top), new Point(rect.Left + spike + radius, rect.Top));
            context.EndFigure(true);
        }

        return geometry;
    }

    private static PlateGeometry MakeSpikedGeometry(Rect outer, Rect faceRect, double thickness)
    {
        double spike = outer.Height * SpikeRatio;
        double reach = Math.Min(faceRect.Width * 0.38, faceRect.Height * 3.2);
        Rect leftEnd = new(faceRect.Left, faceRect.Top, reach, faceRect.Height);
        Rect rightEnd = new(faceRect.Right - reach, faceRect.Top, reach, faceRect.Height);

        return new PlateGeometry(
            MakeRoundedPlate(outer, spike, CornerRadius),
            MakeRoundedPlate(faceRect, spike - (thickness * 0.3), CornerRadius * 0.5),
            [new TexturePatch(MakeEndMask(fromLeft: true), leftEnd), new TexturePatch(MakeEndMask(fromLeft: false), rightEnd)],
            [],
            null);
    }

    private static PlateGeometry MakeRoundGeometry(Rect outer, Rect faceRect)
    {
        return new PlateGeometry(
            new EllipseGeometry(outer),
            new EllipseGeometry(faceRect),
            [new TexturePatch(MakeEdgeMask(), faceRect)],
            [],
            null);
    }

    private static PlateGeometry MakeSquareGeometry(Rect outer, Rect faceRect)
    {
        double radius = Math.Min(CornerRadius, outer.Height * SpikeRatio);

        return new PlateGeometry(
            new RectangleGeometry(outer, radius, radius),
            new RectangleGeometry(faceRect, radius * 0.5, radius * 0.5),
            [new TexturePatch(MakeEdgeMask(), faceRect)],
            [],
            null);
    }

    private static Geometry MakeOctagon(Rect rect, double cut)
    {
        return MakePolygon(
        [
            new Point(rect.Left + cut, rect.Top),
            new Point(rect.Right - cut, rect.Top),
            new Point(rect.Right, rect.Top + cut),
            new Point(rect.Right, rect.Bottom - cut),
            new Point(rect.Right - cut, rect.Bottom),
            new Point(rect.Left + cut, rect.Bottom),
            new Point(rect.Left, rect.Bottom - cut),
            new Point(rect.Left, rect.Top + cut)
        ]);
    }

    private static PlateGeometry MakeOctagonGeometry(Rect outer, Rect faceRect, double thickness)
    {
        double cut = Math.Min(outer.Width, outer.Height) * SpikeRatio;

        return new PlateGeometry(
            MakeOctagon(outer, cut),
            MakeOctagon(faceRect, cut - (thickness * 0.4)),
            [new TexturePatch(MakeEdgeMask(), faceRect)],
            [],
            null);
    }

    private static Geometry MakePolygon(IReadOnlyList<Point> points)
    {
        StreamGeometry geometry = new();

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(points[0]);

            for (int index = 1; index < points.Count; index++)
            {
                context.LineTo(points[index]);
            }

            context.EndFigure(true);
        }

        return geometry;
    }

    private static Geometry MakeStadium(Rect rect)
    {
        StreamGeometry geometry = new();
        double radius = rect.Height / 2.0;
        Size arc = new(radius, radius);

        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new Point(rect.Left + radius, rect.Top), true);
            context.LineTo(new Point(rect.Right - radius, rect.Top));
            context.ArcTo(new Point(rect.Right - radius, rect.Bottom), arc, 0, false, SweepDirection.Clockwise);
            context.LineTo(new Point(rect.Left + radius, rect.Bottom));
            context.ArcTo(new Point(rect.Left + radius, rect.Top), arc, 0, false, SweepDirection.Clockwise);
            context.EndFigure(true);
        }

        return geometry;
    }

    private static PlateGeometry MakePillGeometry(Rect outer, Rect faceRect, double thickness)
    {
        double reach = outer.Height * TickReach;
        double halfHeight = outer.Height * TickHalfHeight;
        double edge = (thickness / 2.0) - reach;
        Geometry[] trim =
        [
            MakeRhombus(new Point(outer.Left + edge, outer.Center.Y), reach, halfHeight),
            MakeRhombus(new Point(outer.Right - edge, outer.Center.Y), reach, halfHeight)
        ];

        return new PlateGeometry(
            MakeStadium(outer),
            MakeStadium(faceRect),
            [new TexturePatch(MakeEdgeMask(), faceRect)],
            trim,
            MakeStadium(faceRect.Deflate(1.5)));
    }

    private static Geometry MakeRhombus(Point center, double reach, double halfHeight)
    {
        return MakePolygon(
        [
            new Point(center.X - reach, center.Y),
            new Point(center.X, center.Y - halfHeight),
            new Point(center.X + reach, center.Y),
            new Point(center.X, center.Y + halfHeight)
        ]);
    }

    private static PlateGeometry MakePanelGeometry(Rect outer, Rect faceRect, double thickness, double radius)
    {
        double faceRadius = radius - (thickness / 2.0);
        double diagonal = Math.Sqrt(0.5);
        double outline = (radius * (1 - diagonal)) - (thickness / 2.0 * diagonal);
        double offset = outline + (PanelTickReach * diagonal);

        Geometry[] ticks =
        [
            MakeDiagonalTick(new Point(outer.Left + offset, outer.Top + offset), new Vector(diagonal, diagonal), PanelTickReach, PanelTickHalfHeight),
            MakeDiagonalTick(new Point(outer.Right - offset, outer.Top + offset), new Vector(-diagonal, diagonal), PanelTickReach, PanelTickHalfHeight),
            MakeDiagonalTick(new Point(outer.Left + offset, outer.Bottom - offset), new Vector(diagonal, -diagonal), PanelTickReach, PanelTickHalfHeight),
            MakeDiagonalTick(new Point(outer.Right - offset, outer.Bottom - offset), new Vector(-diagonal, -diagonal), PanelTickReach, PanelTickHalfHeight)
        ];

        return new PlateGeometry(
            new RectangleGeometry(outer, radius, radius),
            new RectangleGeometry(faceRect, faceRadius, faceRadius),
            [new TexturePatch(MakeEdgeMask(), faceRect)],
            ticks,
            new RectangleGeometry(faceRect.Deflate(1.5), faceRadius - 1.5, faceRadius - 1.5));
    }

    private static Geometry MakeDiagonalTick(Point center, Vector direction, double reach, double halfHeight)
    {
        Vector across = new(-direction.Y, direction.X);

        return MakePolygon(
        [
            center - (direction * reach),
            center + (across * halfHeight),
            center + (direction * reach),
            center - (across * halfHeight)
        ]);
    }

    private void DrawTexture(DrawingContext context, IBrush texture, IBrush mask, Rect region)
    {
        int seed = GetTextureSeed(this);
        double offsetX = (seed * 97) % 512;
        double offsetY = (seed * 31) % 128;
        Matrix shift = Matrix.CreateTranslation(-offsetX, -offsetY);
        Rect tile = region.Translate(new Vector(offsetX, offsetY));

        using (context.PushOpacityMask(mask, region))
        using (context.PushTransform(shift))
        {
            context.FillRectangle(texture, tile);
        }
    }

    private void DrawTrailingSection(DrawingContext context, Geometry face, Rect faceRect)
    {
        double left = Bounds.Width - TrailingSectionWidth;

        using (context.PushGeometryClip(face))
        {
            IBrush? overlay = TrailingSectionOverlay;

            if (overlay is not null)
            {
                context.FillRectangle(overlay, new Rect(left, faceRect.Top, faceRect.Right - left, faceRect.Height));
            }

            IBrush? divider = TrailingSectionDivider;

            if (divider is not null)
            {
                context.DrawLine(new Pen(divider, 1), new Point(left + 0.5, faceRect.Top), new Point(left + 0.5, faceRect.Bottom));
            }
        }
    }

    private List<Lighting.Light> ProjectLights(Lighting lighting)
    {
        List<Lighting.Light> lights = [];

        foreach (Lighting.Light light in lighting.Lights)
        {
            Point? position = lighting.Space.TranslatePoint(light.Position, this);

            if (position is not null)
            {
                lights.Add(light with { Position = position.Value });
            }
        }

        return lights;
    }

    public override void Render(DrawingContext context)
    {
        IBrush? stroke = Stroke;

        if (stroke is null)
        {
            return;
        }

        double thickness = StrokeThickness;
        Rect outer = new Rect(Bounds.Size).Deflate(thickness / 2.0);
        Rect faceRect = outer.Deflate(thickness / 2.0);
        PlateGeometry geometry = Shape switch
        {
            PlateChromeShapeType.Spiked => MakeSpikedGeometry(outer, faceRect, thickness),
            PlateChromeShapeType.Round => MakeRoundGeometry(outer, faceRect),
            PlateChromeShapeType.Square => MakeSquareGeometry(outer, faceRect),
            PlateChromeShapeType.Octagon => MakeOctagonGeometry(outer, faceRect, thickness),
            PlateChromeShapeType.Pill => MakePillGeometry(outer, faceRect, thickness),
            PlateChromeShapeType.Panel => MakePanelGeometry(outer, faceRect, thickness, PanelRadius.TopLeft)
        };

        IBrush? outerShadow = OuterShadow;
        if (outerShadow is not null)
        {
            using (context.PushEffect(new ImmutableBlurEffect(1.5), new Rect(Bounds.Size).Inflate(6)))
            {
                context.DrawGeometry(null, new Pen(outerShadow, thickness + 1.5, lineJoin: PenLineJoin.Round), geometry.Outline);
            }
        }

        IBrush? glowHalo = GlowHalo;
        if (glowHalo is not null)
        {
            using (context.PushEffect(new ImmutableBlurEffect(GlowHaloBlur), new Rect(Bounds.Size).Inflate(GlowHaloReach)))
            {
                context.DrawGeometry(null, new Pen(glowHalo, thickness + GlowWidth, lineJoin: PenLineJoin.Round), geometry.Outline);
            }
        }

        IBrush? glow = Glow;
        if (glow is not null)
        {
            using (context.PushEffect(new ImmutableBlurEffect(GlowBlur), new Rect(Bounds.Size).Inflate(GlowReach)))
            {
                context.DrawGeometry(null, new Pen(glow, thickness + GlowWidth, lineJoin: PenLineJoin.Round), geometry.Outline);
            }
        }

        IBrush? texture = Texture;

        context.DrawGeometry(Fill, null, geometry.Face);

        if (texture is not null)
        {
            using (context.PushGeometryClip(geometry.Face))
            {
                foreach (TexturePatch texturePatch in geometry.TexturePatches)
                {
                    DrawTexture(context, texture, texturePatch.Mask, texturePatch.Region);
                }
            }
        }

        if (TrailingSectionWidth > 0)
        {
            DrawTrailingSection(context, geometry.Face, faceRect);
        }

        IBrush? innerShadow = InnerShadow;
        IBrush? gloss = Gloss;

        if (innerShadow is not null || gloss is not null)
        {
            using (context.PushGeometryClip(geometry.Face))
            {
                if (gloss is not null)
                {
                    Rect upper = new(faceRect.Left, faceRect.Top, faceRect.Width, faceRect.Height / 2.0);

                    context.FillRectangle(gloss, upper);
                }

                if (innerShadow is not null)
                {
                    double depth = InnerShadowDepth;

                    using (context.PushEffect(new ImmutableBlurEffect(depth / 2.0), faceRect.Inflate(depth * 2)))
                    {
                        context.DrawGeometry(null, new Pen(innerShadow, depth, lineJoin: PenLineJoin.Round), geometry.Face);
                    }
                }
            }
        }

        IBrush? outline = Outline;

        if (outline is not null)
        {
            Pen outlinePen = new(outline, thickness + 1.5, lineJoin: PenLineJoin.Round);

            context.DrawGeometry(null, outlinePen, geometry.Outline);

            if (geometry.Gap is not null)
            {
                context.DrawGeometry(null, new Pen(outline, 1), geometry.Gap);
            }
        }

        IBrush? bevel = Bevel;

        if (bevel is not null)
        {
            context.DrawGeometry(null, new Pen(bevel, 1), geometry.Face);
        }

        context.DrawGeometry(null, new Pen(stroke, thickness, lineJoin: PenLineJoin.Round), geometry.Outline);

        IBrush trimBrush = Trim ?? stroke;

        foreach (Geometry trim in geometry.Trim)
        {
            if (outline is not null)
            {
                context.DrawGeometry(null, new Pen(outline, 1.5, lineJoin: PenLineJoin.Miter), trim);
            }

            context.DrawGeometry(trimBrush, null, trim);
        }

        Lighting? lighting = GetLighting(this);

        if (lighting is not null)
        {
            Rect bounds = new(Bounds.Size);
            double tipLength = outer.Height * SpikeRatio;

            context.Custom(new PlateLightRenderOperation(bounds, Shape, thickness, tipLength, CornerRadius, ProjectLights(lighting), lighting.Ambient));
        }
    }
}
