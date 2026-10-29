using System.Globalization;
using Avalonia;
using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;
using SkiaSharp;
using static Gallean.Client.BattleRenderState;

namespace Gallean.Client.Tests;

public sealed class BattleScreenViewModelTests
{
    private sealed class TestAssetPreloader : IAssetPreloader
    {
        private sealed class Leases(List<string> actions) : IDisposable
        {
            public void Dispose()
            {
                actions.Add("Release");
            }
        }

        private readonly List<TaskCompletionSource<IDisposable>> pending = [];

        public List<IReadOnlyList<AssetPreloadItem>> Preloads { get; } = [];

        public List<string> Actions { get; } = [];

        public Task<IDisposable> PreloadAsync(IEnumerable<AssetPreloadItem> items)
        {
            TaskCompletionSource<IDisposable> completion = new();

            Preloads.Add([.. items]);
            pending.Add(completion);

            return completion.Task;
        }

        public void CompletePreload()
        {
            pending[^1].SetResult(new Leases(Actions));
        }
    }

    private static IReadOnlyList<string> FormatItems(IReadOnlyList<AssetPreloadItem> items)
    {
        List<string> rows = [];

        foreach (AssetPreloadItem item in items)
        {
            switch (item)
            {
                case AssetPreloadItem.Sprite sprite:
                    rows.Add($"sprite {sprite.Key.CanonicalId}");
                    break;

                case AssetPreloadItem.Animation animation:
                    rows.Add($"animation {animation.Key.CanonicalId}");
                    break;

                case AssetPreloadItem.Sound sound:
                    rows.Add($"sound {sound.Key.CanonicalId}");
                    break;
            }
        }

        return rows;
    }

    private static string FormatFrames(TimeSpan time, double framesPerSecond)
    {
        return (time.TotalSeconds * framesPerSecond).ToString("0.###", CultureInfo.InvariantCulture);
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

    private static string FormatTarget(AnimationTarget target)
    {
        return target switch
        {
            BattleAnimationTarget.UnitHitpoints hitpoints => $"unit {hitpoints.UnitId.Value} hitpoints",
            BattleAnimationTarget.UnitMaxHitpoints maxHitpoints => $"unit {maxHitpoints.UnitId.Value} max hitpoints",
            BattleAnimationTarget.UnitFeedback feedback => $"unit {feedback.UnitId.Value} feedback",
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
    }

    private static string FormatProperty(AnimationProperty property, double framesPerSecond)
    {
        string from = property.From.ToString(CultureInfo.InvariantCulture);
        string to = property.To.ToString(CultureInfo.InvariantCulture);
        string start = FormatFrames(property.Start, framesPerSecond);
        string duration = FormatFrames(property.Duration, framesPerSecond);

        return $"{FormatTarget(property.Target)} {from} -> {to} at {start} for {duration} frames {FormatEasing(property.Easing)}";
    }

    private static IReadOnlyList<string> FormatProperties(IReadOnlyList<AnimationProperty> properties, double framesPerSecond)
    {
        List<string> rows = [];

        foreach (AnimationProperty property in properties)
        {
            rows.Add(FormatProperty(property, framesPerSecond));
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatRange(AnimationRange range, double framesPerSecond)
    {
        return [.. FormatProperties(range.Current, framesPerSecond), .. FormatProperties(range.Maximum, framesPerSecond)];
    }

    private static IReadOnlyList<string> FormatGrid(BattleGridViewModel grid, double framesPerSecond)
    {
        List<string> rows = [];

        foreach (BattleGridUnitViewModel unit in grid.Units)
        {
            rows.Add($"unit {unit.UnitId.Value} {unit.FaceKey.CanonicalId} at {unit.Row},{unit.Column} span {unit.ColumnSpan} hitpoints {unit.Hitpoints}/{unit.MaxHitpoints} {unit.Presence} current {unit.IsCurrent} {unit.Marker}");

            foreach (UnitStatusViewModel status in unit.Statuses)
            {
                rows.Add($"unit {unit.UnitId.Value} status {status.Name}");
            }

            foreach (BattleFeedbackViewModel feedback in unit.Feedback)
            {
                rows.Add($"unit {unit.UnitId.Value} feedback {feedback.Type} {feedback.Text}");
                rows.AddRange(FormatProperties(feedback.Animation, framesPerSecond));
            }

            rows.AddRange(FormatRange(unit.HitpointsAnimation, framesPerSecond));
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatIdleOffsets(BattleRenderState state)
    {
        List<string> result = [];

        foreach (Unit unit in state.Units)
        {
            result.Add($"unit {unit.Id.Value} idle offset {unit.IdleOffset.Ticks}");
        }

        return result;
    }

    [Test]
    public async Task UseHealItemTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Healing Potion"),
            new GlobalTextId("Heals 30 hit points"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(30),
            AnimationUnitId: null);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(leaderDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId leaderId = new(1);
        BattleUnitId allyId = new(2);
        BattleUnitId enemyId = new(3);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(leaderId, null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(allyId, null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(leader)
            .Add(ally)
            .Add(enemy)
            .AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("ally"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("leader"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Potion), new AssetAnimationMetadata(FrameCount: 31, Width: 111, Height: 348, OffsetX: 366, OffsetY: 246, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(leaderId));
        Assert.That(viewModel.Items.Select(item => (item.Name, item.Slot, item.IsActive, item.SelectCommand.CanExecute(null))),
            Is.EqualTo([("Healing Potion", LeaderEquipmentSlot.Battle1, false, true)]));

        viewModel.SelectItem(LeaderEquipmentSlot.Battle1);

        Assert.That(viewModel.Items.Select(item => item.IsActive), Is.EqualTo([true]));
        Assert.That(viewModel.AttackerGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                leaderId,
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("leader")),
                facing: BattleSide.Attacker,
                row: 2,
                column: 1,
                columnSpan: 1,
                hitpoints: 100,
                maxHitpoints: 100,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None),
            new BattleGridUnitViewModel(
                allyId,
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("ally")),
                facing: BattleSide.Attacker,
                row: 1,
                column: 1,
                columnSpan: 1,
                hitpoints: 20,
                maxHitpoints: 50,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                Theme.Battle.GridMarker.AllyReachableColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));
        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                enemyId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("enemy")),
                facing: BattleSide.Defender,
                row: 2,
                column: 0,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter));

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);

        BattleUnit healedAlly = gameModel.Snapshot.BattleState.GetUnit(allyId);

        Assert.That(healedAlly.Hitpoints, Is.EqualTo(50));
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(allyId));
        Assert.That(gameModel.Snapshot.BattleState.Items, Is.EqualTo([new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: true)]));

        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(viewModel.Items, Is.Empty);
    }

    [Test]
    public async Task NonLeaderTurnTest()
    {
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 50, damage: 5);
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Healing Potion"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(30),
            AnimationUnitId: null);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(soldierDefinition)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnit soldier = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, soldierDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit leader = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(soldier)
            .Add(leader)
            .Add(enemy)
            .AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("leader"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("soldier"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(new BattleUnitId(1)));
        Assert.That(viewModel.Items, Is.Empty);
    }

    [Test]
    public async Task HoverTargetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                enemyId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(enemyDefinition.Id),
                facing: BattleSide.Defender,
                row: 2,
                column: 0,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                Theme.Battle.GridMarker.EnemyReachableColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));

        Assert.That(viewModel.TargetUnit, Is.Null);
        Assert.That(viewModel.IsPlayerTurn, Is.True);
        Assert.That(viewModel.DefenderUnit, Is.Null);

        BattleUnitCardViewModel attackerCard = Assert.NotNull(viewModel.AttackerUnit);

        Assert.That(attackerCard.UnitId, Is.EqualTo(attackerId));
        Assert.That(attackerCard.Side, Is.EqualTo(BattleSide.Attacker));

        List<BattleGridUnitViewModel> defenderUnits = [.. viewModel.DefenderGrid.Units];

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo(defenderUnits).Using<BattleGridUnitViewModel>(ReferenceEquals));

        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                enemyId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(enemyDefinition.Id),
                facing: BattleSide.Defender,
                row: 2,
                column: 0,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                Theme.Battle.GridMarker.EnemyTargetColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));

        BattleUnitCardViewModel target = Assert.NotNull(viewModel.TargetUnit);

        Assert.That(target.UnitId, Is.EqualTo(enemyId));
        Assert.That(target.Side, Is.EqualTo(BattleSide.Defender));
        Assert.That(target.IconKey, Is.EqualTo(new AssetSpriteKey.UnitPortrait(enemyDefinition.Id)));
        Assert.That(target.Level, Is.EqualTo(1));
        Assert.That(target.PortraitKey, Is.EqualTo(new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(enemyDefinition.Id))));
        Assert.That(target.Name, Is.EqualTo("name"));
        Assert.That(target.Hitpoints, Is.EqualTo(30));
        Assert.That(target.MaxHitpoints, Is.EqualTo(30));
        Assert.That(target.HitpointsAnimation, Is.EqualTo(AnimationRange.None));
        Assert.That(target.Statuses, Is.Empty);
        Assert.That(viewModel.DefenderUnit, Is.Null);

        viewModel.Hover(null);

        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                enemyId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(enemyDefinition.Id),
                facing: BattleSide.Defender,
                row: 2,
                column: 0,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                Theme.Battle.GridMarker.EnemyReachableColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));

        Assert.That(viewModel.TargetUnit, Is.Null);
    }

    [Test]
    public async Task InspectUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = new(enemyId, null, enemyDefinition.Id, BattleSide.Defender, 24, 0, 0, 1, [], [new BattlePoisonStatusEffect(RemainingTurns: 2, Damage: 5)], null, [], null, false);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));

        Assert.That(viewModel.InspectedUnit, Is.Null);

        viewModel.ShowUnitDetails(enemyId);

        Assert.That(viewModel.InspectedUnit, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(enemyDefinition.Id)),
            "name",
            "description",
            1,
            24,
            30,
            0,
            1000,
            false,
            false,
            false,
            false,
            "Damage",
            new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, "Weapon", Theme.AttackSource.WeaponColor),
            new StatValueViewModel(TextSpans.FromText("5"), 0),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel(new TextSpans([new TextSpan("1", TextStyle.Bold), new TextSpan(" ", TextStyle.None), new TextSpan("(Ranged)", TextStyle.Italic)]), 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel(TextSpans.FromText("0"), 0)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel(TextSpans.FromText("100%"), 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel(TextSpans.FromText("30"), 0))
            ],
            null,
            null,
            null,
            [
                new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.Poison))
            ],
            [])));

        viewModel.HideUnitDetails();

        Assert.That(viewModel.InspectedUnit, Is.Null);
    }

    [Test]
    public async Task InspectBoostedUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = new(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, 100, 0, 0, 1, [], [new BattleIncreaseDamageStatusEffect(Percent: 50, IsInfinite: false)], null, [], null, false);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));

        viewModel.ShowUnitDetails(attackerId);

        Assert.That(viewModel.InspectedUnit, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(attackerDefinition.Id)),
            "name",
            "description",
            1,
            100,
            100,
            0,
            1000,
            false,
            false,
            false,
            false,
            "Damage",
            new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, "Weapon", Theme.AttackSource.WeaponColor),
            new StatValueViewModel(TextSpans.FromText("50"), 25),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel(new TextSpans([new TextSpan("1", TextStyle.Bold), new TextSpan(" ", TextStyle.None), new TextSpan("(Ranged)", TextStyle.Italic)]), 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel(TextSpans.FromText("0"), 0)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel(TextSpans.FromText("100%"), 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel(TextSpans.FromText("50"), 0))
            ],
            null,
            null,
            null,
            [
                new BattleStatusEffectViewModel(new BattleUnitState.StatusEffect(BattleUnitState.StatusEffectType.IncreaseDamage))
            ],
            [])));
    }

    [Test]
    public async Task OutcomeExperienceTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5) with
        {
            ExperienceKilled = 40
        };

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        TestBattleUnitUpgradeResolver resolver = new();
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                resolver,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.Outcome, Is.EqualTo(new BattleOutcome(BattleSide.Attacker)));
        viewModel.ShowUnitDetails(attackerId);

        Assert.That(viewModel.InspectedUnit, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(attackerDefinition.Id)),
            "name",
            "description",
            1,
            100,
            100,
            40,
            1000,
            false,
            false,
            false,
            false,
            "Damage",
            new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, "Weapon", Theme.AttackSource.WeaponColor),
            new StatValueViewModel(TextSpans.FromText("50"), 0),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel(new TextSpans([new TextSpan("1", TextStyle.Bold), new TextSpan(" ", TextStyle.None), new TextSpan("(Ranged)", TextStyle.Italic)]), 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel(TextSpans.FromText("0"), 0)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel(TextSpans.FromText("100%"), 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel(TextSpans.FromText("50"), 0))
            ],
            null,
            null,
            null,
            [],
            [])));
    }

    [Test]
    public async Task ExitBattleTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.Outcome, Is.EqualTo(new BattleOutcome(BattleSide.Attacker)));
        Assert.That(viewModel.ExitBattleCommand.CanExecute(null), Is.True);

        viewModel.ExitBattleCommand.Execute(null);

        Assert.That(viewModel.TransitionState, Is.EqualTo(new BattleWorldTransitionState(BattleTransitionType.Out)));
        Assert.That(viewModel.PaintsOwnBackground, Is.False);
        Assert.That(gameViewModel.TopScreen, Is.SameAs(viewModel));

        await viewModel.CompleteTransitionAsync();

        Assert.That(gameModel.Snapshot.BattleState, Is.Null);
        Assert.That(gameViewModel.TopScreen, Is.Null);
    }

    [Test]
    public async Task InputAfterExitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;
        viewModel.ExitBattleCommand.Execute(null);
        await viewModel.CompleteTransitionAsync();

        BattleRenderState renderState = viewModel.RenderState;

        viewModel.Hover(null);
        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));
        viewModel.SelectItem(LeaderEquipmentSlot.Battle1);
        viewModel.ShowUnitDetails(attackerId);
        viewModel.HideUnitDetails();

        Assert.That(gameModel.Snapshot.BattleState, Is.Null);
        Assert.That(viewModel.RenderState, Is.EqualTo(renderState));
        Assert.That(FormatGrid(viewModel.AttackerGrid, 1.0), Is.EqualTo([
            "unit 1 unit-face/attacker at 2,1 span 1 hitpoints 100/100 Alive current False ",
            "unit 1 feedback Heal +1 XP"
        ]));
        Assert.That(FormatGrid(viewModel.DefenderGrid, 1.0), Is.EqualTo([
            "unit 2 unit-face/enemy at 2,0 span 1 hitpoints 0/30 Dead current False "
        ]));
        Assert.That(viewModel.CurrentUnit, Is.Null);
        Assert.That(viewModel.IsBattleOver, Is.True);
        Assert.That(viewModel.StatusText, Is.EqualTo(string.Empty));
        Assert.That(viewModel.OutcomeTitle, Is.EqualTo("Victory"));
        Assert.That(viewModel.OutcomeText, Is.EqualTo(TextSpans.Empty));
        Assert.That(viewModel.OutcomeIcon, Is.EqualTo(IconMoniker.Damage));
    }

    [Test]
    public async Task BackgroundVariantTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(3, 3);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldPosition[] positions = [new(0, 0), new(1, 0), new(2, 0), new(0, 1)];
        List<string> backgrounds = [];

        foreach (WorldPosition position in positions)
        {
            (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
                .WithMap(map)
                .WithGlobals(globals)
                .WithDefinition(attackerDefinition)
                .WithDefinition(enemyDefinition)
                .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
                .WithPlayer(player)
                .BuildSession();

            GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

            BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
            BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

            BattleState battleState = new BattleStateBuilder(engine.Definitions)
                .Add(attacker)
                .Add(enemy)
                .Build(BattleModifiers.None);

            EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
            await gameModel.RunAsync(() =>
            {
                gameModel.Session.StartBattle(new BattleSession(
                    battleEngine,
                    battleState,
                    NullBattleUnitUpgradeResolver.Instance,
                    _ =>
                    {
                    },
                    isAttackerAi: false,
                    isDefenderAi: false,
                    canAutoResolve: true,
                    position));
            });
            Assert.That(gameModel.ActiveBattle, Is.Not.Null);

            BattleScreenViewModel viewModel = new(gameModel, gameModel.ActiveBattle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
            backgrounds.Add(viewModel.RenderState.Background.CanonicalId);
        }

        Assert.That(backgrounds, Is.EqualTo([
            "battle-background/terrain/neutral/0",
            "battle-background/terrain/neutral/1",
            "battle-background/terrain/neutral/2",
            "battle-background/terrain/neutral/1"
        ]));
    }

    [Test]
    public async Task BoatBackgroundTest()
    {
        UnitDefinition landDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("land", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 3);
        UnitDefinition seaDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("sea", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 3) with
            {
                IsWaterOnly = true
            };
        UnitDefinition flyingDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("flying", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 3) with
            {
                MoveAbility = UnitMoveAbility.Flying
            };

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1, '~');
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        TestMetadataProviderBuilder metadataBuilder = new();
        UnitDefinition[] definitions = [landDefinition, seaDefinition, flyingDefinition];

        foreach (UnitDefinition definition in definitions)
        {
            metadataBuilder
                .WithAnimation(new AssetAnimationKey.BattleUnit(definition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
                .WithAnimation(new AssetAnimationKey.BattleUnit(definition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null));
        }

        IAssetMetadataProvider assetMetadata = metadataBuilder.Build();

        (UnitDefinition Attacker, UnitDefinition Defender)[] matchups =
        [
            (landDefinition, seaDefinition),
            (seaDefinition, landDefinition),
            (seaDefinition, seaDefinition),
            (flyingDefinition, seaDefinition)
        ];
        List<string> backgrounds = [];

        foreach ((UnitDefinition attackerDefinition, UnitDefinition defenderDefinition) in matchups)
        {
            (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
                .WithMap(map)
                .WithGlobals(globals)
                .WithDefinition(landDefinition)
                .WithDefinition(seaDefinition)
                .WithDefinition(flyingDefinition)
                .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
                .WithPlayer(player)
                .BuildSession();

            GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

            BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
            BattleUnit defender = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, defenderDefinition.Id, BattleSide.Defender, hitpoints: 100);

            BattleState battleState = new BattleStateBuilder(engine.Definitions)
                .Add(attacker)
                .Add(defender)
                .Build(BattleModifiers.None);

            EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
            await gameModel.RunAsync(() =>
            {
                gameModel.Session.StartBattle(new BattleSession(
                    battleEngine,
                    battleState,
                    NullBattleUnitUpgradeResolver.Instance,
                    _ =>
                    {
                    },
                    isAttackerAi: false,
                    isDefenderAi: false,
                    canAutoResolve: true,
                    new WorldPosition(0, 0)));
            });
            Assert.That(gameModel.ActiveBattle, Is.Not.Null);

            BattleScreenViewModel viewModel = new(gameModel, gameModel.ActiveBattle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
            backgrounds.Add(viewModel.RenderState.Background.CanonicalId);
        }

        Assert.That(backgrounds, Is.EqualTo([
            "battle-background/boat/0",
            "battle-background/boat/0",
            "battle-background/terrain/water/0",
            "battle-background/terrain/water/0"
        ]));
    }

    [Test]
    public async Task AiTurnFailureTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        TestBattleUnitUpgradeResolver resolver = new();
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                resolver,
                _ => throw new InvalidOperationException("Battle resolution failed."),
                isAttackerAi: true,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);

        Assert.That(battle.IsAiTurn, Is.True);

        await viewModel.CompleteTransitionAsync();

        ErrorScreenViewModel errorScreen = Assert.IsOfType<ErrorScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.Screens, Is.EqualTo([errorScreen]));
        Assert.That(errorScreen.Message, Is.EqualTo("Failed to play the enemy turn.\nBattle resolution failed."));
        Assert.That(errorScreen.ActionLabel, Is.EqualTo("Back"));
    }

    [Test]
    public async Task IdleOffsetTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 16, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 9, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Assert.That(FormatIdleOffsets(viewModel.RenderState), Is.EqualTo([
            "unit 1 idle offset 7529470",
            "unit 2 idle offset 9504349"
        ]));

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        viewModel.CompleteAnimation(TimeSpan.FromSeconds(100));
        await turnTask;

        Assert.That(FormatIdleOffsets(viewModel.RenderState), Is.EqualTo([
            "unit 1 idle offset -994444445",
            "unit 2 idle offset -994444445"
        ]));
    }

    [Test]
    public async Task SequenceHitpointsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 16, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 9, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);

        BattleUnit damagedEnemy = gameModel.Snapshot.BattleState.GetUnit(enemyId);

        Assert.That(damagedEnemy.Hitpoints, Is.EqualTo(20));
        Assert.That(viewModel.AnimationState, Is.Not.Null);

        double framesPerSecond = viewModel.AnimationState.Rate.FramesPerSecond;

        Assert.That(FormatProperties(viewModel.AnimationState.Sequence.Properties, framesPerSecond), Is.EqualTo([
            "unit 2 hitpoints 30 -> 20 at 7 for 12 frames Decelerate"
        ]));
        Assert.That(FormatGrid(viewModel.DefenderGrid, framesPerSecond), Is.EqualTo([
            "unit 2 unit-face/enemy at 2,0 span 1 hitpoints 30/30 Alive current False #ffac2c2a",
            "unit 2 feedback Damage -10",
            "unit 2 feedback 0 -> 1 at 7 for 3 frames Smooth",
            "unit 2 feedback 1 -> 0 at 19 for 3 frames Smooth",
            "unit 2 hitpoints 30 -> 20 at 7 for 12 frames Decelerate"
        ]));

        BattleUnitCardViewModel animatedTarget = Assert.NotNull(viewModel.TargetUnit);

        Assert.That(animatedTarget.UnitId, Is.EqualTo(enemyId));
        Assert.That(animatedTarget.PortraitKey, Is.EqualTo(new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(enemyDefinition.Id))));
        Assert.That(animatedTarget.Hitpoints, Is.EqualTo(30));
        Assert.That(animatedTarget.MaxHitpoints, Is.EqualTo(30));
        Assert.That(FormatRange(animatedTarget.HitpointsAnimation, framesPerSecond), Is.EqualTo([
            "unit 2 hitpoints 30 -> 20 at 7 for 12 frames Decelerate"
        ]));

        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(viewModel.TargetUnit, Is.Null);

        Assert.That(FormatGrid(viewModel.DefenderGrid, framesPerSecond), Is.EqualTo([
            "unit 2 unit-face/enemy at 2,0 span 1 hitpoints 20/30 Alive current True "
        ]));
    }

    [Test]
    public async Task SequenceCurrentUnitHitpointsTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Healing Potion"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(30),
            AnimationUnitId: null);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId leaderId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(leaderId, null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 60);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(leader)
            .Add(enemy)
            .AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("leader"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Potion), new AssetAnimationMetadata(FrameCount: 31, Width: 111, Height: 348, OffsetX: 366, OffsetY: 246, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        viewModel.SelectItem(LeaderEquipmentSlot.Battle1);

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom));

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);

        BattleUnit healedLeader = gameModel.Snapshot.BattleState.GetUnit(leaderId);

        Assert.That(healedLeader.Hitpoints, Is.EqualTo(90));
        Assert.That(viewModel.AnimationState, Is.Not.Null);

        double framesPerSecond = viewModel.AnimationState.Rate.FramesPerSecond;

        Assert.That(FormatProperties(viewModel.AnimationState.Sequence.Properties, framesPerSecond), Is.EqualTo([
            "unit 1 hitpoints 60 -> 90 at 0 for 12 frames Decelerate"
        ]));
        Assert.That(FormatGrid(viewModel.AttackerGrid, framesPerSecond), Is.EqualTo([
            "unit 1 unit-face/leader at 2,1 span 1 hitpoints 60/100 Alive current True ",
            "unit 1 feedback Action Item",
            "unit 1 feedback 0 -> 1 at 0 for 3 frames Smooth",
            "unit 1 feedback 1 -> 0 at 12 for 3 frames Smooth",
            "unit 1 feedback Heal Healed",
            "unit 1 feedback 0 -> 1 at 0 for 3 frames Smooth",
            "unit 1 feedback 1 -> 0 at 12 for 3 frames Smooth",
            "unit 1 hitpoints 60 -> 90 at 0 for 12 frames Decelerate"
        ]));

        BattleUnitCardViewModel animatedCard = Assert.NotNull(viewModel.CurrentUnit);

        Assert.That(animatedCard.UnitId, Is.EqualTo(leaderId));
        Assert.That(animatedCard.Hitpoints, Is.EqualTo(60));
        Assert.That(animatedCard.MaxHitpoints, Is.EqualTo(100));
        Assert.That(FormatRange(animatedCard.HitpointsAnimation, framesPerSecond), Is.EqualTo([
            "unit 1 hitpoints 60 -> 90 at 0 for 12 frames Decelerate"
        ]));
        Assert.That(viewModel.TargetUnit, Is.Null);

        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        BattleUnitCardViewModel settledCard = Assert.NotNull(viewModel.CurrentUnit);

        Assert.That(settledCard.UnitId, Is.EqualTo(enemyId));
        Assert.That(settledCard.PortraitKey, Is.EqualTo(new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(enemyDefinition.Id))));
        Assert.That(settledCard.Hitpoints, Is.EqualTo(30));
        Assert.That(settledCard.MaxHitpoints, Is.EqualTo(30));
        Assert.That(settledCard.HitpointsAnimation, Is.EqualTo(AnimationRange.None));

        Assert.That(FormatGrid(viewModel.AttackerGrid, framesPerSecond), Is.EqualTo([
            "unit 1 unit-face/leader at 2,1 span 1 hitpoints 90/100 Alive current False #ffac2c2a"
        ]));
    }

    [Test]
    public async Task SummonTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId summonerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(summonerId, null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(summoner)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 8, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 3, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("imp"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        BattleGridTile backCenter = new(BattleSide.Attacker, GridPosition.BackCenter);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(summonerId));
        Assert.That(viewModel.RenderState.Placeholders, Is.EqualTo([
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(backCenter, Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), Theme.Battle.GroundMarker.SummonReachableColor)
        ]));
        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                enemyId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("enemy")),
                facing: BattleSide.Defender,
                row: 2,
                column: 0,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                marker: null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));

        viewModel.Hover(backCenter);

        Assert.That(viewModel.AttackerGrid.Tiles, Is.EqualTo([
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), row: 1, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), row: 0, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), row: 2, column: 0, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(backCenter, row: 1, column: 0, Theme.Battle.GridMarker.SummonTargetColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), row: 0, column: 0, Theme.Battle.GridMarker.SummonReachableColor)
        ]));

        Task turnTask = viewModel.ClickAsync(backCenter);
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        BattleUnit summonedImp = new(new BattleUnitId(3), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summonerId, [], null, false);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.AttackerGrid.GetUnits(), Is.EqualTo([
            (summoner, GridPosition.FrontBottom),
            (summonedImp, GridPosition.BackCenter)
        ]));
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(enemyId));
    }

    [Test]
    public async Task SummonAllTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.All) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId summonerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(summonerId, null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(summoner)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 8, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 3, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("imp"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        BattleGridTile backCenter = new(BattleSide.Attacker, GridPosition.BackCenter);

        viewModel.Hover(backCenter);

        Assert.That(viewModel.RenderState.Placeholders, Is.EqualTo([
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), Theme.Battle.GroundMarker.SummonTargetColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), Theme.Battle.GroundMarker.SummonTargetColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), Theme.Battle.GroundMarker.SummonTargetColor),
            new Placeholder(backCenter, Theme.Battle.GroundMarker.SummonTargetColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), Theme.Battle.GroundMarker.SummonTargetColor)
        ]));

        Assert.That(viewModel.AttackerGrid.Tiles, Is.EqualTo([
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), row: 1, column: 1, Theme.Battle.GridMarker.SummonTargetColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), row: 0, column: 1, Theme.Battle.GridMarker.SummonTargetColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), row: 2, column: 0, Theme.Battle.GridMarker.SummonTargetColor),
            new BattleGridTileViewModel(backCenter, row: 1, column: 0, Theme.Battle.GridMarker.SummonTargetColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), row: 0, column: 0, Theme.Battle.GridMarker.SummonTargetColor)
        ]));

        Task turnTask = viewModel.ClickAsync(backCenter);
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.AttackerGrid.GetUnits(), Is.EqualTo([
            (summoner, GridPosition.FrontBottom),
            (new BattleUnit(new BattleUnitId(3), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summonerId, [], null, false), GridPosition.FrontCenter),
            (new BattleUnit(new BattleUnitId(4), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summonerId, [], null, false), GridPosition.FrontTop),
            (new BattleUnit(new BattleUnitId(5), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summonerId, [], null, false), GridPosition.BackBottom),
            (new BattleUnit(new BattleUnitId(6), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summonerId, [], null, false), GridPosition.BackCenter),
            (new BattleUnit(new BattleUnitId(7), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summonerId, [], null, false), GridPosition.BackTop)
        ]));
    }

    [Test]
    public async Task SummonOverCorpseTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 40, initiative: 40, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId summonerId = new(1);
        BattleUnitId allyId = new(2);
        BattleUnitId enemyId = new(3);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(summonerId, null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit corpse = TestHelpers.SimpleBattleUnit(allyId, null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(summoner, GridPosition.FrontBottom)
            .Add(corpse, GridPosition.FrontCenter)
            .Add(enemy, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 8, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 3, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("imp"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        BattleGridTile corpseTile = new(BattleSide.Attacker, GridPosition.FrontCenter);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(summonerId));
        Assert.That(viewModel.AttackerGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                summonerId,
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("summoner")),
                facing: BattleSide.Attacker,
                row: 2,
                column: 1,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None),
            new BattleGridUnitViewModel(
                allyId,
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("ally")),
                facing: BattleSide.Attacker,
                row: 1,
                column: 1,
                columnSpan: 1,
                hitpoints: 0,
                maxHitpoints: 40,
                BattleUnitState.Presence.Dead,
                isPendingUpgrade: false,
                isCurrent: false,
                null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));
        Assert.That(viewModel.AttackerGrid.Tiles, Is.EqualTo([
            new BattleGridTileViewModel(corpseTile, row: 1, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), row: 0, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), row: 2, column: 0, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter), row: 1, column: 0, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), row: 0, column: 0, Theme.Battle.GridMarker.SummonReachableColor)
        ]));
        Assert.That(viewModel.RenderState.Placeholders, Is.EqualTo([
            new Placeholder(corpseTile, Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), Theme.Battle.GroundMarker.SummonReachableColor)
        ]));

        viewModel.Hover(corpseTile);

        Assert.That(viewModel.AttackerGrid.Tiles, Is.EqualTo([
            new BattleGridTileViewModel(corpseTile, row: 1, column: 1, Theme.Battle.GridMarker.SummonTargetColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), row: 0, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), row: 2, column: 0, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter), row: 1, column: 0, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), row: 0, column: 0, Theme.Battle.GridMarker.SummonReachableColor)
        ]));
        Assert.That(viewModel.RenderState.Placeholders, Is.EqualTo([
            new Placeholder(corpseTile, Theme.Battle.GroundMarker.SummonTargetColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), Theme.Battle.GroundMarker.SummonReachableColor)
        ]));

        Task turnTask = viewModel.ClickAsync(corpseTile);
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        BattleUnit summonedImp = new(new BattleUnitId(4), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summonerId, [], null, false);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.AttackerGrid.GetUnits(), Is.EqualTo([
            (summoner, GridPosition.FrontBottom),
            (summonedImp, GridPosition.FrontCenter)
        ]));
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(enemyId));
    }

    [Test]
    public async Task SummonItemTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitAttackDefinition summonAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summon"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
        {
            Type = UnitAttackType.Summon,
            SummonUnitIds = [impDefinition.Id]
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("Talisman of Summoning"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(summonAttack.Id),
            AnimationUnitId: null);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(leaderDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(summonAttack)
            .WithDefinition(enemyDefinition)
            .WithDefinition(talisman)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId leaderId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(leaderId, null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(leader)
            .Add(enemy)
            .AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: null, Consumed: false))
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("leader"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("imp"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Talisman), new AssetAnimationMetadata(FrameCount: 31, Width: 85, Height: 84, OffsetX: 368, OffsetY: 383, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        BattleGridTile frontCenter = new(BattleSide.Attacker, GridPosition.FrontCenter);

        Assert.That(viewModel.Items.Select(item => (item.Name, item.SelectCommand.CanExecute(null))), Is.EqualTo([("Talisman of Summoning", true)]));

        viewModel.SelectItem(LeaderEquipmentSlot.Battle1);

        Assert.That(viewModel.RenderState.Placeholders, Is.EqualTo([
            new Placeholder(frontCenter, Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), Theme.Battle.GroundMarker.SummonReachableColor)
        ]));

        Task turnTask = viewModel.ClickAsync(frontCenter);
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        BattleUnit summonedImp = new(new BattleUnitId(3), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], leaderId, [], null, false);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.AttackerGrid.GetUnits(), Is.EqualTo([
            (leader, GridPosition.FrontBottom),
            (summonedImp, GridPosition.FrontCenter)
        ]));
    }

    [Test]
    public async Task EscapeTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        BattleState? initialState = gameModel.Snapshot.BattleState;

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { viewModel, menu }));
            Assert.That(menu.SaveTip, Is.EqualTo("Saving is not allowed during battle"));
            Assert.That(menu.SaveGameCommand.CanExecute(null), Is.False);
        }

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([viewModel]));
        Assert.That(gameModel.Snapshot.BattleState, Is.SameAs(initialState));

        viewModel.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel cancelledPrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { viewModel, cancelledPrompt }));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([viewModel]));
        Assert.That(gameModel.Snapshot.BattleState, Is.SameAs(initialState));

        viewModel.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([viewModel]));
        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.Outcome, Is.EqualTo(new BattleOutcome(BattleSide.Attacker)));

        gameViewModel.EscapeCommand.Execute(null);
        await viewModel.CompleteTransitionAsync();

        Assert.That(gameModel.Snapshot.BattleState, Is.Null);
        Assert.That(gameViewModel.TopScreen, Is.Null);
    }

    [Test]
    public async Task AttackCursorTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker, GridPosition.FrontBottom)
            .Add(ally, GridPosition.BackBottom)
            .Add(enemy, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(attackerDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(allyDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(enemyDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.CastSpell));

        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.CastSpellInvalid));

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.BackTop));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));
        viewModel.Hover(null);

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));
    }

    [Test]
    public async Task HealCursorTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("cleric", maxHitpoints: 100, initiative: 50, damage: 0) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("cleric"), initiative: 50, damage: 0, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Heal,
                Heal = 30
            }
        };
        UnitDefinition allyDefinition = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 20, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(allyDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit ally = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, allyDefinition.Id, BattleSide.Attacker, hitpoints: 50);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker, GridPosition.FrontBottom)
            .Add(ally, GridPosition.BackBottom)
            .Add(enemy, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(attackerDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(allyDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(enemyDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));

        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleHeal));

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleHealInvalid));

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.BackTop));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));

        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom));
        viewModel.Hover(null);

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));
    }

    [Test]
    public async Task AiTurnCardTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 30, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 50, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: true,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));

        Assert.That(viewModel.IsPlayerTurn, Is.False);
        Assert.That(viewModel.AttackerUnit, Is.Null);

        BattleUnitCardViewModel defenderCard = Assert.NotNull(viewModel.DefenderUnit);

        Assert.That(defenderCard.UnitId, Is.EqualTo(enemyId));
        Assert.That(defenderCard.Side, Is.EqualTo(BattleSide.Defender));
    }

    [Test]
    public async Task SequenceAssetsTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 16, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnitAura(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 16, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 9, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        TestAssetPreloader preloader = new();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, preloader, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.GetUnit(enemyId).Hitpoints, Is.EqualTo(20));
        Assert.That(viewModel.AnimationState, Is.Null);
        Assert.That(preloader.Preloads, Has.Count.EqualTo(2));
        Assert.That(FormatItems(preloader.Preloads[1]), Is.EqualTo([
            "animation battle-unit-shadow/attacker/attack/attacker",
            "animation battle-unit/attacker/attack/attacker",
            "animation battle-unit-aura/attacker/attack/attacker",
            "animation battle-unit-shadow/enemy/hit/defender",
            "animation battle-unit/enemy/hit/defender",
            "sound battle-unit-attack/attacker/5",
            "sound battle-unit-touch/attacker/2",
            "sound battle-unit-damaged/enemy/3"
        ]));

        preloader.CompletePreload();

        Assert.That(viewModel.AnimationState, Is.Not.Null);

        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(viewModel.AnimationState, Is.Null);
    }

    [Test]
    public async Task HoverAfterTurnTest()
    {
        UnitDefinition firstDefinition = TestHelpers.SimpleUnitDefinition("first", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition secondDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(firstDefinition)
            .WithDefinition(secondDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId firstId = new(1);
        BattleUnitId secondId = new(2);
        BattleUnitId enemyId = new(3);

        BattleUnit first = TestHelpers.SimpleBattleUnit(firstId, null, firstDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit second = TestHelpers.SimpleBattleUnit(secondId, null, secondDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(first, GridPosition.FrontBottom)
            .Add(second, GridPosition.FrontTop)
            .Add(enemy, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("first"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 16, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 7, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("first"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("second"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 9, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(secondId));
        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                enemyId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(enemyDefinition.Id),
                facing: BattleSide.Defender,
                row: 2,
                column: 0,
                columnSpan: 1,
                hitpoints: 20,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                Theme.Battle.GridMarker.EnemyTargetColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));

        BattleUnitCardViewModel target = Assert.NotNull(viewModel.TargetUnit);

        Assert.That(target.UnitId, Is.EqualTo(enemyId));
        Assert.That(target.Hitpoints, Is.EqualTo(20));
    }

    [Test]
    public async Task HoverReachAllItemTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            LeaderAbilities = LeaderAbility.TalismanUse,
            Leadership = 1
        };
        UnitAttackDefinition frostAttack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("frost"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.All);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        UnitDefinition secondDefinition = TestHelpers.SimpleUnitDefinition("second", maxHitpoints: 30, initiative: 20, damage: 5);
        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman"),
            new GlobalTextId("Talisman of Frost"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new AttackItemEffect(frostAttack.Id),
            AnimationUnitId: null);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(leaderDefinition)
            .WithDefinition(frostAttack)
            .WithDefinition(enemyDefinition)
            .WithDefinition(secondDefinition)
            .WithDefinition(talisman)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId leaderId = new(1);
        BattleUnitId enemyId = new(2);
        BattleUnitId secondId = new(3);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(leaderId, null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);
        BattleUnit second = TestHelpers.SimpleBattleUnit(secondId, null, secondDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(leader)
            .Add(enemy, GridPosition.FrontBottom)
            .Add(second, GridPosition.FrontTop)
            .AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Consumed: false))
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("leader"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("second"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        viewModel.SelectItem(LeaderEquipmentSlot.Battle1);
        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                enemyId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(enemyDefinition.Id),
                facing: BattleSide.Defender,
                row: 2,
                column: 0,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                Theme.Battle.GridMarker.EnemyTargetColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None),
            new BattleGridUnitViewModel(
                secondId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop),
                new AssetSpriteKey.UnitFace(secondDefinition.Id),
                facing: BattleSide.Defender,
                row: 0,
                column: 0,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                Theme.Battle.GridMarker.EnemyTargetColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));
    }

    [Test]
    public async Task HoverLargeUnitBackTileTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("attacker"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Any)
        };
        UnitDefinition giantDefinition = TestHelpers.SimpleUnitDefinition("giant", maxHitpoints: 300, initiative: 30, damage: 40) with
        {
            IsLarge = true
        };

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(giantDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId giantId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit giant = TestHelpers.SimpleBattleUnit(giantId, null, giantDefinition.Id, BattleSide.Defender, hitpoints: 300);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(giant, GridPosition.FrontTop)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("giant"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.BackTop));

        Assert.That(viewModel.DefenderGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                giantId,
                new BattleGridTile(BattleSide.Defender, GridPosition.FrontTop),
                new AssetSpriteKey.UnitFace(giantDefinition.Id),
                facing: BattleSide.Defender,
                row: 0,
                column: 0,
                columnSpan: 2,
                hitpoints: 300,
                maxHitpoints: 300,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: false,
                Theme.Battle.GridMarker.EnemyTargetColor,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));
    }

    [Test]
    public async Task NoTargetsTest()
    {
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("soldier"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };
        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(soldierDefinition)
            .WithDefinition(guardDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId soldierId = new(1);
        BattleUnitId guardId = new(2);
        BattleUnitId enemyId = new(3);

        BattleUnit soldier = TestHelpers.SimpleBattleUnit(soldierId, null, soldierDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit guard = TestHelpers.SimpleBattleUnit(guardId, null, guardDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(soldier, GridPosition.BackBottom)
            .Add(guard, GridPosition.FrontBottom)
            .Add(enemy, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(soldierDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(guardDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(enemyDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(soldierId));
        Assert.That(viewModel.HasTargets, Is.False);
        Assert.That(viewModel.DefendCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.WaitCommand.CanExecute(null), Is.True);

        Task waitTask = viewModel.WaitCommand.ExecuteAsync(null);
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await waitTask;

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(guardId));
        Assert.That(viewModel.HasTargets, Is.True);
        Assert.That(viewModel.DefendCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.WaitCommand.CanExecute(null), Is.True);

        Task guardSkipTask = viewModel.SkipCommand.ExecuteAsync(null);
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await guardSkipTask;

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(enemyId));
        Assert.That(viewModel.HasTargets, Is.True);

        Task enemySkipTask = viewModel.SkipCommand.ExecuteAsync(null);
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await enemySkipTask;

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitTurn, Is.EqualTo(new BattleUnitTurn(soldierId, BattleUnitTurnType.Resumed)));
        Assert.That(viewModel.HasTargets, Is.False);
        Assert.That(viewModel.DefendCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.WaitCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task NoAttackTargetsWithItemTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("leader"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent),
            Leadership = 1
        };
        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Healing Potion"),
            new GlobalTextId("Heals 30 hit points"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(30),
            AnimationUnitId: null);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(leaderDefinition)
            .WithDefinition(guardDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId leaderId = new(1);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(leaderId, null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit guard = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, guardDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(leader, GridPosition.BackBottom)
            .Add(guard, GridPosition.FrontBottom)
            .Add(enemy, GridPosition.FrontBottom)
            .AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(leaderDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(guardDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(enemyDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(leaderId));
        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.CastSpellInvalid));
        Assert.That(viewModel.HasTargets, Is.True);
    }

    [Test]
    public async Task AutoResolveAgainstComputerTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: true,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(attackerId));
        Assert.That(viewModel.DefendCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.AutoResolveCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.AutoResolveTip, Is.EqualTo("Resolve the battle automatically"));
    }

    [Test]
    public async Task AutoResolveAgainstHumanTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(attackerId));
        Assert.That(viewModel.DefendCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.AutoResolveCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task ItemCursorTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("leader"), initiative: 50, damage: 10, accuracy: 100, reach: UnitAttackReach.Adjacent),
            Leadership = 1
        };
        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Healing Potion"),
            new GlobalTextId("Heals 30 hit points"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(30),
            AnimationUnitId: null);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(leaderDefinition)
            .WithDefinition(guardDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId leaderId = new(1);

        BattleUnit leader = TestHelpers.SimpleBattleUnit(leaderId, null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit guard = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, guardDefinition.Id, BattleSide.Attacker, hitpoints: 20);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(3), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(leader, GridPosition.BackBottom)
            .Add(guard, GridPosition.FrontBottom)
            .Add(enemy, GridPosition.FrontBottom)
            .AddItem(new BattleItem(BattleSide.Attacker, LeaderEquipmentSlot.Battle1, potion.Id, Charges: null, Consumed: false))
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(leaderDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(guardDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(enemyDefinition.Id, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        viewModel.SelectItem(LeaderEquipmentSlot.Battle1);
        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.SelectUnit));

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));

        viewModel.SelectItem(LeaderEquipmentSlot.Battle1);

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.CastSpellInvalid));
    }

    [Test]
    public async Task SummonCursorTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId summonerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(summonerId, null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(summoner)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 8, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 3, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("imp"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, GridPosition.BackCenter));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.SelectUnit));

        viewModel.Hover(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));

        viewModel.Hover(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(viewModel.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.BattleDefault));
    }

    [Test]
    public async Task LoadTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 10);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        TestAssetPreloader preloader = new();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, preloader, new RandomProvider(0));

        Task loadTask = viewModel.LoadAsync();

        Assert.That(loadTask.IsCompleted, Is.False);
        Assert.That(preloader.Preloads, Has.Count.EqualTo(1));
        Assert.That(FormatItems(preloader.Preloads[0]), Is.EqualTo([
            "sprite battle-background/terrain/neutral/0",
            "animation battle-unit-shadow/attacker/idle/attacker",
            "animation battle-unit/attacker/idle/attacker",
            "animation battle-unit-shadow/attacker/attack/attacker",
            "animation battle-unit/attacker/attack/attacker",
            "animation battle-unit-shadow/attacker/hit/attacker",
            "animation battle-unit/attacker/hit/attacker",
            "animation battle-unit-shadow/enemy/idle/defender",
            "animation battle-unit/enemy/idle/defender",
            "animation battle-unit-shadow/enemy/attack/defender",
            "animation battle-unit/enemy/attack/defender",
            "animation battle-unit-shadow/enemy/hit/defender",
            "animation battle-unit/enemy/hit/defender",
            "animation battle-background-overlay/terrain/neutral/0/0",
            "sprite unit-face/attacker",
            "sprite unit-portrait/attacker",
            "sprite unit-event-portrait/attacker",
            "sprite unit-face/enemy",
            "sprite unit-portrait/enemy",
            "sprite unit-event-portrait/enemy"
        ]));

        preloader.CompletePreload();
        await loadTask;

        Assert.That(preloader.Preloads, Has.Count.EqualTo(1));

        await viewModel.CompleteTransitionAsync();

        Assert.That(preloader.Preloads, Has.Count.EqualTo(2));
        Assert.That(FormatItems(preloader.Preloads[1]), Is.EqualTo([
            "sound battle-unit-attack/attacker/0",
            "sound battle-unit-attack/attacker/1",
            "sound battle-unit-attack/attacker/2",
            "sound battle-unit-attack/attacker/3",
            "sound battle-unit-attack/attacker/4",
            "sound battle-unit-attack/attacker/5",
            "sound battle-unit-hit/attacker/0",
            "sound battle-unit-hit/attacker/1",
            "sound battle-unit-hit/attacker/2",
            "sound battle-unit-hit/attacker/3",
            "sound battle-unit-hit/attacker/4",
            "sound battle-unit-touch/attacker/0",
            "sound battle-unit-touch/attacker/1",
            "sound battle-unit-touch/attacker/2",
            "sound battle-unit-touch/attacker/3",
            "sound battle-unit-touch/attacker/4",
            "sound battle-unit-damaged/attacker/0",
            "sound battle-unit-damaged/attacker/1",
            "sound battle-unit-damaged/attacker/2",
            "sound battle-unit-damaged/attacker/3",
            "sound battle-unit-attack/enemy/0",
            "sound battle-unit-attack/enemy/1",
            "sound battle-unit-attack/enemy/2",
            "sound battle-unit-attack/enemy/3",
            "sound battle-unit-attack/enemy/4",
            "sound battle-unit-attack/enemy/5",
            "sound battle-unit-hit/enemy/0",
            "sound battle-unit-hit/enemy/1",
            "sound battle-unit-hit/enemy/2",
            "sound battle-unit-hit/enemy/3",
            "sound battle-unit-hit/enemy/4",
            "sound battle-unit-touch/enemy/0",
            "sound battle-unit-touch/enemy/1",
            "sound battle-unit-touch/enemy/2",
            "sound battle-unit-touch/enemy/3",
            "sound battle-unit-touch/enemy/4",
            "sound battle-unit-damaged/enemy/0",
            "sound battle-unit-damaged/enemy/1",
            "sound battle-unit-damaged/enemy/2",
            "sound battle-unit-damaged/enemy/3",
            "sound battle-effect-sound-heal",
            "sound battle-effect-sound-death",
            "sound battle-effect-sound-status/poison",
            "sound battle-effect-sound-status/frostbite",
            "sound battle-effect-sound-status/blister",
            "sound battle-effect-sound-status/shatter-armor",
            "sound battle-effect-sound-status/transform",
            "sound battle-effect-sound-status/unsummon",
            "animation battle-effect-death/human/small",
            "sprite battle-corpse/small/0",
            "sprite battle-corpse/small/1",
            "animation battle-effect-heal",
            "animation battle-effect-blister",
            "animation battle-unit-shadow/attacker/idle/defender",
            "animation battle-unit/attacker/idle/defender",
            "animation battle-unit-shadow/attacker/hit/defender",
            "animation battle-unit/attacker/hit/defender",
            "animation battle-unit-shadow/enemy/idle/attacker",
            "animation battle-unit/enemy/idle/attacker",
            "animation battle-unit-shadow/enemy/hit/attacker",
            "animation battle-unit/enemy/hit/attacker",
            "animation battle-effect-level-up/human/small"
        ]));
        Assert.That(preloader.Actions, Is.Empty);

        gameViewModel.OpenScreen(viewModel);
        gameViewModel.CloseScreen(viewModel);

        Assert.That(preloader.Actions, Is.EqualTo(["Release"]));

        preloader.CompletePreload();

        Assert.That(preloader.Actions, Is.EqualTo(["Release", "Release"]));
    }

    [Test]
    public async Task TransitionInTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: true,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);

        Assert.That(viewModel.TransitionState, Is.EqualTo(new BattleWorldTransitionState(BattleTransitionType.In)));
        Assert.That(viewModel.PaintsOwnBackground, Is.False);
        Assert.That(gameViewModel.PaintsOwnBackground, Is.False);
        Assert.That(viewModel.DefendCommand.CanExecute(null), Is.False);
        Assert.That(viewModel.AutoResolveCommand.CanExecute(null), Is.False);

        await viewModel.CompleteTransitionAsync();

        Assert.That(viewModel.TransitionState, Is.Null);
        Assert.That(viewModel.PaintsOwnBackground, Is.True);
        Assert.That(gameViewModel.PaintsOwnBackground, Is.True);
        Assert.That(viewModel.DefendCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.AutoResolveCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task InspectModifiedUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        UnitModifierDefinition banner = TestHelpers.SimpleModifier("banner") with { Armor = new UnitStatModifier(10, 0) };
        UnitModifierDefinition potion = TestHelpers.SimpleModifier("potion") with { Damage = new UnitStatModifier(0, 30) };
        UnitModifierDefinition elixir = TestHelpers.SimpleModifier("elixir") with { Hitpoints = new UnitStatModifier(10, 0) };
        UnitModifierDefinition curse = TestHelpers.SimpleModifier("curse") with { Initiative = new UnitStatModifier(0, -20) };
        UnitModifierDefinition protection = TestHelpers.SimpleModifier("protection") with { Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once)] };

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(banner)
            .WithDefinition(potion)
            .WithDefinition(elixir)
            .WithDefinition(curse)
            .WithDefinition(protection)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = new(
            attackerId,
            null,
            attackerDefinition.Id,
            BattleSide.Attacker,
            100,
            0,
            0,
            1,
            [
                UnitModifier.FromEquipment(banner.Id),
                new UnitModifier(potion.Id, RemainingTurns: 3),
                new UnitModifier(elixir.Id, RemainingTurns: 1),
                new UnitModifier(curse.Id, RemainingTurns: 2)
            ],
            [],
            null,
            [],
            Name: null,
            IsTypeLocked: false)
        {
            WardModifiers = [new BattleUnitWardModifier(enemyId, protection.Id, new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once))]
        };
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));

        Assert.That(viewModel.AttackerGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                attackerId,
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("attacker")),
                facing: BattleSide.Attacker,
                row: 2,
                column: 1,
                columnSpan: 1,
                hitpoints: 100,
                maxHitpoints: 110,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                null,
                feedback: [],
                statuses:
                [
                    new UnitModifierStatusViewModel(UnitModifierType.Weaken, [new UnitModifierViewModel("-20% initiative", null, UnitModifierType.Weaken)]),
                    new UnitModifierStatusViewModel(UnitModifierType.Boost,
                    [
                        new UnitModifierViewModel("+30% damage", null, UnitModifierType.Boost),
                        new UnitModifierViewModel("+10 hit points", null, UnitModifierType.Boost),
                        new UnitModifierViewModel("Ward against Fire", TrSpans("Until the caster's next turn"), UnitModifierType.Boost)
                    ])
                ],
                hitpointsAnimation: AnimationRange.None)
        ]));

        viewModel.ShowUnitDetails(attackerId);

        Assert.That(viewModel.InspectedUnit, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(attackerDefinition.Id)),
            "name",
            "description",
            1,
            100,
            110,
            0,
            1000,
            false,
            false,
            false,
            false,
            "Damage",
            new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, "Weapon", Theme.AttackSource.WeaponColor),
            new StatValueViewModel(TextSpans.FromText("50"), 15),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel(new TextSpans([new TextSpan("1", TextStyle.Bold), new TextSpan(" ", TextStyle.None), new TextSpan("(Ranged)", TextStyle.Italic)]), 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel(TextSpans.FromText("0"), 10)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel(TextSpans.FromText("100%"), 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel(TextSpans.FromText("50"), -10))
            ],
            null,
            null,
            null,
            [
                new UnitModifierStatusViewModel(UnitModifierType.Weaken, [new UnitModifierViewModel("-20% initiative", null, UnitModifierType.Weaken)]),
                new UnitModifierStatusViewModel(UnitModifierType.Boost,
                [
                    new UnitModifierViewModel("+30% damage", null, UnitModifierType.Boost),
                    new UnitModifierViewModel("+10 hit points", null, UnitModifierType.Boost),
                    new UnitModifierViewModel("Ward against Fire", TrSpans("Until the caster's next turn"), UnitModifierType.Boost)
                ])
            ],
            [new UnitPermanentModifierViewModel(IconMoniker.Armor, "+10 armor", "From equipment", null, 1)])));
    }

    [Test]
    public async Task ExperienceFeedbackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5) with
        {
            ExperienceKilled = 40
        };

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));

        Assert.That(viewModel.AnimationState, Is.Not.Null);

        double framesPerSecond = viewModel.AnimationState.Rate.FramesPerSecond;

        Assert.That(FormatGrid(viewModel.AttackerGrid, framesPerSecond), Is.EqualTo([
            "unit 1 unit-face/attacker at 2,1 span 1 hitpoints 100/100 Alive current True ",
            "unit 1 feedback Heal +40 XP",
            "unit 1 feedback 0 -> 1 at 27 for 3 frames Smooth"
        ]));

        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(FormatGrid(viewModel.AttackerGrid, framesPerSecond), Is.EqualTo([
            "unit 1 unit-face/attacker at 2,1 span 1 hitpoints 100/100 Alive current False ",
            "unit 1 feedback Heal +40 XP"
        ]));
        Assert.That(viewModel.IsBattleOver, Is.True);
    }

    [Test]
    public async Task AutoResolveAgainstKeyStackTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: true,
                canAutoResolve: false,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(attackerId));
        Assert.That(viewModel.DefendCommand.CanExecute(null), Is.True);
        Assert.That(viewModel.AutoResolveCommand.CanExecute(null), Is.False);
        Assert.That(viewModel.AutoResolveTip, Is.EqualTo("This battle cannot be resolved automatically"));
    }

    [Test]
    public async Task LeaderVictoryTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100) with { Name = "Uther" };
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(viewModel.IsBattleOver, Is.True);
        Assert.That(viewModel.OutcomeTitle, Is.EqualTo("Victory"));
        Assert.That(viewModel.OutcomeText, Is.EqualTo(new TextSpans([new TextSpan("Uther", TextStyle.Highlight), new TextSpan(" has won the battle.", TextStyle.None)])));
        Assert.That(viewModel.OutcomeIcon, Is.EqualTo(IconMoniker.Damage));
    }

    [Test]
    public async Task LeaderDefeatTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 10, damage: 50) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 1
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 100);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100) with { Name = "Uther" };
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 1), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom));
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(viewModel.IsBattleOver, Is.True);
        Assert.That(viewModel.OutcomeTitle, Is.EqualTo("Defeat"));
        Assert.That(viewModel.OutcomeText, Is.EqualTo(new TextSpans([new TextSpan("Uther", TextStyle.Highlight), new TextSpan(" has fallen in battle.", TextStyle.None)])));
        Assert.That(viewModel.OutcomeIcon, Is.EqualTo(IconMoniker.Defend));
    }

    [Test]
    public async Task SummonVictoryTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        Task turnTask = viewModel.ClickAsync(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom));
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        Assert.That(viewModel.IsBattleOver, Is.True);
        Assert.That(viewModel.OutcomeTitle, Is.EqualTo("Victory"));
        Assert.That(viewModel.OutcomeText, Is.EqualTo(new TextSpans([new TextSpan("name", TextStyle.Highlight), new TextSpan(" has won the battle.", TextStyle.None)])));
        Assert.That(viewModel.OutcomeIcon, Is.EqualTo(IconMoniker.Damage));
    }

    [Test]
    public async Task AutoResolveDuringAiAnimationTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 200, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 200);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: true,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 1, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Human, IsLarge: false), new AssetAnimationMetadata(FrameCount: 26, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);

        Task transition = viewModel.CompleteTransitionAsync();

        Assert.That(viewModel.AnimationState, Is.Not.Null);
        Assert.That(viewModel.AutoResolveCommand.CanExecute(null), Is.True);

        viewModel.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.Outcome, Is.EqualTo(new BattleOutcome(BattleSide.Attacker)));
        Assert.That(viewModel.AutoResolveCommand.CanExecute(null), Is.False);

        viewModel.CompleteAnimation(TimeSpan.Zero);
        await transition;

        Assert.That(gameViewModel.Screens, Is.EqualTo([viewModel]));
        Assert.That(viewModel.AnimationState, Is.Null);
        Assert.That(viewModel.IsBattleOver, Is.True);
        Assert.That(viewModel.ExitBattleCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task InspectAiUnitTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition baseEnemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);
        UnitDefinition enemyDefinition = baseEnemyDefinition with { Attack = baseEnemyDefinition.Attack with { Accuracy = 80 } };
        UnitModifierDefinition curse = TestHelpers.SimpleModifier("curse") with { Accuracy = new UnitStatModifier(0, -10) };

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(curse)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId attackerId = new(1);
        BattleUnitId enemyId = new(2);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(attackerId, null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = new(enemyId, null, enemyDefinition.Id, BattleSide.Defender, 30, 0, 0, 1, [new UnitModifier(curse.Id, RemainingTurns: 2)], [], null, [], null, false);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(new BattleModifiers(BattleSideModifiers.None, new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 5, RandomDamageBonus: 0, CanRetreat: true)));

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: true,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));

        viewModel.ShowUnitDetails(enemyId);

        Assert.That(viewModel.InspectedUnit, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(enemyDefinition.Id)),
            "name",
            "description",
            1,
            30,
            30,
            0,
            1000,
            false,
            false,
            false,
            false,
            "Damage",
            new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, "Weapon", Theme.AttackSource.WeaponColor),
            new StatValueViewModel(TextSpans.FromText("5"), 0),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel(new TextSpans([new TextSpan("1", TextStyle.Bold), new TextSpan(" ", TextStyle.None), new TextSpan("(Ranged)", TextStyle.Italic)]), 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel(TextSpans.FromText("0"), 0)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel(TextSpans.FromText("80%"), -8)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel(TextSpans.FromText("30"), 0))
            ],
            null,
            null,
            null,
            [
                new UnitModifierStatusViewModel(UnitModifierType.Weaken, [new UnitModifierViewModel("-10% accuracy", null, UnitModifierType.Weaken)])
            ],
            [])));
    }

    [Test]
    public async Task LogTest()
    {
        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50);
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, attackerDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(attacker)
            .Add(enemy)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: true,
                canAutoResolve: true,
                new WorldPosition(0, 0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("attacker"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: false, Variant: 0), new AssetSpriteMetadata(Width: 44, Height: 28, PixelWidth: 44, PixelHeight: 28, OffsetX: -22, OffsetY: -20, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        gameViewModel.OpenScreen(viewModel);
        await viewModel.CompleteTransitionAsync();

        await battle.DispatchCommandAsync(new BattleAutoResolveCommand());

        viewModel.OpenLogCommand.Execute(null);

        BattleLogPopupViewModel popup = Assert.IsOfType<BattleLogPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(popup.Entries, Is.EqualTo<BattleLogEntry[]>(
        [
            new BattleLogRoundEntry("Round 1"),
            new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("name", TextStyle.Highlight),
                new TextSpan(" hit ", TextStyle.None),
                new TextSpan("name", TextStyle.Highlight),
                new TextSpan(" for 50 damage.", TextStyle.None)
            ])),
            new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("name", TextStyle.Highlight),
                new TextSpan(" died.", TextStyle.None)
            ])),
            new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("The attacker won.", TextStyle.None)
            ])),
            new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("name", TextStyle.Highlight),
                new TextSpan(" gained 1 experience.", TextStyle.None)
            ]))
        ]));

        popup.CloseCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(viewModel));
    }

    [Test]
    public async Task SummonOverLargeCorpseTest()
    {
        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 20, initiative: 35, damage: 8);
        UnitDefinition summonerDefinition = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 30, initiative: 50, damage: 5) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("summoner"), initiative: 50, damage: 5, accuracy: 100, reach: UnitAttackReach.Any) with
            {
                Type = UnitAttackType.Summon,
                SummonUnitIds = [impDefinition.Id]
            }
        };
        UnitDefinition giantDefinition = TestHelpers.SimpleUnitDefinition("giant", maxHitpoints: 60, initiative: 40, damage: 5) with
        {
            IsLarge = true
        };
        UnitDefinition enemyDefinition = TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 30, initiative: 30, damage: 5);

        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(summonerDefinition)
            .WithDefinition(impDefinition)
            .WithDefinition(giantDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        BattleUnitId summonerId = new(1);
        BattleUnitId giantId = new(2);
        BattleUnitId enemyId = new(3);

        BattleUnit summoner = TestHelpers.SimpleBattleUnit(summonerId, null, summonerDefinition.Id, BattleSide.Attacker, hitpoints: 30);
        BattleUnit corpse = TestHelpers.SimpleBattleUnit(giantId, null, giantDefinition.Id, BattleSide.Attacker, hitpoints: 0);
        BattleUnit enemy = TestHelpers.SimpleBattleUnit(enemyId, null, enemyDefinition.Id, BattleSide.Defender, hitpoints: 30);

        BattleState battleState = new BattleStateBuilder(engine.Definitions)
            .Add(summoner, GridPosition.FrontBottom)
            .Add(corpse, GridPosition.FrontCenter)
            .Add(enemy, GridPosition.FrontBottom)
            .Build(BattleModifiers.None);

        EngineContext battleEngine = new(new RandomProvider(0), engine.Definitions, engine.UnitStats, GameScenarioProvider.Empty, globals);
        await gameModel.RunAsync(() =>
        {
            gameModel.Session.StartBattle(new BattleSession(
                battleEngine,
                battleState,
                NullBattleUnitUpgradeResolver.Instance,
                _ =>
                {
                },
                isAttackerAi: false,
                isDefenderAi: false,
                canAutoResolve: true,
                new WorldPosition(0,
                    0)));
        });
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite sprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation animation = assetBuilder.CreateAnimation(new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null), [sprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        IAssetMetadataProvider assetMetadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("summoner"), AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 8, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: 3, SecondTriggerFrame: 0))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("imp"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("enemy"), AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Defender), new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 375, OffsetY: 420, TriggerFrame: null, SecondTriggerFrame: null))
            .WithSprite(new AssetSpriteKey.BattleCorpse(IsLarge: true, Variant: 0), new AssetSpriteMetadata(Width: 88, Height: 40, PixelWidth: 88, PixelHeight: 40, OffsetX: -44, OffsetY: -26, Additive: false))
            .Build();
        BattleScreenViewModel viewModel = new(gameModel, battle, gameViewModel, assetMetadata, NullAssetPreloader.Instance, new RandomProvider(0));
        await viewModel.CompleteTransitionAsync();

        BattleGridTile backCenter = new(BattleSide.Attacker, GridPosition.BackCenter);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(summonerId));
        Assert.That(viewModel.AttackerGrid.Units, Is.EqualTo([
            new BattleGridUnitViewModel(
                summonerId,
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("summoner")),
                facing: BattleSide.Attacker,
                row: 2,
                column: 1,
                columnSpan: 1,
                hitpoints: 30,
                maxHitpoints: 30,
                BattleUnitState.Presence.Alive,
                isPendingUpgrade: false,
                isCurrent: true,
                null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None),
            new BattleGridUnitViewModel(
                giantId,
                new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter),
                new AssetSpriteKey.UnitFace(new UnitDefinitionId("giant")),
                facing: BattleSide.Attacker,
                row: 1,
                column: 0,
                columnSpan: 2,
                hitpoints: 0,
                maxHitpoints: 60,
                BattleUnitState.Presence.Dead,
                isPendingUpgrade: false,
                isCurrent: false,
                null,
                feedback: [],
                statuses: [],
                hitpointsAnimation: AnimationRange.None)
        ]));
        Assert.That(viewModel.AttackerGrid.Tiles, Is.EqualTo([
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), row: 1, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), row: 0, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), row: 2, column: 0, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(backCenter, row: 1, column: 0, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), row: 0, column: 0, Theme.Battle.GridMarker.SummonReachableColor)
        ]));
        Assert.That(viewModel.RenderState.Placeholders, Is.EqualTo([
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(backCenter, Theme.Battle.GroundMarker.SummonReachableColor),
            new Placeholder(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), Theme.Battle.GroundMarker.SummonReachableColor)
        ]));
        Assert.That(viewModel.RenderState.HitTargets, Is.EqualTo([
            new HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(335.336, 244.781, 89.76, 40.8)),
            new HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontBottom), new Rect(259.281, 305.338, 84.16, 126.24)),
            new HitTarget(new BattleGridTile(BattleSide.Defender, GridPosition.FrontBottom), new Rect(353.191, 377.566, 86.0, 129.0)),
            new HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), new Rect(391.461, 232.498, 82.56, 82.56)),
            new HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), new Rect(502.141, 178.756, 81.2, 81.2)),
            new HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), new Rect(144.161, 216.368, 82.16, 82.16)),
            new HitTarget(backCenter, new Rect(282.421, 158.374, 80.64, 80.64)),
            new HitTarget(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), new Rect(411.561, 122.888, 79.76, 79.76))
        ]));

        viewModel.Hover(backCenter);

        Assert.That(viewModel.AttackerGrid.Tiles, Is.EqualTo([
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontCenter), row: 1, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.FrontTop), row: 0, column: 1, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackBottom), row: 2, column: 0, Theme.Battle.GridMarker.SummonReachableColor),
            new BattleGridTileViewModel(backCenter, row: 1, column: 0, Theme.Battle.GridMarker.SummonTargetColor),
            new BattleGridTileViewModel(new BattleGridTile(BattleSide.Attacker, GridPosition.BackTop), row: 0, column: 0, Theme.Battle.GridMarker.SummonReachableColor)
        ]));

        Task turnTask = viewModel.ClickAsync(backCenter);
        viewModel.CompleteAnimation(TimeSpan.Zero);
        await turnTask;

        BattleUnit summonedImp = new(new BattleUnitId(4), null, impDefinition.Id, BattleSide.Attacker, 20, 0, 0, 1, [], [], summonerId, [], null, false);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameModel.Snapshot.BattleState.AttackerGrid.GetUnits(), Is.EqualTo([
            (summoner, GridPosition.FrontBottom),
            (summonedImp, GridPosition.BackCenter)
        ]));
        Assert.That(gameModel.Snapshot.BattleState.CurrentUnitId, Is.EqualTo(enemyId));
    }
}
