using System.Globalization;
using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class BattleAnimationStateBuilderTests
{
    private static string FrameDuration(TimeSpan time)
    {
        double frames = time.TotalSeconds * AnimationRate.Sprites.FramesPerSecond;

        return frames.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatSize(bool isLarge)
    {
        if (isLarge)
        {
            return "large";
        }
        else
        {
            return "small";
        }
    }

    private static string FormatAuras(BattleUnitState.Auras auras)
    {
        JoinStringBuilder keys = new(", ");

        if (auras.Idle is not null)
        {
            keys.Add(auras.Idle.CanonicalId);
        }

        if (auras.Attack is not null)
        {
            keys.Add(auras.Attack.CanonicalId);
        }

        if (auras.Hit is not null)
        {
            keys.Add(auras.Hit.CanonicalId);
        }

        if (keys.Count > 0)
        {
            return $" auras [{keys}]";
        }
        else
        {
            return string.Empty;
        }
    }

    private static string FormatBody(BattleAnimationState.Unit unit, BattleAnimationState.BodyEntry body)
    {
        if (body.Duration == TimeSpan.Zero)
        {
            return $"unit {unit.Id.Value} body {body.Pose} {body.Presence}";
        }
        else
        {
            return $"unit {unit.Id.Value} body {body.Pose} {body.Presence} for {FrameDuration(body.Duration)} frames";
        }
    }

    private static string FormatOpacity(AnimationProperty property)
    {
        BattleAnimationTarget.UnitBody target = Assert.IsOfType<BattleAnimationTarget.UnitBody>(property.Target);
        string from = property.From.ToString(CultureInfo.InvariantCulture);
        string to = property.To.ToString(CultureInfo.InvariantCulture);

        return $"unit {target.UnitId.Value} opacity {from} -> {to} over {FrameDuration(property.Duration)} frames {FormatEasing(property.Easing)}";
    }

    private static string FormatForm(BattleAnimationState.Unit unit, BattleAnimationState.FormEntry form)
    {
        return $"unit {unit.Id.Value} form {form.DefinitionId.Value} facing {form.Facing}{FormatAuras(form.Auras)}";
    }

    private static string FormatStatus(BattleAnimationState.Unit unit, BattleAnimationState.StatusEntry status)
    {
        return $"unit {unit.Id.Value} frozen {status.Frozen}";
    }

    private static string FormatFeedback(BattleAnimationState.Unit unit, BattleAnimationState.FeedbackEntry feedback)
    {
        return $"unit {unit.Id.Value} feedback +{FrameDuration(feedback.Duration)} {feedback.Feedback.Type} {feedback.Feedback.Text}";
    }

    private static string FormatHitpointsTarget(AnimationTarget target)
    {
        return target switch
        {
            BattleAnimationTarget.UnitHitpoints hitpoints => $"unit {hitpoints.UnitId.Value} hitpoints",
            BattleAnimationTarget.UnitMaxHitpoints maxHitpoints => $"unit {maxHitpoints.UnitId.Value} max hitpoints",
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
    }

    private static string FormatHitpoints(AnimationProperty property)
    {
        string from = property.From.ToString(CultureInfo.InvariantCulture);
        string to = property.To.ToString(CultureInfo.InvariantCulture);

        return $"{FormatHitpointsTarget(property.Target)} {from} -> {to} over {FrameDuration(property.Duration)} frames {FormatEasing(property.Easing)}";
    }

    private static string FormatEasing(AnimationEasing easing)
    {
        return easing switch
        {
            AnimationEasing.LinearEasing => "Linear",
            AnimationEasing.SmoothEasing => "Smooth",
            AnimationEasing.DecelerateEasing => "Decelerate",
            _ => throw new ArgumentOutOfRangeException(nameof(easing), easing, null)
        };
    }

    private static string FormatClip(AnimationClip clip)
    {
        return $"clip {clip.Animation.CanonicalId} for {FrameDuration(clip.Duration)} frames at {FormatPlacement(clip.Placement)}";
    }

    private static string FormatEffect(BattleAnimationState.EffectEntry effect)
    {
        return $"clip {effect.Animation.CanonicalId} for {FrameDuration(effect.Duration)} frames at {FormatFootprint(effect.Footprint)}";
    }

    private static string FormatPlacement(AnimationPlacement placement)
    {
        foreach (BattleSide gridSide in Enum.GetValues<BattleSide>())
        {
            if (BattleRenderLayout.GetGridPlacement(gridSide) == placement)
            {
                return $"{gridSide.ToString().ToLowerInvariant()} grid";
            }
        }

        return placement.ToString();
    }

    private static string FormatFootprint(BattleGridFootprint footprint)
    {
        string line;
        if (footprint.Position.IsFrontLine)
        {
            line = "front";
        }
        else
        {
            line = "back";
        }

        string flank;
        switch (footprint.Position.Flank)
        {
            case 0:
                flank = "bottom";
                break;
            case 1:
                flank = "center";
                break;
            default:
                flank = "top";
                break;
        }

        return $"{footprint.Side.ToString().ToLowerInvariant()} {line}-{flank} {FormatSize(footprint.IsLarge)}";
    }

    private static IReadOnlyList<string> FormatAnimation(BattleAnimationState animation)
    {
        List<(TimeSpan At, int Order, string Text)> entries = [];

        foreach (BattleAnimationState.Unit unit in animation.Units)
        {
            foreach (BattleAnimationState.FormEntry form in unit.Form)
            {
                entries.Add((form.At, 0, FormatForm(unit, form)));
            }

            foreach (BattleAnimationState.BodyEntry body in unit.Body)
            {
                entries.Add((body.At, 1, FormatBody(unit, body)));

                if (body.Opacity is not null)
                {
                    entries.Add((body.Opacity.Start, 2, FormatOpacity(body.Opacity)));
                }
            }

            foreach (BattleAnimationState.FeedbackEntry feedback in unit.Feedback)
            {
                entries.Add((feedback.At, 3, FormatFeedback(unit, feedback)));
            }

            foreach (BattleAnimationState.StatusEntry status in unit.Status)
            {
                entries.Add((status.At, 3, FormatStatus(unit, status)));
            }
        }

        foreach (AnimationProperty property in animation.Sequence.Properties)
        {
            entries.Add((property.Start, 4, FormatHitpoints(property)));
        }

        foreach (BattleAnimationState.EffectEntry effect in animation.Effects)
        {
            entries.Add((effect.At, 5, FormatEffect(effect)));
        }

        foreach (AnimationClip clip in animation.Sequence.Clips)
        {
            entries.Add((clip.Start, 5, FormatClip(clip)));
        }

        foreach (AnimationSound sound in animation.Sequence.Sounds)
        {
            entries.Add((sound.Start, 6, $"sound {sound.Key.CanonicalId}"));
        }

        List<string> rows = [$"sequence {FrameDuration(animation.Sequence.Duration)} frames"];

        foreach ((TimeSpan at, _, string text) in entries.OrderBy(entry => entry.At).ThenBy(entry => entry.Order))
        {
            rows.Add($"@{FrameDuration(at)} {text}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatPauses(BattleAnimationState animation)
    {
        List<string> rows = [];

        foreach (AnimationPause pause in animation.Sequence.Pauses)
        {
            string milliseconds = pause.Duration.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture);

            rows.Add($"@{FrameDuration(pause.Start)} pause {milliseconds} ms");
        }

        return rows;
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<string> FormatImpact(BattleAnimationState animation)
    {
        List<(TimeSpan At, int Order, string Text)> entries = [];

        foreach (BattleAnimationState.Unit unit in animation.Units)
        {
            foreach (BattleAnimationState.MotionEntry motion in unit.Motion)
            {
                string direction = $"{FormatNumber(motion.Direction.X)} {FormatNumber(motion.Direction.Y)}";
                string distance = FormatNumber(motion.Distance);
                string duration = FrameDuration(motion.Duration);

                entries.Add((motion.At, 0, $"unit {unit.Id.Value} recoil {distance} px towards {direction} for {duration} frames"));
            }
        }

        foreach (BattleAnimationState.ShakeEntry shake in animation.Shakes)
        {
            entries.Add((shake.At, 1, $"shake {FormatNumber(shake.Amplitude)} px for {FrameDuration(shake.Duration)} frames"));
        }

        List<string> rows = [];

        foreach ((TimeSpan at, _, string text) in entries.OrderBy(entry => entry.At).ThenBy(entry => entry.Order))
        {
            rows.Add($"@{FrameDuration(at)} {text}");
        }

        return rows;
    }

    private static UnitAttackDefinition Attack(string id, UnitAttackType type, UnitAttackReach reach)
    {
        return new UnitAttackDefinition(
            Id: new UnitAttackDefinitionId(id),
            Type: type,
            Source: UnitAttackSource.Life,
            Initiative: 50,
            Accuracy: 100,
            Reach: reach,
            Damage: 0,
            Heal: 40,
            Level: 1,
            IsInfinite: false,
            SummonUnitIds: [],
            SmallTransformUnitId: null,
            LargeTransformUnitId: null,
            HasCriticalHit: false,
            AlternativeAttackId: null,
            WardModifierIds: []);
    }

    private static ItemDefinition Item(string id, ItemCategory category, UnitDefinitionId? animationUnitId)
    {
        return new ItemDefinition(
            Id: new ItemDefinitionId(id),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            Category: category,
            Value: WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: animationUnitId);
    }

    private static IReadOnlyList<string> CollectEffects(
        IAssetMetadataProvider assets,
        IGameDefinitionProvider definitions,
        BattleRenderState baseState,
        IReadOnlyList<BattleEvent> events)
    {
        SortedSet<string> result = [];
        RandomProvider random = new(0);

        for (int i = 0; i < 300; i++)
        {
            BattleAnimationStateBuilder builder = new(assets, definitions, random, BattleViewerSide.Defender, 1);
            BattleAnimationState animation = Assert.NotNull(builder.Build(baseState, events));

            JoinStringBuilder animations = new(" + ");

            foreach (BattleAnimationState.EffectEntry effect in animation.Effects)
            {
                animations.Add(effect.Animation.CanonicalId);
            }

            result.Add(animations.ToString());
        }

        return [.. result];
    }

    [Test]
    public void SoundFramesTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 2, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 6, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 21 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@2 unit 2 body Hit Alive for 9 frames",
            "@2 unit 2 feedback +15 Damage -10",
            "@2 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@2 sound battle-unit-touch/atk/4",
            "@6 sound battle-unit-attack/atk/4",
            "@11 unit 2 body Idle Alive",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void StrikeBeyondAnimationTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 20, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 6, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 34 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@6 sound battle-unit-attack/atk/4",
            "@15 unit 2 body Hit Alive for 9 frames",
            "@15 unit 2 feedback +15 Damage -10",
            "@15 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@15 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@24 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void SecondStrikeTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 25, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 3, SecondTriggerFrame: 18))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 0, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 28 frames",
            "@0 unit 1 body Attack Alive for 25 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@3 unit 2 body Hit Alive for 10 frames",
            "@3 unit 2 feedback +15 Damage -10",
            "@3 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@3 sound battle-unit-touch/atk/4",
            "@13 unit 2 body Idle Alive",
            "@18 unit 2 body Hit Alive for 10 frames",
            "@25 unit 1 body Idle Alive",
            "@28 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void OverlappingSecondStrikeTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 25, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 3, SecondTriggerFrame: 8))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 0, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 25 frames",
            "@0 unit 1 body Attack Alive for 25 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@3 unit 2 body Hit Alive for 5 frames",
            "@3 unit 2 feedback +15 Damage -10",
            "@3 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@3 sound battle-unit-touch/atk/4",
            "@8 unit 2 body Hit Alive for 10 frames",
            "@18 unit 2 body Idle Alive",
            "@25 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void RecoilSecondStrikeTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 25, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 3, SecondTriggerFrame: 13))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 0, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 25 frames",
            "@0 unit 1 body Attack Alive for 25 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@3 unit 2 body Hit Alive for 9 frames",
            "@3 unit 2 feedback +15 Damage -10",
            "@3 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@3 sound battle-unit-touch/atk/4",
            "@12 unit 2 body Hit Alive for 10 frames",
            "@22 unit 2 body Idle Alive",
            "@25 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void EarlyStrikeTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 3, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 0, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 29 frames",
            "@0 unit 1 body Attack Alive for 26 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@3 unit 2 body Hit Alive for 26 frames",
            "@3 unit 2 feedback +15 Damage -10",
            "@3 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@3 sound battle-unit-touch/atk/4",
            "@26 unit 1 body Idle Alive",
            "@29 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void MissingSoundFrameTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata())
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);

        Assert.That(
            () => builder.Build(
                baseState,
                [
                    new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                    new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10)
                ]),
            Throws.InvalidOperationException);
    }

    [Test]
    public void MissingTriggerFrameTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 0, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);

        Assert.That(
            () => builder.Build(
                baseState,
                [
                    new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                    new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10)
                ]),
            Throws.InvalidOperationException);
    }

    [Test]
    public void NonLethalAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -10",
            "@7 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void LethalAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 40),
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 28 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -40",
            "@7 unit 2 hitpoints 40 -> 0 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 clip battle-effect-death/human/small for 12 frames at defender front-center small",
            "@16 sound battle-effect-sound-death",
            "@22 unit 2 body Idle Dead for 6 frames",
            "@22 unit 2 opacity 0 -> 1 over 4 frames Smooth"
        ]));
    }

    [Test]
    public void LateCorpseRevealTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Dwarf,
            DeathAnimation: UnitDeathAnimation.Dwarf,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Dwarf, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 10, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ATK", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ATK"), 40),
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 28 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -40",
            "@7 unit 2 hitpoints 40 -> 0 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 clip battle-effect-death/dwarf/small for 12 frames at defender front-center small",
            "@16 sound battle-effect-sound-death",
            "@26 unit 2 body Idle Dead for 2 frames",
            "@26 unit 2 opacity 0 -> 1 over 2 frames Smooth"
        ]));
    }

    [Test]
    public void EffectOnlyAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PAR", UnitAttackType.Paralyze, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("PAR")]),
                new BattleUnitEffectAppliedEvent(new BattleUnitId(1), new BattleUnitId(2), new BattleParalyzeStatusEffect(RemainingTurns: 1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Effect Paralyzed",
            "@7 unit 2 frozen True",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void SummonAttackTest()
    {
        BattleRenderState.Unit summoner = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit summon = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("IMP"),
            Race: WorldRace.Heretic,
            DeathAnimation: UnitDeathAnimation.Heretic,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 25,
            MaxHitpoints: 25,
            Presence: BattleUnitState.Presence.Absent,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [summoner, summon],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Attack,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .Build();

        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("IMP", maxHitpoints: 25, initiative: 30, damage: 5);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("SUM", UnitAttackType.Summon, UnitAttackReach.Any))
            .AddUnit(summonDefinition)
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop),
                    [new UnitAttackDefinitionId("SUM")]),
                new BattleUnitSummonedEvent(
                    new BattleUnitId(2),
                    summonDefinition.Id,
                    BattleSide.Attacker,
                    GridPosition.FrontTop,
                    Hitpoints: 25,
                    Level: 1,
                    SummonerId: new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 17 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/touch/body/attack/0 for 10 frames at attacker front-top small",
            "@7 sound battle-unit-touch/atk/4",
            "@8 unit 2 body Idle Alive",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void SummonBeforeDamageOverTimeTest()
    {
        BattleRenderState.Unit summoner = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit enemy = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit summon = new(
            Id: new BattleUnitId(3),
            DefinitionId: new UnitDefinitionId("IMP"),
            Race: WorldRace.Heretic,
            DeathAnimation: UnitDeathAnimation.Heretic,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 25,
            MaxHitpoints: 25,
            Presence: BattleUnitState.Presence.Absent,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [summoner, enemy, summon],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Attack,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Blister, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 14, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("IMP", maxHitpoints: 25, initiative: 30, damage: 5);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("SUM", UnitAttackType.Summon, UnitAttackReach.Any))
            .AddUnit(summonDefinition)
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop),
                    [new UnitAttackDefinitionId("SUM")]),
                new BattleUnitSummonedEvent(
                    new BattleUnitId(3),
                    summonDefinition.Id,
                    BattleSide.Attacker,
                    GridPosition.FrontTop,
                    Hitpoints: 25,
                    Level: 1,
                    SummonerId: new BattleUnitId(1)),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal)),
                new BattleUnitDamagedByEffectEvent(
                    new BattleUnitId(2),
                    new BattleBlisterStatusEffect(RemainingTurns: 2, Damage: 6),
                    Damage: 6,
                    HitpointsAfter: 34)
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 36 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/touch/body/attack/0 for 10 frames at attacker front-top small",
            "@7 sound battle-unit-touch/atk/4",
            "@8 unit 3 body Idle Alive",
            "@16 unit 1 body Idle Alive",
            "@17 unit 2 body Hit Alive for 9 frames",
            "@17 unit 2 feedback +15 Damage -6",
            "@17 unit 2 hitpoints 40 -> 34 over 12 frames Decelerate",
            "@17 clip battle-effect-status/blister/small for 14 frames at defender front-center small",
            "@17 sound battle-effect-sound-status/blister",
            "@26 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void ReduceLevelAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit veteran = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("VET"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, veteran],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.ReduceLevel, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("RED", UnitAttackType.ReduceLevel, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("RED")]),
                new BattleUnitTransformAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    BattleTransformType.LevelDrain,
                    new UnitDefinitionId("REC"),
                    Level: 1,
                    HitpointsAfter: 24,
                    MaxHitpointsAfter: 24,
                    RemainingTurns: 3),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 30 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/touch/body/defend/0 for 12 frames at defender front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@8 unit 2 feedback +15 Effect Level drained",
            "@8 clip battle-effect-status/reduce-level/small for 20 frames at defender front-center small",
            "@8 sound battle-effect-sound-status/transform",
            "@16 unit 1 body Idle Alive",
            "@18 unit 2 form REC facing Defender",
            "@18 unit 2 hitpoints 60 -> 24 over 12 frames Decelerate",
            "@18 unit 2 max hitpoints 60 -> 24 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void TransformEnemyAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnitAura(new UnitDefinitionId("NEW"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 4, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Transform, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("TRA", UnitAttackType.TransformEnemy, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("TRA")]),
                new BattleUnitTransformAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    BattleTransformType.Transform,
                    new UnitDefinitionId("NEW"),
                    1,
                    12,
                    12,
                    3),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 30 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/touch/body/defend/0 for 12 frames at defender front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@8 unit 2 feedback +15 Effect Transformed",
            "@8 clip battle-effect-status/transform/small for 20 frames at defender front-center small",
            "@8 sound battle-effect-sound-status/transform",
            "@16 unit 1 body Idle Alive",
            "@18 unit 2 form NEW facing Defender auras [battle-unit-aura/new/idle/defender]",
            "@18 unit 2 hitpoints 60 -> 12 over 12 frames Decelerate",
            "@18 unit 2 max hitpoints 60 -> 12 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void DoppelgangerAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Doppelganger, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("DOP", UnitAttackType.Doppelganger, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("DOP")]),
                new BattleUnitTransformAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(1),
                    BattleTransformType.Doppelganger,
                    new UnitDefinitionId("DEF"),
                    1,
                    30,
                    60,
                    1),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 29 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 1 feedback +15 Effect Transformed",
            "@7 clip battle-effect-status/doppelganger/small for 20 frames at attacker front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@7 sound battle-effect-sound-status/transform",
            "@16 unit 1 body Idle Alive",
            "@17 unit 1 form DEF facing Attacker",
            "@17 unit 1 hitpoints 60 -> 30 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void TransformSelfAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Attack,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Transform, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("TRS", UnitAttackType.TransformSelf, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("TRS")]),
                new BattleUnitTransformAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(1),
                    BattleTransformType.Transform,
                    new UnitDefinitionId("NEW"),
                    1,
                    60,
                    80,
                    null),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 30 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/touch/body/attack/0 for 12 frames at attacker front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@8 unit 1 feedback +15 Effect Transformed",
            "@8 clip battle-effect-status/transform/small for 20 frames at attacker front-center small",
            "@8 sound battle-effect-sound-status/transform",
            "@16 unit 1 body Idle Alive",
            "@18 unit 1 form NEW facing Attacker",
            "@18 unit 1 hitpoints 60 -> 60 over 12 frames Decelerate",
            "@18 unit 1 max hitpoints 60 -> 80 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void TransformRestoredTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit transformed = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("TRA"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, transformed],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnitAura(new UnitDefinitionId("ORIG"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 4, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Untransform, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, TestGameDefinitionProvider.Empty, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal)),
                new BattleUnitTransformRestoredEvent(new BattleUnitId(2), BattleTransformType.Transform, new UnitDefinitionId("ORIG"), 90, 90),
                new BattleUnitEffectDurationDecreasedEvent(new BattleUnitId(2))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 22 frames",
            "@0 unit 2 feedback +15 Effect Restored",
            "@0 clip battle-effect-status/untransform/small for 20 frames at defender front-center small",
            "@0 sound battle-effect-sound-status/transform",
            "@10 unit 2 form ORIG facing Defender auras [battle-unit-aura/orig/idle/defender]",
            "@10 unit 2 hitpoints 60 -> 90 over 12 frames Decelerate",
            "@10 unit 2 max hitpoints 60 -> 90 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void RestoredByCureTest()
    {
        BattleRenderState.Unit healer = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit transformed = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("TRA"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [healer, transformed],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Attack,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Untransform, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("CURE", UnitAttackType.Cure, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("CURE")]),
                new BattleUnitTransformRestoredEvent(new BattleUnitId(2), BattleTransformType.Transform, new UnitDefinitionId("ORIG"), 90, 90),
                new BattleUnitCuredEvent(new BattleUnitId(2)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 30 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/touch/body/attack/0 for 10 frames at attacker front-top small",
            "@7 sound battle-unit-touch/atk/4",
            "@8 unit 2 feedback +15 Effect Restored",
            "@8 unit 2 feedback +15 Heal Cured",
            "@8 clip battle-effect-status/untransform/small for 20 frames at attacker front-top small",
            "@8 sound battle-effect-sound-status/transform",
            "@16 unit 1 body Idle Alive",
            "@18 unit 2 form ORIG facing Attacker",
            "@18 unit 2 hitpoints 60 -> 90 over 12 frames Decelerate",
            "@18 unit 2 max hitpoints 60 -> 90 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void LevelRestoredTest()
    {
        BattleRenderState.Unit reduced = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("WEAK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [reduced],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.RestoreLevel, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 24, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, TestGameDefinitionProvider.Empty, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)),
                new BattleUnitTransformRestoredEvent(new BattleUnitId(1), BattleTransformType.LevelDrain, new UnitDefinitionId("VETERAN"), 90, 90),
                new BattleUnitEffectDurationDecreasedEvent(new BattleUnitId(1))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 24 frames",
            "@0 unit 1 feedback +15 Effect Level restored",
            "@0 clip battle-effect-status/restore-level/small for 24 frames at defender front-center small",
            "@0 sound battle-effect-sound-status/transform",
            "@12 unit 1 form VETERAN facing Defender",
            "@12 unit 1 hitpoints 60 -> 90 over 12 frames Decelerate",
            "@12 unit 1 max hitpoints 60 -> 90 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void DoppelgangerRestoredTest()
    {
        BattleRenderState.Unit doppelganger = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("COPY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [doppelganger],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Undoppelganger, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, TestGameDefinitionProvider.Empty, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)),
                new BattleUnitTransformRestoredEvent(new BattleUnitId(1), BattleTransformType.Doppelganger, new UnitDefinitionId("ORIG"), 90, 90),
                new BattleUnitEffectDurationDecreasedEvent(new BattleUnitId(1))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 22 frames",
            "@0 unit 1 feedback +15 Effect Restored",
            "@0 clip battle-effect-status/undoppelganger/small for 20 frames at attacker front-center small",
            "@0 sound battle-effect-sound-status/transform",
            "@10 unit 1 form ORIG facing Attacker",
            "@10 unit 1 hitpoints 60 -> 90 over 12 frames Decelerate",
            "@10 unit 1 max hitpoints 60 -> 90 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void ShatterArmorAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("SHATTER", UnitAttackType.ReduceArmor, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("SHATTER")]),
                new BattleUnitEffectAppliedEvent(new BattleUnitId(1), new BattleUnitId(2), new BattleShatterArmorStatusEffect(Armor: 10)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Effect Armor shattered",
            "@7 sound battle-unit-touch/atk/4",
            "@7 sound battle-effect-sound-status/shatter-armor",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void BoostAttackTest()
    {
        BattleRenderState.Unit booster = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [booster, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("BOOST", UnitAttackType.IncreaseDamage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("BOOST")]),
                new BattleUnitEffectAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    new BattleIncreaseDamageStatusEffect(Percent: 50, IsInfinite: false)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Effect Damage boosted",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void GiveProtectionAttackTest()
    {
        BattleRenderState.Unit protector = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [protector, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PROT", UnitAttackType.GiveProtection, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("PROT")]),
                new BattleUnitWardGrantedEvent(
                    new BattleUnitId(2),
                    new BattleUnitWardModifier(
                        new BattleUnitId(1),
                        new UnitModifierDefinitionId("fireward"),
                        new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once))),
                new BattleUnitWardGrantedEvent(
                    new BattleUnitId(2),
                    new BattleUnitWardModifier(
                        new BattleUnitId(1),
                        new UnitModifierDefinitionId("waterward"),
                        new UnitAttackSourceWard(UnitAttackSource.Water, UnitWardCategory.Once))),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Ward Protected",
            "@7 unit 2 feedback +15 Ward Protected",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void GiveAdditionalAttackTest()
    {
        BattleRenderState.Unit alchemist = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [alchemist, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("GIVE", UnitAttackType.GiveAdditionalAttack, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("GIVE")]),
                new BattleUnitAttackGrantedEvent(new BattleUnitId(2)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Heal Extra attack",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void PoisonSecondaryAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("DMG", UnitAttackType.Damage, UnitAttackReach.Any))
            .AddAttack(Attack("PSN", UnitAttackType.Poison, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("DMG"), new UnitAttackDefinitionId("PSN")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG"), 10),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PSN")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PSN"), 0),
                new BattleUnitEffectAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 45 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -10",
            "@7 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@26 unit 2 feedback +15 Effect Poisoned",
            "@26 sound battle-effect-sound-status/poison"
        ]));
    }

    [Test]
    public void DamageSecondaryAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("DMG", UnitAttackType.Damage, UnitAttackReach.Any))
            .AddAttack(Attack("EQP", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("DMG"), new UnitAttackDefinitionId("EQP")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG"), 10),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("EQP")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("EQP"), 4),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 45 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -10",
            "@7 unit 2 hitpoints 60 -> 50 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@26 unit 2 body Hit Alive for 9 frames",
            "@26 unit 2 feedback +15 Damage -4",
            "@26 unit 2 hitpoints 50 -> 46 over 12 frames Decelerate",
            "@35 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void MissedSecondaryAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("DMG", UnitAttackType.Damage, UnitAttackReach.Any))
            .AddAttack(Attack("PSN", UnitAttackType.Poison, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("DMG"), new UnitAttackDefinitionId("PSN")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG"), 10),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PSN")),
                new BattleUnitAttackMissedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PSN")),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -10",
            "@7 unit 2 hitpoints 60 -> 50 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void HealTest()
    {
        BattleRenderState.Unit healer = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 20,
            MaxHitpoints: 20,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [healer, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("HEAL", UnitAttackType.Heal, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("HEAL")]),
                new BattleUnitHealedEvent(new BattleUnitId(2), HitpointsAfter: 40),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Heal Healed",
            "@7 unit 2 hitpoints 20 -> 40 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@7 sound battle-effect-sound-heal",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void DrainHealTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Heal(),
                new AssetAnimationMetadata(FrameCount: 14, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("DRAIN", UnitAttackType.DrainLife, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("DRAIN")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DRAIN")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DRAIN"), 20),
                new BattleUnitHealedEvent(new BattleUnitId(1), HitpointsAfter: 60),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 1 feedback +15 Heal Healed",
            "@7 unit 2 feedback +15 Damage -20",
            "@7 unit 1 hitpoints 40 -> 60 over 12 frames Decelerate",
            "@7 unit 2 hitpoints 40 -> 20 over 12 frames Decelerate",
            "@7 clip battle-effect-heal for 14 frames at attacker front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void CureTest()
    {
        BattleRenderState.Unit healer = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [healer, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("CURE", UnitAttackType.Cure, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("CURE")]),
                new BattleUnitCuredEvent(new BattleUnitId(2)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Heal Cured",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void ReviveTest()
    {
        BattleRenderState.Unit healer = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 0,
            MaxHitpoints: 0,
            Presence: BattleUnitState.Presence.Dead,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [healer, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Revive, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 18, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("REVIVE", UnitAttackType.Revive, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("REVIVE")]),
                new BattleUnitRevivedEvent(new BattleUnitId(2), HitpointsAfter: 40),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Heal Revived",
            "@7 unit 2 hitpoints 0 -> 40 over 12 frames Decelerate",
            "@7 clip battle-effect-status/revive/small for 18 frames at attacker front-top small",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void ItemHealTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Potion),
                new AssetAnimationMetadata(FrameCount: 31, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddItem(Item("POTION", ItemCategory.PotionHeal, animationUnitId: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("POTION"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(1))),
                new BattleUnitHealedEvent(new BattleUnitId(1), HitpointsAfter: 60),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 31 frames",
            "@0 unit 1 feedback +15 Action Item",
            "@0 unit 1 feedback +15 Heal Healed",
            "@0 unit 1 hitpoints 40 -> 60 over 12 frames Decelerate",
            "@0 clip battle-effect-item/potion for 31 frames at attacker front-center small",
            "@0 sound battle-effect-sound-potion/heal",
            "@0 sound battle-effect-sound-heal"
        ]));
    }

    [Test]
    public void ItemReviveTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 0,
            MaxHitpoints: 0,
            Presence: BattleUnitState.Presence.Dead,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Revive, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 18, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Potion),
                new AssetAnimationMetadata(FrameCount: 31, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddItem(Item("POTION", ItemCategory.PotionRevive, animationUnitId: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("POTION"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2))),
                new BattleUnitRevivedEvent(new BattleUnitId(2), HitpointsAfter: 30),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 31 frames",
            "@0 unit 1 feedback +15 Action Item",
            "@0 unit 2 feedback +15 Heal Revived",
            "@0 unit 2 hitpoints 0 -> 30 over 12 frames Decelerate",
            "@0 clip battle-effect-status/revive/small for 18 frames at attacker front-top small",
            "@0 clip battle-effect-item/potion for 31 frames at attacker front-center small",
            "@0 sound battle-effect-sound-potion/revive",
            "@9 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void ItemPermanentBoostTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Potion),
                new AssetAnimationMetadata(FrameCount: 31, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddItem(Item("TEMPORARY", ItemCategory.PotionTemporaryBoost, animationUnitId: null))
            .AddItem(Item("PERMANENT", ItemCategory.PotionPermanentBoost, animationUnitId: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("TEMPORARY"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(1))),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle2,
                    new ItemDefinitionId("PERMANENT"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(1))),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 62 frames",
            "@0 unit 1 feedback +15 Action Item",
            "@0 clip battle-effect-item/potion for 31 frames at attacker front-center small",
            "@0 sound battle-effect-sound-potion/boost",
            "@31 unit 1 feedback +15 Action Item",
            "@31 clip battle-effect-item/potion for 31 frames at attacker front-center small",
            "@31 sound battle-effect-sound-potion/boost"
        ]));
    }

    [Test]
    public void ItemLargeTargetTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: true),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 20,
            MaxHitpoints: 20,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Potion),
                new AssetAnimationMetadata(FrameCount: 31, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("PRIEST"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Attack,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddItem(Item("POTION", ItemCategory.PotionHeal, new UnitDefinitionId("PRIEST")))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("POTION"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2))),
                new BattleUnitHealedEvent(new BattleUnitId(2), HitpointsAfter: 70),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 31 frames",
            "@0 clip battle-effect-attack/priest/touch/body/attack/0 for 10 frames at attacker front-top large",
            "@0 clip battle-effect-item/potion for 31 frames at attacker front-center small",
            "@0 sound battle-effect-sound-potion/heal",
            "@0 sound battle-unit-touch/priest/3",
            "@1 unit 1 feedback +15 Action Item",
            "@1 unit 2 feedback +15 Heal Healed",
            "@1 unit 2 hitpoints 20 -> 70 over 12 frames Decelerate",
            "@1 sound battle-effect-sound-heal"
        ]));
    }

    [Test]
    public void ItemTalismanTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontBottom, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 50,
            MaxHitpoints: 50,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Talisman),
                new AssetAnimationMetadata(FrameCount: 31, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddItem(Item("TALISMAN", ItemCategory.Talisman, animationUnitId: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("TALISMAN"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2))),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 31 frames",
            "@0 unit 1 feedback +15 Action Item",
            "@0 clip battle-effect-item/talisman for 31 frames at attacker front-center small"
        ]));
    }

    [Test]
    public void ItemAttackTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontBottom, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 50,
            MaxHitpoints: 50,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Orb),
                new AssetAnimationMetadata(FrameCount: 31, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("MAGE"),
                    AssetAnimationKey.BattleEffect.AttackType.Hit,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Both,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("MAGE"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("ORB", UnitAttackType.Damage, UnitAttackReach.Any))
            .AddItem(Item("SPHERE", ItemCategory.Orb, new UnitDefinitionId("MAGE")))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("SPHERE"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2))),
                new BattleUnitAttackStepEvent(new BattleUnitId(1), new BattleUnitId(2), [new UnitAttackDefinitionId("ORB")], AccuracyReduction: 0),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("ORB"), 20),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 31 frames",
            "@0 clip battle-effect-item/orb for 31 frames at attacker front-center small",
            "@0 clip battle-effect-attack/mage/hit/body/both/0 for 20 frames at defender grid",
            "@0 sound battle-unit-hit/mage/4",
            "@6 clip battle-effect-attack/mage/touch/body/defend/0 for 10 frames at defender front-bottom small",
            "@6 sound battle-unit-touch/mage/3",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 1 feedback +15 Action Item",
            "@7 unit 2 feedback +15 Damage -20",
            "@7 unit 2 hitpoints 50 -> 30 over 12 frames Decelerate",
            "@16 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void ItemEffectTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontBottom, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 50,
            MaxHitpoints: 50,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Orb),
                new AssetAnimationMetadata(FrameCount: 31, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("GHOST"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Both,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 4, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("FEAR", UnitAttackType.Paralyze, UnitAttackReach.Any))
            .AddItem(Item("SPHERE", ItemCategory.Orb, new UnitDefinitionId("GHOST")))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("SPHERE"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2))),
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("FEAR")]),
                new BattleUnitEffectAppliedEvent(new BattleUnitId(1), new BattleUnitId(2), new BattleParalyzeStatusEffect(RemainingTurns: 1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 31 frames",
            "@0 clip battle-effect-item/orb for 31 frames at attacker front-center small",
            "@0 clip battle-effect-attack/ghost/touch/body/both/0 for 12 frames at defender front-bottom small",
            "@0 sound battle-unit-touch/ghost/3",
            "@4 unit 1 feedback +15 Action Item",
            "@4 unit 2 feedback +15 Effect Paralyzed",
            "@4 unit 2 frozen True"
        ]));
    }

    [Test]
    public void RetreatingTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleAnimationStateBuilder builder = new(
            NullAssetProvider.Instance,
            TestGameDefinitionProvider.Empty,
            new RandomProvider(0),
            BattleViewerSide.Defender,
            1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitRetreatingEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 19 frames",
            "@0 unit 1 form ATK facing Defender",
            "@0 unit 1 feedback +15 Action Retreat"
        ]));
    }

    [Test]
    public void RetreatedTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleAnimationStateBuilder builder = new(
            NullAssetProvider.Instance,
            TestGameDefinitionProvider.Empty,
            new RandomProvider(0),
            BattleViewerSide.Defender,
            1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitRetreatedEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 12 frames",
            "@0 unit 1 body Idle Absent"
        ]));
    }

    [Test]
    public void FearAttackTest()
    {
        BattleRenderState.Unit target = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("WARRIOR"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit caster = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [target, caster],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("FEAR", UnitAttackType.Fear, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(2), new BattleAttackUnitTarget(new BattleUnitId(1)), [new UnitAttackDefinitionId("FEAR")]),
                new BattleUnitRetreatingEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 2 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/def/4",
            "@7 unit 1 form WARRIOR facing Defender",
            "@7 unit 1 feedback +15 Action Retreat",
            "@7 sound battle-unit-touch/def/4",
            "@16 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void EffectTickTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Poison, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 15, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, TestGameDefinitionProvider.Empty, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitDamagedByEffectEvent(
                    new BattleUnitId(1),
                    new BattlePoisonStatusEffect(RemainingTurns: 1, Damage: 5),
                    Damage: 5,
                    HitpointsAfter: 55),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 19 frames",
            "@0 unit 1 body Hit Alive for 9 frames",
            "@0 unit 1 feedback +15 Damage -5",
            "@0 unit 1 hitpoints 60 -> 55 over 12 frames Decelerate",
            "@0 clip battle-effect-status/poison/small for 15 frames at defender front-center small",
            "@0 sound battle-effect-sound-status/poison",
            "@9 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void NoAnimatableEventsTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleAnimationStateBuilder builder = new(
            NullAssetProvider.Instance,
            TestGameDefinitionProvider.Empty,
            new RandomProvider(0),
            BattleViewerSide.Defender,
            1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);

        Assert.That(animation, Is.Null);
    }

    [Test]
    public void DefendTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleAnimationStateBuilder builder = new(
            NullAssetProvider.Instance,
            TestGameDefinitionProvider.Empty,
            new RandomProvider(0),
            BattleViewerSide.Defender,
            1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitDefendedEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 19 frames",
            "@0 unit 1 feedback +15 Action Defend"
        ]));
    }

    [Test]
    public void WaitTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleAnimationStateBuilder builder = new(
            NullAssetProvider.Instance,
            TestGameDefinitionProvider.Empty,
            new RandomProvider(0),
            BattleViewerSide.Defender,
            1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitWaitedEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 19 frames",
            "@0 unit 1 feedback +15 Action Wait"
        ]));
    }

    [Test]
    public void SkipTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleAnimationStateBuilder builder = new(
            NullAssetProvider.Instance,
            TestGameDefinitionProvider.Empty,
            new RandomProvider(0),
            BattleViewerSide.Defender,
            1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitTurnSkippedEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 19 frames",
            "@0 unit 1 feedback +15 Action Skip"
        ]));
    }

    [Test]
    public void EndedTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleAnimationStateBuilder builder = new(
            NullAssetProvider.Instance,
            TestGameDefinitionProvider.Empty,
            new RandomProvider(0),
            BattleViewerSide.Defender,
            1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleRoundEndedEvent(),
                new BattleEndedEvent(BattleSide.Attacker)
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 15 frames"
        ]));
    }

    [Test]
    public void EndedAfterLethalAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("REVIVE", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE"), 40),
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                new BattleRoundEndedEvent(),
                new BattleEndedEvent(BattleSide.Attacker)
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 43 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -40",
            "@7 unit 2 hitpoints 40 -> 0 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 clip battle-effect-death/human/small for 12 frames at defender front-center small",
            "@16 sound battle-effect-sound-death",
            "@22 unit 2 body Idle Dead for 6 frames",
            "@22 unit 2 opacity 0 -> 1 over 4 frames Smooth"
        ]));
    }

    [Test]
    public void ExperienceGainedTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("REVIVE", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE"), 40),
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                new BattleUnitExperienceGainedEvent(new BattleUnitId(1), ExperienceGained: 25),
                new BattleRoundEndedEvent(),
                new BattleEndedEvent(BattleSide.Attacker),
                new BattleUnitExperienceGainedEvent(new BattleUnitId(1), ExperienceGained: 30)
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 47 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -40",
            "@7 unit 2 hitpoints 40 -> 0 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 clip battle-effect-death/human/small for 12 frames at defender front-center small",
            "@16 sound battle-effect-sound-death",
            "@22 unit 2 body Idle Dead for 6 frames",
            "@22 unit 2 opacity 0 -> 1 over 4 frames Smooth",
            "@28 unit 1 feedback +15 Heal +30 XP"
        ]));
    }

    [Test]
    public void EndedRestoresTransformedTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Untransform, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("REVIVE", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE"), 40),
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                new BattleRoundEndedEvent(),
                new BattleUnitTransformRestoredEvent(new BattleUnitId(1), BattleTransformType.Transform, new UnitDefinitionId("HERO"), 120, 120),
                new BattleUnitTransformRestoredEvent(new BattleUnitId(2), BattleTransformType.Transform, new UnitDefinitionId("ORIG"), 0, 50),
                new BattleEndedEvent(BattleSide.Attacker)
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 65 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -40",
            "@7 unit 2 hitpoints 40 -> 0 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 clip battle-effect-death/human/small for 12 frames at defender front-center small",
            "@16 sound battle-effect-sound-death",
            "@22 unit 2 body Idle Dead for 6 frames",
            "@22 unit 2 opacity 0 -> 1 over 4 frames Smooth",
            "@28 unit 1 feedback +15 Effect Restored",
            "@28 clip battle-effect-status/untransform/small for 20 frames at attacker front-center small",
            "@28 sound battle-effect-sound-status/transform",
            "@38 unit 1 form HERO facing Attacker",
            "@38 unit 1 hitpoints 60 -> 120 over 12 frames Decelerate",
            "@38 unit 1 max hitpoints 60 -> 120 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void TwoEffectTicksTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Poison, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 15, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Frostbite, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, TestGameDefinitionProvider.Empty, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)),
                new BattleUnitDamagedByEffectEvent(
                    new BattleUnitId(1),
                    new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5),
                    Damage: 5,
                    HitpointsAfter: 55),
                new BattleUnitDamagedByEffectEvent(
                    new BattleUnitId(1),
                    new BattleFrostbiteStatusEffect(RemainingTurns: 2, Damage: 4),
                    Damage: 4,
                    HitpointsAfter: 51)
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 38 frames",
            "@0 unit 1 body Hit Alive for 9 frames",
            "@0 unit 1 feedback +15 Damage -5",
            "@0 unit 1 hitpoints 60 -> 55 over 12 frames Decelerate",
            "@0 clip battle-effect-status/poison/small for 15 frames at defender front-center small",
            "@0 sound battle-effect-sound-status/poison",
            "@9 unit 1 body Idle Alive",
            "@19 unit 1 body Hit Alive for 9 frames",
            "@19 unit 1 feedback +15 Damage -4",
            "@19 unit 1 hitpoints 55 -> 51 over 12 frames Decelerate",
            "@19 clip battle-effect-status/frostbite/small for 12 frames at defender front-center small",
            "@19 sound battle-effect-sound-status/frostbite",
            "@28 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void AttackThenEffectTickTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Poison, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 15, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("REVIVE", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal)),
                new BattleUnitDamagedByEffectEvent(
                    new BattleUnitId(2),
                    new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5),
                    Damage: 5,
                    HitpointsAfter: 25)
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 45 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -10",
            "@7 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@26 unit 2 body Hit Alive for 9 frames",
            "@26 unit 2 feedback +15 Damage -5",
            "@26 unit 2 hitpoints 30 -> 25 over 12 frames Decelerate",
            "@26 clip battle-effect-status/poison/small for 15 frames at defender front-center small",
            "@26 sound battle-effect-sound-status/poison",
            "@35 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void LethalAttackClipOrderTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 6, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("REVIVE", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("REVIVE"), 40),
                new BattleUnitDiedEvent(new BattleUnitId(2))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 29 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/touch/body/defend/0 for 6 frames at defender front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@8 unit 2 body Hit Alive for 9 frames",
            "@8 unit 2 feedback +15 Damage -40",
            "@8 unit 2 hitpoints 40 -> 0 over 12 frames Decelerate",
            "@8 sound battle-unit-damaged/def/2",
            "@16 unit 1 body Idle Alive",
            "@17 unit 2 body Idle Alive",
            "@17 clip battle-effect-death/human/small for 12 frames at defender front-center small",
            "@17 sound battle-effect-sound-death",
            "@23 unit 2 body Idle Dead for 6 frames",
            "@23 unit 2 opacity 0 -> 1 over 4 frames Smooth"
        ]));
    }

    [Test]
    public void ItemSummonTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit summon = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("IMP"),
            Race: WorldRace.Heretic,
            DeathAnimation: UnitDeathAnimation.Heretic,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 25,
            MaxHitpoints: 25,
            Presence: BattleUnitState.Presence.Absent,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader, summon],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddItem(Item("SCROLL", ItemCategory.Scroll, animationUnitId: null))
            .Build();

        BattleAnimationStateBuilder builder = new(NullAssetProvider.Instance, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("SCROLL"),
                    new BattleUnitId(1),
                    new BattleAttackTileTarget(BattleSide.Attacker, GridPosition.FrontTop)),
                new BattleUnitSummonedEvent(
                    new BattleUnitId(2),
                    new UnitDefinitionId("IMP"),
                    BattleSide.Attacker,
                    GridPosition.FrontTop,
                    Hitpoints: 25,
                    Level: 1,
                    SummonerId: new BattleUnitId(1))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 19 frames",
            "@0 unit 2 body Idle Alive",
            "@0 unit 1 feedback +15 Action Item"
        ]));
    }

    [Test]
    public void DeathWithoutRecoilTest()
    {
        BattleRenderState.Unit unit = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [unit, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, TestGameDefinitionProvider.Empty, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitDefendedEvent(new BattleUnitId(2)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)),
                new BattleUnitDiedEvent(new BattleUnitId(1))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 40 frames",
            "@0 unit 2 feedback +15 Action Defend",
            "@19 unit 1 body Hit Alive for 9 frames",
            "@28 unit 1 body Idle Alive",
            "@28 clip battle-effect-death/human/small for 12 frames at defender front-center small",
            "@28 sound battle-effect-sound-death",
            "@34 unit 1 body Idle Dead for 6 frames",
            "@34 unit 1 opacity 0 -> 1 over 4 frames Smooth"
        ]));
    }

    [Test]
    public void ParalyzeThenSkipTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PAR", UnitAttackType.Paralyze, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("PAR")]),
                new BattleUnitEffectAppliedEvent(new BattleUnitId(1), new BattleUnitId(2), new BattleParalyzeStatusEffect(RemainingTurns: 1)),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal)),
                new BattleUnitEffectDurationDecreasedEvent(new BattleUnitId(2)),
                new BattleUnitEffectExpiredEvent(new BattleUnitId(2), new BattleParalyzeStatusEffect(RemainingTurns: 0)),
                new BattleUnitTurnSkippedEvent(new BattleUnitId(2)),
                new BattleUnitTurnEndingEvent(new BattleUnitId(2)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(2)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 45 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Effect Paralyzed",
            "@7 unit 2 frozen True",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@26 unit 2 feedback +15 Action Skip",
            "@41 unit 2 frozen False"
        ]));
    }

    [Test]
    public void LayeredAttackEffectTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Shadow,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 8, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Aura,
                    AssetAnimationKey.BattleEffect.AttackSide.Both,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PAR", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR"), 10),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 27 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/touch/shadow/defend/0 for 8 frames at defender front-center small",
            "@7 clip battle-effect-attack/atk/touch/body/defend/0 for 10 frames at defender front-center small",
            "@7 clip battle-effect-attack/atk/touch/aura/both/0 for 12 frames at defender front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@8 unit 2 body Hit Alive for 9 frames",
            "@8 unit 2 feedback +15 Damage -10",
            "@8 unit 2 hitpoints 60 -> 50 over 12 frames Decelerate",
            "@8 sound battle-unit-damaged/def/2",
            "@16 unit 1 body Idle Alive",
            "@17 unit 2 body Idle Alive"
        ]));
    }

    [Test]
    public void AttackEffectVariantTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Shadow,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 8, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Shadow,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 1),
                new AssetAnimationMetadata(FrameCount: 8, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Shadow,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 2),
                new AssetAnimationMetadata(FrameCount: 8, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 1),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 2),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PAR", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        IReadOnlyList<string> effects = CollectEffects(
            assets,
            definitions,
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR"), 10),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1))
            ]);
        Assert.That(effects, Is.EqualTo([
            "battle-effect-attack/atk/touch/shadow/defend/0 + battle-effect-attack/atk/touch/body/defend/0",
            "battle-effect-attack/atk/touch/shadow/defend/1 + battle-effect-attack/atk/touch/body/defend/1",
            "battle-effect-attack/atk/touch/shadow/defend/2 + battle-effect-attack/atk/touch/body/defend/2"
        ]));
    }

    [Test]
    public void AttackEffectPartialVariantTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 1),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PAR", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        IReadOnlyList<string> effects = CollectEffects(
            assets,
            definitions,
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR"), 10),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1))
            ]);
        Assert.That(effects, Is.EqualTo([
            "battle-effect-attack/atk/touch/body/defend/0",
            "battle-effect-attack/atk/touch/body/defend/1"
        ]));
    }

    [Test]
    public void AttackEffectIncompleteTakeTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Shadow,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 8, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 1),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 2),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PAR", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        IReadOnlyList<string> effects = CollectEffects(
            assets,
            definitions,
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR"), 10),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1))
            ]);
        Assert.That(effects, Is.EqualTo([
            "battle-effect-attack/atk/touch/shadow/defend/0 + battle-effect-attack/atk/touch/body/defend/0"
        ]));
    }

    [Test]
    public void BlisterAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Blister(),
                new AssetAnimationMetadata(FrameCount: 14, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("BLI", UnitAttackType.Blister, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("BLI")]),
                new BattleUnitEffectAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    new BattleBlisterStatusEffect(RemainingTurns: 3, Damage: 6)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Effect Blistered",
            "@7 clip battle-effect-blister for 14 frames at defender front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@7 sound battle-effect-sound-status/blister",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void LargeUnitTransformTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: true),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Transform, IsLarge: true),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("TRA", UnitAttackType.TransformEnemy, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("TRA")]),
                new BattleUnitTransformAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    BattleTransformType.Transform,
                    new UnitDefinitionId("NEW"),
                    1,
                    12,
                    12,
                    3),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 29 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Effect Transformed",
            "@7 clip battle-effect-status/transform/large for 20 frames at defender front-center large",
            "@7 sound battle-unit-touch/atk/4",
            "@7 sound battle-effect-sound-status/transform",
            "@16 unit 1 body Idle Alive",
            "@17 unit 2 form NEW facing Defender",
            "@17 unit 2 hitpoints 60 -> 12 over 12 frames Decelerate",
            "@17 unit 2 max hitpoints 60 -> 12 over 12 frames Decelerate"
        ]));
    }

    [Test]
    public void KilledSummonTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit summon = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("IMP"),
            Race: WorldRace.Heretic,
            DeathAnimation: UnitDeathAnimation.Heretic,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, summon],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("IMP"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Heretic, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Unsummon, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("TRA", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("TRA")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("TRA"), 60),
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                new BattleUnitUnsummonedEvent(new BattleUnitId(2))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 28 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -60",
            "@7 unit 2 hitpoints 60 -> 0 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 clip battle-effect-death/heretic/small for 12 frames at defender front-center small",
            "@16 sound battle-effect-sound-death",
            "@22 unit 2 body Idle Absent for 6 frames"
        ]));
    }

    [Test]
    public void UnsummonedTest()
    {
        BattleRenderState.Unit summon = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("IMP"),
            Race: WorldRace.Heretic,
            DeathAnimation: UnitDeathAnimation.Heretic,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [summon],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Unsummon, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, TestGameDefinitionProvider.Empty, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitUnsummonedEvent(new BattleUnitId(1))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 16 frames",
            "@0 clip battle-effect-status/unsummon/small for 16 frames at defender front-center small",
            "@0 sound battle-effect-sound-status/unsummon",
            "@8 unit 1 body Idle Absent"
        ]));
    }

    [Test]
    public void GridAttackEffectTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit firstDefender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontBottom, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit secondDefender = new(
            Id: new BattleUnitId(3),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.BackTop, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, firstDefender, secondDefender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Hit,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Both,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 5, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("TRA", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("TRA")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("TRA"), 10),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("TRA")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("TRA"), 10),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 31 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/hit/body/both/0 for 20 frames at defender grid",
            "@7 sound battle-unit-hit/atk/4",
            "@12 unit 2 body Hit Alive for 9 frames",
            "@12 unit 3 body Hit Alive for 9 frames",
            "@12 unit 2 feedback +15 Damage -10",
            "@12 unit 3 feedback +15 Damage -10",
            "@12 unit 2 hitpoints 60 -> 50 over 12 frames Decelerate",
            "@12 unit 3 hitpoints 60 -> 50 over 12 frames Decelerate",
            "@12 sound battle-unit-touch/atk/2",
            "@16 unit 1 body Idle Alive",
            "@21 unit 2 body Idle Alive",
            "@21 unit 3 body Idle Alive"
        ]));
    }

    [Test]
    public void GridAndTouchAttackEffectTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit firstDefender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontBottom, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit secondDefender = new(
            Id: new BattleUnitId(3),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.BackTop, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, firstDefender, secondDefender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Hit,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 5, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ATK"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 1, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("TRA", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("TRA")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("TRA"), 10),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("TRA")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("TRA"), 10),
                new BattleUnitTurnEndingEvent(new BattleUnitId(1)),
                new BattleUnitTurnEndedEvent(new BattleUnitId(1))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 32 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 clip battle-effect-attack/atk/hit/body/defend/0 for 20 frames at defender grid",
            "@7 sound battle-unit-hit/atk/4",
            "@12 clip battle-effect-attack/atk/touch/body/defend/0 for 10 frames at defender back-top small",
            "@12 clip battle-effect-attack/atk/touch/body/defend/0 for 10 frames at defender front-bottom small",
            "@12 sound battle-unit-touch/atk/2",
            "@13 unit 2 body Hit Alive for 9 frames",
            "@13 unit 3 body Hit Alive for 9 frames",
            "@13 unit 2 feedback +15 Damage -10",
            "@13 unit 3 feedback +15 Damage -10",
            "@13 unit 2 hitpoints 60 -> 50 over 12 frames Decelerate",
            "@13 unit 3 hitpoints 60 -> 50 over 12 frames Decelerate",
            "@13 sound battle-unit-damaged/def/3",
            "@16 unit 1 body Idle Alive",
            "@22 unit 2 body Idle Alive",
            "@22 unit 3 body Idle Alive"
        ]));
    }

    [Test]
    public void ImmuneAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("HIT", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("HIT")]),
                new BattleUnitAttackBlockedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    new UnitAttackDefinitionId("HIT"),
                    new UnitAttackSourceWard(UnitAttackSource.Weapon, UnitWardCategory.Always)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Ward Immune",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void WardedAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("HIT", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        UnitWard ward = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once);

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("HIT")]),
                new BattleUnitAttackBlockedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("HIT"), ward),
                new BattleUnitWardConsumedEvent(new BattleUnitId(2), ward),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 feedback +15 Ward Ward",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void PauseTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 25, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 3, SecondTriggerFrame: 18))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 0, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        BattleAnimationState resolved = Assert.NotNull(animation);

        Assert.That(FormatPauses(resolved), Is.EqualTo([
            "@3 pause 67.5 ms",
            "@18 pause 67.5 ms"
        ]));
        Assert.That(FrameDuration(resolved.Sequence.Duration), Is.EqualTo("30.025"));
    }

    [Test]
    public void LethalPauseTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 6, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 40),
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatPauses(Assert.NotNull(animation)), Is.EqualTo([
            "@7 pause 90 ms"
        ]));
    }

    [Test]
    public void FastPauseTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 2);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 20),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatPauses(Assert.NotNull(animation)), Is.EqualTo([
            "@3.5 pause 37.5 ms"
        ]));
    }

    [Test]
    public void ImpactMotionTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 2, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 6, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatImpact(Assert.NotNull(animation)), Is.EqualTo([
            "@2 unit 2 recoil 0.625 px towards 0.797 0.604 for 5 frames"
        ]));
    }

    [Test]
    public void SecondStrikeImpactTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 25, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 3, SecondTriggerFrame: 18))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 0, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatImpact(Assert.NotNull(animation)), Is.EqualTo([
            "@3 unit 2 recoil 0.625 px towards 0.797 0.604 for 5 frames",
            "@18 unit 2 recoil 0.625 px towards 0.797 0.604 for 5 frames"
        ]));
    }

    [Test]
    public void OverlappingSecondStrikeImpactTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 25, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 3, SecondTriggerFrame: 8))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 10, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 0, StartTime: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        BattleAnimationState resolved = Assert.NotNull(animation);

        Assert.That(FormatImpact(resolved), Is.EqualTo([
            "@3 unit 2 recoil 0.625 px towards 0.797 0.604 for 5 frames",
            "@8 unit 2 recoil 0.625 px towards 0.797 0.604 for 5 frames"
        ]));
        Assert.That(FormatPauses(resolved), Is.EqualTo([
            "@3 pause 67.5 ms",
            "@8 pause 67.5 ms"
        ]));
    }

    [Test]
    public void HeavyImpactShakeTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 30),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatImpact(Assert.NotNull(animation)), Is.EqualTo([
            "@7 unit 2 recoil 0.875 px towards 0.797 0.604 for 5 frames",
            "@7 shake 1.6 px for 3 frames"
        ]));
    }

    [Test]
    public void EnemyHeavyImpactShakeTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Attacker, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 30),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatImpact(Assert.NotNull(animation)), Is.EqualTo([
            "@7 unit 2 recoil 0.875 px towards 0.797 0.604 for 5 frames"
        ]));
    }

    [Test]
    public void RangedImpactTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.BackCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("BOW", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("BOW")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("BOW")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("BOW"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        BattleAnimationState resolved = Assert.NotNull(animation);

        Assert.That(FormatImpact(resolved), Is.Empty);
        Assert.That(FormatPauses(resolved), Is.Empty);
    }

    [Test]
    public void FieldImpactTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("BOLT", UnitAttackType.Damage, UnitAttackReach.All))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackTileTarget(BattleSide.Defender, GridPosition.FrontCenter),
                    [new UnitAttackDefinitionId("BOLT")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("BOLT")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("BOLT"), 10),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        BattleAnimationState resolved = Assert.NotNull(animation);

        Assert.That(FormatImpact(resolved), Is.Empty);
        Assert.That(FormatPauses(resolved), Is.Empty);
    }

    [Test]
    public void NonDamagingImpactTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PAR", UnitAttackType.Paralyze, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(new BattleUnitId(1), new BattleAttackUnitTarget(new BattleUnitId(2)), [new UnitAttackDefinitionId("PAR")]),
                new BattleUnitEffectAppliedEvent(new BattleUnitId(1), new BattleUnitId(2), new BattleParalyzeStatusEffect(RemainingTurns: 1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        BattleAnimationState resolved = Assert.NotNull(animation);

        Assert.That(FormatImpact(resolved), Is.Empty);
        Assert.That(FormatPauses(resolved), Is.Empty);
    }

    [Test]
    public void DamagedSoundRollTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 2, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSound(new AssetSoundKey.BattleUnitAttack(new UnitDefinitionId("ATK"), Variant: 0), new AssetSoundMetadata(AnimationFrame: 6, StartTime: null))
            .Build();

        IReadOnlyList<BattleEvent> events =
        [
            new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR")),
            new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR"), 10),
            new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
        ];

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("PAR", UnitAttackType.Damage, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder silentBuilder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? silentAnimation = silentBuilder.Build(baseState, events);

        BattleAnimationStateBuilder audibleBuilder = new(assets, definitions, new RandomProvider(3), BattleViewerSide.Defender, 1);
        BattleAnimationState? audibleAnimation = audibleBuilder.Build(baseState, events);

        Assert.That(FormatAnimation(Assert.NotNull(silentAnimation)), Is.EqualTo([
            "sequence 21 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@2 unit 2 body Hit Alive for 9 frames",
            "@2 unit 2 feedback +15 Damage -10",
            "@2 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@2 sound battle-unit-touch/atk/4",
            "@6 sound battle-unit-attack/atk/4",
            "@11 unit 2 body Idle Alive",
            "@16 unit 1 body Idle Alive"
        ]));
        Assert.That(FormatAnimation(Assert.NotNull(audibleAnimation)), Is.EqualTo([
            "sequence 21 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@2 unit 2 body Hit Alive for 9 frames",
            "@2 unit 2 feedback +15 Damage -10",
            "@2 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@2 sound battle-unit-touch/atk/3",
            "@2 sound battle-unit-damaged/def/3",
            "@6 sound battle-unit-attack/atk/1",
            "@11 unit 2 body Idle Alive",
            "@16 unit 1 body Idle Alive"
        ]));
    }

    [Test]
    public void AllReachSecondaryAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit neighbor = new(
            Id: new BattleUnitId(3),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender, neighbor],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("DMG", UnitAttackType.Damage, UnitAttackReach.All))
            .AddAttack(Attack("PSN", UnitAttackType.Poison, UnitAttackReach.All))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("DMG"), new UnitAttackDefinitionId("PSN")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG"), 10),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PSN")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PSN"), 0),
                new BattleUnitEffectAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5)),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("DMG")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("DMG"), 10),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("PSN")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("PSN"), 0),
                new BattleUnitEffectAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(3),
                    new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 45 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 3 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -10",
            "@7 unit 3 feedback +15 Damage -10",
            "@7 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@7 unit 3 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 unit 3 body Idle Alive",
            "@26 unit 2 feedback +15 Effect Poisoned",
            "@26 unit 3 feedback +15 Effect Poisoned",
            "@26 sound battle-effect-sound-status/poison",
            "@26 sound battle-effect-sound-status/poison"
        ]));
    }

    [Test]
    public void LargeTargetImpactTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: true),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 30),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatImpact(Assert.NotNull(animation)), Is.EqualTo([
            "@7 shake 1.6 px for 3 frames"
        ]));
    }

    [Test]
    public void BackLineTargetImpactTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.BackCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("MELEE", UnitAttackType.Damage, UnitAttackReach.Adjacent))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("MELEE")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("MELEE"), 30),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatImpact(Assert.NotNull(animation)), Is.Empty);
    }

    [Test]
    public void ItemBoostTest()
    {
        BattleRenderState.Unit leader = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: Theme.Battle.GroundMarker.CurrentColor,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit ally = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("ALLY"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.BackBottom, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 20,
            MaxHitpoints: 20,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [leader, ally],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Potion),
                new AssetAnimationMetadata(FrameCount: 31, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("ALCHEMIST"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Attack,
                    Variant: 0),
                new AssetAnimationMetadata(FrameCount: 12, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 2, SecondTriggerFrame: 0))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddItem(Item("POTION", ItemCategory.PotionTemporaryBoost, new UnitDefinitionId("ALCHEMIST")))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleItemUsedEvent(
                    BattleSide.Attacker,
                    LeaderEquipmentSlot.Battle1,
                    new ItemDefinitionId("POTION"),
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2))),
                new BattleUnitModifierAppliedEvent(new BattleUnitId(2), new UnitModifier(new UnitModifierDefinitionId("BOOST"), RemainingTurns: 1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 31 frames",
            "@0 clip battle-effect-item/potion for 31 frames at attacker front-center small",
            "@0 clip battle-effect-attack/alchemist/touch/body/attack/0 for 12 frames at attacker back-bottom small",
            "@0 sound battle-effect-sound-potion/boost",
            "@0 sound battle-unit-touch/alchemist/3",
            "@2 unit 1 feedback +15 Action Item"
        ]));
    }

    [Test]
    public void ParalyzeSecondaryAttackTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 60,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("DMG", UnitAttackType.Damage, UnitAttackReach.Any))
            .AddAttack(Attack("PAR", UnitAttackType.Paralyze, UnitAttackReach.Any))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("DMG"), new UnitAttackDefinitionId("PAR")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DMG"), 10),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("PAR"), 0),
                new BattleUnitEffectAppliedEvent(
                    new BattleUnitId(1),
                    new BattleUnitId(2),
                    new BattleParalyzeStatusEffect(RemainingTurns: 1)),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 45 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 2 feedback +15 Damage -10",
            "@7 unit 2 hitpoints 40 -> 30 over 12 frames Decelerate",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 unit 2 frozen True",
            "@26 unit 2 feedback +15 Effect Paralyzed"
        ]));
    }

    [Test]
    public void AllReachDrainHealTest()
    {
        BattleRenderState.Unit attacker = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("ATK"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit defender = new(
            Id: new BattleUnitId(2),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState.Unit neighbor = new(
            Id: new BattleUnitId(3),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontTop, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: 40,
            MaxHitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [attacker, defender, neighbor],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker),
                new AssetAnimationMetadata(FrameCount: 16, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender),
                new AssetAnimationMetadata(FrameCount: 9, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Heal(),
                new AssetAnimationMetadata(FrameCount: 14, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddAttack(Attack("DRAIN", UnitAttackType.DrainLife, UnitAttackReach.All))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, definitions, new RandomProvider(0), BattleViewerSide.Defender, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitAttackEvent(
                    new BattleUnitId(1),
                    new BattleAttackUnitTarget(new BattleUnitId(2)),
                    [new UnitAttackDefinitionId("DRAIN")]),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DRAIN")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("DRAIN"), 20),
                new BattleUnitHealedEvent(new BattleUnitId(1), HitpointsAfter: 50),
                new BattleUnitAttackedEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("DRAIN")),
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(3), new UnitAttackDefinitionId("DRAIN"), 20),
                new BattleUnitHealedEvent(new BattleUnitId(1), HitpointsAfter: 60),
                new BattleUnitTurnStartedEvent(new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal))
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 26 frames",
            "@0 unit 1 body Attack Alive for 16 frames",
            "@0 sound battle-unit-attack/atk/4",
            "@7 unit 2 body Hit Alive for 9 frames",
            "@7 unit 3 body Hit Alive for 9 frames",
            "@7 unit 1 feedback +15 Heal Healed",
            "@7 unit 1 feedback +15 Heal Healed",
            "@7 unit 2 feedback +15 Damage -20",
            "@7 unit 3 feedback +15 Damage -20",
            "@7 unit 1 hitpoints 40 -> 60 over 12 frames Decelerate",
            "@7 unit 2 hitpoints 40 -> 20 over 12 frames Decelerate",
            "@7 unit 3 hitpoints 40 -> 20 over 12 frames Decelerate",
            "@7 clip battle-effect-heal for 14 frames at attacker front-center small",
            "@7 clip battle-effect-heal for 14 frames at attacker front-center small",
            "@7 sound battle-unit-touch/atk/4",
            "@16 unit 1 body Idle Alive",
            "@16 unit 2 body Idle Alive",
            "@16 unit 3 body Idle Alive"
        ]));
    }

    [Test]
    public void UnitUpgradedTest()
    {
        BattleRenderState.Unit squire = new(
            Id: new BattleUnitId(1),
            DefinitionId: new UnitDefinitionId("SQUIRE"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Attacker,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 1,
            Hitpoints: 60,
            MaxHitpoints: 90,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);

        BattleRenderState baseState = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0),
            [squire],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.LevelUp(WorldRace.Human, IsLarge: false),
                new AssetAnimationMetadata(FrameCount: 20, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleAnimationStateBuilder builder = new(assets, TestGameDefinitionProvider.Empty, new RandomProvider(0), BattleViewerSide.Attacker, 1);
        BattleAnimationState? animation = builder.Build(
            baseState,
            [
                new BattleUnitUpgradedEvent(new BattleUnitId(1), new UnitDefinitionId("SQUIRE"), new UnitDefinitionId("KNIGHT"), Level: 1, Hitpoints: 150, MaxHitpoints: 150)
            ]);
        Assert.That(FormatAnimation(Assert.NotNull(animation)), Is.EqualTo([
            "sequence 22 frames",
            "@0 unit 1 feedback +15 Heal Level up!",
            "@0 clip battle-effect-level-up/human/small for 20 frames at attacker front-center small",
            "@10 unit 1 form KNIGHT facing Attacker",
            "@10 unit 1 hitpoints 60 -> 150 over 12 frames Decelerate",
            "@10 unit 1 max hitpoints 90 -> 150 over 12 frames Decelerate"
        ]));
    }
}
