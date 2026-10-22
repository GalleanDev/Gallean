using Avalonia;
using Avalonia.Media;
using Avalonia.Skia;
using Gallean.Common;
using SkiaSharp;
using static Gallean.Client.BattleRenderState;
using static Gallean.Client.BattleUnitState;

namespace Gallean.Client;

internal sealed partial class BattleRenderer
{
    private readonly record struct BodyFrame(
        AssetAnimationKey.BattleUnitPose State,
        bool Loop,
        bool Frozen,
        double Progress,
        double Opacity,
        Presence Presence,
        Color? Highlight);

    private readonly record struct UnitFrame(
        Unit Unit,
        BodyFrame Body,
        UnitDefinitionId DefinitionId,
        BattleSide Facing,
        Auras Auras,
        Vector Motion,
        TimeSpan IdleTime);

    private readonly record struct LightSource(
        AssetAnimationKey Animation,
        int FrameIndex,
        bool Looping,
        AnimationPlacement Placement,
        Vector Motion,
        double Opacity,
        double ShadowFade);

    private sealed record SceneLight(Point Position, double Radius, Vector3D Radiance, double Power, Point Foot);

    private const double MarkerDiscOpacity = 0.4;
    private const double MarkerRingOpacity = 0.6;
    private const double ShakeFrequency = 12.0;
    private const double ShakeVerticalFrequencyRatio = 1.37;
    private const double ShakeVerticalRatio = 0.55;
    private const int RigLightCapacity = 8;
    private const int SpriteLightLimit = 16;
    private const int ShadowSmoothingFrames = 4;
    private const int ShadowFadeFrames = 6;
    private const double LightMinimumHeight = 24.0;
    private const double ShadowMaximumSlope = 1.2;
    private const double ShadowMinimumDistance = 30.0;
    private const double ShadowMinimumLight = 0.01;
    private const double TransitionScale = 0.5;
    private const double TransitionRadius = 1.25;
    private const double TransitionRadiusPower = 1.6;
    private const double TransitionBlur = 0.22;
    private const double TransitionOutPower = 1.5;
    private const double SunTint = 0.5;
    private const double SunExposure = 0.6;
    private const double SunExposurePivot = 1.77;

    private static readonly TimeSpan TransitionInDelay = TimeSpan.FromSeconds(0.45);

    [ThreadStatic]
    private static BackgroundCache? backgroundCache;

    public void Render(
        SKCanvas canvas,
        BattleRenderContent content,
        BattleRenderState state,
        BattleAnimationState? animation,
        BattleWorldTransitionState? transition,
        IAssetProvider assets,
        TimeSpan loopTime,
        TimeSpan sequenceTime,
        TimeSpan transitionTime,
        float renderResolution,
        bool lightOverlay,
        bool hitboxOverlay)
    {
        if (transition is not null)
        {
            RenderTransition(
                canvas,
                content,
                state,
                animation,
                transition,
                assets,
                loopTime,
                sequenceTime,
                transitionTime,
                renderResolution,
                lightOverlay,
                hitboxOverlay);
        }
        else
        {
            if (renderResolution < 1)
            {
                using ScaledSurface surface = ScaledSurface.Create(canvas, renderResolution, SKMatrix.Identity);
                RenderScene(surface.Canvas, content, state, animation, assets, loopTime, sequenceTime, lightOverlay, hitboxOverlay);
                surface.DrawTo(canvas);
            }
            else
            {
                RenderScene(canvas, content, state, animation, assets, loopTime, sequenceTime, lightOverlay, hitboxOverlay);
            }
        }
    }

    private void RenderTransition(
        SKCanvas canvas,
        BattleRenderContent content,
        BattleRenderState state,
        BattleAnimationState? animation,
        BattleWorldTransitionState transition,
        IAssetProvider assets,
        TimeSpan loopTime,
        TimeSpan sequenceTime,
        TimeSpan transitionTime,
        float renderResolution,
        bool lightOverlay,
        bool hitboxOverlay)
    {
        double blend = ResolveTransitionBlend(transition, transitionTime);

        if (blend > 0)
        {
            SKRectI bounds = canvas.DeviceClipBounds;
            double remaining = 1 - blend;
            float scale = (float)Math.Exp(Math.Log(TransitionScale) * remaining * remaining * remaining);
            SKMatrix grow = SKMatrix.CreateScale(scale, scale, bounds.MidX, bounds.MidY);

            using ScaledSurface surface = ScaledSurface.Create(canvas, renderResolution, grow);

            RenderScene(surface.Canvas, content, state, animation, assets, loopTime, sequenceTime, lightOverlay, hitboxOverlay);

            using SKShader image = surface.ToShader();
            using BattleWorldTransitionShaderProgram.Editor editor = content.TransitionShaderProgram.Edit();

            editor.SetImage(image);
            editor.SetCenter(new SKPoint(bounds.MidX, bounds.MidY));
            editor.SetExtent(new SKPoint(bounds.Width / 2.0f, bounds.Height / 2.0f));
            editor.SetRadius((float)(TransitionRadius * Math.Pow(blend, TransitionRadiusPower)));
            editor.SetBlur((float)(TransitionBlur * remaining * remaining));
            editor.SetTime((float)loopTime.TotalSeconds);

            using SKShader shader = editor.Build();
            using SKPaint paint = new() { Shader = shader };

            canvas.Save();
            canvas.ResetMatrix();
            canvas.DrawRect(bounds, paint);
            canvas.Restore();
        }
    }

    private static double ResolveTransitionBlend(BattleWorldTransitionState transition, TimeSpan transitionTime)
    {
        return transition.Type switch
        {
            BattleTransitionType.In => transitionTime.ToAnimationProgress(TransitionInDelay, transition.Duration - TransitionInDelay),
            BattleTransitionType.Out => Math.Pow(1 - transitionTime.ToAnimationProgress(TimeSpan.Zero, transition.Duration), TransitionOutPower)
        };
    }

    private static void RenderScene(
        SKCanvas canvas,
        BattleRenderContent content,
        BattleRenderState state,
        BattleAnimationState? animation,
        IAssetProvider assets,
        TimeSpan loopTime,
        TimeSpan sequenceTime,
        bool lightOverlay,
        bool hitboxOverlay)
    {
        IReadOnlyList<UnitFrame> frames = ResolveUnitFrames(state, animation, loopTime, sequenceTime);
        IReadOnlyList<LightSource> lightSources = ResolveLightSources(assets, frames, animation, sequenceTime);
        (IReadOnlyList<SceneLight> spriteLights, IReadOnlyList<SceneLight> shadowLights) = ResolveLights(assets, lightSources);
        Vector shake = ResolveShake(animation?.Shakes ?? [], sequenceTime);

        ConfigureLighting(content, state.LightConfiguration, state.RainConfiguration, spriteLights, loopTime);

        canvas.Save();
        canvas.Translate((float)shake.X, (float)shake.Y);

        DrawBackground(canvas, content);
        DrawUnitShadows(canvas, content, state.LightConfiguration, assets, frames, shadowLights);

        if (state.RainConfiguration is not null)
        {
            DrawRain(canvas, content, state.RainConfiguration.Backdrop, loopTime);
        }

        for (int index = 0; index < state.FluidConfigurations.Count; index++)
        {
            AssetAnimationKey.BattleBackgroundOverlay key = new(state.Background.Location, state.Background.Variant, index);

            DrawFluid(canvas, content.FluidShaderPrograms[index], assets, key, state.FluidConfigurations[index], loopTime);
        }

        if (animation is null)
        {
            foreach (Placeholder placeholder in state.Placeholders)
            {
                DrawGroundMarker(
                    canvas,
                    new BattleGridFootprint(placeholder.Tile.Side, placeholder.Tile.Position, IsLarge: false),
                    placeholder.Marker,
                    loopTime);
            }

            foreach (UnitFrame frame in frames)
            {
                DrawGroundMarker(canvas, frame.Unit.Footprint, frame.Unit.Marker, loopTime);
            }
        }

        DrawUnits(canvas, content, assets, frames, animation?.Effects ?? [], sequenceTime);

        if (animation is not null)
        {
            DrawClips(canvas, assets, animation.Sequence.Clips, sequenceTime);
        }

        if (state.RainConfiguration is not null)
        {
            DrawRain(canvas, content, state.RainConfiguration.Overlay, loopTime);
        }

        canvas.Restore();

        if (lightOverlay)
        {
            DrawBattleLightOverlay(state.LightConfiguration, canvas, spriteLights, loopTime);
        }

        if (hitboxOverlay)
        {
            DrawHitboxOverlay(canvas, state);
        }
    }

    private static IReadOnlyList<UnitFrame> ResolveUnitFrames(
        BattleRenderState state,
        BattleAnimationState? animation,
        TimeSpan loopTime,
        TimeSpan sequenceTime)
    {
        Dictionary<BattleUnitId, BattleAnimationState.Unit> timelines = [];

        if (animation is not null)
        {
            foreach (BattleAnimationState.Unit timeline in animation.Units)
            {
                timelines[timeline.Id] = timeline;
            }
        }

        List<UnitFrame> frames = [];

        foreach (Unit unit in state.Units)
        {
            timelines.TryGetValue(unit.Id, out BattleAnimationState.Unit? timeline);
            frames.Add(ResolveUnitFrame(unit, timeline, animation is not null, loopTime, sequenceTime));
        }

        return frames;
    }

    private static UnitFrame ResolveUnitFrame(
        Unit unit,
        BattleAnimationState.Unit? timeline,
        bool isAnimating,
        TimeSpan loopTime,
        TimeSpan sequenceTime)
    {
        AssetAnimationKey.BattleUnitPose resolvedPose = AssetAnimationKey.BattleUnitPose.Idle;
        bool resolvedLoop = true;
        TimeSpan resolvedIdleTime = loopTime + unit.IdleOffset;
        double resolvedProgress = 0;
        double resolvedOpacity = 1;
        Presence resolvedPresence = unit.Presence;
        UnitDefinitionId resolvedDefinitionId = unit.DefinitionId;
        BattleSide resolvedFacing = unit.Facing;
        Auras resolvedAuras = unit.Auras;
        Vector resolvedMotion = ResolveMotion(timeline?.Motion ?? [], sequenceTime);
        Color? resolvedHighlight = isAnimating ? null : unit.Highlight;
        bool resolvedFrozen = unit.Frozen;

        if (timeline is not null)
        {
            if (LatestAt(timeline.Body, sequenceTime) is { } body)
            {
                resolvedPresence = body.Presence;

                if (body.Opacity is not null)
                {
                    resolvedOpacity = body.Opacity.ValueAt(sequenceTime);
                }

                if (sequenceTime < body.At + body.Duration)
                {
                    resolvedPose = body.Pose;
                    resolvedLoop = false;
                    resolvedProgress = (sequenceTime - body.At) / body.Duration;
                }
                else
                {
                    resolvedIdleTime = sequenceTime - body.At;
                }
            }

            if (LatestAt(timeline.Form, sequenceTime) is { } form)
            {
                resolvedDefinitionId = form.DefinitionId;
                resolvedFacing = form.Facing;
                resolvedAuras = form.Auras;
            }

            if (LatestAt(timeline.Status, sequenceTime) is { } status)
            {
                resolvedFrozen = status.Frozen;
            }
        }

        BodyFrame bodyFrame = new(
            resolvedPose,
            resolvedLoop,
            resolvedFrozen,
            resolvedProgress,
            resolvedOpacity,
            resolvedPresence,
            resolvedHighlight);

        return new UnitFrame(
            unit,
            bodyFrame,
            resolvedDefinitionId,
            resolvedFacing,
            resolvedAuras,
            resolvedMotion,
            resolvedIdleTime);
    }

    private static Point GetRigLightPosition(BattleLightConfiguration lightConfiguration, BattleLight light, TimeSpan loopTime)
    {
        double angle = GetRigLightAngle(lightConfiguration, loopTime);

        return new Point(
            light.Position.X + light.Sway.X * Math.Sin(angle * light.SwayRate.X + light.SwayPhase.X),
            light.Position.Y + light.Sway.Y * Math.Sin(angle * light.SwayRate.Y + light.SwayPhase.Y));
    }

    private static SKPoint3 GetRigLightRadiance(BattleLightConfiguration lightConfiguration, BattleLight light, TimeSpan loopTime)
    {
        double angle = GetRigLightAngle(lightConfiguration, loopTime);
        double slow = Math.Sin(angle * light.FlickerRate + light.SwayPhase.X);
        double fast = Math.Sin(angle * (light.FlickerRate * 2 + 1) + light.SwayPhase.Y);
        double power = Math.Max(light.Power + light.Flicker * (0.65 * slow + 0.35 * fast), 0);

        return GetRadiance(light.Color, power);
    }

    private static double GetRigLightAngle(BattleLightConfiguration lightConfiguration, TimeSpan loopTime)
    {
        return Math.Tau * (loopTime / lightConfiguration.Cycle);
    }

    private static IReadOnlyList<LightSource> ResolveLightSources(
        IAssetMetadataProvider assets,
        IReadOnlyList<UnitFrame> frames,
        BattleAnimationState? animation,
        TimeSpan sequenceTime)
    {
        List<LightSource> sources = [];

        foreach (UnitFrame frame in frames)
        {
            AssetAnimationKey.BattleUnitAura? auraKey = frame.Auras.Get(frame.Body.State);

            if (frame.Body.Presence == Presence.Alive && auraKey is not null)
            {
                AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(auraKey);
                int frameIndex = ResolveFrameIndex(metadata, frame.Body, frame.IdleTime, frame.Unit);
                AnimationPlacement placement = BattleRenderLayout.GetUnitPlacement(frame.Unit.Footprint);

                sources.Add(new LightSource(auraKey, frameIndex, frame.Body.Loop, placement, frame.Motion, frame.Body.Opacity, ShadowFade: 1));
            }
        }

        if (animation is not null)
        {
            foreach (BattleAnimationState.EffectEntry effect in animation.Effects)
            {
                if (IsPlaying(effect.At, effect.Duration, sequenceTime))
                {
                    AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(effect.Animation);
                    int frameIndex = GetPlayingFrameIndex(metadata, effect.At, effect.Duration, sequenceTime);
                    AnimationPlacement placement = BattleRenderLayout.GetUnitPlacement(effect.Footprint);

                    double fade = GetShadowFade(metadata, frameIndex);

                    sources.Add(new LightSource(effect.Animation, frameIndex, Looping: false, placement, new Vector(0, 0), Opacity: 1, fade));
                }
            }

            foreach (AnimationClip clip in animation.Sequence.Clips)
            {
                if (IsPlaying(clip.Start, clip.Duration, sequenceTime))
                {
                    AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(clip.Animation);
                    int frameIndex = GetPlayingFrameIndex(metadata, clip.Start, clip.Duration, sequenceTime);

                    double fade = GetShadowFade(metadata, frameIndex);

                    sources.Add(new LightSource(clip.Animation, frameIndex, Looping: false, clip.Placement, new Vector(0, 0), Opacity: 1, fade));
                }
            }
        }

        return sources;
    }

    private static double GetShadowFade(AssetAnimationMetadata metadata, int frameIndex)
    {
        int edgeDistance = Math.Min(frameIndex + 1, metadata.FrameCount - frameIndex);

        return Math.Min(edgeDistance / (ShadowFadeFrames + 1.0), 1);
    }

    private static (IReadOnlyList<SceneLight> SpriteLights, IReadOnlyList<SceneLight> ShadowLights) ResolveLights(
        IAssetProvider assets,
        IReadOnlyList<LightSource> sources)
    {
        List<SceneLight> spriteLights = [];
        List<SceneLight> shadowLights = [];

        foreach (LightSource source in sources)
        {
            using AssetCache<Animation>.Lease lease = assets.GetRequiredAnimation(source.Animation);

            Animation animation = lease.Value;
            Sprite sprite = animation.FrameByIndex(source.FrameIndex, source.Looping);
            Rect bounds = BattleRenderLayout.GetFrameBounds(source.Placement, GetContent(sprite.Metadata));

            spriteLights.AddRange(CreateSpriteLights(
                sprite,
                bounds.Translate(source.Motion),
                source.Placement.ScaleOrigin + source.Motion,
                source.Opacity));

            SceneLight? shadowLight = CreateShadowLight(animation, source);

            if (shadowLight is not null)
            {
                shadowLights.Add(shadowLight);
            }
        }

        spriteLights.Sort(static (left, right) => right.Power.CompareTo(left.Power));

        if (spriteLights.Count > SpriteLightLimit)
        {
            spriteLights.RemoveRange(SpriteLightLimit, spriteLights.Count - SpriteLightLimit);
        }

        return (spriteLights, shadowLights);
    }

    private static SceneLight? CreateShadowLight(Animation animation, LightSource source)
    {
        double weight = 0;
        double power = 0;
        double x = 0;
        double y = 0;
        double radius = 0;
        Vector3D radiance = new(0, 0, 0);

        for (int offset = -ShadowSmoothingFrames; offset <= ShadowSmoothingFrames; offset++)
        {
            Sprite sprite = animation.FrameByIndex(source.FrameIndex + offset, source.Looping);
            double frameWeight = ShadowSmoothingFrames + 1 - Math.Abs(offset);

            weight += frameWeight;

            foreach (SpriteLight light in sprite.Lights)
            {
                double lightWeight = light.Power * frameWeight;

                power += lightWeight;
                x += (sprite.Metadata.OffsetX + light.Position.X) * lightWeight;
                y += (sprite.Metadata.OffsetY + light.Position.Y) * lightWeight;
                radius += light.Radius * lightWeight;
                radiance += light.Radiance * frameWeight;
            }
        }

        if (power > 0)
        {
            (Point anchor, double d, Point origin) = source.Placement;
            double intensity = d * d * source.Opacity * source.ShadowFade / weight;

            Point position = new(
                origin.X + (x / power + anchor.X - origin.X) * d + source.Motion.X,
                origin.Y + (y / power + anchor.Y - origin.Y) * d + source.Motion.Y);

            return new SceneLight(position, radius / power * d, radiance * intensity, power * intensity, origin + source.Motion);
        }
        else
        {
            return null;
        }
    }

    private static List<SceneLight> CreateSpriteLights(Sprite sprite, Rect bounds, Point foot, double opacity)
    {
        double scaleX = bounds.Width / sprite.Metadata.Width;
        double scaleY = bounds.Height / sprite.Metadata.Height;
        double scale = scaleX * scaleY * opacity;
        List<SceneLight> lights = [];

        foreach (SpriteLight light in sprite.Lights)
        {
            Point position = new(bounds.Left + light.Position.X * scaleX, bounds.Top + light.Position.Y * scaleY);

            lights.Add(new SceneLight(position, light.Radius * scaleX, light.Radiance * scale, light.Power * scale, foot));
        }

        return lights;
    }

    private static double GetLightHeight(SceneLight light)
    {
        return Math.Max(light.Foot.Y - light.Position.Y + light.Radius, LightMinimumHeight);
    }

    private static Vector3D GetGroundLightPosition(BattleGround ground, SceneLight light)
    {
        Point groundPoint = new(light.Position.X, light.Foot.Y);
        Vector3D lift = BattleRenderLayout.GroundNormal * (GetLightHeight(light) * BattleRenderLayout.HeightPerPixel);

        return new Vector3D(groundPoint.X, groundPoint.Y, -ground.GetDistance(groundPoint)) + lift;
    }

    private static double GetSpriteLightRange(BattleLightConfiguration lightConfiguration, double radius)
    {
        return Math.Max(radius * lightConfiguration.SpriteLightReach, lightConfiguration.SpriteLightMinimumRange);
    }

    private static double GetBackgroundSpriteLightRange(BattleLightConfiguration lightConfiguration, double radius)
    {
        return GetSpriteLightRange(lightConfiguration, radius) * lightConfiguration.BackgroundSpriteLightReach;
    }

    private static double GetBackgroundSpriteLightPower(BattleLightConfiguration lightConfiguration, SceneLight light)
    {
        return light.Power * lightConfiguration.SpriteLightGain * lightConfiguration.BackgroundSpriteLightBoost;
    }

    private static Vector ResolveMotion(IReadOnlyList<BattleAnimationState.MotionEntry> motions, TimeSpan sequenceTime)
    {
        double offsetX = 0;
        double offsetY = 0;

        foreach (BattleAnimationState.MotionEntry motion in motions)
        {
            if (sequenceTime >= motion.At && sequenceTime < motion.At + motion.Duration)
            {
                double shape = BattleUnitMotionCurve.Evaluate((sequenceTime - motion.At) / motion.Duration);

                offsetX += motion.Direction.X * shape * motion.Distance;
                offsetY += motion.Direction.Y * shape * motion.Distance;
            }
        }

        return new Vector(offsetX, offsetY);
    }

    private static Vector ResolveShake(IReadOnlyList<BattleAnimationState.ShakeEntry> shakes, TimeSpan sequenceTime)
    {
        double offsetX = 0;
        double offsetY = 0;

        foreach (BattleAnimationState.ShakeEntry shake in shakes)
        {
            if (sequenceTime >= shake.At && sequenceTime < shake.At + shake.Duration)
            {
                double progress = (sequenceTime - shake.At) / shake.Duration;
                double remaining = 1 - progress;
                double amplitude = shake.Amplitude * remaining * remaining;
                double seconds = (sequenceTime - shake.At).TotalSeconds;

                offsetX += amplitude * Math.Sin(Math.Tau * ShakeFrequency * seconds);
                offsetY += amplitude * ShakeVerticalRatio * Math.Sin(Math.Tau * ShakeFrequency * ShakeVerticalFrequencyRatio * seconds);
            }
        }

        return new Vector(offsetX, offsetY);
    }

    private static TEntry? LatestAt<TEntry>(IReadOnlyList<TEntry> entries, TimeSpan sequenceTime)
        where TEntry : class, IBattleAnimationEntry
    {
        TEntry? found = null;

        for (int index = 0; index < entries.Count && entries[index].At <= sequenceTime; index++)
        {
            found = entries[index];
        }

        return found;
    }

    private static void DrawBackground(SKCanvas canvas, BattleRenderContent content)
    {
        backgroundCache ??= new BackgroundCache();
        backgroundCache.Draw(canvas, content.BackgroundShaderProgram);
    }

    private static void ConfigureLighting(
        BattleRenderContent content,
        BattleLightConfiguration lightConfiguration,
        BattleRainConfiguration? rainConfiguration,
        IReadOnlyList<SceneLight> spriteLights,
        TimeSpan loopTime)
    {
        Span<SKColorF> unitRigLightPositions = stackalloc SKColorF[RigLightCapacity];
        Span<SKColorF> groundRigLightPositions = stackalloc SKColorF[RigLightCapacity];
        Span<SKColorF> rigLightRadiances = stackalloc SKColorF[RigLightCapacity];

        for (int index = 0; index < lightConfiguration.RigLights.Count; index++)
        {
            BattleLight light = lightConfiguration.RigLights[index];
            Point position = GetRigLightPosition(lightConfiguration, light, loopTime);
            SKPoint3 radiance = GetRigLightRadiance(lightConfiguration, light, loopTime);

            double groundStandoff = light.Standoff - content.Ground.GetDistance(position);

            unitRigLightPositions[index] = new SKColorF((float)position.X, (float)position.Y, (float)light.Standoff, (float)light.Range);
            groundRigLightPositions[index] = new SKColorF((float)position.X, (float)position.Y, (float)groundStandoff, (float)light.Range);
            rigLightRadiances[index] = new SKColorF(radiance.X, radiance.Y, radiance.Z, 0.0f);
        }

        double exposure = 1;
        SKPoint3 stormSkyRadiance = new(0, 0, 0);
        BattleStormConfiguration? storm = lightConfiguration.Storm;

        if (storm is not null)
        {
            StormFlash flash = ResolveStormFlash(storm, loopTime);

            if (flash.Intensity > 0)
            {
                int index = lightConfiguration.RigLights.Count;
                SKPoint3 radiance = GetRadiance(storm.Color, storm.Power * flash.Intensity);
                double groundStandoff = storm.Standoff - content.Ground.GetDistance(flash.Position);

                unitRigLightPositions[index] = new SKColorF((float)flash.Position.X, (float)flash.Position.Y, (float)storm.Standoff, (float)storm.Range);
                groundRigLightPositions[index] = new SKColorF((float)flash.Position.X, (float)flash.Position.Y, (float)groundStandoff, (float)storm.Range);
                rigLightRadiances[index] = new SKColorF(radiance.X, radiance.Y, radiance.Z, 0.0f);

                exposure = 1 + (storm.Exposure * flash.Intensity);
                stormSkyRadiance = Scale(radiance, storm.SkyGain);
            }
        }

        Span<SKColorF> unitSpriteLightPositions = stackalloc SKColorF[SpriteLightLimit];
        Span<SKColorF> groundSpriteLightPositions = stackalloc SKColorF[SpriteLightLimit];
        Span<SKColorF> spriteLightRadiances = stackalloc SKColorF[SpriteLightLimit];

        for (int index = 0; index < spriteLights.Count; index++)
        {
            SceneLight light = spriteLights[index];
            float range = (float)GetSpriteLightRange(lightConfiguration, light.Radius);
            Vector3D groundPosition = GetGroundLightPosition(content.Ground, light);

            unitSpriteLightPositions[index] = new SKColorF(
                (float)light.Position.X,
                (float)light.Position.Y,
                (float)lightConfiguration.SpriteLightStandoff,
                range);

            groundSpriteLightPositions[index] = new SKColorF(
                (float)groundPosition.X,
                (float)groundPosition.Y,
                (float)groundPosition.Z,
                range);

            spriteLightRadiances[index] = new SKColorF(
                (float)(light.Radiance.X * lightConfiguration.SpriteLightGain),
                (float)(light.Radiance.Y * lightConfiguration.SpriteLightGain),
                (float)(light.Radiance.Z * lightConfiguration.SpriteLightGain),
                0.0f);
        }

        SKPoint3 sunDirection = lightConfiguration.SunDirection.ToSKPoint3();
        SKPoint3 sunRadiance = GetRadiance(lightConfiguration.SunColor, lightConfiguration.SunPower);
        SKPoint3 sunGrade = Scale(GetSunGrade(sunRadiance), exposure);
        SKPoint3 skyRadiance = GetSkyRadiance(sunRadiance, lightConfiguration.BackgroundAmbient, lightConfiguration.UnitSkyGain);
        SKPoint3 unitSkyRadiance = Scale(skyRadiance, exposure);

        using BattleUnitShaderProgram.Editor unitEditor = content.UnitShaderProgram.Edit();

        unitEditor.SetSunDirection(sunDirection);
        unitEditor.SetSunRadiance(sunRadiance);
        unitEditor.SetSunGrade(sunGrade);
        unitEditor.SetSkyRadiance(unitSkyRadiance);
        unitEditor.SetRigLightPositions(unitRigLightPositions);
        unitEditor.SetRigLightRadiances(rigLightRadiances);
        unitEditor.SetSpriteLightPositions(unitSpriteLightPositions);
        unitEditor.SetSpriteLightRadiances(spriteLightRadiances);

        using BattleBackgroundShaderProgram.Editor backgroundEditor = content.BackgroundShaderProgram.Edit();

        backgroundEditor.SetSunDirection(sunDirection);
        backgroundEditor.SetSunRadiance(sunRadiance);
        backgroundEditor.SetSunGrade(sunGrade);
        backgroundEditor.SetRigLightPositions(groundRigLightPositions);
        backgroundEditor.SetRigLightRadiances(rigLightRadiances);
        backgroundEditor.SetSpriteLightPositions(groundSpriteLightPositions);
        backgroundEditor.SetSpriteLightRadiances(spriteLightRadiances);
        backgroundEditor.SetAmbient((float)lightConfiguration.BackgroundAmbient);
        backgroundEditor.SetShadingCap((float)lightConfiguration.BackgroundShadingCap);
        backgroundEditor.SetRollOffThreshold((float)lightConfiguration.BackgroundRollOffThreshold);
        backgroundEditor.SetSpriteLightBoost((float)lightConfiguration.BackgroundSpriteLightBoost);
        backgroundEditor.SetSpriteLightReach((float)lightConfiguration.BackgroundSpriteLightReach);
        backgroundEditor.SetDepthScale((float)content.Ground.DepthScale);

        foreach (BattleFluidShaderProgram fluidShaderProgram in content.FluidShaderPrograms)
        {
            using BattleFluidShaderProgram.Editor fluidEditor = fluidShaderProgram.Edit();

            fluidEditor.SetSunRadiance(sunRadiance);
            fluidEditor.SetRigLightPositions(groundRigLightPositions);
            fluidEditor.SetRigLightRadiances(rigLightRadiances);
            fluidEditor.SetSpriteLightPositions(groundSpriteLightPositions);
            fluidEditor.SetSpriteLightRadiances(spriteLightRadiances);
            fluidEditor.SetDepthScale((float)content.Ground.DepthScale);
        }

        using BattleRainShaderProgram.Editor rainEditor = content.RainShaderProgram.Edit();

        rainEditor.SetRigLightPositions(groundRigLightPositions);
        rainEditor.SetRigLightRadiances(rigLightRadiances);
        rainEditor.SetDepthScale((float)content.Ground.DepthScale);

        if (rainConfiguration is not null)
        {
            rainEditor.SetSkyRadiance(GetSkyRadiance(sunRadiance, lightConfiguration.BackgroundAmbient, rainConfiguration.SkyGain) + stormSkyRadiance);
            rainEditor.SetStreakRadiance(GetRadiance(rainConfiguration.StreakColor, 1));
            rainEditor.SetSplashRadiance(GetRadiance(rainConfiguration.SplashColor, 1));
            rainEditor.SetHazeRadiance(GetRadiance(rainConfiguration.HazeColor, 1));
        }
    }

    private static void DrawGroundMarker(SKCanvas canvas, BattleGridFootprint footprint, Color? marker, TimeSpan loopTime)
    {
        if (marker is not null)
        {
            SKColor color = marker.Value.ToSKColor();
            Point foot = BattleRenderLayout.GetFootPoint(footprint);
            double radius = BattleRenderLayout.GetMarkerRadius(footprint);

            canvas.Save();
            canvas.Concat(GetMarkerMatrix(foot, radius, loopTime));

            DrawMarkerOverlay(canvas);
            DrawMarkerPath(canvas, BattleMarkerGeometry.Disc, WithOpacity(color, MarkerDiscOpacity));
            DrawMarkerPath(canvas, BattleMarkerGeometry.Ring, WithOpacity(color, MarkerRingOpacity));

            canvas.Restore();
        }
    }

    private static void DrawUnitShadows(
        SKCanvas canvas,
        BattleRenderContent content,
        BattleLightConfiguration lightConfiguration,
        IAssetProvider assets,
        IReadOnlyList<UnitFrame> frames,
        IReadOnlyList<SceneLight> shadowLights)
    {
        foreach (UnitFrame frame in frames)
        {
            Point footPoint = BattleRenderLayout.GetFootPoint(frame.Unit.Footprint) + frame.Motion;

            if (frame.Body.Presence == Presence.Alive)
            {
                SceneLight? light = FindShadowLight(lightConfiguration, shadowLights, footPoint);

                if (light is not null)
                {
                    DrawUnitShadow(canvas, content, lightConfiguration, assets, frame, footPoint, light);
                }
            }
        }
    }

    private static SceneLight? FindShadowLight(
        BattleLightConfiguration lightConfiguration,
        IReadOnlyList<SceneLight> shadowLights,
        Point footPoint)
    {
        SceneLight? strongestLight = null;
        double strongestFootLight = ShadowMinimumLight;

        foreach (SceneLight light in shadowLights)
        {
            double footLight = GetFootLight(light);

            if (GetGroundDistance(light.Foot, footPoint) >= ShadowMinimumDistance && footLight >= strongestFootLight)
            {
                strongestLight = light;
                strongestFootLight = footLight;
            }
        }

        return strongestLight;

        double GetFootLight(SceneLight light)
        {
            Point ground = new(light.Position.X, light.Foot.Y);
            double groundDistance = GetGroundDistance(ground, footPoint);
            double height = GetShadowLightHeight(light, footPoint) * BattleRenderLayout.HeightPerPixel;
            double distance = Math.Sqrt(groundDistance * groundDistance + height * height);
            double relative = distance / GetBackgroundSpriteLightRange(lightConfiguration, light.Radius);

            return GetBackgroundSpriteLightPower(lightConfiguration, light) * height / distance / (1 + relative * relative) / Math.PI;
        }
    }

    private static double GetGroundDistance(Point from, Point to)
    {
        double across = to.X - from.X;
        double depth = (to.Y - from.Y) * BattleRenderLayout.GroundDepthPerPixel;

        return Math.Sqrt(across * across + depth * depth);
    }

    private static void DrawUnitShadow(
        SKCanvas canvas,
        BattleRenderContent content,
        BattleLightConfiguration lightConfiguration,
        IAssetProvider assets,
        UnitFrame frame,
        Point foot,
        SceneLight light)
    {
        using AssetCache<Animation>.Lease body = assets.GetRequiredAnimation(
            new AssetAnimationKey.BattleUnit(frame.DefinitionId, frame.Body.State, frame.Facing));

        Animation animation = body.Value;
        Sprite sprite = animation[ResolveFrameIndex(animation.Metadata, frame.Body, frame.IdleTime, frame.Unit)];
        Rect bounds = GetUnitFrameBounds(sprite, frame.Unit);
        Point ground = new(light.Position.X, light.Foot.Y);
        double height = GetShadowLightHeight(light, foot);
        Vector span = GetShadowSpan(foot, ground);
        double penumbra = lightConfiguration.ShadowPenumbraLimit;
        Rect caster = bounds.Translate(frame.Motion);
        double top = Math.Max(caster.Top, foot.Y - lightConfiguration.ShadowRiseLimit * height);
        double bottom = caster.Bottom + penumbra;

        if (caster is { Width: > 0, Height: > 0 } && top < bottom)
        {
            SKRect area = new((float)(caster.Left - penumbra), (float)top, (float)(caster.Right + penumbra), (float)bottom);

            using SKShader imageShader = MakeUnitImageShader(sprite.Image, caster.ToSKRect());
            using SKShader shader = BuildShadowShader(content, lightConfiguration, imageShader, foot, light, height, span, frame.Body.Opacity);
            using SKPaint paint = new() { Shader = shader, BlendMode = SKBlendMode.Modulate };

            canvas.Save();
            canvas.Concat(GetShadowMatrix(foot, ground, height, span));
            canvas.DrawRect(area, paint);
            canvas.Restore();
        }
    }

    private static SKShader BuildShadowShader(
        BattleRenderContent content,
        BattleLightConfiguration lightConfiguration,
        SKShader source,
        Point foot,
        SceneLight light,
        double height,
        Vector span,
        double opacity)
    {
        using BattleShadowShaderProgram.Editor editor = content.ShadowShaderProgram.Edit();

        editor.SetSource(source);
        editor.SetFoot(foot.ToSKPoint());
        editor.SetLight(new SKPoint3((float)light.Position.X, (float)light.Foot.Y, (float)height));
        editor.SetLightPower((float)GetBackgroundSpriteLightPower(lightConfiguration, light));
        editor.SetLightTint(GetShadowTint(light.Radiance));
        editor.SetLightRange((float)GetBackgroundSpriteLightRange(lightConfiguration, light.Radius));
        editor.SetLightRadius((float)light.Radius);
        editor.SetSpan(span.ToSKPoint());
        editor.SetOpacity((float)opacity);

        return editor.Build();
    }

    private static SKMatrix GetShadowMatrix(Point foot, Point ground, double height, Vector span)
    {
        double offset = height - foot.Y;

        return new SKMatrix
        {
            ScaleX = (float)span.X,
            SkewX = (float)(ground.X / height),
            TransX = (float)((ground.X * offset / height) + foot.X - (span.X * foot.X) - ground.X),
            SkewY = (float)span.Y,
            ScaleY = (float)(ground.Y / height),
            TransY = (float)((ground.Y * offset / height) + foot.Y - (span.Y * foot.X) - ground.Y),
            Persp0 = 0,
            Persp1 = (float)(1 / height),
            Persp2 = (float)(offset / height)
        };
    }

    private static Vector GetShadowSpan(Point foot, Point ground)
    {
        double depthPerPixel = BattleRenderLayout.GroundDepthPerPixel;
        Vector away = new(foot.X - ground.X, (foot.Y - ground.Y) * depthPerPixel);
        double length = away.Length;

        if (length > 0)
        {
            Vector across = away.Y >= 0 ? new Vector(away.Y, -away.X) : new Vector(-away.Y, away.X);

            return new Vector(across.X / length, across.Y / length / depthPerPixel);
        }
        else
        {
            return new Vector(1, 0);
        }
    }

    private static double GetShadowLightHeight(SceneLight light, Point foot)
    {
        Point ground = new(light.Position.X, light.Foot.Y);
        double reach = GetGroundDistance(foot, ground) / ShadowMaximumSlope / BattleRenderLayout.HeightPerPixel;

        return Math.Max(GetLightHeight(light), reach);
    }

    private static SKPoint3 GetShadowTint(Vector3D radiance)
    {
        double peak = Math.Max(Math.Max(radiance.X, radiance.Y), Math.Max(radiance.Z, 1e-9));

        return new SKPoint3((float)(radiance.X / peak), (float)(radiance.Y / peak), (float)(radiance.Z / peak));
    }

    private static void DrawBody(SKCanvas canvas, BattleRenderContent content, IAssetProvider assets, UnitFrame frame)
    {
        BodyFrame body = frame.Body;

        if (body.Presence == Presence.Alive)
        {
            DrawAliveBody(canvas, content, assets, frame);
        }
        else if (body.Presence == Presence.Dead)
        {
            DrawDeadBody(canvas, content, assets, frame.Unit, body.Opacity);
        }
    }

    private static void DrawAliveBody(SKCanvas canvas, BattleRenderContent content, IAssetProvider assets, UnitFrame frame)
    {
        Unit unit = frame.Unit;
        BodyFrame body = frame.Body;

        canvas.Save();
        canvas.Translate((float)frame.Motion.X, (float)frame.Motion.Y);

        using (AssetCache<Animation>.Lease shadow = assets.GetRequiredAnimation(
            new AssetAnimationKey.BattleUnitShadow(frame.DefinitionId, body.State, frame.Facing)))
        {
            double shadowOpacity = Math.Min(content.ShadowStrength, 1);

            DrawAnimationLayer(canvas, shadow, body with { Opacity = body.Opacity * shadowOpacity }, frame.IdleTime, unit);
        }

        using (AssetCache<Animation>.Lease sprite = assets.GetRequiredAnimation(
            new AssetAnimationKey.BattleUnit(frame.DefinitionId, body.State, frame.Facing)))
        {
            DrawBodyLayer(canvas, content, frame.Motion, sprite, body, frame.IdleTime, unit);
        }

        AssetAnimationKey.BattleUnitAura? auraKey = frame.Auras.Get(body.State);

        if (auraKey is not null)
        {
            using AssetCache<Animation>.Lease aura = assets.GetRequiredAnimation(auraKey);
            DrawAnimationLayer(canvas, aura.Value, body, frame.IdleTime, unit);
        }

        canvas.Restore();
    }

    private static void DrawDeadBody(SKCanvas canvas, BattleRenderContent content, IAssetProvider assets, Unit unit, double opacity)
    {
        AssetSpriteKey.BattleCorpse corpseKey = new(unit.Footprint.IsLarge, unit.CorpseVariant);
        using AssetCache<Sprite>.Lease corpse = assets.GetRequiredSprite(corpseKey);

        Sprite sprite = corpse.Value;
        AnimationPlacement placement = BattleRenderLayout.GetCorpsePlacement(unit.Footprint);
        Rect bounds = BattleRenderLayout.GetContentBounds(placement, GetContent(sprite.Metadata));
        SKImage? normal = sprite.Normal;

        if (normal is not null)
        {
            DrawUnitImage(canvas, content, default, sprite.Image, normal, bounds, opacity, highlight: null, TimeSpan.Zero);
        }
        else
        {
            DrawImage(canvas, sprite.Image, bounds, opacity);
        }
    }

    private static void DrawUnits(
        SKCanvas canvas,
        BattleRenderContent content,
        IAssetProvider assets,
        IReadOnlyList<UnitFrame> frames,
        IReadOnlyList<BattleAnimationState.EffectEntry> effects,
        TimeSpan sequenceTime)
    {
        int effectCursor = 0;

        foreach (UnitFrame frame in frames)
        {
            int unitDepth = BattleRenderDepth.GetUnitDepth(frame.Unit.Footprint);

            while (effectCursor < effects.Count &&
                   BattleRenderDepth.GetEffectDepth(effects[effectCursor].Footprint) < unitDepth)
            {
                DrawEffect(canvas, assets, effects[effectCursor], sequenceTime);
                effectCursor++;
            }

            DrawBody(canvas, content, assets, frame);
        }

        while (effectCursor < effects.Count)
        {
            DrawEffect(canvas, assets, effects[effectCursor], sequenceTime);
            effectCursor++;
        }
    }

    private static void DrawEffect(SKCanvas canvas, IAssetProvider assets, BattleAnimationState.EffectEntry effect, TimeSpan sequenceTime)
    {
        AnimationPlacement placement = BattleRenderLayout.GetUnitPlacement(effect.Footprint);

        DrawFrame(canvas, assets, effect.Animation, placement, effect.At, effect.Duration, sequenceTime);
    }

    private static void DrawClips(SKCanvas canvas, IAssetProvider assets, IReadOnlyList<AnimationClip> clips, TimeSpan sequenceTime)
    {
        foreach (AnimationClip clip in clips)
        {
            DrawFrame(canvas, assets, clip.Animation, clip.Placement, clip.Start, clip.Duration, sequenceTime);
        }
    }

    private static void DrawFrame(
        SKCanvas canvas,
        IAssetProvider assets,
        AssetAnimationKey key,
        AnimationPlacement placement,
        TimeSpan start,
        TimeSpan duration,
        TimeSpan sequenceTime)
    {
        if (IsPlaying(start, duration, sequenceTime))
        {
            using AssetCache<Animation>.Lease frameAnimation = assets.GetRequiredAnimation(key);

            Sprite sprite = GetPlayingFrame(frameAnimation.Value, start, duration, sequenceTime);
            Rect bounds = BattleRenderLayout.GetFrameBounds(placement, GetContent(sprite.Metadata));

            DrawImage(canvas, sprite.Image, bounds, opacity: 1);
        }
    }

    private static bool IsPlaying(TimeSpan start, TimeSpan duration, TimeSpan sequenceTime)
    {
        return sequenceTime >= start && sequenceTime < start + duration;
    }

    private static Sprite GetPlayingFrame(Animation animation, TimeSpan start, TimeSpan duration, TimeSpan sequenceTime)
    {
        int frameIndex = GetPlayingFrameIndex(animation.Metadata, start, duration, sequenceTime);

        return animation.FrameByIndex(frameIndex, looping: false);
    }

    private static int GetPlayingFrameIndex(AssetAnimationMetadata metadata, TimeSpan start, TimeSpan duration, TimeSpan sequenceTime)
    {
        double progress = (sequenceTime - start) / duration;

        return (int)(progress * metadata.FrameCount);
    }

    private static void DrawAnimationLayer(SKCanvas canvas, Animation animation, BodyFrame frame, TimeSpan idleTime, Unit unit)
    {
        int frameIndex = ResolveFrameIndex(animation.Metadata, frame, idleTime, unit);
        Sprite sprite = animation[frameIndex];
        Rect bounds = GetUnitFrameBounds(sprite, unit);

        DrawImage(canvas, sprite.Image, bounds, frame.Opacity);
    }

    private static void DrawBodyLayer(
        SKCanvas canvas,
        BattleRenderContent content,
        Vector motion,
        Animation animation,
        BodyFrame frame,
        TimeSpan idleTime,
        Unit unit)
    {
        int frameIndex = ResolveFrameIndex(animation.Metadata, frame, idleTime, unit);
        Sprite sprite = animation[frameIndex];
        Rect bounds = GetUnitFrameBounds(sprite, unit);
        SKImage? normal = sprite.Normal;

        if (normal is not null)
        {
            DrawUnitImage(canvas, content, motion, sprite.Image, normal, bounds, frame.Opacity, frame.Highlight, idleTime);
        }
        else
        {
            DrawImage(canvas, sprite.Image, bounds, frame.Opacity);
        }
    }

    private static int ResolveFrameIndex(AssetAnimationMetadata metadata, BodyFrame frame, TimeSpan idleTime, Unit unit)
    {
        int frameCount = metadata.FrameCount;

        if (frame.Loop)
        {
            int frameIndex;

            if (frame.Frozen)
            {
                frameIndex = AnimationRate.Sprites.GetFrameIndex(unit.IdleOffset);
            }
            else
            {
                frameIndex = AnimationRate.Sprites.GetFrameIndex(idleTime);
            }

            return frameIndex.Mod(frameCount);
        }
        else
        {
            return Math.Clamp((int)(frame.Progress * frameCount), 0, frameCount - 1);
        }
    }

    private static Rect GetUnitFrameBounds(Sprite sprite, Unit unit)
    {
        AnimationPlacement placement = BattleRenderLayout.GetUnitPlacement(unit.Footprint);
        Rect content = GetContent(sprite.Metadata);

        return BattleRenderLayout.GetFrameBounds(placement, content);
    }

    private static Rect GetContent(AssetSpriteMetadata metadata)
    {
        return new Rect(metadata.OffsetX, metadata.OffsetY, metadata.Width, metadata.Height);
    }

    private static void DrawImage(SKCanvas canvas, SKImage image, Rect bounds, double opacity)
    {
        if (bounds is { Width: > 0, Height: > 0 })
        {
            SKRect destination = bounds.ToSKRect();
            using SKPaint paint = SkiaHelpers.MakeImagePaint();
            paint.Color = WithOpacity(paint.Color, opacity);
            canvas.DrawImage(image, destination, RenderSamplingOptions.Linear, paint);
        }
    }

    private static void DrawFluid(
        SKCanvas canvas,
        BattleFluidShaderProgram program,
        IAssetProvider assets,
        AssetAnimationKey.BattleBackgroundOverlay overlayKey,
        BattleFluidConfiguration configuration,
        TimeSpan loopTime)
    {
        using AssetCache<Animation>.Lease? overlay = assets.GetOptionalAnimation(overlayKey);

        if (overlay is not null)
        {
            Animation animation = overlay;
            int frameIndex = configuration.Rate.GetFrameIndex(loopTime).Mod(animation.Metadata.FrameCount);
            Sprite frame = animation[frameIndex];

            SKMatrix gridToScene = SKMatrix.CreateScaleTranslation(
                frame.Metadata.Width / (float)frame.Image.Width,
                frame.Metadata.Height / (float)frame.Image.Height,
                frame.Metadata.OffsetX,
                frame.Metadata.OffsetY);

            using SKShader thickness = frame.Image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, RenderSamplingOptions.Linear, gridToScene);
            using SKShader shader = BuildFluidShader(program, thickness);
            using SKPaint paint = new() { Shader = shader };

            canvas.DrawRect(canvas.LocalClipBounds, paint);
        }
    }

    private static SKShader BuildFluidShader(BattleFluidShaderProgram program, SKShader thickness)
    {
        using BattleFluidShaderProgram.Editor editor = program.Edit();

        editor.SetThickness(thickness);

        return editor.Build();
    }

    private static void DrawRain(SKCanvas canvas, BattleRenderContent content, BattleRainLayer layer, TimeSpan loopTime)
    {
        using SKShader shader = BuildRainShader(content, layer, loopTime);
        using SKPaint paint = new() { Shader = shader };

        canvas.DrawRect(canvas.LocalClipBounds, paint);
    }

    private static SKShader BuildRainShader(BattleRenderContent content, BattleRainLayer layer, TimeSpan loopTime)
    {
        using BattleRainShaderProgram.Editor editor = content.RainShaderProgram.Edit();

        editor.SetTime((float)loopTime.TotalSeconds);
        editor.SetNearDepth((float)layer.NearDepth);
        editor.SetFarDepth((float)layer.FarDepth);
        editor.SetSplashOpacity((float)layer.SplashOpacity);

        return editor.Build();
    }

    private static void DrawUnitImage(
        SKCanvas canvas,
        BattleRenderContent content,
        Vector motion,
        SKImage image,
        SKImage normal,
        Rect bounds,
        double opacity,
        Color? highlight,
        TimeSpan highlightTime)
    {
        if (bounds is { Width: > 0, Height: > 0 })
        {
            SKRect destination = bounds.ToSKRect();
            using SKShader imageShader = MakeUnitImageShader(image, destination);
            using SKShader normalShader = MakeUnitImageShader(normal, destination);
            using SKShader shader = BuildUnitShader(content, imageShader, normalShader, motion, highlight, highlightTime);
            using SKPaint paint = SkiaHelpers.MakeImagePaint();

            paint.Shader = shader;
            paint.Color = WithOpacity(paint.Color, opacity);
            canvas.DrawRect(destination, paint);
        }
    }

    private static SKShader BuildUnitShader(
        BattleRenderContent content,
        SKShader source,
        SKShader normals,
        Vector motion,
        Color? highlight,
        TimeSpan highlightTime)
    {
        using BattleUnitShaderProgram.Editor editor = content.UnitShaderProgram.Edit();
        SKColorF highlightColor;

        if (highlight is { } color)
        {
            highlightColor = color.ToSKColor();
        }
        else
        {
            highlightColor = SKColorF.Empty;
        }

        editor.SetSource(source);
        editor.SetNormals(normals);
        editor.SetSceneOffset(new SKPoint((float)motion.X, (float)motion.Y));
        editor.SetHighlightColor(highlightColor);
        editor.SetHighlightTime((float)highlightTime.TotalSeconds);

        return editor.Build();
    }

    private static SKShader MakeUnitImageShader(SKImage image, SKRect destination)
    {
        SKMatrix local = SKMatrix.CreateScaleTranslation(
            destination.Width / image.Width,
            destination.Height / image.Height,
            destination.Left,
            destination.Top);

        return image.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, RenderSamplingOptions.Linear, local);
    }

    private static SKPoint3 GetSkyRadiance(SKPoint3 sunRadiance, double ambient, double gain)
    {
        return new SKPoint3(
            (float)((sunRadiance.X + ambient) * gain),
            (float)((sunRadiance.Y + ambient) * gain),
            (float)((sunRadiance.Z + ambient) * gain));
    }

    private static SKPoint3 GetSunGrade(SKPoint3 sunRadiance)
    {
        double luminance = (0.2126 * sunRadiance.X) + (0.7152 * sunRadiance.Y) + (0.0722 * sunRadiance.Z);
        double chromaScale = 1.0 / Math.Max(luminance, 1e-3);
        double exposure = 1 + (((luminance / SunExposurePivot) - 1) * SunExposure);

        return new SKPoint3(
            (float)((1 + (((sunRadiance.X * chromaScale) - 1) * SunTint)) * exposure),
            (float)((1 + (((sunRadiance.Y * chromaScale) - 1) * SunTint)) * exposure),
            (float)((1 + (((sunRadiance.Z * chromaScale) - 1) * SunTint)) * exposure));
    }

    private static SKPoint3 GetRadiance(Color color, double power)
    {
        return new SKPoint3(
            (float)(ConvertSrgbToLinear(color.R) * power),
            (float)(ConvertSrgbToLinear(color.G) * power),
            (float)(ConvertSrgbToLinear(color.B) * power));
    }

    private static SKMatrix GetMarkerMatrix(Point foot, double radius, TimeSpan loopTime)
    {
        Matrix transform = BattleRenderLayout.MarkerTransform;

        SKMatrix result = new()
        {
            ScaleX = (float)transform.M11,
            SkewX = (float)transform.M21,
            SkewY = (float)transform.M12,
            ScaleY = (float)transform.M22,
            TransX = (float)foot.X,
            TransY = (float)foot.Y,
            Persp2 = 1
        };

        float spin = (float)(-loopTime.TotalSeconds * BattleMarkerGeometry.RotationDegreesPerSecond % 360.0);

        result = result.PreConcat(SKMatrix.CreateRotationDegrees(spin));
        result = result.PreConcat(SKMatrix.CreateScale((float)radius, (float)radius));

        return result;
    }

    private static void DrawMarkerOverlay(SKCanvas canvas)
    {
        using SKPaint paint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = Theme.Battle.GroundMarker.OverlayColor.ToSKColor(),
            BlendMode = SKBlendMode.Multiply
        };

        canvas.DrawPath(BattleMarkerGeometry.Plate, paint);
    }

    private static void DrawMarkerPath(SKCanvas canvas, SKPath path, SKColor color)
    {
        using SKPaint paint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = color
        };

        canvas.DrawPath(path, paint);
    }

    private static SKColor WithOpacity(SKColor color, double opacity)
    {
        return color.WithAlpha((byte)Math.Round(color.Alpha * Math.Clamp(opacity, 0, 1)));
    }

    private static byte ConvertLinearToSrgb(double channel)
    {
        return (byte)Math.Round(Math.Pow(Math.Clamp(channel, 0, 1), 1.0 / 2.2) * 255);
    }

    private static float ConvertSrgbToLinear(byte channel)
    {
        return MathF.Pow(channel / 255.0f, 2.2f);
    }
}
