using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class BattleGridViewModelTests
{
    [Test]
    public void UpdateTest()
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

        BattleRenderStateBuilder builder = new(
            state,
            state,
            stats,
            definitions,
            NullAssetProvider.Instance,
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: new BattleActionTarget.Unit([new BattleUnitId(2)]),
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter)] = new BattleActionTarget.Unit([new BattleUnitId(2)])
            },
            previousState: null);

        BattleGridViewModel attackerGridViewModel = new();
        attackerGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Attacker, isFacingRight: true));

        Assert.That(attackerGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(1),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(attackerDefinition.Id),
                row: 1,
                column: 1,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: [])
        ]));

        BattleGridViewModel defenderGridViewModel = new();
        defenderGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Defender, isFacingRight: false));

        Assert.That(defenderGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(2),
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(defenderDefinition.Id),
                row: 1,
                column: 0,
                columnSpan: 1,
                hitpoints: 40,
                maxHitpoints: 40,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: Theme.Battle.GridMarker.EnemyTargetColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: [])
        ]));
    }

    [Test]
    public void ReachableTargetTest()
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

        BattleRenderStateBuilder builder = new(
            state,
            state,
            stats,
            definitions,
            NullAssetProvider.Instance,
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>
            {
                [new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter)] = new BattleActionTarget.Unit([new BattleUnitId(2)])
            },
            previousState: null);

        BattleGridViewModel defenderGridViewModel = new();
        defenderGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Defender, isFacingRight: false));

        Assert.That(defenderGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(2),
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(defenderDefinition.Id),
                row: 1,
                column: 0,
                columnSpan: 1,
                hitpoints: 40,
                maxHitpoints: 40,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: Theme.Battle.GridMarker.EnemyReachableColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: [])
        ]));
    }

    [Test]
    public void LargeUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10) with
        {
            IsLarge = true
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10) with
        {
            IsLarge = true
        };

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 30);

        BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 40);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(attacker, GridPosition.FrontCenter, isLarge: true)
            .Build();

        UnitGrid<BattleUnit> defenderGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(defender, GridPosition.FrontCenter, isLarge: true)
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

        BattleRenderStateBuilder builder = new(
            state,
            state,
            stats,
            definitions,
            NullAssetProvider.Instance,
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null);

        BattleGridViewModel attackerGridViewModel = new();
        attackerGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Attacker, isFacingRight: true));

        Assert.That(attackerGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(1),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(attackerDefinition.Id),
                row: 1,
                column: 0,
                columnSpan: 2,
                hitpoints: 30,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: [])
        ]));

        BattleGridViewModel defenderGridViewModel = new();
        defenderGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Defender, isFacingRight: false));

        Assert.That(defenderGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(2),
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(defenderDefinition.Id),
                row: 1,
                column: 0,
                columnSpan: 2,
                hitpoints: 40,
                maxHitpoints: 40,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: [])
        ]));
    }

    [Test]
    public void StatusEffectsTest()
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
            StatusEffects: [new BattleDefendStatusEffect(), new BattleCureStatusEffect(), new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5)],
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

        BattleRenderStateBuilder builder = new(
            state,
            state,
            stats,
            definitions,
            NullAssetProvider.Instance,
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null);

        BattleGridViewModel attackerGridViewModel = new();
        attackerGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Attacker, isFacingRight: true));

        Assert.That(attackerGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(1),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(attackerDefinition.Id),
                row: 1,
                column: 1,
                columnSpan: 1,
                hitpoints: 60,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                marker: null,
                feedback: [],
                statuses:
                [
                    new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Poison)),
                    new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Defend))
                ],
                hitpointsAnimation: [])
        ]));
    }

    [Test]
    public void PresenceTest()
    {
        UnitDefinition retreatedDefinition = TestHelpers.SimpleUnitDefinition("RET", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition deadDefinition = TestHelpers.SimpleUnitDefinition("DED", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit retreated = new(
            Id: new BattleUnitId(1),
            WorldUnitId: null,
            DefinitionId: retreatedDefinition.Id,
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

        BattleUnit dead = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, deadDefinition.Id, BattleSide.Attacker, hitpoints: 0);

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(retreated, GridPosition.FrontBottom, isLarge: false)
            .Place(dead, GridPosition.FrontTop, isLarge: false)
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
            .AddUnit(retreatedDefinition)
            .AddUnit(deadDefinition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        BattleRenderStateBuilder builder = new(
            state,
            state,
            stats,
            definitions,
            NullAssetProvider.Instance,
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null);

        BattleGridViewModel attackerGridViewModel = new();
        attackerGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Attacker, isFacingRight: true));

        Assert.That(attackerGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(1),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(retreatedDefinition.Id),
                row: 2,
                column: 1,
                columnSpan: 1,
                hitpoints: 60,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Absent,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [],
                statuses:
                [
                    new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Retreated))
                ],
                hitpointsAnimation: []),
            new BattleGridUnitViewModel(
                new BattleUnitId(2),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop),
                new AssetSpriteKey.UnitFace(deadDefinition.Id),
                row: 0,
                column: 1,
                columnSpan: 1,
                hitpoints: 0,
                maxHitpoints: 40,
                presence: BattleUnitState.Presence.Dead,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: [])
        ]));
    }

    [Test]
    public void UpdateNoChangeTest()
    {
        BattleGridViewModel grid = new();

        BattleGridUnitViewModel first = new(
            new BattleUnitId(1),
            new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop),
            new AssetSpriteKey.UnitFace(new UnitDefinitionId("A")),
            row: 0,
            column: 0,
            columnSpan: 1,
            hitpoints: 40,
            maxHitpoints: 40,
            presence: BattleUnitState.Presence.Alive,
            isPendingUpgrade: false,
            isCurrent: false,
            marker: null,
            feedback: [],
            statuses: [],
            hitpointsAnimation: []);

        BattleGridUnitViewModel second = new(
            new BattleUnitId(2),
            new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter),
            new AssetSpriteKey.UnitFace(new UnitDefinitionId("B")),
            row: 1,
            column: 0,
            columnSpan: 1,
            hitpoints: 40,
            maxHitpoints: 40,
            presence: BattleUnitState.Presence.Alive,
            isPendingUpgrade: false,
            isCurrent: false,
            marker: null,
            feedback: [],
            statuses: [],
            hitpointsAnimation: []);

        BattleGridUnitViewModel equalFirst = new(
            new BattleUnitId(1),
            new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop),
            new AssetSpriteKey.UnitFace(new UnitDefinitionId("A")),
            row: 0,
            column: 0,
            columnSpan: 1,
            hitpoints: 40,
            maxHitpoints: 40,
            presence: BattleUnitState.Presence.Alive,
            isPendingUpgrade: false,
            isCurrent: false,
            marker: null,
            feedback: [],
            statuses: [],
            hitpointsAnimation: []);

        BattleGridUnitViewModel changedSecond = new(
            new BattleUnitId(2),
            new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter),
            new AssetSpriteKey.UnitFace(new UnitDefinitionId("B")),
            row: 1,
            column: 0,
            columnSpan: 1,
            hitpoints: 40,
            maxHitpoints: 40,
            presence: BattleUnitState.Presence.Alive,
            isPendingUpgrade: false,
            isCurrent: false,
            marker: Theme.Battle.GridMarker.EnemyTargetColor,
            feedback: [],
            statuses: [],
            hitpointsAnimation: []);

        grid.UpdateUnits([first, second]);
        grid.UpdateUnits([equalFirst, changedSecond]);

        Assert.That(grid.Units, Is.EqualTo([equalFirst, changedSecond]));
        Assert.That(grid.Units, Is.EqualTo([first, second]).Using<BattleGridUnitViewModel>(ReferenceEquals));
    }

    [Test]
    public void UpdateRemoveTest()
    {
        BattleGridViewModel grid = new();

        BattleGridUnitViewModel first = new(
            new BattleUnitId(1),
            new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop),
            new AssetSpriteKey.UnitFace(new UnitDefinitionId("A")),
            row: 0,
            column: 0,
            columnSpan: 1,
            hitpoints: 40,
            maxHitpoints: 40,
            presence: BattleUnitState.Presence.Alive,
            isPendingUpgrade: false,
            isCurrent: false,
            marker: null,
            feedback: [],
            statuses: [],
            hitpointsAnimation: []);

        BattleGridUnitViewModel second = new(
            new BattleUnitId(2),
            new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter),
            new AssetSpriteKey.UnitFace(new UnitDefinitionId("B")),
            row: 1,
            column: 0,
            columnSpan: 1,
            hitpoints: 40,
            maxHitpoints: 40,
            presence: BattleUnitState.Presence.Alive,
            isPendingUpgrade: false,
            isCurrent: false,
            marker: null,
            feedback: [],
            statuses: [],
            hitpointsAnimation: []);

        grid.UpdateUnits([first, second]);
        grid.UpdateUnits([first]);

        Assert.That(grid.Units, Is.EqualTo([first]));
    }

    [Test]
    public void OverlayTest()
    {
        BattleGridUnitViewModel unit = new(
            new BattleUnitId(1),
            new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
            new AssetSpriteKey.UnitFace(new UnitDefinitionId("A")),
            row: 1,
            column: 1,
            columnSpan: 1,
            hitpoints: 60,
            maxHitpoints: 60,
            presence: BattleUnitState.Presence.Alive,
            isPendingUpgrade: false,
            isCurrent: false,
            marker: null,
            feedback: [],
            statuses:
            [
                new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.IncreaseDamage)),
                new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Poison)),
                new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Paralyze))
            ],
            hitpointsAnimation: []);

        Assert.That(unit.Overlay, Is.EqualTo(Theme.Battle.StatusEffect.PoisonOverlayColor));
    }

    [Test]
    public void AiTurnTest()
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

        BattleRenderStateBuilder builder = new(
            state,
            state,
            stats,
            definitions,
            NullAssetProvider.Instance,
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: true,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null);

        BattleGridViewModel attackerGridViewModel = new();
        attackerGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Attacker, isFacingRight: true));

        Assert.That(attackerGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(1),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(attackerDefinition.Id),
                row: 1,
                column: 1,
                columnSpan: 1,
                hitpoints: 60,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: [])
        ]));
    }

    [Test]
    public void ExperienceFeedbackTest()
    {
        UnitDefinition definition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);

        BattleUnit survivor = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, definition.Id, BattleSide.Attacker, hitpoints: 60) with
        {
            ExperienceGained = 40
        };

        BattleUnit idle = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, definition.Id, BattleSide.Attacker, hitpoints: 60);

        BattleUnit dead = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, definition.Id, BattleSide.Attacker, hitpoints: 0) with
        {
            ExperienceGained = 25
        };

        BattleUnit retreated = TestHelpers.SimpleBattleUnit(new BattleUnitId(4), null, definition.Id, BattleSide.Attacker, hitpoints: 60) with
        {
            ExperienceGained = 15,
            StatusEffects = [new BattleRetreatedStatusEffect()]
        };

        UnitGrid<BattleUnit> attackerGrid = new UnitGrid<BattleUnit>.Builder()
            .Place(survivor, GridPosition.FrontCenter, isLarge: false)
            .Place(idle, GridPosition.FrontTop, isLarge: false)
            .Place(dead, GridPosition.FrontBottom, isLarge: false)
            .Place(retreated, GridPosition.BackCenter, isLarge: false)
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
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(definition)
            .Build();
        IUnitStatsProvider stats = new UnitStatsProvider(definitions, new GlobalVariables(), GameScenarioProvider.Empty.Definition);

        BattleRenderStateBuilder builder = new(
            state,
            state,
            stats,
            definitions,
            NullAssetProvider.Instance,
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null);

        BattleGridViewModel attackerGridViewModel = new();
        attackerGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Attacker, isFacingRight: true));

        Assert.That(attackerGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(1),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(definition.Id),
                row: 1,
                column: 1,
                columnSpan: 1,
                hitpoints: 60,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [new BattleFeedbackViewModel(BattleUnitState.FeedbackType.Heal, "+40 XP", Animation: [])],
                statuses: [],
                hitpointsAnimation: []),
            new BattleGridUnitViewModel(
                new BattleUnitId(2),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop),
                new AssetSpriteKey.UnitFace(definition.Id),
                row: 0,
                column: 1,
                columnSpan: 1,
                hitpoints: 60,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: []),
            new BattleGridUnitViewModel(
                new BattleUnitId(3),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(definition.Id),
                row: 2,
                column: 1,
                columnSpan: 1,
                hitpoints: 0,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Dead,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: []),
            new BattleGridUnitViewModel(
                new BattleUnitId(4),
                new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter),
                new AssetSpriteKey.UnitFace(definition.Id),
                row: 1,
                column: 0,
                columnSpan: 1,
                hitpoints: 60,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Absent,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [],
                statuses:
                [
                    new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Retreated))
                ],
                hitpointsAnimation: [])
        ]));
    }

    [Test]
    public void UndecidedExperienceFeedbackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("ATK", maxHitpoints: 60, initiative: 50, damage: 10);
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("DEF", maxHitpoints: 40, initiative: 50, damage: 10);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 60) with
        {
            ExperienceGained = 40
        };

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

        BattleRenderStateBuilder builder = new(
            state,
            state,
            stats,
            definitions,
            NullAssetProvider.Instance,
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            new BattleEnvironment(BattleTimeOfDay.Midday, Rain: false, Storm: false),
            isAiTurn: false,
            selectedTarget: null,
            actionTargets: new Dictionary<BattleGridTile, BattleActionTarget>(),
            previousState: null);

        BattleGridViewModel attackerGridViewModel = new();
        attackerGridViewModel.UpdateUnits(builder.BuildUnits(BattleSide.Attacker, isFacingRight: true));

        Assert.That(attackerGridViewModel.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                new BattleUnitId(1),
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(attackerDefinition.Id),
                row: 1,
                column: 1,
                columnSpan: 1,
                hitpoints: 60,
                maxHitpoints: 60,
                presence: BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: [])
        ]));
    }
}
