using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gallean.Client;

internal sealed class BuildingGraphVisual : Control, BuildingStatus.IVisitor<BuildingGraphVisual.LinkStyle>
{
    internal readonly record struct LinkStyle(int Order, double Thickness, double Opacity, DashStyle? Dash);

    private const double CurveTension = 0.3;
    private const double RingThickness = 1;
    private const double RingEndSize = 2;
    private const double LabelPadding = 14;
    private const double ForkRadius = 4;
    private const double ForkOutlineThickness = 1.5;
    private const double LabelAngleDegrees = 180;
    private const double LabelGap = 4;

    private static readonly DashStyle LinkDash = new([4, 3], 0);
    private static readonly DashStyle RequirementDash = new([0, 2.5], 0);
    private static readonly LinkStyle SelectionLockedStyle = new(0, 1.2, 0.35, LinkDash);

    public static readonly StyledProperty<BuildingGraphViewModel?> GraphProperty =
        AvaloniaProperty.Register<BuildingGraphVisual, BuildingGraphViewModel?>(nameof(Graph));

    public static readonly StyledProperty<BuildingNodeViewModel?> SelectedNodeProperty =
        AvaloniaProperty.Register<BuildingGraphVisual, BuildingNodeViewModel?>(nameof(SelectedNode));

    public static readonly StyledProperty<double> UnitSizeProperty =
        AvaloniaProperty.Register<BuildingGraphVisual, double>(nameof(UnitSize));

    public static readonly StyledProperty<IBrush?> RingBrushProperty =
        AvaloniaProperty.Register<BuildingGraphVisual, IBrush?>(nameof(RingBrush));

    public static readonly StyledProperty<IBrush?> ForkOutlineBrushProperty =
        AvaloniaProperty.Register<BuildingGraphVisual, IBrush?>(nameof(ForkOutlineBrush));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<BuildingGraphVisual>();

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner<BuildingGraphVisual>();

    public static readonly StyledProperty<double> FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner<BuildingGraphVisual>();

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner<BuildingGraphVisual>();

    public static readonly StyledProperty<IBrush?> OutlineProperty =
        AvaloniaProperty.Register<BuildingGraphVisual, IBrush?>(nameof(Outline));

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        AvaloniaProperty.Register<BuildingGraphVisual, double>(nameof(OutlineThickness));

    static BuildingGraphVisual()
    {
        AffectsRender<BuildingGraphVisual>(
            GraphProperty,
            SelectedNodeProperty,
            UnitSizeProperty,
            RingBrushProperty,
            ForkOutlineBrushProperty,
            ForegroundProperty,
            FontFamilyProperty,
            FontSizeProperty,
            FontWeightProperty,
            OutlineProperty,
            OutlineThicknessProperty);
    }

    public BuildingGraphVisual()
    {
        IsHitTestVisible = false;
    }

    public BuildingGraphViewModel? Graph
    {
        get => GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    public BuildingNodeViewModel? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public double UnitSize
    {
        get => GetValue(UnitSizeProperty);
        set => SetValue(UnitSizeProperty, value);
    }

    public IBrush? RingBrush
    {
        get => GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    public IBrush? ForkOutlineBrush
    {
        get => GetValue(ForkOutlineBrushProperty);
        set => SetValue(ForkOutlineBrushProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
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
        BuildingGraphViewModel? graph = Graph;

        if (graph is not null && !graph.IsEmpty)
        {
            Point origin = new(Bounds.Width / 2.0, Bounds.Height / 2.0);

            DrawRings(context, graph, origin);
            DrawRingLabels(context, graph, origin);
            DrawTrunks(context, graph, origin);
            DrawLinks(context, graph, origin);
        }
    }

    LinkStyle BuildingStatus.IVisitor<LinkStyle>.Visit(BuildingStatus.Constructible status)
    {
        return new LinkStyle(3, 2.2, 0.95, null);
    }

    LinkStyle BuildingStatus.IVisitor<LinkStyle>.Visit(BuildingStatus.Built status)
    {
        return new LinkStyle(4, 3.0, 1.0, null);
    }

    LinkStyle BuildingStatus.IVisitor<LinkStyle>.Visit(BuildingStatus.BranchLocked status)
    {
        return new LinkStyle(0, 1.2, 0.25, LinkDash);
    }

    LinkStyle BuildingStatus.IVisitor<LinkStyle>.Visit(BuildingStatus.MissingPrerequisite status)
    {
        return new LinkStyle(1, 1.5, 0.55, LinkDash);
    }

    LinkStyle BuildingStatus.IVisitor<LinkStyle>.Visit(BuildingStatus.ScenarioLocked status)
    {
        return new LinkStyle(0, 1.2, 0.3, LinkDash);
    }

    LinkStyle BuildingStatus.IVisitor<LinkStyle>.Visit(BuildingStatus.Unavailable status)
    {
        return new LinkStyle(2, 2.0, 0.7, null);
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        return Color.FromArgb((byte)Math.Round(color.A * opacity), color.R, color.G, color.B);
    }

    private static Vector Delta(Point from, Point to)
    {
        Point moved = to - from;

        return new Vector(moved.X, moved.Y);
    }

    private static Vector Direction(Point from, Point to)
    {
        Vector delta = Delta(from, to);

        if (delta.Length <= 0)
        {
            return default;
        }
        else
        {
            return delta / delta.Length;
        }
    }

    private static StreamGeometry BuildCurve(Point origin, Point start, Point end)
    {
        double distance = Delta(start, end).Length;
        Point control1 = start + Direction(origin, start) * (distance * CurveTension);
        Point control2 = end - Direction(origin, end) * (distance * CurveTension);
        StreamGeometry geometry = new();

        using StreamGeometryContext path = geometry.Open();
        path.BeginFigure(start, isFilled: false);
        path.CubicBezierTo(control1, control2, end);
        path.EndFigure(isClosed: false);

        return geometry;
    }

    private static void DrawCurve(
        DrawingContext context,
        Point origin,
        Point start,
        Point end,
        Color color,
        LinkStyle style)
    {
        ImmutableSolidColorBrush brush = new(WithOpacity(color, style.Opacity));
        Pen pen = new(brush, style.Thickness, style.Dash, PenLineCap.Round);

        context.DrawGeometry(null, pen, BuildCurve(origin, start, end));
    }

    private Point ToPoint(Point origin, double x, double y)
    {
        return new Point(origin.X + x * UnitSize, origin.Y + y * UnitSize);
    }

    private Point HubEdgeTowards(BuildingGraphViewModel graph, Point origin, double x, double y)
    {
        double normalizedX = x / graph.HubRadiusX;
        double normalizedY = y / graph.HubRadiusY;
        double scale = 1.0 / Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

        return ToPoint(origin, x * scale, y * scale);
    }

    private void DrawRings(DrawingContext context, BuildingGraphViewModel graph, Point origin)
    {
        IBrush? ringBrush = RingBrush;

        if (ringBrush is not null)
        {
            Pen pen = new(ringBrush, RingThickness);
            Typeface typeface = new(FontFamily, FontStyle.Normal, FontWeight);
            double labelAngle = double.DegreesToRadians(LabelAngleDegrees);

            foreach (BuildingRingViewModel ring in graph.Rings)
            {
                double radiusX = ring.Radius * UnitSize;
                double radiusY = radiusX * graph.ProjectionY;
                double gap = (MeasureLabel(ring, typeface) / 2.0 + LabelPadding) / RingSpeed(radiusX, radiusY, labelAngle);
                double start = labelAngle + gap;
                double end = labelAngle + 2.0 * Math.PI - gap;

                context.DrawGeometry(null, pen, BuildArc(origin, radiusX, radiusY, start, end));
                DrawArcEnd(context, ringBrush, origin, radiusX, radiusY, start);
                DrawArcEnd(context, ringBrush, origin, radiusX, radiusY, end);
            }
        }
    }

    private static StreamGeometry BuildArc(Point origin, double radiusX, double radiusY, double start, double end)
    {
        StreamGeometry geometry = new();

        using (StreamGeometryContext path = geometry.Open())
        {
            path.BeginFigure(RingPoint(origin, radiusX, radiusY, start), isFilled: false);
            path.ArcTo(RingPoint(origin, radiusX, radiusY, end), new Size(radiusX, radiusY), 0.0, end - start > Math.PI, SweepDirection.Clockwise);
            path.EndFigure(isClosed: false);
        }

        return geometry;
    }

    private static void DrawArcEnd(
        DrawingContext context,
        IBrush brush,
        Point origin,
        double radiusX,
        double radiusY,
        double angle)
    {
        Point center = RingPoint(origin, radiusX, radiusY, angle);
        Vector normal = RingNormal(radiusX, radiusY, angle) * RingEndSize;
        Vector tangent = new(-normal.Y, normal.X);
        StreamGeometry marker = new();

        using (StreamGeometryContext path = marker.Open())
        {
            path.BeginFigure(center + normal, isFilled: true);
            path.LineTo(center + tangent);
            path.LineTo(center - normal);
            path.LineTo(center - tangent);
            path.EndFigure(isClosed: true);
        }

        context.DrawGeometry(brush, null, marker);
    }

    private double MeasureLabel(BuildingRingViewModel ring, Typeface typeface)
    {
        double length = 0.0;

        foreach (char character in ring.Title)
        {
            FormattedText glyph = new(character.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, FontSize, Brushes.Transparent);

            length += glyph.WidthIncludingTrailingWhitespace;
        }

        return length;
    }

    private void DrawRingLabels(DrawingContext context, BuildingGraphViewModel graph, Point origin)
    {
        IBrush? foreground = Foreground;

        if (foreground is not null)
        {
            Typeface typeface = new(FontFamily, FontStyle.Normal, FontWeight);
            Pen? outline = CreateOutlinePen();

            foreach (BuildingRingViewModel ring in graph.Rings)
            {
                DrawRingLabel(context, graph, origin, ring, typeface, foreground, outline);
            }
        }
    }

    private void DrawRingLabel(
        DrawingContext context,
        BuildingGraphViewModel graph,
        Point origin,
        BuildingRingViewModel ring,
        Typeface typeface,
        IBrush foreground,
        Pen? outline)
    {
        double radiusX = ring.Radius * UnitSize;
        double radiusY = radiusX * graph.ProjectionY;
        double labelAngle = double.DegreesToRadians(LabelAngleDegrees);
        double speed = RingSpeed(radiusX, radiusY, labelAngle);
        List<FormattedText> glyphs = [];
        double length = 0.0;

        foreach (char character in ring.Title)
        {
            FormattedText glyph = new(character.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, FontSize, foreground);

            glyphs.Add(glyph);
            length += glyph.WidthIncludingTrailingWhitespace;
        }

        double angle = labelAngle - length / 2.0 / speed;

        foreach (FormattedText glyph in glyphs)
        {
            double advance = glyph.WidthIncludingTrailingWhitespace;
            double center = angle + advance / 2.0 / speed;
            Vector normal = RingNormal(radiusX, radiusY, center);
            Point baseline = RingPoint(origin, radiusX, radiusY, center) + normal * LabelGap;
            double rotation = Math.Atan2(normal.Y, normal.X) + Math.PI / 2.0;
            Matrix transform = Matrix.CreateTranslation(-advance / 2.0, -glyph.Baseline) *
                               Matrix.CreateRotation(rotation) *
                               Matrix.CreateTranslation(baseline.X, baseline.Y);
            Geometry? geometry = glyph.BuildGeometry(default);

            if (geometry is not null)
            {
                using (context.PushTransform(transform))
                {
                    if (outline is not null)
                    {
                        context.DrawGeometry(null, outline, geometry);
                    }

                    context.DrawGeometry(foreground, null, geometry);
                }
            }

            angle += advance / speed;
        }
    }

    private static Point RingPoint(Point origin, double radiusX, double radiusY, double angle)
    {
        return new Point(origin.X + radiusX * Math.Cos(angle), origin.Y + radiusY * Math.Sin(angle));
    }

    private static Vector RingNormal(double radiusX, double radiusY, double angle)
    {
        Vector gradient = new(Math.Cos(angle) / radiusX, Math.Sin(angle) / radiusY);

        return gradient / gradient.Length;
    }

    private static double RingSpeed(double radiusX, double radiusY, double angle)
    {
        Vector velocity = new(-radiusX * Math.Sin(angle), radiusY * Math.Cos(angle));

        return velocity.Length;
    }

    private Pen? CreateOutlinePen()
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

    private void DrawTrunks(DrawingContext context, BuildingGraphViewModel graph, Point origin)
    {
        foreach (BuildingBranchViewModel branch in graph.Branches)
        {
            List<BuildingNodeViewModel> roots = [];

            foreach (BuildingNodeViewModel node in graph.Nodes)
            {
                if (node.IsBranchRoot && node.Branch == branch)
                {
                    roots.Add(node);
                }
            }

            if (roots.Count > 0)
            {
                Point fork = ToPoint(origin, branch.ForkX, branch.ForkY);
                LinkStyle style = ResolveStyle(roots[0]);

                foreach (BuildingNodeViewModel root in roots)
                {
                    LinkStyle rootStyle = ResolveStyle(root);

                    if (rootStyle.Order > style.Order)
                    {
                        style = rootStyle;
                    }
                }

                DrawCurve(context, origin, HubEdgeTowards(graph, origin, branch.ForkX, branch.ForkY), fork, branch.Color, style);

                foreach (BuildingNodeViewModel root in roots)
                {
                    DrawConnection(context, origin, fork, root, ResolveStyle(root));
                }

                DrawFork(context, fork, branch.Color, style);
            }
        }
    }

    private void DrawFork(DrawingContext context, Point fork, Color color, LinkStyle style)
    {
        ImmutableSolidColorBrush fill = new(WithOpacity(color, style.Opacity));
        Pen? outline = ForkOutlineBrush is null ? null : new Pen(ForkOutlineBrush, ForkOutlineThickness);

        context.DrawEllipse(fill, outline, fork, ForkRadius, ForkRadius);
    }

    private void DrawLinks(DrawingContext context, BuildingGraphViewModel graph, Point origin)
    {
        foreach (BuildingLink link in graph.Links)
        {
            DrawConnection(context, origin, ToPoint(origin, link.From.X, link.From.Y), link.To, ResolveStyle(link));
        }
    }

    private LinkStyle ResolveStyle(BuildingLink link)
    {
        LinkStyle style = ResolveStyle(link.To);

        return link.Type switch
        {
            BuildingLinkType.Upgrade => style,
            BuildingLinkType.Requirement => style with { Dash = RequirementDash }
        };
    }

    private LinkStyle ResolveStyle(BuildingNodeViewModel node)
    {
        if (node.IsSelectionLocked)
        {
            return SelectionLockedStyle;
        }
        else
        {
            return node.Status.Accept(this);
        }
    }

    private void DrawConnection(
        DrawingContext context,
        Point origin,
        Point start,
        BuildingNodeViewModel target,
        LinkStyle style)
    {
        Point end = ToPoint(origin, target.X, target.Y);

        DrawCurve(context, origin, start, end, target.BranchColor, style);
    }
}
