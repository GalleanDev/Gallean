using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderer
{
    private const double LightMarkerRadius = 4.0;
    private const double LightMarkerMargin = 4.0;

    private static void DrawBattleLightOverlay(BattleLightConfiguration lightConfiguration, SKCanvas canvas, IReadOnlyList<SceneLight> spriteLights, TimeSpan loopTime)
    {
        using SKPaint strokePaint = SkiaHelpers.MakeStrokePaint(Theme.DevTools.LightStrokeColor, 1);
        using SKPaint rangePaint = SkiaHelpers.MakeStrokePaint(Theme.DevTools.LightRangeColor, 1);

        SKRect visible = canvas.LocalClipBounds;
        Rect area = new(visible.Left, visible.Top, visible.Width, visible.Height);

        DrawSunMarker(lightConfiguration, canvas, area, strokePaint);

        foreach (BattleLight light in lightConfiguration.RigLights)
        {
            DrawRigLightMarker(lightConfiguration, canvas, area, light, strokePaint, rangePaint, loopTime);
        }

        foreach (SceneLight light in spriteLights)
        {
            DrawSpriteLightMarker(lightConfiguration, canvas, light, strokePaint, rangePaint);
        }
    }

    private static void DrawSunMarker(BattleLightConfiguration lightConfiguration, SKCanvas canvas, Rect area, SKPaint strokePaint)
    {
        Vector3D sunDirection = lightConfiguration.SunDirection;
        Vector toSun = new(sunDirection.X, sunDirection.Y);
        SKPoint3 radiance = GetRadiance(lightConfiguration.SunColor, lightConfiguration.SunPower);
        double size = GetLightMarkerSize(GetLuminance(radiance));
        Point marker = GetEdgePoint(GetLightMarkerArea(area, size), toSun);
        Point rayEnd = marker - toSun * size * 3;

        using SKPaint fillPaint = SkiaHelpers.MakeAntialiasFillPaint(lightConfiguration.SunColor);

        canvas.DrawLine(marker.ToSKPoint(), rayEnd.ToSKPoint(), strokePaint);
        canvas.DrawCircle(marker.ToSKPoint(), (float)size, fillPaint);
        canvas.DrawCircle(marker.ToSKPoint(), (float)size, strokePaint);
    }

    private static void DrawRigLightMarker(
        BattleLightConfiguration lightConfiguration,
        SKCanvas canvas,
        Rect area,
        BattleLight light,
        SKPaint strokePaint,
        SKPaint rangePaint,
        TimeSpan loopTime)
    {
        Point position = GetRigLightPosition(lightConfiguration, light, loopTime);
        SKPoint3 radiance = GetRigLightRadiance(lightConfiguration, light, loopTime);
        double luminance = GetLuminance(radiance);
        double markerSize = GetLightMarkerSize(luminance);
        Point marker = ClampToArea(position, GetLightMarkerArea(area, markerSize));
        SKPoint center = position.ToSKPoint();
        SKRect rect = new(
            (float)(marker.X - markerSize),
            (float)(marker.Y - markerSize),
            (float)(marker.X + markerSize),
            (float)(marker.Y + markerSize));

        using SKPaint fillPaint = SkiaHelpers.MakeAntialiasFillPaint(light.Color);

        canvas.DrawCircle(center, (float)light.Range, rangePaint);
        canvas.DrawLine(marker.ToSKPoint(), center, rangePaint);
        canvas.DrawRect(rect, fillPaint);
        canvas.DrawRect(rect, strokePaint);
    }

    private static void DrawSpriteLightMarker(BattleLightConfiguration lightConfiguration, SKCanvas canvas, SceneLight light, SKPaint strokePaint, SKPaint rangePaint)
    {
        SKPoint center = light.Position.ToSKPoint();
        double markerSize = GetLightMarkerSize(light.Power * lightConfiguration.SpriteLightGain);
        Color color = GetColor(light.Radiance);

        using SKPaint fillPaint = SkiaHelpers.MakeAntialiasFillPaint(color);

        canvas.DrawCircle(center, (float)GetSpriteLightRange(lightConfiguration, light.Radius), rangePaint);
        canvas.DrawCircle(center, (float)markerSize, fillPaint);
        canvas.DrawCircle(center, (float)markerSize, strokePaint);

        static Color GetColor(Vector3D radiance)
        {
            double maxRadiance = Math.Max(Math.Max(radiance.X, radiance.Y), Math.Max(radiance.Z, 1e-9));

            return Color.FromRgb(
                ConvertLinearToSrgb(radiance.X / maxRadiance),
                ConvertLinearToSrgb(radiance.Y / maxRadiance),
                ConvertLinearToSrgb(radiance.Z / maxRadiance));
        }
    }

    private static double GetLightMarkerSize(double luminance)
    {
        return (1 + Math.Log10(1 + luminance * 100)) * LightMarkerRadius;
    }

    private static Rect GetLightMarkerArea(Rect area, double size)
    {
        double inset = Math.Min(size + LightMarkerMargin, Math.Min(area.Width, area.Height) / 2.0);

        return area.Deflate(inset);
    }

    private static Point ClampToArea(Point point, Rect area)
    {
        return new Point(Math.Clamp(point.X, area.Left, area.Right), Math.Clamp(point.Y, area.Top, area.Bottom));
    }

    private static Point GetEdgePoint(Rect area, Vector direction)
    {
        Point center = area.Center;

        if (direction.Length > 1e-6)
        {
            double distance = Math.Min(
                GetEdgeDistance(center.X, direction.X, area.Left, area.Right),
                GetEdgeDistance(center.Y, direction.Y, area.Top, area.Bottom));

            return center + direction * distance;
        }
        else
        {
            return center;
        }
    }

    private static double GetEdgeDistance(double origin, double direction, double low, double high)
    {
        if (direction > 0)
        {
            return (high - origin) / direction;
        }
        else if (direction < 0)
        {
            return (low - origin) / direction;
        }
        else
        {
            return double.PositiveInfinity;
        }
    }

    private static double GetLuminance(SKPoint3 radiance)
    {
        return 0.2126 * radiance.X + 0.7152 * radiance.Y + 0.0722 * radiance.Z;
    }
}
