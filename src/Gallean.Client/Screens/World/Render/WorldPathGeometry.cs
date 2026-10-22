using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class WorldPathGeometry
{
    private readonly record struct Dimensions(
        double DashLength,
        double DashGap,
        double DashWidth,
        double DashOutlineWidth,
        double DashOutlineOverhang,
        double DashPixelsPerSecond,
        double WaypointHalfWidth,
        double WaypointHalfHeight,
        double WaypointOutlineWidth,
        double WaypointInteriorScale,
        double WaypointCenterScale,
        double LabelFontSize,
        double LabelOutlineWidth,
        double LabelGap,
        Size OriginRadius,
        Size WaypointRadius,
        Size DestinationRadius);

    private readonly Dimensions dimensions;

    private WorldPathGeometry(Dimensions dimensions)
    {
        this.dimensions = dimensions;

        Waypoint = BuildDiamond(dimensions, 1.0);
        WaypointInterior = BuildDiamond(dimensions, dimensions.WaypointInteriorScale);
        WaypointCenter = BuildDiamond(dimensions, dimensions.WaypointCenterScale);
    }

    public static WorldPathGeometry SelectedPath { get; } = new(new Dimensions(
        DashLength: 4.0,
        DashGap: 3.0,
        DashWidth: 1.6,
        DashOutlineWidth: 2.8,
        DashOutlineOverhang: 0.4,
        DashPixelsPerSecond: 6.0,
        WaypointHalfWidth: 6.6,
        WaypointHalfHeight: 5.2,
        WaypointOutlineWidth: 2.6,
        WaypointInteriorScale: 0.55,
        WaypointCenterScale: 0.25,
        LabelFontSize: 9.0,
        LabelOutlineWidth: 2.4,
        LabelGap: 2.0,
        OriginRadius: new Size(18.0, 9.0),
        WaypointRadius: new Size(9.0, 9.0),
        DestinationRadius: new Size(22.0, 11.0)));

    public double DashLength => dimensions.DashLength;
    public double DashGap => dimensions.DashGap;
    public double DashPeriod => dimensions.DashLength + dimensions.DashGap;
    public double DashWidth => dimensions.DashWidth;
    public double DashOutlineWidth => dimensions.DashOutlineWidth;
    public double DashOutlineOverhang => dimensions.DashOutlineOverhang;
    public double DashPixelsPerSecond => dimensions.DashPixelsPerSecond;
    public double WaypointOutlineWidth => dimensions.WaypointOutlineWidth;
    public double WaypointGradientInnerRadius => dimensions.WaypointHalfWidth * dimensions.WaypointInteriorScale;
    public double WaypointGradientOuterRadius => dimensions.WaypointHalfWidth;
    public double WaypointLabelOffset => dimensions.WaypointHalfHeight + dimensions.LabelGap;
    public double DestinationLabelOffset => dimensions.DestinationRadius.Height + dimensions.LabelGap;
    public double LabelFontSize => dimensions.LabelFontSize;
    public double LabelOutlineWidth => dimensions.LabelOutlineWidth;
    public Size OriginRadius => dimensions.OriginRadius;
    public Size WaypointRadius => dimensions.WaypointRadius;
    public Size DestinationRadius => dimensions.DestinationRadius;
    public SKPath Waypoint { get; }
    public SKPath WaypointInterior { get; }
    public SKPath WaypointCenter { get; }

    public static Point GetEdge(Point center, Vector direction, Size radius)
    {
        double x = direction.X / radius.Width;
        double y = direction.Y / radius.Height;

        return center + (direction / Math.Sqrt((x * x) + (y * y)));
    }

    private static SKPath BuildDiamond(Dimensions dimensions, double scale)
    {
        float halfWidth = (float)(dimensions.WaypointHalfWidth * scale);
        float halfHeight = (float)(dimensions.WaypointHalfHeight * scale);

        SKPathBuilder builder = new();

        builder.MoveTo(0, -halfHeight);
        builder.LineTo(halfWidth, 0);
        builder.LineTo(0, halfHeight);
        builder.LineTo(-halfWidth, 0);
        builder.Close();

        return builder.Detach();
    }
}
