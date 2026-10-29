using Avalonia;
using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class BattleRenderStateBuilderTests
{
    private static IReadOnlyList<string> FormatIdleOffsets(BattleRenderState state)
    {
        List<string> result = [];

        foreach (BattleRenderState.Unit unit in state.Units)
        {
            result.Add($"unit {unit.Id.Value} idle offset {unit.IdleOffset.Ticks}");
        }

        return result;
    }

    [Test]
    public void RenderStateTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background: background, environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: new BattleActionTarget.Unit([new BattleUnitId(2)]),
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter)] = new BattleActionTarget.Unit([new BattleUnitId(2)])
            },
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 30,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.EnemyTargetColor,
                    Highlight: Theme.Battle.Highlight.EnemyTargetColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void DeadUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 0);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteMetadata corpseMetadata = new(Width: 44, Height: 30, PixelWidth: 44, PixelHeight: 30, OffsetX: -22, OffsetY: -20, Additive: false);
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 1), corpseMetadata)
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 0,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Dead,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(410.037, 288.226, 45.408, 30.96)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(15.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void DeadUnitReviveTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit priest = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 60);

        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 0);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(priest, GridPosition.FrontCenter, isLarge: false)
            .Place(ally, GridPosition.BackCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 2), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();

        BattleRenderState reachableState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter)] = new BattleActionTarget.Unit([ally.Id])
            },
            previousState: null).BuildState();

        BattleRenderState selectedState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: new BattleActionTarget.Unit([ally.Id]),
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter)] = new BattleActionTarget.Unit([ally.Id])
            },
            previousState: null).BuildState();

        Assert.That(reachableState.Units, Is.EqualTo([
            new BattleRenderState.Unit(
                Id: new BattleUnitId(2),
                DefinitionId: attackerDefinition.Id,
                Race: WorldRace.Human,
                DeathAnimation: UnitDeathAnimation.Human,
                Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.BackCenter, IsLarge: false),
                Facing: BattleSide.Attacker,
                IdleOffset: TimeSpan.FromTicks(6524244),
                CorpseVariant: 0,
                Hitpoints: 0,
                MaxHitpoints: 60,
                Presence: BattleUnitState.Presence.Dead,
                Marker: Theme.Battle.GroundMarker.AllyReachableColor,
                Highlight: Theme.Battle.Highlight.AllyReachableColor,
                Auras: BattleUnitState.Auras.None,
                Frozen: false),
            new BattleRenderState.Unit(
                Id: new BattleUnitId(1),
                DefinitionId: attackerDefinition.Id,
                Race: WorldRace.Human,
                DeathAnimation: UnitDeathAnimation.Human,
                Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                Facing: BattleSide.Attacker,
                IdleOffset: TimeSpan.FromTicks(7676067),
                CorpseVariant: 1,
                Hitpoints: 60,
                MaxHitpoints: 60,
                Presence: BattleUnitState.Presence.Alive,
                Marker: Theme.Battle.GroundMarker.CurrentColor,
                Highlight: Theme.Battle.Highlight.CurrentColor,
                Auras: BattleUnitState.Auras.None,
                Frozen: false),
            new BattleRenderState.Unit(
                Id: new BattleUnitId(3),
                DefinitionId: defenderDefinition.Id,
                Race: WorldRace.Human,
                DeathAnimation: UnitDeathAnimation.Human,
                Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                Facing: BattleSide.Defender,
                IdleOffset: TimeSpan.FromTicks(666057),
                CorpseVariant: 1,
                Hitpoints: 40,
                MaxHitpoints: 40,
                Presence: BattleUnitState.Presence.Alive,
                Marker: null,
                Highlight: null,
                Auras: BattleUnitState.Auras.None,
                Frozen: false)
        ]));
        Assert.That(selectedState.Units, Is.EqualTo([
            new BattleRenderState.Unit(
                Id: new BattleUnitId(2),
                DefinitionId: attackerDefinition.Id,
                Race: WorldRace.Human,
                DeathAnimation: UnitDeathAnimation.Human,
                Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.BackCenter, IsLarge: false),
                Facing: BattleSide.Attacker,
                IdleOffset: TimeSpan.FromTicks(6524244),
                CorpseVariant: 0,
                Hitpoints: 0,
                MaxHitpoints: 60,
                Presence: BattleUnitState.Presence.Dead,
                Marker: Theme.Battle.GroundMarker.AllyTargetColor,
                Highlight: Theme.Battle.Highlight.AllyTargetColor,
                Auras: BattleUnitState.Auras.None,
                Frozen: false),
            new BattleRenderState.Unit(
                Id: new BattleUnitId(1),
                DefinitionId: attackerDefinition.Id,
                Race: WorldRace.Human,
                DeathAnimation: UnitDeathAnimation.Human,
                Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                Facing: BattleSide.Attacker,
                IdleOffset: TimeSpan.FromTicks(7676067),
                CorpseVariant: 1,
                Hitpoints: 60,
                MaxHitpoints: 60,
                Presence: BattleUnitState.Presence.Alive,
                Marker: Theme.Battle.GroundMarker.CurrentColor,
                Highlight: Theme.Battle.Highlight.CurrentColor,
                Auras: BattleUnitState.Auras.None,
                Frozen: false),
            new BattleRenderState.Unit(
                Id: new BattleUnitId(3),
                DefinitionId: defenderDefinition.Id,
                Race: WorldRace.Human,
                DeathAnimation: UnitDeathAnimation.Human,
                Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                Facing: BattleSide.Defender,
                IdleOffset: TimeSpan.FromTicks(666057),
                CorpseVariant: 1,
                Hitpoints: 40,
                MaxHitpoints: 40,
                Presence: BattleUnitState.Presence.Alive,
                Marker: null,
                Highlight: null,
                Auras: BattleUnitState.Auras.None,
                Frozen: false)
        ]));
    }

    [Test]
    public void RetreatingUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = new(
            Id: new BattleUnitId(1),
            WorldUnitId: null,
            DefinitionId: attackerDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 60,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [new BattleRetreatStatusEffect()],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void ParalyzedUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = new(
            Id: new BattleUnitId(1),
            WorldUnitId: null,
            DefinitionId: attackerDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 60,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [new BattleParalyzeStatusEffect(RemainingTurns: 2)],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: true),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void PetrifiedUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = new(
            Id: new BattleUnitId(1),
            WorldUnitId: null,
            DefinitionId: attackerDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 60,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [new BattlePetrifyStatusEffect(RemainingTurns: 2)],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 8, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: true),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void RetreatedUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = new(
            Id: new BattleUnitId(1),
            WorldUnitId: null,
            DefinitionId: attackerDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 60,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [new BattleRetreatedStatusEffect()],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(2), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Absent,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void AuraHitboxTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 60);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder().Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetAnimationMetadata auraMetadata = new(FrameCount: 1, Width: 140, Height: 180, OffsetX: 355, OffsetY: 400, TriggerFrame: null, SecondTriggerFrame: null);
        AssetAnimationMetadata standardBody = new(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnitAura(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), auraMetadata)
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), standardBody)
            .Build();

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        Assert.That(renderState.HitTargets, Is.EqualTo([
            new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(381.141, 222.178, 144.48, 185.76))
        ]));
    }

    [Test]
    public void DecidedOutcomeTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: new BattleActionTarget.Unit([new BattleUnitId(2)]),
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter)] = new BattleActionTarget.Unit([new BattleUnitId(2)])
            },
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 30,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(15.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void LargeUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            IsLarge = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: true)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: true),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 100,
                    MaxHitpoints: 100,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(367.721, 225.554, 81.6, 122.4)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void UnitSortingTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 60);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontBottom, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontTop, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontBottom, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7529470),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontTop, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(949347),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), new Rect(259.281, 305.338, 84.16, 126.24)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop), new Rect(618.421, 248.19, 82.72, 124.08))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void DeselectedEnemyTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 60);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void ReachAllTargetsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 60);

        BattleUnit firstDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        BattleUnit secondDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(firstDefender, GridPosition.FrontTop, isLarge: false)
            .Place(secondDefender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: new BattleActionTarget.Unit([new BattleUnitId(2), new BattleUnitId(3)]),
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop)] = new BattleActionTarget.Unit([new BattleUnitId(2), new BattleUnitId(3)]),
                [new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter)] = new BattleActionTarget.Unit([new BattleUnitId(2), new BattleUnitId(3)])
            },
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontTop, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(949347),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.EnemyTargetColor,
                    Highlight: Theme.Battle.Highlight.EnemyTargetColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(3),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 1,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.EnemyTargetColor,
                    Highlight: Theme.Battle.Highlight.EnemyTargetColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop), new Rect(618.421, 248.19, 82.72, 124.08)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void ReachAllHealTargetTest()
    {
        UnitDefinition healerDefinition = TestHelpers.SimpleUnitDefinition("HEAL", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ALY", maxHitpoints: 50, initiative: 50, damage: 10);

        BattleUnit healer = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, healerDefinition.Id, BattleSide.Attacker, hitpoints: 30);

        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(healer, GridPosition.FrontCenter, isLarge: false)
            .Place(ally, GridPosition.FrontBottom, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder().Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(healerDefinition)
            .AddUnit(allyDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ALY"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("HEAL"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: new BattleActionTarget.Unit([new BattleUnitId(1), new BattleUnitId(3)]),
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom)] = new BattleActionTarget.Unit([new BattleUnitId(1), new BattleUnitId(3)])
            },
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: healerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 30,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.AllyTargetColor,
                    Highlight: Theme.Battle.Highlight.AllyTargetColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(3),
                    DefinitionId: allyDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontBottom, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7529470),
                    CorpseVariant: 1,
                    Hitpoints: 20,
                    MaxHitpoints: 50,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.AllyTargetColor,
                    Highlight: Theme.Battle.Highlight.AllyTargetColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), new Rect(259.281, 305.338, 84.16, 126.24))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void FriendlyTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ALY", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);

        BattleUnit selectedAlly = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 20);

        BattleUnit reachableAlly = TestHelpers.SimpleBattleUnit(new BattleUnitId(4), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 30);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Place(selectedAlly, GridPosition.FrontBottom, isLarge: false)
            .Place(reachableAlly, GridPosition.BackBottom, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(allyDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ALY"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: new BattleActionTarget.Unit([new BattleUnitId(3)]),
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom)] = new BattleActionTarget.Unit([new BattleUnitId(3)]),
                [new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom)] = new BattleActionTarget.Unit([new BattleUnitId(4)])
            },
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 30,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(4),
                    DefinitionId: allyDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.BackBottom, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(8987715),
                    CorpseVariant: 0,
                    Hitpoints: 30,
                    MaxHitpoints: 50,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.AllyReachableColor,
                    Highlight: Theme.Battle.Highlight.AllyReachableColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(3),
                    DefinitionId: allyDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontBottom, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7529470),
                    CorpseVariant: 1,
                    Hitpoints: 20,
                    MaxHitpoints: 50,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.AllyTargetColor,
                    Highlight: Theme.Battle.Highlight.AllyTargetColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), new Rect(154.431, 226.638, 82.16, 123.24)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), new Rect(259.281, 305.338, 84.16, 126.24)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void SummonTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        BattleUnit deadAlly = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 0);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(summoner, GridPosition.FrontCenter, isLarge: false)
            .Place(deadAlly, GridPosition.FrontTop, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteMetadata corpseMetadata = new(Width: 44, Height: 30, PixelWidth: 44, PixelHeight: 30, OffsetX: -22, OffsetY: -20, Additive: false);
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 1), corpseMetadata)
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: new BattleActionTarget.Summon([new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter)]),
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop)] = new BattleActionTarget.Summon([new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop)]),
                [new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter)] = new BattleActionTarget.Summon([new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter)])
            },
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(3),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(798133),
                    CorpseVariant: 1,
                    Hitpoints: 0,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Dead,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: attackerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 30,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders:
            [
                new BattleRenderState.Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), Theme.Battle.GroundMarker.SummonReachableColor),
                new BattleRenderState.Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter), Theme.Battle.GroundMarker.SummonTargetColor)
            ],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), new Rect(520.411, 233.566, 44.66, 30.45)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), new Rect(502.141, 178.756, 81.2, 81.2)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter), new Rect(282.421, 158.374, 80.64, 80.64))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void AbsentUnitsTest()
    {
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("IMP", maxHitpoints: 25, initiative: 30, damage: 5);

        BattleUnit summonerUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit enemyUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 40);
        BattleUnit summonUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, summonDefinition.Id, BattleSide.Attacker, hitpoints: 25);

        UnitGrid<BattleUnit> steadyAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(summonerUnit, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> resolvedAttackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(summonerUnit, GridPosition.FrontCenter, isLarge: false)
            .Place(summonUnit, GridPosition.FrontTop, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(enemyUnit, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState baseState = new(
            steadyAttackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        BattleState currentState = new(
            resolvedAttackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(3), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(summonerDefinition)
            .AddUnit(enemyDefinition)
            .AddUnit(summonDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleRenderState renderState = new BattleRenderStateBuilder(baseState, currentState, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background: background, environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(3),
                    DefinitionId: summonDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontTop, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(798133),
                    CorpseVariant: 1,
                    Hitpoints: 25,
                    MaxHitpoints: 25,
                    Presence: BattleUnitState.Presence.Absent,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: summonerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: enemyDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void IdleOffsetCarryTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attackerDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ATK"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        BattleRenderState first = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleAnimationState animation = new(
            new AnimationSequence(TimeSpan.FromSeconds(2), Clips: [], Sounds: [], Properties: [], Pauses: []),
            [
                new BattleAnimationState.Unit(
                    new BattleUnitId(2),
                    [
                        new BattleAnimationState.BodyEntry(
                            TimeSpan.Zero,
                            TimeSpan.FromSeconds(1.2),
                            AssetAnimationKey.BattleUnitPose.Hit,
                            BattleUnitState.Presence.Alive,
                            Opacity: null),
                        new BattleAnimationState.BodyEntry(
                            TimeSpan.FromSeconds(1.2),
                            TimeSpan.Zero,
                            AssetAnimationKey.BattleUnitPose.Idle,
                            BattleUnitState.Presence.Alive,
                            Opacity: null)
                    ],
                    Form: [],
                    Feedback: [],
                    Motion: [],
                    Status: [])
            ],
            Effects: [],
            Animations: [],
            Shakes: [],
            Rate: new AnimationRate(15));

        BattleRenderState completed = first.WithFinishedAnimation(animation, TimeSpan.FromSeconds(100));

        BattleRenderState carried = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: completed).BuildState();

        Assert.That(FormatIdleOffsets(first), Is.EqualTo([
            "unit 1 idle offset 7676067",
            "unit 2 idle offset 666057"
        ]));

        Assert.That(FormatIdleOffsets(completed), Is.EqualTo([
            "unit 1 idle offset 7676067",
            "unit 2 idle offset -992000000"
        ]));

        Assert.That(FormatIdleOffsets(carried), Is.EqualTo([
            "unit 1 idle offset 7676067",
            "unit 2 idle offset -992000000"
        ]));
    }

    [Test]
    public void RainTest()
    {
        BattleState state = new(
            UnitGrid<BattleUnit>.Empty,
            UnitGrid<BattleUnit>.Empty,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder().Build();
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Undead), 0);

        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background, new BattleEnvironment(BattleTimeOfDay.Midday, Rain: true, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            [BattleFluidConfiguration.MakeFog()],
            BattleRainConfiguration.MakeDefault(),
            new AnimationRate(30.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void MandatoryRainTest()
    {
        BattleState state = new(
            UnitGrid<BattleUnit>.Empty,
            UnitGrid<BattleUnit>.Empty,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder().Build();
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.City(), 3);

        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background, new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday(
            [
                BattleLight.MakeLantern(new Point(-136.0, 58.0), range: 60.0, power: 2.03),
                BattleLight.MakeLantern(new Point(1089.0, 123.0), range: 122.0, power: 1.90),
                BattleLight.MakeLantern(new Point(429.0, 91.0), range: 173.0, power: 2.05),
                BattleLight.MakeLantern(new Point(617.0, 79.0), range: 178.0, power: 1.99)
            ]),
            [BattleFluidConfiguration.MakeFog()],
            BattleRainConfiguration.MakeDefault(),
            new AnimationRate(30.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void FluidTest()
    {
        BattleState state = new(
            UnitGrid<BattleUnit>.Empty,
            UnitGrid<BattleUnit>.Empty,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder().Build();
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Capital(WorldRace.Elf), 0);

        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background, new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday(
            [
                BattleLight.MakeGlowingWater(new Point(222.0, 171.0), range: 102.0, power: 2.26),
                BattleLight.MakeGlowingWater(new Point(849.0, 174.0), range: 181.0, power: 3.00),
                BattleLight.MakeGlowingWater(new Point(69.0, 164.0), range: 120.0, power: 2.15),
                BattleLight.MakeGlowingWater(new Point(657.0, 167.0), range: 78.0, power: 2.04)
            ]),
            [BattleFluidConfiguration.MakeWater()],
            RainConfiguration: null,
            new AnimationRate(15.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void TimeOfDayTest()
    {
        BattleState state = new(
            UnitGrid<BattleUnit>.Empty,
            UnitGrid<BattleUnit>.Empty,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder().Build();
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Capital(WorldRace.Human), 0);

        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background, new BattleEnvironment(BattleTimeOfDay.Dusk, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeDusk(
            [
                BattleLight.MakeWindow(new Point(397.0, 2.0), range: 54.0, power: 1.48),
                BattleLight.MakeWindow(new Point(736.0, 10.0), range: 176.0, power: 1.87)
            ]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(15.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void EnvironmentTest()
    {
        BattleState state = new(
            UnitGrid<BattleUnit>.Empty,
            UnitGrid<BattleUnit>.Empty,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder().Build();
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Undead), 0);

        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background, new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            [BattleFluidConfiguration.MakeFog()],
            RainConfiguration: null,
            new AnimationRate(15.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void StormTest()
    {
        BattleState state = new(
            UnitGrid<BattleUnit>.Empty,
            UnitGrid<BattleUnit>.Empty,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder().Build();
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Undead), 0);

        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background, new BattleEnvironment(BattleTimeOfDay.Midday, Rain: true, Storm: true),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]) with { Storm = BattleStormConfiguration.MakeDefault() },
            [BattleFluidConfiguration.MakeFog()],
            BattleRainConfiguration.MakeDefault(),
            new AnimationRate(30.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void StormlessRainTest()
    {
        BattleState state = new(
            UnitGrid<BattleUnit>.Empty,
            UnitGrid<BattleUnit>.Empty,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder().Build();
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Undead), 0);

        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata, background, new BattleEnvironment(BattleTimeOfDay.Dusk, Rain: true, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeDusk([]),
            [BattleFluidConfiguration.MakeFog()],
            BattleRainConfiguration.MakeDefault(),
            new AnimationRate(30.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void DoppelgangerTargetsTest()
    {
        UnitDefinition doppelgangerDefinition = TestHelpers.SimpleUnitDefinition("DOP", maxHitpoints: 60, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("DOP"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Doppelganger
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ALY", maxHitpoints: 50, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit doppelganger = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, doppelgangerDefinition.Id, BattleSide.Attacker, hitpoints: 60);

        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);

        BattleUnit copyableDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        BattleUnit uncopyableDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(4), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(doppelganger, GridPosition.FrontCenter, isLarge: false)
            .Place(ally, GridPosition.FrontBottom, isLarge: false)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(copyableDefender, GridPosition.FrontCenter, isLarge: false)
            .Place(uncopyableDefender, GridPosition.FrontBottom, isLarge: false)
            .Build();

        BattleState state = new(
            attackerGrid,
            defenderGrid,
            new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Doppelganger),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Opening(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(doppelgangerDefinition)
            .AddUnit(allyDefinition)
            .AddUnit(defenderDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DOP"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ALY"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("DEF"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleRenderState renderState = new BattleRenderStateBuilder(
            state, state, stats, definitions, TestGameTextProvider.Instance, assetMetadata,
            background: background,
            environment: new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom)] = new BattleActionTarget.Unit([new BattleUnitId(2)]),
                [new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter)] = new BattleActionTarget.Unit([new BattleUnitId(3)])
            },
            previousState: null).BuildState();

        BattleRenderState expected = new(
            background,
            [
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(1),
                    DefinitionId: doppelgangerDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7676067),
                    CorpseVariant: 1,
                    Hitpoints: 60,
                    MaxHitpoints: 60,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.CurrentColor,
                    Highlight: Theme.Battle.Highlight.CurrentColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(2),
                    DefinitionId: allyDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Attacker, GridPosition.FrontBottom, IsLarge: false),
                    Facing: BattleSide.Attacker,
                    IdleOffset: TimeSpan.FromTicks(7529470),
                    CorpseVariant: 0,
                    Hitpoints: 50,
                    MaxHitpoints: 50,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.AllyReachableColor,
                    Highlight: Theme.Battle.Highlight.AllyReachableColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(3),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(666057),
                    CorpseVariant: 1,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: Theme.Battle.GroundMarker.EnemyReachableColor,
                    Highlight: Theme.Battle.Highlight.EnemyReachableColor,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false),
                new BattleRenderState.Unit(
                    Id: new BattleUnitId(4),
                    DefinitionId: defenderDefinition.Id,
                    Race: WorldRace.Human,
                    DeathAnimation: UnitDeathAnimation.Human,
                    Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontBottom, IsLarge: false),
                    Facing: BattleSide.Defender,
                    IdleOffset: TimeSpan.FromTicks(9504349),
                    CorpseVariant: 0,
                    Hitpoints: 40,
                    MaxHitpoints: 40,
                    Presence: BattleUnitState.Presence.Alive,
                    Marker: null,
                    Highlight: null,
                    Auras: BattleUnitState.Auras.None,
                    Frozen: false)
            ],
            Placeholders: [],
            HitTargets:
            [
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(401.781, 242.818, 82.56, 123.84)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), new Rect(259.281, 305.338, 84.16, 126.24)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter), new Rect(496.791, 313.946, 84.4, 126.6)),
                new BattleRenderState.HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom), new Rect(353.191, 377.566, 86.0, 129.0))
            ],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            new AnimationRate(60.0));

        Assert.That(renderState, Is.EqualTo(expected));
    }
}
