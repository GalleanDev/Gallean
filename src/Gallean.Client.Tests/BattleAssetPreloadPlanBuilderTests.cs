using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class BattleAssetPreloadPlanBuilderTests
{
    private static readonly AssetAnimationMetadata AnimationMetadata = new(FrameCount: 4, Width: 10, Height: 10, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null);

    private static IReadOnlyList<string> FormatPlan(AssetPreloadPlan plan)
    {
        List<string> rows = [];

        rows.AddRange(FormatItems("required", plan.InitialRequired));
        rows.AddRange(FormatItems("optional", plan.InitialOptional));
        rows.AddRange(FormatItems("remaining", plan.Remaining));

        return rows;
    }

    private static IEnumerable<string> FormatItems(string bucket, IReadOnlyList<AssetPreloadItem> items)
    {
        foreach (AssetPreloadItem item in items)
        {
            switch (item)
            {
                case AssetPreloadItem.Sprite sprite:
                    yield return $"{bucket} sprite {sprite.Key.CanonicalId}";
                    break;

                case AssetPreloadItem.Animation animation:
                    yield return $"{bucket} animation {animation.Key.CanonicalId}";
                    break;

                case AssetPreloadItem.Sound sound:
                    yield return $"{bucket} sound {sound.Key.CanonicalId}";
                    break;
            }
        }
    }

    private static BattleRenderState.Unit RenderUnit(UnitDefinition definition, int id, BattleSide side, BattleUnitState.Auras auras)
    {
        return new BattleRenderState.Unit(
            Id: new BattleUnitId(id),
            DefinitionId: definition.Id,
            Race: definition.Race,
            DeathAnimation: definition.DeathAnimation,
            Footprint: new BattleGridFootprint(side, GridPosition.FrontCenter, definition.IsLarge),
            Facing: side,
            IdleOffset: TimeSpan.Zero,
            CorpseVariant: 0,
            Hitpoints: definition.MaxHitpoints,
            MaxHitpoints: definition.MaxHitpoints,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: auras,
            Frozen: false);
    }

    private static BattleUnit BattleUnit(UnitDefinition definition, int id, BattleSide side)
    {
        return TestHelpers.SimpleBattleUnit(new BattleUnitId(id), null, definition.Id, side, hitpoints: definition.MaxHitpoints);
    }

    private static BattleState BattleState(UnitDefinition attacker, UnitDefinition defender)
    {
        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(BattleUnit(attacker, 1, BattleSide.Attacker), GridPosition.FrontCenter, attacker.IsLarge)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(BattleUnit(defender, 2, BattleSide.Defender), GridPosition.FrontCenter, defender.IsLarge)
            .Build();

        return new BattleState(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);
    }

    [Test]
    public void PlanTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10) with
        {
            IsLarge = true
        };

        AssetAnimationKey.BattleUnitAura attackerAura = new(attackerDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(attackerAura, AnimationMetadata)
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    attackerDefinition.Id,
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Attack,
                    Variant: 0),
                AnimationMetadata)
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    attackerDefinition.Id,
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Attack,
                    Variant: 1),
                AnimationMetadata)
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), AnimationMetadata)
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: true), AnimationMetadata)
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();

        BattleRenderState state = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            [
                RenderUnit(attackerDefinition, 1, BattleSide.Attacker, new BattleUnitState.Auras(attackerAura, Attack: null, Hit: null)),
                RenderUnit(defenderDefinition, 2, BattleSide.Defender, BattleUnitState.Auras.None)
            ],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            [BattleFluidConfiguration.MakeLava(), BattleFluidConfiguration.MakeFog()],
            RainConfiguration: null,
            AnimationRate.Sprites);

        AssetPreloadPlan plan = new BattleAssetPreloadPlanBuilder(state, BattleState(attackerDefinition, defenderDefinition), definitions, assets).Build();

        Assert.That(FormatPlan(plan), Is.EqualTo([
            "required sprite battle-background/terrain/human/1",
            "required animation battle-unit-shadow/atk/idle/attacker",
            "required animation battle-unit/atk/idle/attacker",
            "required animation battle-unit-aura/atk/idle/attacker",
            "required animation battle-unit-shadow/atk/attack/attacker",
            "required animation battle-unit/atk/attack/attacker",
            "required animation battle-unit-shadow/atk/hit/attacker",
            "required animation battle-unit/atk/hit/attacker",
            "required animation battle-unit-shadow/def/idle/defender",
            "required animation battle-unit/def/idle/defender",
            "required animation battle-unit-shadow/def/attack/defender",
            "required animation battle-unit/def/attack/defender",
            "required animation battle-unit-shadow/def/hit/defender",
            "required animation battle-unit/def/hit/defender",
            "optional animation battle-background-overlay/terrain/human/1/0",
            "optional animation battle-background-overlay/terrain/human/1/1",
            "optional sprite unit-face/atk",
            "optional sprite unit-portrait/atk",
            "optional sprite unit-event-portrait/atk",
            "optional sprite unit-face/def",
            "optional sprite unit-portrait/def",
            "optional sprite unit-event-portrait/def",
            "remaining sound battle-unit-attack/atk/0",
            "remaining sound battle-unit-attack/atk/1",
            "remaining sound battle-unit-attack/atk/2",
            "remaining sound battle-unit-attack/atk/3",
            "remaining sound battle-unit-attack/atk/4",
            "remaining sound battle-unit-attack/atk/5",
            "remaining sound battle-unit-hit/atk/0",
            "remaining sound battle-unit-hit/atk/1",
            "remaining sound battle-unit-hit/atk/2",
            "remaining sound battle-unit-hit/atk/3",
            "remaining sound battle-unit-hit/atk/4",
            "remaining sound battle-unit-touch/atk/0",
            "remaining sound battle-unit-touch/atk/1",
            "remaining sound battle-unit-touch/atk/2",
            "remaining sound battle-unit-touch/atk/3",
            "remaining sound battle-unit-touch/atk/4",
            "remaining sound battle-unit-damaged/atk/0",
            "remaining sound battle-unit-damaged/atk/1",
            "remaining sound battle-unit-damaged/atk/2",
            "remaining sound battle-unit-damaged/atk/3",
            "remaining sound battle-unit-attack/def/0",
            "remaining sound battle-unit-attack/def/1",
            "remaining sound battle-unit-attack/def/2",
            "remaining sound battle-unit-attack/def/3",
            "remaining sound battle-unit-attack/def/4",
            "remaining sound battle-unit-attack/def/5",
            "remaining sound battle-unit-hit/def/0",
            "remaining sound battle-unit-hit/def/1",
            "remaining sound battle-unit-hit/def/2",
            "remaining sound battle-unit-hit/def/3",
            "remaining sound battle-unit-hit/def/4",
            "remaining sound battle-unit-touch/def/0",
            "remaining sound battle-unit-touch/def/1",
            "remaining sound battle-unit-touch/def/2",
            "remaining sound battle-unit-touch/def/3",
            "remaining sound battle-unit-touch/def/4",
            "remaining sound battle-unit-damaged/def/0",
            "remaining sound battle-unit-damaged/def/1",
            "remaining sound battle-unit-damaged/def/2",
            "remaining sound battle-unit-damaged/def/3",
            "remaining sound battle-effect-sound-heal",
            "remaining sound battle-effect-sound-death",
            "remaining sound battle-effect-sound-status/poison",
            "remaining sound battle-effect-sound-status/frostbite",
            "remaining sound battle-effect-sound-status/blister",
            "remaining sound battle-effect-sound-status/shatter-armor",
            "remaining sound battle-effect-sound-status/transform",
            "remaining sound battle-effect-sound-status/unsummon",
            "remaining animation battle-effect-attack/atk/touch/body/attack/0",
            "remaining animation battle-effect-attack/atk/touch/body/attack/1",
            "remaining animation battle-effect-death/human/small",
            "remaining animation battle-effect-death/human/large",
            "remaining sprite battle-corpse/small/0",
            "remaining sprite battle-corpse/small/1",
            "remaining sprite battle-corpse/large/0",
            "remaining sprite battle-corpse/large/1",
            "remaining animation battle-effect-heal",
            "remaining animation battle-effect-blister",
            "remaining animation battle-unit-shadow/atk/idle/defender",
            "remaining animation battle-unit/atk/idle/defender",
            "remaining animation battle-unit-shadow/atk/hit/defender",
            "remaining animation battle-unit/atk/hit/defender",
            "remaining animation battle-unit-shadow/def/idle/attacker",
            "remaining animation battle-unit/def/idle/attacker",
            "remaining animation battle-unit-shadow/def/hit/attacker",
            "remaining animation battle-unit/def/hit/attacker",
            "remaining animation battle-effect-level-up/human/small",
            "remaining animation battle-effect-level-up/human/large"
        ]));
    }

    [Test]
    public void SummonedUnitTest()
    {
        UnitDefinition summonedDefinition = TestHelpers.SimpleUnitDefinition("WLF", maxHitpoints: 30, initiative: 60, damage: 15);
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        attackerDefinition = attackerDefinition with
        {
            Attack = attackerDefinition.Attack with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [summonedDefinition.Id]
            }
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder().Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .AddUnit(summonedDefinition)
            .Build();

        BattleRenderState state = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            [
                RenderUnit(attackerDefinition, 1, BattleSide.Attacker, BattleUnitState.Auras.None),
                RenderUnit(defenderDefinition, 2, BattleSide.Defender, BattleUnitState.Auras.None)
            ],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        AssetPreloadPlan plan = new BattleAssetPreloadPlanBuilder(state, BattleState(attackerDefinition, defenderDefinition), definitions, assets).Build();

        Assert.That(FormatPlan(plan), Is.EqualTo([
            "required sprite battle-background/terrain/human/1",
            "required animation battle-unit-shadow/atk/idle/attacker",
            "required animation battle-unit/atk/idle/attacker",
            "required animation battle-unit-shadow/atk/attack/attacker",
            "required animation battle-unit/atk/attack/attacker",
            "required animation battle-unit-shadow/atk/hit/attacker",
            "required animation battle-unit/atk/hit/attacker",
            "required animation battle-unit-shadow/def/idle/defender",
            "required animation battle-unit/def/idle/defender",
            "required animation battle-unit-shadow/def/attack/defender",
            "required animation battle-unit/def/attack/defender",
            "required animation battle-unit-shadow/def/hit/defender",
            "required animation battle-unit/def/hit/defender",
            "optional sprite unit-face/atk",
            "optional sprite unit-portrait/atk",
            "optional sprite unit-event-portrait/atk",
            "optional sprite unit-face/def",
            "optional sprite unit-portrait/def",
            "optional sprite unit-event-portrait/def",
            "remaining sound battle-unit-attack/atk/0",
            "remaining sound battle-unit-attack/atk/1",
            "remaining sound battle-unit-attack/atk/2",
            "remaining sound battle-unit-attack/atk/3",
            "remaining sound battle-unit-attack/atk/4",
            "remaining sound battle-unit-attack/atk/5",
            "remaining sound battle-unit-hit/atk/0",
            "remaining sound battle-unit-hit/atk/1",
            "remaining sound battle-unit-hit/atk/2",
            "remaining sound battle-unit-hit/atk/3",
            "remaining sound battle-unit-hit/atk/4",
            "remaining sound battle-unit-touch/atk/0",
            "remaining sound battle-unit-touch/atk/1",
            "remaining sound battle-unit-touch/atk/2",
            "remaining sound battle-unit-touch/atk/3",
            "remaining sound battle-unit-touch/atk/4",
            "remaining sound battle-unit-damaged/atk/0",
            "remaining sound battle-unit-damaged/atk/1",
            "remaining sound battle-unit-damaged/atk/2",
            "remaining sound battle-unit-damaged/atk/3",
            "remaining sound battle-unit-attack/def/0",
            "remaining sound battle-unit-attack/def/1",
            "remaining sound battle-unit-attack/def/2",
            "remaining sound battle-unit-attack/def/3",
            "remaining sound battle-unit-attack/def/4",
            "remaining sound battle-unit-attack/def/5",
            "remaining sound battle-unit-hit/def/0",
            "remaining sound battle-unit-hit/def/1",
            "remaining sound battle-unit-hit/def/2",
            "remaining sound battle-unit-hit/def/3",
            "remaining sound battle-unit-hit/def/4",
            "remaining sound battle-unit-touch/def/0",
            "remaining sound battle-unit-touch/def/1",
            "remaining sound battle-unit-touch/def/2",
            "remaining sound battle-unit-touch/def/3",
            "remaining sound battle-unit-touch/def/4",
            "remaining sound battle-unit-damaged/def/0",
            "remaining sound battle-unit-damaged/def/1",
            "remaining sound battle-unit-damaged/def/2",
            "remaining sound battle-unit-damaged/def/3",
            "remaining sound battle-effect-sound-heal",
            "remaining sound battle-effect-sound-death",
            "remaining sound battle-effect-sound-status/poison",
            "remaining sound battle-effect-sound-status/frostbite",
            "remaining sound battle-effect-sound-status/blister",
            "remaining sound battle-effect-sound-status/shatter-armor",
            "remaining sound battle-effect-sound-status/transform",
            "remaining sound battle-effect-sound-status/unsummon",
            "remaining animation battle-effect-death/human/small",
            "remaining sprite battle-corpse/small/0",
            "remaining sprite battle-corpse/small/1",
            "remaining animation battle-effect-status/unsummon/small",
            "remaining animation battle-effect-heal",
            "remaining animation battle-effect-blister",
            "remaining sprite unit-face/wlf",
            "remaining sprite unit-portrait/wlf",
            "remaining sprite unit-event-portrait/wlf",
            "remaining sound battle-unit-attack/wlf/0",
            "remaining sound battle-unit-attack/wlf/1",
            "remaining sound battle-unit-attack/wlf/2",
            "remaining sound battle-unit-attack/wlf/3",
            "remaining sound battle-unit-attack/wlf/4",
            "remaining sound battle-unit-attack/wlf/5",
            "remaining sound battle-unit-hit/wlf/0",
            "remaining sound battle-unit-hit/wlf/1",
            "remaining sound battle-unit-hit/wlf/2",
            "remaining sound battle-unit-hit/wlf/3",
            "remaining sound battle-unit-hit/wlf/4",
            "remaining sound battle-unit-touch/wlf/0",
            "remaining sound battle-unit-touch/wlf/1",
            "remaining sound battle-unit-touch/wlf/2",
            "remaining sound battle-unit-touch/wlf/3",
            "remaining sound battle-unit-touch/wlf/4",
            "remaining sound battle-unit-damaged/wlf/0",
            "remaining sound battle-unit-damaged/wlf/1",
            "remaining sound battle-unit-damaged/wlf/2",
            "remaining sound battle-unit-damaged/wlf/3",
            "remaining animation battle-unit-shadow/wlf/idle/attacker",
            "remaining animation battle-unit/wlf/idle/attacker",
            "remaining animation battle-unit-shadow/wlf/attack/attacker",
            "remaining animation battle-unit/wlf/attack/attacker",
            "remaining animation battle-unit-shadow/wlf/hit/attacker",
            "remaining animation battle-unit/wlf/hit/attacker",
            "remaining animation battle-unit-shadow/atk/idle/defender",
            "remaining animation battle-unit/atk/idle/defender",
            "remaining animation battle-unit-shadow/atk/hit/defender",
            "remaining animation battle-unit/atk/hit/defender",
            "remaining animation battle-unit-shadow/def/idle/attacker",
            "remaining animation battle-unit/def/idle/attacker",
            "remaining animation battle-unit-shadow/def/hit/attacker",
            "remaining animation battle-unit/def/hit/attacker",
            "remaining animation battle-effect-level-up/human/small"
        ]));
    }

    [Test]
    public void ParalyzingAttackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        attackerDefinition = attackerDefinition with
        {
            Attack = attackerDefinition.Attack with { Type = UnitAttackType.Paralyze }
        };

        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder().Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();

        BattleRenderState state = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            [
                RenderUnit(attackerDefinition, 1, BattleSide.Attacker, BattleUnitState.Auras.None),
                RenderUnit(defenderDefinition, 2, BattleSide.Defender, BattleUnitState.Auras.None)
            ],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        AssetPreloadPlan plan = new BattleAssetPreloadPlanBuilder(state, BattleState(attackerDefinition, defenderDefinition), definitions, assets).Build();

        Assert.That(FormatPlan(plan), Is.EqualTo([
            "required sprite battle-background/terrain/human/1",
            "required animation battle-unit-shadow/atk/idle/attacker",
            "required animation battle-unit/atk/idle/attacker",
            "required animation battle-unit-shadow/atk/attack/attacker",
            "required animation battle-unit/atk/attack/attacker",
            "required animation battle-unit-shadow/atk/hit/attacker",
            "required animation battle-unit/atk/hit/attacker",
            "required animation battle-unit-shadow/def/idle/defender",
            "required animation battle-unit/def/idle/defender",
            "required animation battle-unit-shadow/def/attack/defender",
            "required animation battle-unit/def/attack/defender",
            "required animation battle-unit-shadow/def/hit/defender",
            "required animation battle-unit/def/hit/defender",
            "optional sprite unit-face/atk",
            "optional sprite unit-portrait/atk",
            "optional sprite unit-event-portrait/atk",
            "optional sprite unit-face/def",
            "optional sprite unit-portrait/def",
            "optional sprite unit-event-portrait/def",
            "remaining sound battle-unit-attack/atk/0",
            "remaining sound battle-unit-attack/atk/1",
            "remaining sound battle-unit-attack/atk/2",
            "remaining sound battle-unit-attack/atk/3",
            "remaining sound battle-unit-attack/atk/4",
            "remaining sound battle-unit-attack/atk/5",
            "remaining sound battle-unit-hit/atk/0",
            "remaining sound battle-unit-hit/atk/1",
            "remaining sound battle-unit-hit/atk/2",
            "remaining sound battle-unit-hit/atk/3",
            "remaining sound battle-unit-hit/atk/4",
            "remaining sound battle-unit-touch/atk/0",
            "remaining sound battle-unit-touch/atk/1",
            "remaining sound battle-unit-touch/atk/2",
            "remaining sound battle-unit-touch/atk/3",
            "remaining sound battle-unit-touch/atk/4",
            "remaining sound battle-unit-damaged/atk/0",
            "remaining sound battle-unit-damaged/atk/1",
            "remaining sound battle-unit-damaged/atk/2",
            "remaining sound battle-unit-damaged/atk/3",
            "remaining sound battle-unit-attack/def/0",
            "remaining sound battle-unit-attack/def/1",
            "remaining sound battle-unit-attack/def/2",
            "remaining sound battle-unit-attack/def/3",
            "remaining sound battle-unit-attack/def/4",
            "remaining sound battle-unit-attack/def/5",
            "remaining sound battle-unit-hit/def/0",
            "remaining sound battle-unit-hit/def/1",
            "remaining sound battle-unit-hit/def/2",
            "remaining sound battle-unit-hit/def/3",
            "remaining sound battle-unit-hit/def/4",
            "remaining sound battle-unit-touch/def/0",
            "remaining sound battle-unit-touch/def/1",
            "remaining sound battle-unit-touch/def/2",
            "remaining sound battle-unit-touch/def/3",
            "remaining sound battle-unit-touch/def/4",
            "remaining sound battle-unit-damaged/def/0",
            "remaining sound battle-unit-damaged/def/1",
            "remaining sound battle-unit-damaged/def/2",
            "remaining sound battle-unit-damaged/def/3",
            "remaining sound battle-effect-sound-heal",
            "remaining sound battle-effect-sound-death",
            "remaining sound battle-effect-sound-status/poison",
            "remaining sound battle-effect-sound-status/frostbite",
            "remaining sound battle-effect-sound-status/blister",
            "remaining sound battle-effect-sound-status/shatter-armor",
            "remaining sound battle-effect-sound-status/transform",
            "remaining sound battle-effect-sound-status/unsummon",
            "remaining animation battle-effect-death/human/small",
            "remaining sprite battle-corpse/small/0",
            "remaining sprite battle-corpse/small/1",
            "remaining animation battle-effect-heal",
            "remaining animation battle-effect-blister",
            "remaining animation battle-unit-shadow/atk/idle/defender",
            "remaining animation battle-unit/atk/idle/defender",
            "remaining animation battle-unit-shadow/atk/hit/defender",
            "remaining animation battle-unit/atk/hit/defender",
            "remaining animation battle-unit-shadow/def/idle/attacker",
            "remaining animation battle-unit/def/idle/attacker",
            "remaining animation battle-unit-shadow/def/hit/attacker",
            "remaining animation battle-unit/def/hit/attacker",
            "remaining animation battle-effect-level-up/human/small"
        ]));
    }

    [Test]
    public void UpgradedUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition upgradedDefinition = TestHelpers.SimpleUnitDefinition("ATK2", maxHitpoints: 80, initiative: 50, damage: 15);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder().Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(upgradedDefinition)
            .AddUnit(defenderDefinition)
            .AddUnitUpgrade(new UnitUpgradeDefinition(attackerDefinition.Id, upgradedDefinition.Id, RequiredBuildingId: null))
            .Build();

        BattleRenderState state = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            [
                RenderUnit(attackerDefinition, 1, BattleSide.Attacker, BattleUnitState.Auras.None),
                RenderUnit(defenderDefinition, 2, BattleSide.Defender, BattleUnitState.Auras.None)
            ],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        AssetPreloadPlan plan = new BattleAssetPreloadPlanBuilder(state, BattleState(attackerDefinition, defenderDefinition), definitions, assets).Build();

        Assert.That(FormatPlan(plan), Is.EqualTo([
            "required sprite battle-background/terrain/human/1",
            "required animation battle-unit-shadow/atk/idle/attacker",
            "required animation battle-unit/atk/idle/attacker",
            "required animation battle-unit-shadow/atk/attack/attacker",
            "required animation battle-unit/atk/attack/attacker",
            "required animation battle-unit-shadow/atk/hit/attacker",
            "required animation battle-unit/atk/hit/attacker",
            "required animation battle-unit-shadow/def/idle/defender",
            "required animation battle-unit/def/idle/defender",
            "required animation battle-unit-shadow/def/attack/defender",
            "required animation battle-unit/def/attack/defender",
            "required animation battle-unit-shadow/def/hit/defender",
            "required animation battle-unit/def/hit/defender",
            "optional sprite unit-face/atk",
            "optional sprite unit-portrait/atk",
            "optional sprite unit-event-portrait/atk",
            "optional sprite unit-face/def",
            "optional sprite unit-portrait/def",
            "optional sprite unit-event-portrait/def",
            "remaining sound battle-unit-attack/atk/0",
            "remaining sound battle-unit-attack/atk/1",
            "remaining sound battle-unit-attack/atk/2",
            "remaining sound battle-unit-attack/atk/3",
            "remaining sound battle-unit-attack/atk/4",
            "remaining sound battle-unit-attack/atk/5",
            "remaining sound battle-unit-hit/atk/0",
            "remaining sound battle-unit-hit/atk/1",
            "remaining sound battle-unit-hit/atk/2",
            "remaining sound battle-unit-hit/atk/3",
            "remaining sound battle-unit-hit/atk/4",
            "remaining sound battle-unit-touch/atk/0",
            "remaining sound battle-unit-touch/atk/1",
            "remaining sound battle-unit-touch/atk/2",
            "remaining sound battle-unit-touch/atk/3",
            "remaining sound battle-unit-touch/atk/4",
            "remaining sound battle-unit-damaged/atk/0",
            "remaining sound battle-unit-damaged/atk/1",
            "remaining sound battle-unit-damaged/atk/2",
            "remaining sound battle-unit-damaged/atk/3",
            "remaining sound battle-unit-attack/def/0",
            "remaining sound battle-unit-attack/def/1",
            "remaining sound battle-unit-attack/def/2",
            "remaining sound battle-unit-attack/def/3",
            "remaining sound battle-unit-attack/def/4",
            "remaining sound battle-unit-attack/def/5",
            "remaining sound battle-unit-hit/def/0",
            "remaining sound battle-unit-hit/def/1",
            "remaining sound battle-unit-hit/def/2",
            "remaining sound battle-unit-hit/def/3",
            "remaining sound battle-unit-hit/def/4",
            "remaining sound battle-unit-touch/def/0",
            "remaining sound battle-unit-touch/def/1",
            "remaining sound battle-unit-touch/def/2",
            "remaining sound battle-unit-touch/def/3",
            "remaining sound battle-unit-touch/def/4",
            "remaining sound battle-unit-damaged/def/0",
            "remaining sound battle-unit-damaged/def/1",
            "remaining sound battle-unit-damaged/def/2",
            "remaining sound battle-unit-damaged/def/3",
            "remaining sound battle-effect-sound-heal",
            "remaining sound battle-effect-sound-death",
            "remaining sound battle-effect-sound-status/poison",
            "remaining sound battle-effect-sound-status/frostbite",
            "remaining sound battle-effect-sound-status/blister",
            "remaining sound battle-effect-sound-status/shatter-armor",
            "remaining sound battle-effect-sound-status/transform",
            "remaining sound battle-effect-sound-status/unsummon",
            "remaining animation battle-effect-death/human/small",
            "remaining sprite battle-corpse/small/0",
            "remaining sprite battle-corpse/small/1",
            "remaining animation battle-effect-heal",
            "remaining animation battle-effect-blister",
            "remaining animation battle-unit-shadow/atk/idle/defender",
            "remaining animation battle-unit/atk/idle/defender",
            "remaining animation battle-unit-shadow/atk/hit/defender",
            "remaining animation battle-unit/atk/hit/defender",
            "remaining animation battle-unit-shadow/def/idle/attacker",
            "remaining animation battle-unit/def/idle/attacker",
            "remaining animation battle-unit-shadow/def/hit/attacker",
            "remaining animation battle-unit/def/hit/attacker",
            "remaining animation battle-effect-level-up/human/small",
            "remaining sprite unit-face/atk2",
            "remaining sprite unit-portrait/atk2",
            "remaining sprite unit-event-portrait/atk2",
            "remaining sound battle-unit-attack/atk2/0",
            "remaining sound battle-unit-attack/atk2/1",
            "remaining sound battle-unit-attack/atk2/2",
            "remaining sound battle-unit-attack/atk2/3",
            "remaining sound battle-unit-attack/atk2/4",
            "remaining sound battle-unit-attack/atk2/5",
            "remaining sound battle-unit-hit/atk2/0",
            "remaining sound battle-unit-hit/atk2/1",
            "remaining sound battle-unit-hit/atk2/2",
            "remaining sound battle-unit-hit/atk2/3",
            "remaining sound battle-unit-hit/atk2/4",
            "remaining sound battle-unit-touch/atk2/0",
            "remaining sound battle-unit-touch/atk2/1",
            "remaining sound battle-unit-touch/atk2/2",
            "remaining sound battle-unit-touch/atk2/3",
            "remaining sound battle-unit-touch/atk2/4",
            "remaining sound battle-unit-damaged/atk2/0",
            "remaining sound battle-unit-damaged/atk2/1",
            "remaining sound battle-unit-damaged/atk2/2",
            "remaining sound battle-unit-damaged/atk2/3",
            "remaining animation battle-unit-shadow/atk2/idle/attacker",
            "remaining animation battle-unit/atk2/idle/attacker",
            "remaining animation battle-unit-shadow/atk2/attack/attacker",
            "remaining animation battle-unit/atk2/attack/attacker",
            "remaining animation battle-unit-shadow/atk2/hit/attacker",
            "remaining animation battle-unit/atk2/hit/attacker"
        ]));
    }

    [Test]
    public void ItemTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        IAssetMetadataProvider assets = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Orb), AnimationMetadata)
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("MAGE"),
                    AssetAnimationKey.BattleEffect.AttackType.Hit,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Both,
                    Variant: 0),
                AnimationMetadata)
            .WithAnimation(
                new AssetAnimationKey.BattleEffect.Attack(
                    new UnitDefinitionId("MAGE"),
                    AssetAnimationKey.BattleEffect.AttackType.Touch,
                    AssetAnimationKey.BattleLayer.Body,
                    AssetAnimationKey.BattleEffect.AttackSide.Defend,
                    Variant: 0),
                AnimationMetadata)
            .Build();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .AddItem(new ItemDefinition(
                new ItemDefinitionId("SPHERE"),
                new GlobalTextId("name"),
                new GlobalTextId("description"),
                ItemCategory.Orb,
                WorldResourceSet.Empty,
                EquipmentModifierId: null,
                EquipmentAttackId: null,
                Effect: null,
                AnimationUnitId: new UnitDefinitionId("MAGE")))
            .Build();

        BattleRenderState state = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            [
                RenderUnit(attackerDefinition, 1, BattleSide.Attacker, BattleUnitState.Auras.None),
                RenderUnit(defenderDefinition, 2, BattleSide.Defender, BattleUnitState.Auras.None)
            ],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleState battle = BattleState(attackerDefinition, defenderDefinition) with
        {
            Items = [new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, new ItemDefinitionId("SPHERE"), Charges: null, Used: false)]
        };

        AssetPreloadPlan plan = new BattleAssetPreloadPlanBuilder(state, battle, definitions, assets).Build();

        Assert.That(FormatPlan(plan), Is.EqualTo([
            "required sprite battle-background/terrain/human/1",
            "required animation battle-unit-shadow/atk/idle/attacker",
            "required animation battle-unit/atk/idle/attacker",
            "required animation battle-unit-shadow/atk/attack/attacker",
            "required animation battle-unit/atk/attack/attacker",
            "required animation battle-unit-shadow/atk/hit/attacker",
            "required animation battle-unit/atk/hit/attacker",
            "required animation battle-unit-shadow/def/idle/defender",
            "required animation battle-unit/def/idle/defender",
            "required animation battle-unit-shadow/def/attack/defender",
            "required animation battle-unit/def/attack/defender",
            "required animation battle-unit-shadow/def/hit/defender",
            "required animation battle-unit/def/hit/defender",
            "optional sprite unit-face/atk",
            "optional sprite unit-portrait/atk",
            "optional sprite unit-event-portrait/atk",
            "optional sprite unit-face/def",
            "optional sprite unit-portrait/def",
            "optional sprite unit-event-portrait/def",
            "remaining sound battle-unit-attack/atk/0",
            "remaining sound battle-unit-attack/atk/1",
            "remaining sound battle-unit-attack/atk/2",
            "remaining sound battle-unit-attack/atk/3",
            "remaining sound battle-unit-attack/atk/4",
            "remaining sound battle-unit-attack/atk/5",
            "remaining sound battle-unit-hit/atk/0",
            "remaining sound battle-unit-hit/atk/1",
            "remaining sound battle-unit-hit/atk/2",
            "remaining sound battle-unit-hit/atk/3",
            "remaining sound battle-unit-hit/atk/4",
            "remaining sound battle-unit-touch/atk/0",
            "remaining sound battle-unit-touch/atk/1",
            "remaining sound battle-unit-touch/atk/2",
            "remaining sound battle-unit-touch/atk/3",
            "remaining sound battle-unit-touch/atk/4",
            "remaining sound battle-unit-damaged/atk/0",
            "remaining sound battle-unit-damaged/atk/1",
            "remaining sound battle-unit-damaged/atk/2",
            "remaining sound battle-unit-damaged/atk/3",
            "remaining sound battle-unit-attack/def/0",
            "remaining sound battle-unit-attack/def/1",
            "remaining sound battle-unit-attack/def/2",
            "remaining sound battle-unit-attack/def/3",
            "remaining sound battle-unit-attack/def/4",
            "remaining sound battle-unit-attack/def/5",
            "remaining sound battle-unit-hit/def/0",
            "remaining sound battle-unit-hit/def/1",
            "remaining sound battle-unit-hit/def/2",
            "remaining sound battle-unit-hit/def/3",
            "remaining sound battle-unit-hit/def/4",
            "remaining sound battle-unit-touch/def/0",
            "remaining sound battle-unit-touch/def/1",
            "remaining sound battle-unit-touch/def/2",
            "remaining sound battle-unit-touch/def/3",
            "remaining sound battle-unit-touch/def/4",
            "remaining sound battle-unit-damaged/def/0",
            "remaining sound battle-unit-damaged/def/1",
            "remaining sound battle-unit-damaged/def/2",
            "remaining sound battle-unit-damaged/def/3",
            "remaining sound battle-effect-sound-heal",
            "remaining sound battle-effect-sound-death",
            "remaining sound battle-effect-sound-status/poison",
            "remaining sound battle-effect-sound-status/frostbite",
            "remaining sound battle-effect-sound-status/blister",
            "remaining sound battle-effect-sound-status/shatter-armor",
            "remaining sound battle-effect-sound-status/transform",
            "remaining sound battle-effect-sound-status/unsummon",
            "remaining animation battle-effect-death/human/small",
            "remaining sprite battle-corpse/small/0",
            "remaining sprite battle-corpse/small/1",
            "remaining animation battle-effect-heal",
            "remaining animation battle-effect-blister",
            "remaining animation battle-effect-item/orb",
            "remaining animation battle-effect-attack/mage/hit/body/both/0",
            "remaining animation battle-effect-attack/mage/touch/body/defend/0",
            "remaining sound battle-unit-hit/mage/0",
            "remaining sound battle-unit-hit/mage/1",
            "remaining sound battle-unit-hit/mage/2",
            "remaining sound battle-unit-hit/mage/3",
            "remaining sound battle-unit-hit/mage/4",
            "remaining sound battle-unit-touch/mage/0",
            "remaining sound battle-unit-touch/mage/1",
            "remaining sound battle-unit-touch/mage/2",
            "remaining sound battle-unit-touch/mage/3",
            "remaining sound battle-unit-touch/mage/4",
            "remaining animation battle-unit-shadow/atk/idle/defender",
            "remaining animation battle-unit/atk/idle/defender",
            "remaining animation battle-unit-shadow/atk/hit/defender",
            "remaining animation battle-unit/atk/hit/defender",
            "remaining animation battle-unit-shadow/def/idle/attacker",
            "remaining animation battle-unit/def/idle/attacker",
            "remaining animation battle-unit-shadow/def/hit/attacker",
            "remaining animation battle-unit/def/hit/attacker",
            "remaining animation battle-effect-level-up/human/small"
        ]));
    }
}
