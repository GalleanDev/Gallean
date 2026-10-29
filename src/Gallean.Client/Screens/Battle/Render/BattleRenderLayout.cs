using Avalonia;

namespace Gallean.Client;

internal static class BattleRenderLayout
{
    private const int BackgroundWidth = 1440;

    private const int SceneWidth = 950;
    private const int SceneHeight = 600;

    private const int BoardWidth = 800;
    private const int BoardOriginX = SceneWidth - BoardWidth;

    private const int PlaceholderSize = 80;

    private const double UnitScale = 0.95;
    private const double BattlefieldScale = 1.1;

    public const double PerspectiveScalePerPixel = 0.0003;

    private const double MarkerElevationAdjust = 6.0;

    private const int RoundDecimals = 3;

    private static readonly Point GridCenter = new(473.417, 306.667);

    private static readonly Vector FootOffset = new(PlaceholderSize / 2.0, PlaceholderSize - 6);

    private static readonly Vector SmallAnimationOffset = new(-365, -410);
    private static readonly Vector LargeAnimationOffset = new(-365, -365);

    private static readonly Vector LargeAttackerOffset = new(-30, -60);
    private static readonly Vector LargeDefenderOffset = new(35, -20);

    private static readonly double SmallMarkerRadius = ScaleMarkerRadius(0.32);
    private static readonly double LargeMarkerRadius = ScaleMarkerRadius(0.6);

    private static readonly double PlacementScale = Round(UnitScale * BattlefieldScale);

    private static readonly Vector GroundFlankStep = new(118.6, -57.0);
    private static readonly Vector GroundDepthStep = new(95.5, 68.3);

    private static readonly (int X, int Y)[] AttackerPlaceholders =
    [
        (116, 253), (245, 195), (345, 145),
        (20, 180), (145, 126), (262, 93)
    ];

    private static readonly (int X, int Y)[] DefenderPlaceholders =
    [
        (202, 320), (332, 261), (442, 200),
        (304, 406), (446, 348), (542, 265)
    ];

    public static Matrix GroundToSceneTransform { get; } = ComputeGroundTransform();
    public static Matrix SceneToGroundTransform { get; } = GroundToSceneTransform.Invert();

    private static double CameraElevation { get; } = ComputeCameraElevation();
    public static Rect BackgroundBounds { get; } = new((SceneWidth - BackgroundWidth) / 2.0, 0, BackgroundWidth, SceneHeight);
    public static Point BattlefieldCenter { get; } = new(475, 350.3);
    public static Vector3D GroundNormal { get; } = new(0, -Math.Cos(CameraElevation), Math.Sin(CameraElevation));
    public static double GroundDepthPerPixel { get; } = 1.0 / Math.Sin(CameraElevation);
    public static double SceneDepthPerPixel { get; } = 1.0 / Math.Tan(CameraElevation);
    public static double HeightPerPixel { get; } = 1.0 / Math.Cos(CameraElevation);
    public static Matrix MarkerTransform { get; } = ComputeMarkerTransform();

    public static AnimationPlacement GetUnitPlacement(BattleGridFootprint footprint)
    {
        return GetScaledPlacement(GetAnimationAnchor(footprint), GetFoot(footprint));
    }

    public static AnimationPlacement GetGridPlacement(BattleSide side)
    {
        BattleGridFootprint front = new(side, GridPosition.FrontCenter, IsLarge: false);
        BattleGridFootprint back = new(side, GridPosition.BackCenter, IsLarge: false);

        return GetScaledPlacement(
            GetCenter(GetAnimationAnchor(front), GetAnimationAnchor(back)),
            GetCenter(GetFoot(front), GetFoot(back)));
    }

    public static AnimationPlacement GetCorpsePlacement(BattleGridFootprint footprint)
    {
        Point foot = GetFoot(footprint);

        return GetScaledPlacement(foot, foot);
    }

    public static Rect GetContentBounds(AnimationPlacement placement, Rect content)
    {
        Point anchor = placement.Anchor;
        Point origin = placement.ScaleOrigin;
        double scale = placement.Scale;

        return new Rect(
            Round(origin.X + (content.X + anchor.X - origin.X) * scale),
            Round(origin.Y + (content.Y + anchor.Y - origin.Y) * scale),
            Round(content.Width * scale),
            Round(content.Height * scale));
    }

    public static Rect GetFrameBounds(AnimationPlacement placement, Rect content)
    {
        Rect bounds = GetContentBounds(placement, content);

        if (content.Y == 0 && bounds.Top > 0)
        {
            return new Rect(bounds.X, 0, bounds.Width, Round(bounds.Bottom));
        }
        else
        {
            return bounds;
        }
    }

    public static Rect GetPlaceholderBounds(BattleGridFootprint footprint)
    {
        AnimationPlacement placement = GetScaledPlacement(GetBoardAnchor(footprint), GetFoot(footprint));

        return GetContentBounds(placement, new Rect(0, 0, PlaceholderSize, PlaceholderSize));
    }

    public static Point GetFootPoint(BattleGridFootprint footprint)
    {
        return ScaleToBattlefield(GetFoot(footprint));
    }

    public static Vector GetImpactDirection(BattleGridFootprint source, BattleGridFootprint target)
    {
        Vector between = GetFootPoint(target) - GetFootPoint(source);
        Vector direction = between.Normalize();

        return new Vector(Round(direction.X), Round(direction.Y));
    }

    public static double GetMarkerRadius(BattleGridFootprint footprint)
    {
        double scale = GetPerspectiveScale(GetFootPoint(footprint));

        if (footprint.IsLarge)
        {
            return Round(LargeMarkerRadius * scale);
        }
        else
        {
            return Round(SmallMarkerRadius * scale);
        }
    }

    public static Matrix GetViewMatrix(double viewportWidth, double viewportHeight)
    {
        double scale = GetSceneScale(viewportHeight);

        return Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(GetSceneOriginX(viewportWidth, scale), 0);
    }

    public static Point ScreenToScene(Point screenPoint, double viewportWidth, double viewportHeight)
    {
        Matrix view = GetViewMatrix(viewportWidth, viewportHeight);

        return screenPoint.Transform(view.Invert());
    }

    private static Point GetBoardAnchor(BattleGridFootprint footprint)
    {
        Point placeholder = GetPlaceholderAnchor(footprint);

        if (footprint.IsLarge)
        {
            return placeholder + GetLargeAnchorOffset(footprint.Side);
        }
        else
        {
            return placeholder;
        }
    }

    private static Point GetAnimationAnchor(BattleGridFootprint footprint)
    {
        Point anchor = GetBoardAnchor(footprint);

        if (footprint.IsLarge)
        {
            return anchor + LargeAnimationOffset;
        }
        else
        {
            return anchor + SmallAnimationOffset;
        }
    }

    private static Point GetFoot(BattleGridFootprint footprint)
    {
        Point foot = GetPlaceholderAnchor(footprint) + FootOffset;

        if (footprint.IsLarge)
        {
            return foot + GetLargeFootOffset(footprint.Side);
        }
        else
        {
            return foot;
        }
    }

    private static Point GetPlaceholderAnchor(BattleGridFootprint footprint)
    {
        (int x, int y) = GetPlaceholders(footprint.Side)[footprint.Position.ToIndex()];

        return new Point(BoardOriginX + x, y);
    }

    private static (int X, int Y)[] GetPlaceholders(BattleSide side)
    {
        if (side == BattleSide.Attacker)
        {
            return AttackerPlaceholders;
        }
        else
        {
            return DefenderPlaceholders;
        }
    }

    private static Vector GetLargeAnchorOffset(BattleSide side)
    {
        if (side == BattleSide.Attacker)
        {
            return LargeAttackerOffset;
        }
        else
        {
            return LargeDefenderOffset;
        }
    }

    private static Vector GetLargeFootOffset(BattleSide side)
    {
        if (side == BattleSide.Attacker)
        {
            return GroundDepthStep * -0.5;
        }
        else
        {
            return GroundDepthStep * 0.5;
        }
    }

    private static Point GetCenter(Point first, Point second)
    {
        return first + (second - first) * 0.5;
    }

    private static double ScaleMarkerRadius(double radius)
    {
        return Round(radius * UnitScale);
    }

    private static Point ScaleToBattlefield(Point point)
    {
        return Round(BattlefieldCenter + (point - GridCenter) * BattlefieldScale);
    }

    private static AnimationPlacement GetScaledPlacement(Point anchor, Point foot)
    {
        Point origin = ScaleToBattlefield(foot);

        double scale = Round(PlacementScale * GetPerspectiveScale(origin));

        return new AnimationPlacement(Round(anchor + (origin - foot)), scale, origin);
    }

    private static double GetPerspectiveScale(Point origin)
    {
        return 1 + (origin.Y - BattlefieldCenter.Y) * PerspectiveScalePerPixel;
    }

    private static double Round(double value)
    {
        return Math.Round(value, RoundDecimals);
    }

    private static Point Round(Point point)
    {
        return new Point(Round(point.X), Round(point.Y));
    }

    private static double GetSceneScale(double viewportHeight)
    {
        return viewportHeight / SceneHeight;
    }

    private static double GetSceneOriginX(double viewportWidth, double scale)
    {
        return (viewportWidth - SceneWidth * scale) / 2.0;
    }

    private static Matrix ComputeGroundTransform()
    {
        Matrix transform = Matrix.CreateScale(BattlefieldScale, BattlefieldScale);

        transform *= new Matrix(
            GroundFlankStep.X, GroundFlankStep.Y,
            GroundDepthStep.X, GroundDepthStep.Y,
            0, 0);

        return transform;
    }

    private static double ComputeCameraElevation()
    {
        Matrix ground = GroundToSceneTransform;

        double screenArea = Math.Abs((ground.M11 * ground.M22) - (ground.M12 * ground.M21));
        double groundArea = (ground.M11 * ground.M11) + (ground.M21 * ground.M21);

        return Math.Asin(screenArea / groundArea);
    }

    private static Matrix ComputeMarkerTransform()
    {
        Matrix ground = GroundToSceneTransform;

        double width = Math.Sqrt((ground.M11 * ground.M11) + (ground.M21 * ground.M21));
        double elevation = CameraElevation - (MarkerElevationAdjust * Math.PI / 180.0);

        return Matrix.CreateScale(width, width * Math.Sin(elevation));
    }
}
