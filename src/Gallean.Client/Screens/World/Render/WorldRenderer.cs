using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using Gallean.Common;
using SkiaSharp;
using static Gallean.Client.WorldRenderContent;
using static Gallean.Client.WorldRenderContentProvider;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderer
{
    private const double CullMargin = 160.0;
    private const double ShadowOffset = 1.5;
    private const double CursorRuneOpacity = 0.8;
    private const double CursorGhostOpacity = 0.3;
    private const double StackGhostOpacity = 0.5;
    private const float HighlightGlowRadius = 12.0f;
    private const double BattleTransitionZoom = 4.5;
    private const double BattleTransitionFocalLength = 1.2;
    private const double BattleTransitionCentering = 0.6;
    private const double BattleTransitionBlur = 0.07;
    private const double BattleTransitionVignette = 0.75;
    private const double BattleTransitionDesaturation = 0.35;

    private static readonly double BattleTransitionTilt = double.DegreesToRadians(28.0);

    public void Render(
        SKCanvas canvas,
        WorldRenderContent content,
        WorldRenderState state,
        IAssetProvider assets,
        TimeSpan loopTime,
        TimeSpan animationTime,
        float renderResolution,
        bool hitboxOverlay)
    {
        BattleTransition? transition = state.Animation?.Accept(BattleTransitionVisitor.Instance, Empty.Value);

        if (transition is not null)
        {
            RenderBattleTransition(canvas, content, state, transition, assets, loopTime, animationTime, renderResolution, hitboxOverlay);
        }
        else
        {
            RenderScene(canvas, content, state, assets, loopTime, animationTime, hitboxOverlay);
        }
    }

    private static void RenderBattleTransition(
        SKCanvas canvas,
        WorldRenderContent content,
        WorldRenderState state,
        BattleTransition transition,
        IAssetProvider assets,
        TimeSpan loopTime,
        TimeSpan animationTime,
        float renderResolution,
        bool hitboxOverlay)
    {
        double progress = animationTime.ToAnimationProgress(TimeSpan.Zero, transition.Duration);
        double blend = ResolveBattleTransitionBlend(transition.Type, progress);
        double motion = 4 * progress * (1 - progress);

        SKRectI bounds = canvas.DeviceClipBounds;
        Point position = WorldTransform.ToWorldPoint(transition.Position);
        SKPoint focus = canvas.TotalMatrix.MapPoint((float)position.X, (float)position.Y);
        float centering = (float)AnimationEasing.Smooth.Ease(blend / BattleTransitionCentering);
        SKPoint pivot = new(
            focus.X + ((bounds.MidX - focus.X) * centering),
            focus.Y + ((bounds.MidY - focus.Y) * centering));
        SKMatrix lens = BuildBattleTransitionLens(focus, pivot, blend, bounds.Height);

        using ScaledSurface surface = ScaledSurface.Create(canvas, renderResolution, lens);

        RenderScene(surface.Canvas, content, state, assets, loopTime, animationTime, hitboxOverlay);

        using SKShader image = surface.ToShader();
        using WorldBattleTransitionShaderProgram.Editor editor = content.BattleTransitionShaderProgram.Edit();

        editor.SetImage(image);
        editor.SetCenter(pivot);
        editor.SetExtent(new SKPoint(bounds.Width / 2.0f, bounds.Height / 2.0f));
        editor.SetBlur((float)(BattleTransitionBlur * motion));
        editor.SetVignette((float)(BattleTransitionVignette * blend));
        editor.SetDesaturation((float)(BattleTransitionDesaturation * blend));
        editor.SetShade(Theme.World.BackdropColor.ToSKColorF());

        using SKShader shader = editor.Build();
        using SKPaint paint = new() { Shader = shader };

        canvas.Save();
        canvas.ResetMatrix();
        canvas.DrawRect(bounds, paint);
        canvas.Restore();
    }

    private static double ResolveBattleTransitionBlend(BattleTransitionType type, double progress)
    {
        double eased = AnimationEasing.Smooth.Ease(progress);

        return type switch
        {
            BattleTransitionType.In => eased,
            BattleTransitionType.Out => 1 - eased
        };
    }

    private static SKMatrix BuildBattleTransitionLens(SKPoint focus, SKPoint pivot, double blend, int viewportHeight)
    {
        float zoom = (float)Math.Exp(Math.Log(BattleTransitionZoom) * blend);
        double tilt = BattleTransitionTilt * blend;
        double focalLength = BattleTransitionFocalLength * viewportHeight;

        SKMatrix perspective = new(
            1, 0, 0,
            0, (float)Math.Cos(tilt), 0,
            0, (float)(-Math.Sin(tilt) / focalLength), 1);

        return SKMatrix.CreateTranslation(-focus.X, -focus.Y)
            .PostConcat(SKMatrix.CreateScale(zoom, zoom))
            .PostConcat(perspective)
            .PostConcat(SKMatrix.CreateTranslation(pivot.X, pivot.Y));
    }

    private static void RenderScene(
        SKCanvas canvas,
        WorldRenderContent content,
        WorldRenderState state,
        IAssetProvider assets,
        TimeSpan loopTime,
        TimeSpan animationTime,
        bool hitboxOverlay)
    {
        Rect visible = GetVisibleRect(canvas);
        int frameIndex = AnimationRate.Sprites.GetFrameIndex(loopTime);
        IReadOnlyList<AnimationRenderItem> animations = BuildAnimationRenderItems(state, loopTime, animationTime);

        DrawBackdrop(canvas, content, visible);
        DrawTerrain(canvas, content, frameIndex, loopTime, visible);
        DrawReach(canvas, content);

        foreach (ObjectiveMarker marker in state.ObjectiveMarkers)
        {
            DrawObjectiveMarker(canvas, marker, visible);
        }

        DrawObjects(canvas, content, animations, assets, frameIndex, visible);

        foreach (Cursor cursor in state.Cursors)
        {
            DrawCursorGhost(canvas, cursor, WorldTransform.ToWorldPoint(cursor.Position), loopTime, visible);
        }

        DrawStackGhosts(canvas, content, animations, assets, frameIndex, visible);
        DrawHighlights(canvas, content, assets, frameIndex, visible);

        DrawClouds(canvas, content, state, assets, frameIndex, loopTime, visible);
        DrawFog(canvas, content);

        if (state.SelectedPath is not null)
        {
            DrawSelectedPath(canvas, state.SelectedPath, loopTime, visible);
        }

        if (hitboxOverlay)
        {
            DrawHitboxOverlay(canvas, content, visible);
        }
    }

    private static void DrawTerrain(SKCanvas canvas, WorldRenderContent content, int frameIndex, TimeSpan loopTime, Rect visible)
    {
        using SKPaint terrainPaint = SkiaHelpers.MakeImagePaint();

        canvas.Save();
        canvas.ClipPath(content.MapClip);

        Water.Draw(canvas, content.Water, loopTime, canvas.TotalMatrix.ScaleX, frameIndex);

        content.CoastAtlas.UpdateAnimations(frameIndex, visible);
        content.CoastAtlas.Draw(canvas, terrainPaint, visible);

        foreach (GroundLayer layer in content.GroundLayers)
        {
            terrainPaint.Shader = layer.TerrainShader;
            canvas.DrawVertices(layer.Mesh, SKBlendMode.Modulate, terrainPaint);
        }

        terrainPaint.Shader = null;
        content.BorderAtlas.UpdateAnimations(frameIndex, visible);
        content.BorderAtlas.Draw(canvas, terrainPaint, visible);
        content.RoadAtlas.UpdateAnimations(frameIndex, visible);
        content.RoadAtlas.Draw(canvas, terrainPaint, visible);

        canvas.Restore();
    }

    private static void DrawObjects(
        SKCanvas canvas,
        WorldRenderContent content,
        IReadOnlyList<AnimationRenderItem> animations,
        IAssetProvider assets,
        int frameIndex,
        Rect visible)
    {
        using SKPaint atlasPaint = SkiaHelpers.MakeImagePaint();
        using SKPaint directPaint = SkiaHelpers.MakeImagePaint();

        content.ObjectAtlas.UpdateAnimations(frameIndex, visible);

        int objectCount = content.ObjectAtlas.SpriteCount;
        IReadOnlyList<int> objectDepths = content.ObjectSpriteDepths;
        IReadOnlyList<DirectSprite> directSprites = content.DirectSprites;
        IReadOnlyList<StackSprite> stacks = content.Stacks;
        int directCount = directSprites.Count;
        int stackCount = stacks.Count;
        int animationCount = animations.Count;

        int objectCursor = 0;
        int directCursor = 0;
        int stackCursor = 0;
        int animationCursor = 0;

        while (objectCursor < objectCount || directCursor < directCount || stackCursor < stackCount || animationCursor < animationCount)
        {
            int objectDepth = objectCursor < objectCount ? objectDepths[objectCursor] : int.MaxValue;
            int directDepth = directCursor < directCount ? directSprites[directCursor].Depth : int.MaxValue;
            int stackDepth = stackCursor < stackCount ? stacks[stackCursor].Stack.Depth : int.MaxValue;
            int animationDepth = animationCursor < animationCount ? animations[animationCursor].Depth : int.MaxValue;
            int limit = Math.Min(directDepth, Math.Min(stackDepth, animationDepth));

            if (objectDepth <= limit)
            {
                int start = objectCursor;

                while (objectCursor < objectCount && objectDepths[objectCursor] <= limit)
                {
                    objectCursor++;
                }

                content.ObjectAtlas.DrawRange(canvas, atlasPaint, visible, start, objectCursor - start);
            }
            else if (directDepth <= Math.Min(stackDepth, animationDepth))
            {
                DirectSprite sprite = directSprites[directCursor];

                if (visible.Intersects(sprite.Bounds))
                {
                    directPaint.ColorF = new SKColorF(0, 0, 0, (float)sprite.Opacity);
                    WorldDirectSpriteRenderer.Draw(canvas, directPaint, assets, sprite, frameIndex);
                }

                directCursor++;
            }
            else if (stackDepth <= animationDepth)
            {
                (Stack stack, Rect bounds) = stacks[stackCursor];

                if (visible.Intersects(bounds))
                {
                    Point center = WorldTransform.ToWorldPoint(stack.Position);
                    DrawStackObject(
                        canvas,
                        assets,
                        center,
                        stack.AssetKey,
                        stack.ShadowKey,
                        stack.AuraKey,
                        stack.BannerKey,
                        stack.Opacity,
                        frameIndex,
                        visible);
                }

                stackCursor++;
            }
            else
            {
                animations[animationCursor].Render(canvas, atlasPaint, content.HighlightShaderProgram, assets, frameIndex, visible);
                animationCursor++;
            }
        }
    }

    private static void DrawStackGhosts(
        SKCanvas canvas,
        WorldRenderContent content,
        IReadOnlyList<AnimationRenderItem> animations,
        IAssetProvider assets,
        int frameIndex,
        Rect visible)
    {
        using SKPaint layerPaint = MakeOpacityPaint(StackGhostOpacity);

        IReadOnlyList<StackSprite> stacks = content.Stacks;
        int stackCount = stacks.Count;
        int animationCount = animations.Count;

        int stackCursor = 0;
        int animationCursor = 0;

        canvas.SaveLayer(layerPaint);

        while (stackCursor < stackCount || animationCursor < animationCount)
        {
            int stackDepth = stackCursor < stackCount ? stacks[stackCursor].Stack.Depth : int.MaxValue;
            int animationDepth = animationCursor < animationCount ? animations[animationCursor].Depth : int.MaxValue;

            if (stackDepth <= animationDepth)
            {
                (Stack stack, Rect bounds) = stacks[stackCursor];

                if (visible.Intersects(bounds))
                {
                    Point center = WorldTransform.ToWorldPoint(stack.Position);
                    DrawStackGhost(canvas, assets, center, stack.AssetKey, stack.BannerKey, stack.Opacity, frameIndex, visible);
                }

                stackCursor++;
            }
            else
            {
                animations[animationCursor].RenderGhost(canvas, assets, frameIndex, visible);
                animationCursor++;
            }
        }

        canvas.Restore();
    }

    private static IReadOnlyList<AnimationRenderItem> BuildAnimationRenderItems(WorldRenderState state, TimeSpan loopTime, TimeSpan animationTime)
    {
        List<AnimationRenderItem> items = [];

        if (state.Animation is not null)
        {
            IReadOnlyList<AnimationRenderItem> animations = state.Animation.Accept(new AnimationRenderItemVisitor(), animationTime);

            items.AddRange(animations);
        }

        foreach (Cursor cursor in state.Cursors)
        {
            items.Add(new AnimationRenderItem.Cursor(cursor, loopTime));
        }

        return [.. items.OrderBy(item => item.Depth)];
    }

    private static void DrawHighlights(SKCanvas canvas, WorldRenderContent content, IAssetProvider assets, int frameIndex, Rect visible)
    {
        foreach (HighlightLayer highlight in content.Highlights)
        {
            Rect bounds = highlight.Bounds.Inflate(HighlightGlowRadius);

            if (visible.Intersects(bounds))
            {
                DrawHighlight(canvas, content.HighlightShaderProgram, highlight.Color, bounds, (bodyCanvas, paint) =>
                {
                    foreach (DirectSprite sprite in highlight.Sprites)
                    {
                        WorldDirectSpriteRenderer.Draw(bodyCanvas, paint, assets, sprite, frameIndex);
                    }
                });
            }
        }
    }

    private static void DrawHighlight(
        SKCanvas canvas,
        WorldHighlightShaderProgram program,
        Color color,
        Rect bounds,
        Action<SKCanvas, SKPaint> drawBodyFunc)
    {
        SKRect area = bounds.ToSKRect();
        SKMatrix transform = SKMatrix.CreateTranslation(area.Left, area.Top);

        using SKPicture body = RecordBody(area, drawBodyFunc);
        using SKShader bodyShader = body.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, SKFilterMode.Linear, transform, area);
        using SKShader shader = BuildHighlightShader(program, bodyShader, color);
        using SKPaint paint = new() { Shader = shader };

        canvas.DrawRect(area, paint);
    }

    private static SKPicture RecordBody(SKRect area, Action<SKCanvas, SKPaint> drawBodyFunc)
    {
        using SKPictureRecorder recorder = new();
        using SKPaint paint = SkiaHelpers.MakeImagePaint();

        SKCanvas canvas = recorder.BeginRecording(area);
        drawBodyFunc(canvas, paint);

        return recorder.EndRecording();
    }

    private static SKShader BuildHighlightShader(WorldHighlightShaderProgram program, SKShader body, Color color)
    {
        using WorldHighlightShaderProgram.Editor editor = program.Edit();

        editor.SetBody(body);
        editor.SetColor(color.ToSKColorF());
        editor.SetGlowRadius(HighlightGlowRadius);

        return editor.Build();
    }

    private static void DrawReach(SKCanvas canvas, WorldRenderContent content)
    {
        if (content.Reach is not null)
        {
            using SKPaint fillPaint = SkiaHelpers.MakeAntialiasFillPaint(Theme.World.Reach.FillColor);
            using SKShader shader = BuildReachShader(content.ReachShaderProgram, content.Reach, canvas.TotalMatrix.ScaleX);
            using SKPaint edgePaint = SkiaHelpers.MakeImagePaint();
            edgePaint.Shader = shader;

            if (content.Reach.InteriorMesh is not null)
            {
                canvas.DrawVertices(content.Reach.InteriorMesh, SKBlendMode.Modulate, fillPaint);
            }

            canvas.DrawVertices(content.Reach.EdgeMesh, SKBlendMode.Modulate, edgePaint);
        }
    }

    private static SKShader BuildReachShader(WorldReachShaderProgram program, ReachLayer reach, float zoom)
    {
        using WorldReachShaderProgram.Editor editor = program.Edit();

        editor.SetTiles(reach.Tiles);
        editor.SetPixelWidth(1.0f / zoom);

        return editor.Build();
    }

    private static void DrawFog(SKCanvas canvas, WorldRenderContent content)
    {
        if (content.Fog is not null)
        {
            using SKPaint fogPaint = SkiaHelpers.MakeImagePaint();
            fogPaint.Shader = content.Fog.Shader;

            canvas.DrawVertices(content.Fog.Mesh, SKBlendMode.Modulate, fogPaint);

            if (content.Fog.MapEdgeMesh is not null)
            {
                using SKColorFilter backdropFilter = SKColorFilter.CreateBlendMode(
                    Theme.World.BackdropColor.ToSKColor(),
                    SKBlendMode.SrcIn);

                fogPaint.ColorFilter = backdropFilter;

                canvas.DrawVertices(content.Fog.MapEdgeMesh, SKBlendMode.Modulate, fogPaint);
            }
        }
    }

    private static Rect GetVisibleRect(SKCanvas canvas)
    {
        SKRect clip = canvas.LocalClipBounds;

        return new Rect(
            clip.Left - CullMargin,
            clip.Top - CullMargin,
            clip.Width + 2 * CullMargin,
            clip.Height + 2 * CullMargin);
    }

    private static void DrawBackdrop(SKCanvas canvas, WorldRenderContent content, Rect visible)
    {
        using SKPath path = BuildBackdropPath(content.MapClip, visible);
        using SKPaint paint = SkiaHelpers.MakeFillPaint(Theme.World.BackdropColor);

        canvas.DrawPath(path, paint);
    }

    private static SKPath BuildBackdropPath(SKPath mapClip, Rect visible)
    {
        SKPathBuilder builder = new();
        builder.FillType = SKPathFillType.EvenOdd;
        builder.AddRect(visible.ToSKRect());
        builder.AddPath(mapClip);

        return builder.Detach();
    }

    private static void DrawClouds(
        SKCanvas canvas,
        WorldRenderContent content,
        WorldRenderState state,
        IAssetProvider assets,
        int frameIndex,
        TimeSpan loopTime,
        Rect visible)
    {
        if (state.Clouds.Count <= 0)
        {
            return;
        }

        WorldMap map = state.Terrain.Map;

        if (map.Width <= 0 || map.Height <= 0)
        {
            return;
        }

        double minX = -(map.Height - 1) * WorldTransform.TileHalfWidth;
        double maxX = (map.Width - 1) * WorldTransform.TileHalfWidth;
        double minY = 0;
        double maxY = (map.Width - 1 + map.Height - 1) * WorldTransform.TileHalfHeight;
        double span = maxX - minX;

        canvas.Save();
        canvas.ClipPath(content.MapClip);

        using SKPaint paint = SkiaHelpers.MakeImagePaint();

        foreach (Cloud cloud in state.Clouds)
        {
            using AssetCache<Animation>.Lease lease = assets.GetRequiredAnimation(cloud.AssetKey);
            Animation animation = lease.Value;

            double cycle = ((loopTime.TotalSeconds / cloud.PeriodSeconds) + cloud.Phase) % 1.0;
            double travel = cloud.LeftToRight ? cycle : 1 - cycle;

            double centerX = minX + (span * travel);
            double centerY = minY + ((maxY - minY) * cloud.HeightFraction);

            Sprite sprite = animation.FrameByIndex(frameIndex + cloud.FrameOffset, looping: true);

            double left = centerX + sprite.Metadata.OffsetX;
            double top = centerY + sprite.Metadata.OffsetY;
            double right = left + sprite.Metadata.Width;
            double bottom = top + sprite.Metadata.Height;

            if (visible.Intersects(new Rect(new Point(left, top), new Point(right, bottom))))
            {
                SKRect destination = new((float)left, (float)top, (float)right, (float)bottom);
                canvas.DrawImage(sprite.Image, destination, RenderSamplingOptions.Linear, paint);
            }
        }

        canvas.Restore();
    }

    private static void DrawSelectedPath(SKCanvas canvas, PathOverlay pathOverlay, TimeSpan loopTime, Rect visible)
    {
        WorldPathGeometry geometry = WorldPathGeometry.SelectedPath;
        IReadOnlyList<PathStep> steps = pathOverlay.Steps;

        int endIndex = steps.Count - 1;
        Point previous = WorldTransform.ToWorldPoint(pathOverlay.Origin);

        double overhang = geometry.DashOutlineOverhang;
        double dashPhase = geometry.DashPeriod - (loopTime.TotalSeconds * geometry.DashPixelsPerSecond % geometry.DashPeriod);
        double outlinePhase = (dashPhase + geometry.DashPeriod - overhang) % geometry.DashPeriod;

        using SKPathEffect dash = MakePathDash(geometry.DashLength, geometry.DashGap, dashPhase);
        using SKPathEffect dashOutline = MakePathDash(geometry.DashLength + (2 * overhang), geometry.DashGap - (2 * overhang), outlinePhase);

        for (int index = 0; index < steps.Count; index++)
        {
            Point position = WorldTransform.ToWorldPoint(steps[index].Position);
            Size startRadius = index == 0 ? geometry.OriginRadius : geometry.WaypointRadius;
            Size endRadius = index == endIndex ? geometry.DestinationRadius : geometry.WaypointRadius;

            DrawPathSegment(canvas, geometry, previous, position, startRadius, endRadius, steps[index], dash, dashOutline, visible);

            previous = position;
        }

        for (int index = 0; index < endIndex; index++)
        {
            Point position = WorldTransform.ToWorldPoint(steps[index].Position);

            if (visible.Contains(position))
            {
                DrawPathWaypoint(canvas, geometry, position, steps[index]);
            }
        }

        using SKFont labelFont = new(ThemeFont.Regular, (float)geometry.LabelFontSize);

        for (int index = 0; index < steps.Count; index++)
        {
            Point position = WorldTransform.ToWorldPoint(steps[index].Position);
            double offset = index == endIndex ? geometry.DestinationLabelOffset : geometry.WaypointLabelOffset;

            if (visible.Contains(position))
            {
                DrawPathCostLabel(canvas, geometry, labelFont, new Point(position.X, position.Y - offset), steps[index]);
            }
        }
    }

    private static void DrawPathCostLabel(SKCanvas canvas, WorldPathGeometry geometry, SKFont font, Point baseline, PathStep step)
    {
        float x = (float)baseline.X;
        float y = (float)baseline.Y;

        using SKPaint outlinePaint = SkiaHelpers.MakeAntialiasStrokePaint(Theme.World.Path.OutlineColor, geometry.LabelOutlineWidth);
        outlinePaint.StrokeJoin = SKStrokeJoin.Round;

        using SKPaint fillPaint = SkiaHelpers.MakeAntialiasFillPaint(step.InnerColor);

        canvas.DrawText(step.CostLabel, x, y, SKTextAlign.Center, font, outlinePaint);
        canvas.DrawText(step.CostLabel, x, y, SKTextAlign.Center, font, fillPaint);
    }

    private static void DrawPathSegment(
        SKCanvas canvas,
        WorldPathGeometry geometry,
        Point start,
        Point end,
        Size startRadius,
        Size endRadius,
        PathStep step,
        SKPathEffect dash,
        SKPathEffect dashOutline,
        Rect visible)
    {
        Vector direction = Vector.Normalize(end - start);
        Point from = WorldPathGeometry.GetEdge(start, direction, startRadius);
        Point to = WorldPathGeometry.GetEdge(end, -direction, endRadius);

        if (Vector.Dot(to - from, direction) > 0 && visible.Intersects(new Rect(start, end)))
        {
            using SKShader gradient = MakePathDashGradient(from, direction, geometry.DashWidth, step);

            using SKPaint outlinePaint = SkiaHelpers.MakeAntialiasStrokePaint(Theme.World.Path.OutlineColor, geometry.DashOutlineWidth);
            outlinePaint.PathEffect = dashOutline;

            using SKPaint dashPaint = SkiaHelpers.MakeAntialiasStrokePaint(step.OuterColor, geometry.DashWidth);
            dashPaint.PathEffect = dash;
            dashPaint.Shader = gradient;

            canvas.DrawLine(from.ToSKPoint(), to.ToSKPoint(), outlinePaint);
            canvas.DrawLine(from.ToSKPoint(), to.ToSKPoint(), dashPaint);
        }
    }

    private static void DrawPathWaypoint(SKCanvas canvas, WorldPathGeometry geometry, Point center, PathStep step)
    {
        canvas.Save();
        canvas.Translate((float)center.X, (float)center.Y);

        using (SKPaint outlinePaint = SkiaHelpers.MakeAntialiasStrokePaint(Theme.World.Path.OutlineColor, geometry.WaypointOutlineWidth))
        {
            canvas.DrawPath(geometry.Waypoint, outlinePaint);
        }

        using (SKShader gradient = MakePathWaypointGradient(geometry, step))
        using (SKPaint borderPaint = SkiaHelpers.MakeAntialiasFillPaint(step.OuterColor))
        {
            borderPaint.Shader = gradient;
            canvas.DrawPath(geometry.Waypoint, borderPaint);
        }

        using (SKPaint interiorPaint = SkiaHelpers.MakeAntialiasFillPaint(Theme.World.Path.MarkerFillColor))
        {
            canvas.DrawPath(geometry.WaypointInterior, interiorPaint);
        }

        using (SKPaint centerPaint = SkiaHelpers.MakeAntialiasFillPaint(step.InnerColor))
        {
            canvas.DrawPath(geometry.WaypointCenter, centerPaint);
        }

        canvas.Restore();
    }

    private static SKPathEffect MakePathDash(double length, double gap, double phase)
    {
        return SKPathEffect.CreateDash([(float)length, (float)gap], (float)phase);
    }

    private static SKShader MakePathDashGradient(Point from, Vector direction, double width, PathStep step)
    {
        Vector across = new Vector(-direction.Y, direction.X) * (width / 2.0);
        SKColor[] colors = [step.OuterColor.ToSKColor(), step.InnerColor.ToSKColor(), step.OuterColor.ToSKColor()];

        return SKShader.CreateLinearGradient((from - across).ToSKPoint(), (from + across).ToSKPoint(), colors, SKShaderTileMode.Clamp);
    }

    private static SKShader MakePathWaypointGradient(WorldPathGeometry geometry, PathStep step)
    {
        SKColor[] colors = [step.InnerColor.ToSKColor(), step.OuterColor.ToSKColor()];
        float[] stops = [(float)(geometry.WaypointGradientInnerRadius / geometry.WaypointGradientOuterRadius), 1.0f];

        return SKShader.CreateRadialGradient(SKPoint.Empty, (float)geometry.WaypointGradientOuterRadius, colors, stops, SKShaderTileMode.Clamp);
    }

    private static void DrawCursor(
        SKCanvas canvas,
        Cursor cursor,
        Point center,
        TimeSpan loopTime,
        Rect visible)
    {
        if (visible.Contains(center))
        {
            WorldCursorGeometry geometry = WorldCursorGeometry.Highlight;

            canvas.Save();
            TransformToCursor(canvas, cursor, geometry, center, loopTime);
            DrawCursorShape(canvas, cursor, geometry);
            canvas.Restore();
        }
    }

    private static void DrawCursorGhost(
        SKCanvas canvas,
        Cursor cursor,
        Point center,
        TimeSpan loopTime,
        Rect visible)
    {
        if (visible.Contains(center))
        {
            WorldCursorGeometry geometry = WorldCursorGeometry.Highlight;
            SKRect layerBounds = geometry.Frame.Bounds;
            layerBounds.Inflate((float)geometry.HaloWidth, (float)geometry.HaloWidth);

            using SKPaint ghostPaint = new() { Color = WithOpacity(SKColors.Black, CursorGhostOpacity) };

            canvas.Save();
            TransformToCursor(canvas, cursor, geometry, center, loopTime);
            canvas.SaveLayer(layerBounds, ghostPaint);
            DrawCursorShape(canvas, cursor, geometry);
            canvas.Restore();
            canvas.Restore();
        }
    }

    private static void TransformToCursor(SKCanvas canvas, Cursor cursor, WorldCursorGeometry geometry, Point center, TimeSpan loopTime)
    {
        float spin = (float)(loopTime.TotalSeconds * geometry.RotationDegreesPerSecond % 360.0);

        canvas.Translate((float)center.X, (float)center.Y);
        canvas.Scale((float)(WorldTransform.TileHalfWidth * cursor.Scale), (float)(WorldTransform.TileHalfHeight * cursor.Scale));
        canvas.RotateDegrees(spin);
    }

    private static void DrawCursorShape(SKCanvas canvas, Cursor cursor, WorldCursorGeometry geometry)
    {
        using (SKPaint haloPaint = SkiaHelpers.MakeAntialiasStrokePaint(Theme.World.CursorHaloColor, geometry.HaloWidth))
        {
            haloPaint.StrokeJoin = SKStrokeJoin.Round;
            canvas.DrawPath(geometry.Frame, haloPaint);
            canvas.DrawPath(geometry.RuneBand, haloPaint);
        }

        using SKShader gradient = MakeCursorGradient(cursor, geometry);

        using (SKPaint framePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true, Shader = gradient })
        {
            canvas.DrawPath(geometry.Frame, framePaint);
        }

        using (SKPaint runePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true, Shader = gradient, Color = WithOpacity(SKColors.White, CursorRuneOpacity) })
        {
            canvas.DrawPath(geometry.RuneBand, runePaint);
        }
    }

    private static SKShader MakeCursorGradient(Cursor cursor, WorldCursorGeometry geometry)
    {
        SKColor[] colors = [cursor.InnerColor.ToSKColor(), cursor.OuterColor.ToSKColor()];
        float[] stops = [(float)(geometry.GradientInnerRadius / geometry.GradientOuterRadius), 1.0f];

        return SKShader.CreateRadialGradient(SKPoint.Empty, (float)geometry.GradientOuterRadius, colors, stops, SKShaderTileMode.Clamp);
    }

    private static SKColor WithOpacity(SKColor color, double opacity)
    {
        return color.WithAlpha((byte)Math.Round(color.Alpha * opacity));
    }

    private static SKPaint MakeOpacityPaint(double opacity)
    {
        SKPaint paint = SkiaHelpers.MakeImagePaint();
        paint.ColorF = new SKColorF(0, 0, 0, (float)opacity);

        return paint;
    }

    private static double GetStackBodyOpacity(double opacity)
    {
        return 1 - ((1 - opacity) / (1 - (opacity * StackGhostOpacity)));
    }

    private static void DrawStackObject(
        SKCanvas canvas,
        IAssetProvider assets,
        Point center,
        AssetAnimationKey assetKey,
        AssetAnimationKey? shadowKey,
        AssetAnimationKey? auraKey,
        AssetSpriteKey bannerKey,
        double opacity,
        int frameIndex,
        Rect visible)
    {
        using SKPaint paint = MakeOpacityPaint(opacity);
        using SKPaint bodyPaint = MakeOpacityPaint(GetStackBodyOpacity(opacity));

        if (shadowKey is not null)
        {
            using AssetCache<Animation>.Lease shadowLease = assets.GetRequiredAnimation(shadowKey);
            DrawStackFrame(canvas, paint, shadowLease.Value, center, frameIndex, visible);
        }

        if (auraKey is not null)
        {
            using AssetCache<Animation>.Lease auraLease = assets.GetRequiredAnimation(auraKey);
            DrawStackFrame(canvas, paint, auraLease.Value, center, frameIndex, visible);
        }

        DrawStackBody(canvas, bodyPaint, assets, center, assetKey, bannerKey, frameIndex, visible);
    }

    private static void DrawStackGhost(
        SKCanvas canvas,
        IAssetProvider assets,
        Point center,
        AssetAnimationKey assetKey,
        AssetSpriteKey bannerKey,
        double opacity,
        int frameIndex,
        Rect visible)
    {
        using SKPaint paint = MakeOpacityPaint(opacity);

        DrawStackBody(canvas, paint, assets, center, assetKey, bannerKey, frameIndex, visible);
    }

    private static void DrawStackBody(
        SKCanvas canvas,
        SKPaint paint,
        IAssetProvider assets,
        Point center,
        AssetAnimationKey assetKey,
        AssetSpriteKey bannerKey,
        int frameIndex,
        Rect visible)
    {
        using AssetCache<Animation>.Lease lease = assets.GetRequiredAnimation(assetKey);
        DrawStackFrame(canvas, paint, lease.Value, center, frameIndex, visible);

        using AssetCache<Sprite>.Lease bannerLease = assets.GetRequiredSprite(bannerKey);
        DrawStackBanner(canvas, paint, bannerLease.Value, center, visible);
    }

    private static Rect GetStackBodyBounds(IAssetProvider assets, Point center, AssetAnimationKey assetKey, AssetSpriteKey bannerKey)
    {
        using AssetCache<Animation>.Lease lease = assets.GetRequiredAnimation(assetKey);
        using AssetCache<Sprite>.Lease bannerLease = assets.GetRequiredSprite(bannerKey);
        AssetAnimationMetadata metadata = lease.Value.Metadata;
        Sprite banner = bannerLease.Value;
        double bannerHeight = banner.Image.Height * (banner.Metadata.Width / (double)banner.Image.Width);
        Rect body = new(center.X + metadata.OffsetX, center.Y + metadata.OffsetY, metadata.Width, metadata.Height);
        Rect bannerBounds = new(center.X + banner.Metadata.OffsetX, center.Y + banner.Metadata.OffsetY, banner.Metadata.Width, bannerHeight);

        return body.Union(bannerBounds);
    }

    private static void DrawStackBanner(SKCanvas canvas, SKPaint atlasPaint, Sprite sprite, Point center, Rect visible)
    {
        double x = center.X + sprite.Metadata.OffsetX;
        double y = center.Y + sprite.Metadata.OffsetY;
        double right = x + sprite.Metadata.Width;
        double bottom = y + (sprite.Image.Height * (sprite.Metadata.Width / (double)sprite.Image.Width));

        if (visible.Intersects(new Rect(new Point(x, y), new Point(right, bottom))))
        {
            SKRect destination = new((float)x, (float)y, (float)right, (float)bottom);

            canvas.DrawImage(sprite.Image, destination, RenderSamplingOptions.Linear, atlasPaint);
        }
    }

    private static void DrawEffect(
        SKCanvas canvas,
        SKPaint atlasPaint,
        IAssetProvider assets,
        AssetAnimationKey assetKey,
        int frameCount,
        WorldPosition position,
        int frameIndex,
        Rect visible)
    {
        if (frameIndex >= frameCount)
        {
            return;
        }

        using AssetCache<Animation>.Lease lease = assets.GetRequiredAnimation(assetKey);
        Sprite sprite = lease.Value.FrameByIndex(frameIndex, looping: false);

        Point center = WorldTransform.ToWorldPoint(position);
        double x = center.X + sprite.Metadata.OffsetX;
        double y = center.Y + sprite.Metadata.OffsetY;
        double right = x + sprite.Metadata.Width;
        double bottom = y + sprite.Metadata.Height;

        if (visible.Intersects(new Rect(new Point(x, y), new Point(right, bottom))))
        {
            SKRect destination = new((float)x, (float)y, (float)right, (float)bottom);

            canvas.DrawImage(sprite.Image, destination, RenderSamplingOptions.Linear, atlasPaint);
        }
    }

    private static void DrawStackFrame(SKCanvas canvas, SKPaint atlasPaint, Animation animation, Point center, int frameIndex, Rect visible)
    {
        Sprite sprite = animation.FrameByIndex(frameIndex, looping: true);

        double x = center.X + sprite.Metadata.OffsetX;
        double y = center.Y + sprite.Metadata.OffsetY;
        double right = x + sprite.Metadata.Width;
        double bottom = y + sprite.Metadata.Height;

        if (visible.Intersects(new Rect(new Point(x, y), new Point(right, bottom))))
        {
            SKRect destination = new((float)x, (float)y, (float)right, (float)bottom);

            canvas.DrawImage(sprite.Image, destination, RenderSamplingOptions.Linear, atlasPaint);
        }
    }

    private static void DrawObjectiveMarker(SKCanvas canvas, ObjectiveMarker marker, Rect visible)
    {
        Point center = WorldRenderLayout.Footprint.GetAnchor(marker.Footprint);

        DrawObjectiveMarker(canvas, center, GetObjectiveMarkerRadius(marker.Footprint), marker.HasRunes, visible);
    }

    private static void DrawStackObjectiveMarker(SKCanvas canvas, Point center, Rect visible)
    {
        WorldFootprint footprint = new(new WorldPosition(0, 0), 1, 1);

        DrawObjectiveMarker(canvas, center, GetObjectiveMarkerRadius(footprint), hasRunes: false, visible);
    }

    private static void DrawObjectiveMarker(
        SKCanvas canvas,
        Point center,
        double radiusX,
        bool hasRunes,
        Rect visible)
    {
        if (!visible.Inflate(radiusX).Contains(center))
        {
            return;
        }

        canvas.Save();
        canvas.Translate((float)center.X, (float)center.Y);
        canvas.Scale((float)radiusX, (float)(radiusX * WorldTransform.TileHalfHeight / WorldTransform.TileHalfWidth));

        using (SKPaint shadowPaint = SkiaHelpers.MakeAntialiasFillPaint(Theme.World.Objective.ShadowColor))
        {
            canvas.Save();
            canvas.Translate(0, (float)(ShadowOffset / radiusX));
            canvas.DrawPath(WorldObjectiveMarkerGeometry.Ring, shadowPaint);
            canvas.Restore();
        }

        using (SKPaint ringPaint = SkiaHelpers.MakeAntialiasFillPaint(Theme.World.Objective.RingColor))
        {
            canvas.DrawPath(WorldObjectiveMarkerGeometry.Ring, ringPaint);
        }

        using (SKPaint runePaint = SkiaHelpers.MakeAntialiasFillPaint(Theme.World.Objective.RuneColor))
        {
            if (hasRunes)
            {
                canvas.DrawPath(WorldObjectiveMarkerGeometry.RuneBand, runePaint);
            }
            else
            {
                canvas.DrawPath(WorldObjectiveMarkerGeometry.Band, runePaint);
            }
        }

        canvas.Restore();
    }

    private static double GetObjectiveMarkerRadius(WorldFootprint footprint)
    {
        double tiles = (footprint.Width + footprint.Height) / 2.0;

        return tiles * WorldTransform.TileHalfWidth * WorldObjectiveMarkerGeometry.FootprintRadius;
    }
}
