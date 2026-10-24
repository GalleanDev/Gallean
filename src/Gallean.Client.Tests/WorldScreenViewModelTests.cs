using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class WorldScreenViewModelTests
{
    private sealed class TestAssetPreloader : IAssetPreloader
    {
        private readonly TaskCompletionSource<IDisposable> initialCompleted = new();

        public List<string> Actions { get; } = [];

        public Task<IDisposable> PreloadAsync(IEnumerable<AssetPreloadItem> items)
        {
            Actions.Add("Preload");

            return initialCompleted.Task;
        }

        public void CompleteInitial()
        {
            initialCompleted.SetResult(new CleanupList());
        }
    }

    private sealed class TestAudioPlayer : IAudioPlayer
    {
        public List<string> Actions { get; } = [];
        public float SoundVolume { get; set; }
        public float MusicVolume { get; set; }

        public void PlaySound(AssetSoundKey key)
        {
            Actions.Add($"Sound:{key.CanonicalId}");
        }

        public IMusicPlayback? PlayMusic(MusicQueue queue, MusicPosition position)
        {
            Actions.Add($"Music:{string.Join(',', queue.Tracks.Select(track => track.CanonicalId))}");

            return new RecordingPlayback(string.Join(',', queue.Tracks.Select(track => track.CanonicalId)), Actions);
        }

        public IAudioPlayback? PlayVoice(AssetSoundKey key)
        {
            Actions.Add($"Voice:{key.CanonicalId}");

            return new RecordingPlayback(key.CanonicalId, Actions);
        }

        public IAmbientAudioPlayback? PlayAmbientSound(AssetSoundKey key, float volume, float pan)
        {
            return null;
        }
    }

    private sealed class RecordingPlayback(string name, List<string> actions) : IMusicPlayback
    {
        public MusicPosition Position => MusicPosition.Start;

        public void Stop()
        {
            actions.Add($"Stop:{name}");
        }
    }

    private static IReadOnlyList<string> FormatLines(IReadOnlyList<TextSpans> lines)
    {
        List<string> rows = [];

        foreach (TextSpans line in lines)
        {
            rows.Add(line.Text);
        }

        return rows;
    }

    private static IReadOnlyList<WorldThiefActionType> FormatThiefActions(ThiefActionScreenViewModel screen)
    {
        List<WorldThiefActionType> types = [];

        foreach (ThiefActionItemViewModel action in screen.Actions)
        {
            types.Add(action.Type);
        }

        return types;
    }

    private static List<string> FormatStackItems(IEnumerable<WorldStackItemViewModel> items)
    {
        List<string> rows = [];

        foreach (WorldStackItemViewModel item in items)
        {
            string selection = item.IsSelected ? "selected" : "-";
            string finished = item.IsFinished ? "finished" : "-";
            rows.Add($"{item.StackId.Value} | {item.PortraitKey} | {item.LeaderName} | level {item.Level} | {item.MovementPoints} / {item.MaxMovementPoints} | {selection} | {finished}");
        }

        return rows;
    }

    private static WorldRenderState BuildWorldState(WorldScreenViewModel world)
    {
        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.StackBattleEffect(),
                new AssetAnimationMetadata(FrameCount: 17, Width: 86, Height: 67, OffsetX: -43, OffsetY: -34, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        return new WorldRenderComposer(world.GameModel.Definitions, GameScenarioProvider.Empty, metadata).Compose(world.RenderRequest);
    }

    [Test]
    public async Task StartBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitDefinition defender = TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 50, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .AddUnit(defender)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 10, 3, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("defender"), defender.Id, 10, 4, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack2)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAssetPreloader preloader = new();

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).WithAssetPreloader(preloader).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(2, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(2, 0), 1054, Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, 1.0)],
            Highlights = new WorldRenderState.HighlightOverlay(
            [
                new WorldRenderState.Highlight(
                    new WorldRenderState.Stack(
                        Position: new WorldPosition(0, 0),
                        Depth: 391,
                        AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                        ShadowKey: null,
                        AuraKey: null,
                        BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                        Opacity: 1.0),
                    Theme.World.Highlight.SelectedColor),
                new WorldRenderState.Highlight(
                    new WorldRenderState.Stack(
                        Position: new WorldPosition(2, 0),
                        Depth: 1415,
                        AssetKey: new AssetAnimationKey.StackUnit(defender.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                        ShadowKey: null,
                        AuraKey: null,
                        BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                        Opacity: 1.0),
                    Theme.World.Highlight.HostileColor)
            ]),
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, "4")
            ])
        }));
        Assert.That(gameModel.Snapshot.BattleState, Is.Null);

        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Stacks =
            [
                new WorldRenderState.Stack(
                    Position: new WorldPosition(2, 0),
                    Depth: 1415,
                    AssetKey: new AssetAnimationKey.StackUnit(defender.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap = renderState.Minimap with
            {
                Markers =
                [
                    new MinimapState.Marker(new WorldPosition(1, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack),
                    new MinimapState.Marker(new WorldPosition(2, 0), Theme.World.Race.HereticColor, MinimapState.MarkerType.Stack)
                ]
            },
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(2, 0), 1054, Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(1, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, "2")
            ]),
            Animation = new WorldRenderState.StackMove(
                stack1.Id,
                new AssetSoundKey.StackUnitMove(leader.Id, Variant: 1),
                new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                [
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(0, 0),
                        391,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.Zero),
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(1, 0),
                        903,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.FromMilliseconds(200))
                ],
                Opacity: 1.0,
                IsObjective: false,
                Highlight: Theme.World.Highlight.SelectedColor)
        }));
        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(1, 0)));

        world.CompleteAnimation();

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Stacks =
            [
                new WorldRenderState.Stack(
                    Position: new WorldPosition(1, 0),
                    Depth: 903,
                    AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0),
                new WorldRenderState.Stack(
                    Position: new WorldPosition(2, 0),
                    Depth: 1415,
                    AssetKey: new AssetAnimationKey.StackUnit(defender.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Animation = new WorldRenderState.StackBattle(
                new WorldPosition(2, 0),
                132910,
                new AssetAnimationKey.StackBattleEffect(),
                FrameCount: 17)
        }));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(preloader.Actions, Is.EqualTo(["Preload"]));
        Assert.That(world.IsInBattle, Is.False);

        preloader.CompleteInitial();

        BattleScreenViewModel battle = Assert.IsOfType<BattleScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(preloader.Actions, Is.EqualTo(["Preload"]));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.BattleStart(new WorldPosition(2, 0))));
        Assert.That(world.IsInBattle, Is.True);

        await battle.CompleteTransitionAsync();

        Assert.That(preloader.Actions, Is.EqualTo(["Preload", "Preload"]));
    }

    [Test]
    public async Task LeaderUpgradePromptAfterBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 100, initiative: 50, damage: 100) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Race = WorldRace.Undead,
            Leadership = 6,
            MovementPoints = 6,
            ExperienceNextLevel = 10
        };
        UnitDefinition defender = TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 1, damage: 1) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6,
            ExperienceKilled = 20
        };
        UnitModifierDefinition banner = TestHelpers.SimpleModifier("banner");
        LeaderUpgradeDefinition upgrade = new(
            player1.LordId,
            banner.Id,
            MinLevel: 1,
            Priority: 80,
            new GlobalTextId("nameBanner"),
            new GlobalTextId("descriptionBanner"));

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .AddUnit(defender)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("defender"), defender.Id, 10, 0, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(defender)
            .WithDefinition(banner)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                leader.Id,
                UpgradeChangeLevel: 5,
                LowLevelUpgrade: UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 1, ExperienceKilled = 5, ExperienceNextLevel = 50 },
                HighLevelUpgrade: UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 1, ExperienceKilled = 5, ExperienceNextLevel = 50 }))
            .WithLeaderUpgrade(upgrade)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack2)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        world.CompleteAnimation();

        BattleScreenViewModel battle = Assert.IsOfType<BattleScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.BattleStart(new WorldPosition(2, 0))));

        await battle.CompleteTransitionAsync();
        battle.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(battle));

        battle.Close();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(battle));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.BattleEnd(new WorldPosition(2, 0))));
        Assert.That(world.IsInBattle, Is.False);

        await battle.CompleteTransitionAsync();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.BattleEnd(new WorldPosition(2, 0))));

        world.CompleteAnimation();

        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.StackDestroy(new WorldPosition(2, 0), defender.Id)));

        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<LeaderUpgradeViewModel>());
    }

    [Test]
    public async Task LeaderUpgradePromptWhileModalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 100, initiative: 50, damage: 100) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Race = WorldRace.Undead,
            Leadership = 6,
            MovementPoints = 6,
            ExperienceNextLevel = 10
        };
        UnitDefinition defender = TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 1, damage: 1) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6,
            ExperienceKilled = 20
        };
        UnitModifierDefinition banner = TestHelpers.SimpleModifier("banner");
        LeaderUpgradeDefinition upgrade = new(
            player1.LordId,
            banner.Id,
            MinLevel: 1,
            Priority: 80,
            new GlobalTextId("nameBanner"),
            new GlobalTextId("descriptionBanner"));

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .AddUnit(defender)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("defender"), defender.Id, 10, 0, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(defender)
            .WithDefinition(banner)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                leader.Id,
                UpgradeChangeLevel: 5,
                LowLevelUpgrade: UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 1, ExperienceKilled = 5, ExperienceNextLevel = 50 },
                HighLevelUpgrade: UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 1, ExperienceKilled = 5, ExperienceNextLevel = 50 }))
            .WithLeaderUpgrade(upgrade)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack2)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        world.CompleteAnimation();

        BattleScreenViewModel battle = Assert.IsOfType<BattleScreenViewModel>(gameViewModel.TopScreen);

        await battle.CompleteTransitionAsync();
        battle.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);

        battle.Close();
        await battle.CompleteTransitionAsync();
        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.StackDestroy(new WorldPosition(2, 0), defender.Id)));

        LoadingScreenViewModel blocking = new("Busy");
        gameViewModel.OpenScreen(blocking);
        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(blocking));

        gameViewModel.CloseScreen(blocking);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<LeaderUpgradeViewModel>());
    }

    [Test]
    public async Task BankedLeaderUpgradePromptTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 100, initiative: 50, damage: 100) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Race = WorldRace.Undead,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitDefinition defender = TestHelpers.SimpleUnitDefinition("Unit_Stack2", maxHitpoints: 10, initiative: 1, damage: 1) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitModifierDefinition banner = TestHelpers.SimpleModifier("banner");
        LeaderUpgradeDefinition upgrade = new(
            player1.LordId,
            banner.Id,
            MinLevel: 1,
            Priority: 80,
            new GlobalTextId("nameBanner"),
            new GlobalTextId("descriptionBanner"));

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .AddUnit(defender)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), new GridPosition(0, 0))]),
                movementPoints: 6) with
            {
                PendingLeaderUpgrades = 1
            };

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("defender"), defender.Id, 10, 0, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(defender)
            .WithDefinition(banner)
            .WithLeaderUpgrade(upgrade)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack2)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        HotseatTurnScreenViewModel hotseatTurn = Assert.IsOfType<HotseatTurnScreenViewModel>(gameViewModel.TopScreen);

        hotseatTurn.Close();

        LeaderUpgradeViewModel upgradeScreen = Assert.IsOfType<LeaderUpgradeViewModel>(gameViewModel.TopScreen);

        upgradeScreen.Close();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        world.CompleteAnimation();

        BattleScreenViewModel battle = Assert.IsOfType<BattleScreenViewModel>(gameViewModel.TopScreen);

        await battle.CompleteTransitionAsync();
        battle.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);
        battle.Close();
        await battle.CompleteTransitionAsync();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public async Task BankedLeaderUpgradeOtherRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleUnitDefinition("Unit_Stack1", maxHitpoints: 100, initiative: 50, damage: 100) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Race = WorldRace.Dwarf,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitModifierDefinition banner = TestHelpers.SimpleModifier("banner");
        LeaderUpgradeDefinition upgrade = new(
            player1.LordId,
            banner.Id,
            MinLevel: 1,
            Priority: 80,
            new GlobalTextId("nameBanner"),
            new GlobalTextId("descriptionBanner"));

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 8, [], null, false), new GridPosition(0, 0))]),
                movementPoints: 6) with
            {
                PendingLeaderUpgrades = 7
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(banner)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                leader.Id,
                UpgradeChangeLevel: 5,
                LowLevelUpgrade: UnitDynamicUpgrade.Empty,
                HighLevelUpgrade: UnitDynamicUpgrade.Empty))
            .WithLeaderUpgrade(upgrade)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        HotseatTurnScreenViewModel hotseatTurn = Assert.IsOfType<HotseatTurnScreenViewModel>(gameViewModel.TopScreen);

        hotseatTurn.Close();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(gameModel.World.State.Stacks[stack1.Id].PendingLeaderUpgrades, Is.EqualTo(7));
    }

    [Test]
    public async Task ScenarioPopupTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects: [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsActive, Is.True);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(
            popup.Messages,
            Is.EqualTo(new ScenarioEventPopupMessageViewModel[]
            {
                new(new ScenarioEventPopupMessage([new ScenarioTextSpan("Greetings Milord!", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true))
            }));
        Assert.That(world.IsActive, Is.False);

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsActive, Is.True);
    }

    [Test]
    public async Task ScenarioPopupQueueTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition firstUnit = TestHelpers.SimpleUnitDefinition("G000UU0001", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            NameTextId = new GlobalTextId("Squire")
        };
        UnitDefinition secondUnit = TestHelpers.SimpleUnitDefinition("G000UU0002", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            NameTextId = new GlobalTextId("Wizard")
        };
        ScenarioEventPortraitId firstPortraitId = ScenarioEventPortraitId.Unit(firstUnit.Id);
        ScenarioEventPortraitId secondPortraitId = ScenarioEventPortraitId.Unit(secondUnit.Id);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects:
            [
                new ScenarioEventPopupEffect(new ScenarioTextId("First"), ScenarioEventPopupAudience.Triggerer, firstPortraitId, IsPortraitLeft: true, IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(new ScenarioTextId("Second"), ScenarioEventPopupAudience.Triggerer, secondPortraitId, IsPortraitLeft: false, IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(firstUnit)
            .WithDefinition(secondUnit)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ScenarioEventPopupMessage firstMessage = new([new ScenarioTextSpan("First", ScenarioTextSpanType.Text)], "Squire", new AssetSpriteKey.UnitEventPortrait(firstPortraitId), IsPortraitLeft: true);
        ScenarioEventPopupMessage secondMessage = new([new ScenarioTextSpan("Second", ScenarioTextSpanType.Text)], "Wizard", new AssetSpriteKey.UnitEventPortrait(secondPortraitId), IsPortraitLeft: false);

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(popup.Messages, Is.EqualTo([new ScenarioEventPopupMessageViewModel(firstMessage)]));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(popup));

        popup.CloseCommand.Execute(null);

        Assert.That(popup.Messages, Is.EqualTo([new ScenarioEventPopupMessageViewModel(firstMessage), new ScenarioEventPopupMessageViewModel(secondMessage)]));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(popup));

        popup.CloseCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public async Task ScenarioPopupTriggererUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        ScenarioLocationDefinition zone = new(
            new ScenarioLocationId("Zone1"),
            new ScenarioTextId("Greeting"),
            new WorldPosition(1, 0),
            0);
        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventEnteringZoneCondition(zone.Id)],
            Effects: [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: false, IsPortraitTriggerer: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(stack.Id, zone.Position));

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.Pause(TimeSpan.FromSeconds(0.25))));

        world.CompleteAnimation();

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(
            popup.Messages,
            Is.EqualTo(new ScenarioEventPopupMessageViewModel[]
            {
                new(new ScenarioEventPopupMessage([new ScenarioTextSpan("Welcome.", ScenarioTextSpanType.Text)], "name", new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leader.Id)), IsPortraitLeft: false))
            }));
    }

    [Test]
    public async Task ScenarioPopupNamedPortraitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        ScenarioEventPortraitId portraitId = new("FYRWENN");

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects:
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("The elves speak."),
                    ScenarioEventPopupAudience.Triggerer,
                    portraitId,
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(
            popup.Messages,
            Is.EqualTo(new ScenarioEventPopupMessageViewModel[]
            {
                new(new ScenarioEventPopupMessage([new ScenarioTextSpan("The elves speak.", ScenarioTextSpanType.Text)], string.Empty, new AssetSpriteKey.UnitEventPortrait(portraitId), IsPortraitLeft: true))
            }));
    }

    [Test]
    public async Task ScenarioPopupOtherTriggererTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer computer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Undead],
            VerifiedRaces: [WorldRace.Undead],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects: [new ScenarioEventPopupEffect(new ScenarioTextId("Rise, my minions."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, human.Id))
            .WithDefinition(TestHelpers.SimpleLord(computer.LordId, computer.Race, LordCategory.Warrior))
            .WithPlayer(computer)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public async Task ScenarioPopupForAllPlayersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer computer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Undead],
            VerifiedRaces: [WorldRace.Undead],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects: [new ScenarioEventPopupEffect(new ScenarioTextId("The dead walk again."), ScenarioEventPopupAudience.AllPlayers, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, human.Id))
            .WithDefinition(TestHelpers.SimpleLord(computer.LordId, computer.Race, LordCategory.Warrior))
            .WithPlayer(computer)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(
            popup.Messages,
            Is.EqualTo(new ScenarioEventPopupMessageViewModel[]
            {
                new(new ScenarioEventPopupMessage([new ScenarioTextSpan("The dead walk again.", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true))
            }));
    }

    [Test]
    public async Task ScenarioPopupForAffectedRacesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer computer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Undead, WorldRace.Human],
            VerifiedRaces: [WorldRace.Undead, WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects: [new ScenarioEventPopupEffect(new ScenarioTextId("The graves are empty."), ScenarioEventPopupAudience.AffectedRaces, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, human.Id))
            .WithDefinition(TestHelpers.SimpleLord(computer.LordId, computer.Race, LordCategory.Warrior))
            .WithPlayer(computer)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(
            popup.Messages,
            Is.EqualTo(new ScenarioEventPopupMessageViewModel[]
            {
                new(new ScenarioEventPopupMessage([new ScenarioTextSpan("The graves are empty.", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true))
            }));
    }

    [Test]
    public async Task ScenarioPopupUnaffectedRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer computer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Undead],
            VerifiedRaces: [WorldRace.Undead],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects: [new ScenarioEventPopupEffect(new ScenarioTextId("The graves are empty."), ScenarioEventPopupAudience.AffectedRaces, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, human.Id))
            .WithDefinition(TestHelpers.SimpleLord(computer.LordId, computer.Race, LordCategory.Warrior))
            .WithPlayer(computer)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public void FogSwitchTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["?."])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(gameViewModel.DevTools.IsFogEnabled, Is.True);
        Assert.That(renderState, Is.EqualTo(new WorldRenderState(
            Terrain: new WorldRenderState.MapTerrain(
                map,
                [
                    new WorldRenderState.MapTile(
                        new WorldPosition(0, 0),
                        new WorldRenderState.MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: null),
                    new WorldRenderState.MapTile(
                        new WorldPosition(1, 0),
                        new WorldRenderState.MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: null)
                ],
                [WorldMapTerrain.Neutral],
                []),
            MapObjects: [],
            Stacks: [],
            Animation: null,
            Clouds: [],
            FogTiles:
            [
                new WorldRenderState.FogTile(new WorldPosition(0, 0))
            ],
            ObjectiveMarkers: [], AmbientSounds: [],
            SelectedPath: null,
            Cursors: [],
            Reach: null,
            Highlights: new WorldRenderState.HighlightOverlay([]),
            Minimap: new MinimapState(
                2,
                1,
                [
                    new WorldPosition(0, 0)
                ],
                []))));

        gameViewModel.DevTools.IsFogEnabled = false;

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            FogTiles = [],
            Minimap = new MinimapState(
                2,
                1,
                [],
                [])
        }));
    }

    [Test]
    public void StackItemsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "...",
                "?.?"
            ])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack first = TestHelpers.SimpleStack(
            new WorldStackId("First"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 2, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack enemy = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack second = TestHelpers.SimpleStack(
            new WorldStackId("Second"),
            player1.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader3"), leader.Id, 100, 0, 3, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack hidden = TestHelpers.SimpleStack(
            new WorldStackId("Hidden"),
            player1.Id,
            new WorldPosition(0, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader4"), leader.Id, 100, 0, 4, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack third = TestHelpers.SimpleStack(
            new WorldStackId("Third"),
            player1.Id,
            new WorldPosition(1, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader5"), leader.Id, 100, 0, 2, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                leader.Id,
                UpgradeChangeLevel: 10,
                UnitDynamicUpgrade.Empty,
                UnitDynamicUpgrade.Empty))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(first)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(enemy)
            .WithStack(second)
            .WithStack(hidden)
            .WithStack(third)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Second | unit-event-portrait/leader | name | level 3 | 6 / 6 | - | -",
            "First | unit-event-portrait/leader | name | level 2 | 6 / 6 | - | -",
            "Third | unit-event-portrait/leader | name | level 2 | 6 / 6 | - | -"
        ]));

        world.SelectStackItemCommand.Execute(third.Id);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with { Cursors = [] }));
        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Second | unit-event-portrait/leader | name | level 3 | 6 / 6 | - | -",
            "First | unit-event-portrait/leader | name | level 2 | 6 / 6 | - | -",
            "Third | unit-event-portrait/leader | name | level 2 | 6 / 6 | selected | -"
        ]));
    }

    [Test]
    public void SelectStackItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        List<(WorldPosition Position, TimeSpan Duration)> cameraFocusRequests = [];
        world.CameraFocusRequested += (position, duration) => cameraFocusRequests.Add((position, duration));

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(0, 0)));

        world.SelectStackItemCommand.Execute(stack.Id);

        WorldRenderState selectedState = BuildWorldState(world);

        Assert.That(selectedState, Is.EqualTo(selectedState with { Cursors = [] }));
        Assert.That(cameraFocusRequests, Is.EqualTo([(new WorldPosition(0, 0), TimeSpan.Zero)]));

        world.SelectStackItemCommand.Execute(stack.Id);

        Assert.That(cameraFocusRequests, Is.EqualTo([(new WorldPosition(0, 0), TimeSpan.Zero), (new WorldPosition(0, 0), TimeSpan.Zero)]));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public void SelectionPreservesTerrainTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack first = TestHelpers.SimpleStack(
            new WorldStackId("First"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack second = TestHelpers.SimpleStack(
            new WorldStackId("Second"),
            player.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(first)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(second)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        WorldRenderComposer composer = new(world.GameModel.Definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        WorldRenderState initialState = composer.Compose(world.RenderRequest);

        world.SelectStackItemCommand.Execute(first.Id);

        WorldRenderState renderState = composer.Compose(world.RenderRequest);

        Assert.That(renderState, Is.EqualTo(renderState with { Cursors = [] }));
        Assert.That(renderState.Terrain, Is.SameAs(initialState.Terrain));
        Assert.That(renderState.MapObjects, Is.SameAs(initialState.MapObjects));

        world.SelectStackItemCommand.Execute(second.Id);

        renderState = composer.Compose(world.RenderRequest);

        Assert.That(renderState, Is.EqualTo(renderState with { Cursors = [] }));
        Assert.That(renderState.Terrain, Is.SameAs(initialState.Terrain));
        Assert.That(renderState.MapObjects, Is.SameAs(initialState.MapObjects));
    }

    [Test]
    public void CameraFocusTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithCapital(capital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));
        Assert.That(renderState, Is.EqualTo(renderState with { Cursors = [] }));
    }

    [Test]
    public async Task CameraFocusStartTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCapital capital1 = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital1"),
            player1.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldCapital capital2 = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital2"),
            player2.Id,
            new WorldPosition(9, 9),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCapital(capital1)
            .WithCapital(capital2)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        HotseatTurnScreenViewModel firstTurn = Assert.IsOfType<HotseatTurnScreenViewModel>(gameViewModel.TopScreen);

        firstTurn.Close();
        world.CompleteAnimation();
        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        HotseatTurnScreenViewModel secondTurn = Assert.IsOfType<HotseatTurnScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));

        secondTurn.Close();

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(9, 9)));
    }

    [Test]
    public async Task CameraFocusStartTurnStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack alpha = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player.Id,
            new WorldPosition(2, 7),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack beta = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player.Id,
            new WorldPosition(7, 2),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 3, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                leader.Id,
                UpgradeChangeLevel: 10,
                UnitDynamicUpgrade.Empty,
                UnitDynamicUpgrade.Empty))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(alpha)
            .WithStack(beta)
            .WithCapital(capital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Beta | unit-event-portrait/leader | name | level 3 | 6 / 6 | - | -",
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(7, 2)));
    }

    [Test]
    public async Task CameraFocusStartTurnSelectedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack alpha = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player.Id,
            new WorldPosition(2, 7),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack beta = TestHelpers.SimpleStack(
            new WorldStackId("Beta"),
            player.Id,
            new WorldPosition(7, 2),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 3, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                leader.Id,
                UpgradeChangeLevel: 10,
                UnitDynamicUpgrade.Empty,
                UnitDynamicUpgrade.Empty))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(alpha)
            .WithStack(beta)
            .WithCapital(capital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));

        world.SelectStackItemCommand.Execute(alpha.Id);
        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Beta | unit-event-portrait/leader | name | level 3 | 6 / 6 | - | -",
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -"
        ]));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(2, 7)));
    }

    [Test]
    public async Task CameraFocusStartTurnCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            human.Id,
            new WorldPosition(6, 6),
            UnitGrid<WorldUnit>.Empty);

        WorldStack mover = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                ai.Id,
                new WorldPosition(0, 0),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
                movementPoints: 6) with
            {
                Order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("Target"), new WorldPosition(2, 0), Radius: 0)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(mover)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithCapital(capital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        List<WorldPosition?> focuses = [];
        world.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorldScreenViewModel.CameraFocus))
            {
                focuses.Add(world.CameraFocus);
            }

            if (e.PropertyName == nameof(WorldScreenViewModel.RenderRequest) && world.RenderRequest.Animation is not null)
            {
                world.CompleteAnimation();
            }
        };

        await world.EndTurnCommand.ExecuteAsync(null);

        Assert.That(focuses, Is.EqualTo([new WorldPosition(1, 0), new WorldPosition(6, 6)]));
    }

    [Test]
    public async Task StackMoveAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(2, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(2, 0), 1344, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4")
            ])
        }));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 0)));

        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Stacks = [],
            Minimap = renderState.Minimap with
            {
                Markers = [new MinimapState.Marker(new WorldPosition(2, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack)]
            },
            Cursors = [],
            SelectedPath = null,
            Animation = new WorldRenderState.StackMove(
                stack.Id,
                new AssetSoundKey.StackUnitMove(leader.Id, Variant: 2),
                new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                [
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(0, 0),
                        391,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.Zero),
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(1, 0),
                        903,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.FromMilliseconds(200)),
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(2, 0),
                        1415,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.FromMilliseconds(400))
                ],
                Opacity: 1.0,
                IsObjective: false,
                Highlight: Theme.World.Highlight.SelectedColor)
        }));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(2, 0)));

        world.CompleteAnimation();

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            MapObjects = [],
            Stacks =
            [
                new WorldRenderState.Stack(
                    Position: new WorldPosition(2, 0),
                    Depth: 1415,
                    AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap = renderState.Minimap with
            {
                Markers = [new MinimapState.Marker(new WorldPosition(2, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack)]
            },
            Cursors = [],
            Animation = null
        }));
    }

    [Test]
    public async Task StackMoveFogRevealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["....?"])
        };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                VisionRadius = 1
            };
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(3, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            FogTiles = [new WorldRenderState.FogTile(new WorldPosition(4, 0))],
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(3, 0), 1856, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4"),
                new WorldRenderState.PathStep(new WorldPosition(3, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "6")
            ])
        }));

        await world.HandleMapClickAsync(new WorldPosition(3, 0), [], 1);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            FogTiles = [],
            Stacks = [],
            Minimap = renderState.Minimap with
            {
                HiddenPositions = [],
                Markers = [new MinimapState.Marker(new WorldPosition(3, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack)]
            },
            Cursors = [],
            Animation = new WorldRenderState.StackMove(
                stack.Id,
                new AssetSoundKey.StackUnitMove(leader.Id, Variant: 1),
                new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                [
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(0, 0),
                        391,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.Zero),
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(1, 0),
                        903,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.FromMilliseconds(200)),
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(2, 0),
                        1415,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.FromMilliseconds(400)),
                    new WorldRenderState.StackMoveStep(
                        new WorldPosition(3, 0),
                        1927,
                        new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                        ShadowKey: null,
                        AuraKey: null,
                        TimeSpan.FromMilliseconds(600))
                ],
                Opacity: 1.0,
                IsObjective: false,
                Highlight: Theme.World.Highlight.SelectedColor)
        }));

        world.CompleteAnimation();

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            FogTiles = [],
            Stacks =
            [
                new WorldRenderState.Stack(
                    Position: new WorldPosition(3, 0),
                    Depth: 1927,
                    AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap = renderState.Minimap with
            {
                HiddenPositions = [],
                Markers = [new MinimapState.Marker(new WorldPosition(3, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack)]
            },
            Cursors = [],
            SelectedPath = null,
            Animation = null
        }));
    }

    [Test]
    public async Task StackDestroyAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Summoned"),
                player.Id,
                new WorldPosition(1, 0),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
                movementPoints: 6) with
            {
                ExpirationTurn = 1
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(world.RenderRequest.Animation, Is.EqualTo(
            new WorldAnimationState.StackDestroy(new WorldPosition(1, 0), leader.Id)));
        Assert.That(world.RenderRequest.State.Stacks, Is.Empty);

        world.CompleteAnimation();

        Assert.That(world.RenderRequest.Animation, Is.Null);
        Assert.That(world.RenderRequest.State, Is.SameAs(gameModel.Snapshot.WorldState));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task CapitalStackCreateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition race = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leader.Id],
            Units: [],
            MaleLeaderNames: ["Nigosh"],
            FemaleLeaderNames: ["Orissia"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital1"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithRace(race)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        await gameModel.World.DispatchCommandAsync(new WorldPurchaseLeaderCommand(capital.Id, leader.Id));

        Assert.That(world.RenderRequest.Animation, Is.Null);
        Assert.That(world.RenderRequest.State, Is.SameAs(gameModel.Snapshot.WorldState));
        Assert.That(world.RenderRequest.State.Stacks.Keys, Is.EqualTo([new WorldStackId("Stack0001")]));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task AiStackMoveAnimationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack mover = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                ai.Id,
                new WorldPosition(0, 0),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
                movementPoints: 6) with
            {
                Order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("Target"), new WorldPosition(2, 0), Radius: 0)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(mover)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        List<WorldAnimationState> animations = [];
        world.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorldScreenViewModel.RenderRequest) && world.RenderRequest.Animation is { } current)
            {
                animations.Add(current);
                world.CompleteAnimation();
            }
        };

        await world.EndTurnCommand.ExecuteAsync(null);

        Assert.That(animations, Is.EqualTo([
            new WorldAnimationState.StackMove(
                mover.Id,
                [
                    new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                    new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(200)),
                    new WorldAnimationState.StackMoveStep(new WorldPosition(2, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(400))
                ])
        ]));
        Assert.That(world.RenderRequest.Animation, Is.Null);
        Assert.That(world.RenderRequest.State, Is.SameAs(gameModel.Snapshot.WorldState));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(1, 0)));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[mover.Id].Position, Is.EqualTo(new WorldPosition(2, 0)));
        Assert.That(gameModel.Snapshot.WorldState.TurnInfo.CurrentPlayerId, Is.EqualTo(human.Id));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task AiStackMoveHiddenTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["???."])
        };
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack mover = TestHelpers.SimpleStack(
                new WorldStackId("Mover"),
                ai.Id,
                new WorldPosition(0, 0),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
                movementPoints: 6) with
            {
                Order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("Target"), new WorldPosition(2, 0), Radius: 0)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(mover)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        List<WorldAnimationState> animations = [];
        world.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorldScreenViewModel.RenderRequest) && world.RenderRequest.Animation is { } current)
            {
                animations.Add(current);
                world.CompleteAnimation();
            }
        };

        await world.EndTurnCommand.ExecuteAsync(null);

        Assert.That(animations, Is.Empty);
        Assert.That(gameModel.Snapshot.WorldState.Stacks[mover.Id].Position, Is.EqualTo(new WorldPosition(2, 0)));
        Assert.That(gameModel.Snapshot.WorldState.TurnInfo.CurrentPlayerId, Is.EqualTo(human.Id));
    }

    [Test]
    public async Task AiTurnCommandsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 2);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack otherHumanStack = TestHelpers.SimpleStack(
            new WorldStackId("OtherHumanStack"),
            human.Id,
            new WorldPosition(4, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack aiStack = TestHelpers.SimpleStack(
                new WorldStackId("AiStack"),
                ai.Id,
                new WorldPosition(2, 0),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(new WorldUnit(new WorldUnitId("leader3"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Stand.Instance
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(humanStack)
            .WithStack(otherHumanStack)
            .WithStack(aiStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
        Assert.That(world.SelectStackItemCommand.CanExecute(otherHumanStack.Id), Is.True);
        Assert.That(world.FastForwardCommand.CanExecute(null), Is.False);

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        Assert.That(world.EndTurnCommand.CanExecute(null), Is.False);
        Assert.That(world.SelectStackItemCommand.CanExecute(otherHumanStack.Id), Is.False);
        Assert.That(world.OpenSpellbookCommand.CanExecute(null), Is.False);

        world.UpdateHover(new WorldPosition(4, 0), [new WorldPosition(4, 0)]);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Wait));

        Assert.That(world.FastForwardCommand.CanExecute(null), Is.True);

        world.FastForwardCommand.Execute(null);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        Assert.That(world.FastForwardCommand.CanExecute(null), Is.False);

        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);
        world.UpdateHover(new WorldPosition(1, 0), []);

        WorldRenderState state = BuildWorldState(world);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsFastForward, Is.True);
        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Wait));
        Assert.That(state, Is.EqualTo(state with { Cursors = [] }));

        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(aiStack.Id, new WorldPosition(2, 1)));

        Assert.That(gameModel.Snapshot.WorldState.Stacks[aiStack.Id].Position, Is.EqualTo(new WorldPosition(2, 1)));
        Assert.That(world.IsFastForward, Is.True);

        world.CompleteAnimation();

        Assert.That(world.RenderRequest.Animation, Is.Null);
        Assert.That(world.IsFastForward, Is.True);

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        Assert.That(gameModel.Snapshot.WorldState.TurnInfo.CurrentPlayerId, Is.EqualTo(human.Id));
        Assert.That(world.IsFastForward, Is.False);
        Assert.That(world.FastForwardCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task HoverPathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [],
            SelectedPath = null
        }));

        world.UpdateHover(new WorldPosition(2, 0), []);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(2, 0), 1344, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4")
            ])
        }));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 0)));

        world.UpdateHover(new WorldPosition(3, 0), []);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(3, 0), 1856, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4"),
                new WorldRenderState.PathStep(new WorldPosition(3, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "6")
            ])
        }));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 0)));

        await world.HandleMapClickAsync(new WorldPosition(3, 0), [], 1);
        world.CompleteAnimation();
        world.CompleteAnimation();
        world.CompleteAnimation();

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Stacks =
            [
                new WorldRenderState.Stack(
                    Position: new WorldPosition(3, 0),
                    Depth: 1927,
                    AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap = renderState.Minimap with
            {
                Markers = [new MinimapState.Marker(new WorldPosition(3, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack)]
            },
            Cursors = [],
            SelectedPath = null,
            Animation = null
        }));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(3, 0)));
    }

    [Test]
    public async Task SelectionAcrossTurnsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);

        await world.EndTurnCommand.ExecuteAsync(null);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [],
            Reach = new WorldRenderState.ReachOverlay(
            [
                new WorldPosition(0, 0),
                new WorldPosition(1, 0),
                new WorldPosition(2, 0),
                new WorldPosition(3, 0)
            ]),
            SelectedPath = null
        }));

        await world.HandleMapClickAsync(new WorldPosition(3, 0), [], 1);
        world.CompleteAnimation();
        world.CompleteAnimation();
        world.CompleteAnimation();

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Stacks =
            [
                new WorldRenderState.Stack(
                    Position: new WorldPosition(3, 0),
                    Depth: 1927,
                    AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap = renderState.Minimap with
            {
                Markers = [new MinimapState.Marker(new WorldPosition(3, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack)]
            },
            Cursors = [],
            SelectedPath = null,
            Animation = null
        }));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(3, 0)));
    }

    [Test]
    public async Task PathAfterAiTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);

        await world.EndTurnCommand.ExecuteAsync(null);

        world.UpdateHover(new WorldPosition(3, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(3, 0), 1856, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4"),
                new WorldRenderState.PathStep(new WorldPosition(3, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "6")
            ])
        }));
        Assert.That(gameModel.Snapshot.WorldState.TurnInfo.CurrentPlayerId, Is.EqualTo(human.Id));
    }

    [Test]
    public async Task PathAfterWorldChangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        WorldStack other = TestHelpers.SimpleStack(
            new WorldStackId("Other"),
            player1.Id,
            new WorldPosition(5, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(other)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(3, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(3, 0), 1856, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4"),
                new WorldRenderState.PathStep(new WorldPosition(3, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "6")
            ])
        }));

        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(other.Id, new WorldPosition(2, 0)));
        world.CompleteAnimation();
        world.CompleteAnimation();
        world.CompleteAnimation();

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Stacks =
            [
                new WorldRenderState.Stack(
                    Position: new WorldPosition(0, 0),
                    Depth: 391,
                    AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0),
                new WorldRenderState.Stack(
                    Position: new WorldPosition(2, 0),
                    Depth: 1415,
                    AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 2),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap = renderState.Minimap with
            {
                Markers =
                [
                    new MinimapState.Marker(new WorldPosition(2, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack),
                    new MinimapState.Marker(new WorldPosition(0, 0), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Stack)
                ]
            },
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(3, 0), 1856, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 1), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4"),
                new WorldRenderState.PathStep(new WorldPosition(3, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "6")
            ])
        }));
    }

    [Test]
    public async Task HoverPathFollowsSelectionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        WorldStack other = TestHelpers.SimpleStack(
            new WorldStackId("Other"),
            player.Id,
            new WorldPosition(5, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithStack(other)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(3, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(3, 0), 1856, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4"),
                new WorldRenderState.PathStep(new WorldPosition(3, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "6")
            ])
        }));

        await world.HandleMapClickAsync(new WorldPosition(5, 1), [], 1);
        world.UpdateHover(new WorldPosition(2, 1), []);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(2, 1), 1856, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(5, 1),
            [
                new WorldRenderState.PathStep(new WorldPosition(4, 1), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(3, 1), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4"),
                new WorldRenderState.PathStep(new WorldPosition(2, 1), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "6")
            ])
        }));
    }

    [Test]
    public async Task HoverCursorTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.UpdateHover(new WorldPosition(1, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = []
        }));

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(2, 0), []);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors =
            [
                new WorldRenderState.Cursor(new WorldPosition(2, 0), 1344, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)
            ],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4")
            ])
        }));

        world.UpdateHover(new WorldPosition(0, 0), []);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = []
        }));

        world.UpdateHover(new WorldPosition(1, 0), []);
        world.ClearHover();

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = []
        }));
    }

    [Test]
    public async Task HoverStackMarkersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.UpdateHover(new WorldPosition(0, 0), [new WorldPosition(1, 0), new WorldPosition(0, 0)]);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors =
            [
                new WorldRenderState.Cursor(new WorldPosition(0, 0), 30, Theme.World.HoverCursorInnerColor, Theme.World.HoverCursorOuterColor, 1.0),
                new WorldRenderState.Cursor(new WorldPosition(1, 0), 542, Theme.World.HoverCursorInnerColor, Theme.World.HoverCursorOuterColor, 1.0)
            ]
        }));

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);
        world.UpdateHover(new WorldPosition(2, 0), [new WorldPosition(1, 0)]);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors =
            [
                new WorldRenderState.Cursor(new WorldPosition(2, 0), 1344, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)
            ]
        }));
    }

    [Test]
    public async Task OpenCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        gameViewModel.DevTools.IsFogEnabled = false;

        Assert.That(world.OpenCapitalCommand.CanExecute(null), Is.True);

        world.OpenCapitalCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(screen.Tabs.Select(tab => tab.TabHeader), Is.EqualTo(["City", "Party", "Buildings", "Spellbook", "Diplomacy"]));
        Assert.IsOfType<CityTabViewModel>(screen.SelectedTab);

        gameViewModel.CloseScreen(screen);

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ManagementScreenViewModel>());

        gameViewModel.CloseScreen(Assert.NotNull(gameViewModel.TopScreen));

        await world.HandleMapClickAsync(new WorldPosition(7, 7), [], 1);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public async Task OpenStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(2, 2),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        gameViewModel.DevTools.IsFogEnabled = false;

        Assert.That(world.OpenStackCommand.CanExecute(null), Is.False);

        world.SelectStackItemCommand.Execute(stack.Id);

        Assert.That(world.OpenStackCommand.CanExecute(null), Is.True);

        world.OpenStackCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(screen.SelectedTab, Is.InstanceOf<MapStackTabViewModel>());
    }

    [Test]
    public async Task MusicOnViewPlayerChangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        IReadOnlyList<AssetSoundKey> tracks =
        [
            new AssetSoundKey.AmbientMusic(3),
            new AssetSoundKey.WorldMusic(7),
            new AssetSoundKey.AmbientMusic(5),
            new AssetSoundKey.WorldMusic(9),
            new AssetSoundKey.AmbientMusic(7),
            new AssetSoundKey.WorldMusic(8),
            new AssetSoundKey.AmbientMusic(6),
            new AssetSoundKey.WorldMusic(0),
            new AssetSoundKey.AmbientMusic(10),
            new AssetSoundKey.WorldMusic(5),
            new AssetSoundKey.AmbientMusic(1),
            new AssetSoundKey.WorldMusic(2),
            new AssetSoundKey.AmbientMusic(4),
            new AssetSoundKey.WorldMusic(10),
            new AssetSoundKey.AmbientMusic(0),
            new AssetSoundKey.WorldMusic(4),
            new AssetSoundKey.AmbientMusic(2),
            new AssetSoundKey.WorldMusic(6),
            new AssetSoundKey.AmbientMusic(9),
            new AssetSoundKey.WorldMusic(1),
            new AssetSoundKey.AmbientMusic(8),
            new AssetSoundKey.WorldMusic(3)
        ];

        MusicQueue queue = Assert.NotNull(world.Music.Queue);

        Assert.That(queue.Tracks, Is.EqualTo(tracks));

        session.World.DispatchCommand(new WorldStartGameCommand());
        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        MusicQueue queueAfterFirstTurn = Assert.NotNull(world.Music.Queue);

        Assert.That(gameModel.Snapshot.ViewPlayerId, Is.EqualTo(player2.Id));
        Assert.That(queueAfterFirstTurn.Tracks, Is.EqualTo(tracks));

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        MusicQueue queueAfterSecondTurn = Assert.NotNull(world.Music.Queue);

        Assert.That(gameModel.Snapshot.ViewPlayerId, Is.EqualTo(player1.Id));
        Assert.That(queueAfterSecondTurn.Tracks, Is.EqualTo(tracks));
    }

    [Test]
    public async Task PerspectiveOnAiTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(Gold: 250, DeathMana: 1, RuneMana: 2, LifeMana: 3, InfernalMana: 4, GroveMana: 5)
        };
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(Gold: 900, DeathMana: 91, RuneMana: 92, LifeMana: 93, InfernalMana: 94, GroveMana: 95)
        };

        WorldCapital humanCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("HumanCapital"),
            human.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldCapital aiCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("AiCapital"),
            ai.Id,
            new WorldPosition(9, 9),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithCapital(humanCapital)
            .WithCapital(aiCapital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        IReadOnlyList<AssetSoundKey> tracks =
        [
            new AssetSoundKey.AmbientMusic(3),
            new AssetSoundKey.WorldMusic(7),
            new AssetSoundKey.AmbientMusic(5),
            new AssetSoundKey.WorldMusic(9),
            new AssetSoundKey.AmbientMusic(7),
            new AssetSoundKey.WorldMusic(8),
            new AssetSoundKey.AmbientMusic(6),
            new AssetSoundKey.WorldMusic(0),
            new AssetSoundKey.AmbientMusic(10),
            new AssetSoundKey.WorldMusic(5),
            new AssetSoundKey.AmbientMusic(1),
            new AssetSoundKey.WorldMusic(2),
            new AssetSoundKey.AmbientMusic(4),
            new AssetSoundKey.WorldMusic(10),
            new AssetSoundKey.AmbientMusic(0),
            new AssetSoundKey.WorldMusic(4),
            new AssetSoundKey.AmbientMusic(2),
            new AssetSoundKey.WorldMusic(6),
            new AssetSoundKey.AmbientMusic(9),
            new AssetSoundKey.WorldMusic(1),
            new AssetSoundKey.AmbientMusic(8),
            new AssetSoundKey.WorldMusic(3)
        ];

        MusicQueue queue = Assert.NotNull(world.Music.Queue);

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));
        Assert.That(queue.Tracks, Is.EqualTo(tracks));

        session.World.DispatchCommand(new WorldStartGameCommand());
        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        MusicQueue queueAfterAiTurn = Assert.NotNull(world.Music.Queue);

        Assert.That(gameModel.Snapshot.ViewPlayerId, Is.EqualTo(human.Id));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));
        Assert.That(queueAfterAiTurn.Tracks, Is.EqualTo(tracks));

        WorldStatusViewModel expectedStatus = new(
            human.Id,
            1,
            new AssetSpriteKey.LordFace(WorldRace.Undead, 0, IsLarge: false),
            new ResourceSetViewModel(new WorldResourceSet(350, 26, 2, 3, 4, 5), new WorldResourceSet(250, 1, 2, 3, 4, 5), ResourceSetProjection.All));

        Assert.That(world.Status, Is.EqualTo(expectedStatus));
    }

    [Test]
    public async Task CameraFocusOnBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition attacker = TestHelpers.SimpleUnitDefinition("Unit_Attacker", maxHitpoints: 100, initiative: 50, damage: 50) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitDefinition defender = TestHelpers.SimpleUnitDefinition("Unit_Defender", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(attacker)
            .AddUnit(defender)
            .Build();

        WorldCapital humanCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("HumanCapital"),
            human.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("AiStack"),
            ai.Id,
            new WorldPosition(8, 8),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("attacker"), attacker.Id, 100, 3, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(9, 8),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("defender"), defender.Id, 10, 4, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, ai.Id))
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithCapital(humanCapital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(aiStack)
            .WithStack(humanStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(gameModel.Snapshot.ViewPlayerId, Is.EqualTo(human.Id));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));

        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(aiStack.Id, humanStack.Position));

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(9, 8)));
    }

    [Test]
    public async Task CameraFocusOnCaptureTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldCapital humanCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("HumanCapital"),
            human.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1),
            WorldResourceSet.Empty);
        WorldCity humanCity = TestHelpers.SimpleCity(
            new WorldFortificationId("HumanCity"),
            cityDefinition,
            human.Id,
            new WorldPosition(8, 8),
            UnitGrid<WorldUnit>.Empty);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("AiStack"),
            ai.Id,
            new WorldPosition(5, 9),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, ai.Id))
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithCapital(humanCapital)
            .WithCity(humanCity)
            .WithStack(aiStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(gameModel.Snapshot.ViewPlayerId, Is.EqualTo(human.Id));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));

        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(aiStack.Id, humanCity.Entrance));

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(6, 9)));

        world.CompleteAnimation();

        WorldCity capturedCity = Assert.IsOfType<WorldCity>(gameModel.Snapshot.WorldState.Fortifications[humanCity.Id]);

        Assert.That(capturedCity.OwnerId, Is.EqualTo(ai.Id));
        Assert.That(world.CameraFocus, Is.EqualTo(humanCity.Entrance));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.Pause(TimeSpan.FromSeconds(0.75))));

        world.CompleteAnimation();

        Assert.That(world.RenderRequest.Animation, Is.Null);
    }

    [Test]
    public async Task CameraFocusOnScenarioPopupTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(8, 8),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(
            new ScenarioLocationId("Zone1"),
            new ScenarioTextId("Greeting"),
            new WorldPosition(6, 6),
            0);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventEnteringZoneCondition(zone.Id)],
            Effects: [new ScenarioEventPopupEffect(new ScenarioTextId("Welcome."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: false, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .WithScenarioLocation(zone)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(stack.Id, zone.Position));

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(7, 7)));

        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.Pause(TimeSpan.FromSeconds(0.25))));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(6, 6)));

        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ScenarioEventPopupViewModel>());
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(6, 6)));
    }

    [Test]
    public async Task CameraFocusOnUnfogTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map) with { IsAi = true };
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(8, 8),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        ScenarioLocationDefinition zone = new(
            new ScenarioLocationId("Zone1"),
            new ScenarioTextId("Greeting"),
            new WorldPosition(6, 6),
            0);

        ScenarioLocationDefinition crypt = new(
            new ScenarioLocationId("Crypt"),
            new ScenarioTextId("Crypt"),
            new WorldPosition(8, 1),
            2);

        ScenarioLocationDefinition watchtower = new(
            new ScenarioLocationId("Watchtower"),
            new ScenarioTextId("Watchtower"),
            new WorldPosition(1, 2),
            2);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventEnteringZoneCondition(zone.Id)],
            Effects:
            [
                new ScenarioEventUnfogEffect(crypt.Id, Reveal: true, Radius: null, [other.Id]),
                new ScenarioEventUnfogEffect(watchtower.Id, Reveal: true, Radius: null, [player.Id]),
                new ScenarioEventPopupEffect(new ScenarioTextId("The watchtower is in sight."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: false, IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithCapital(capital)
            .WithStack(stack)
            .WithScenarioLocation(zone)
            .WithScenarioLocation(crypt)
            .WithScenarioLocation(watchtower)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        await gameModel.World.DispatchCommandAsync(new WorldMoveStackCommand(stack.Id, zone.Position));

        world.CompleteAnimation();
        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ScenarioEventPopupViewModel>());
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(6, 6)));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.Pause(TimeSpan.FromSeconds(0.75))));
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(1, 2)));

        world.CompleteAnimation();

        Assert.That(world.RenderRequest.Animation, Is.Null);
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(1, 2)));
    }

    [Test]
    public async Task CameraFocusOnLeaderUpgradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                Race = WorldRace.Undead
            };
        UnitModifierDefinition banner = TestHelpers.SimpleModifier("banner");
        LeaderUpgradeDefinition upgrade = new(
            player.LordId,
            banner.Id,
            MinLevel: 1,
            Priority: 80,
            new GlobalTextId("nameBanner"),
            new GlobalTextId("descriptionBanner"));

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(8, 8),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                PendingLeaderUpgrades = 1
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(banner)
            .WithLeaderUpgrade(upgrade)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(4, 4)));

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<LeaderUpgradeViewModel>());
        Assert.That(world.CameraFocus, Is.EqualTo(new WorldPosition(8, 8)));
    }

    [Test]
    public void SpellCursorTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.UpdateHover(new WorldPosition(2, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with { Cursors = [] }));

        world.SelectAction(new WorldCursorAction.CastSpell(new WorldSpellDefinitionId("restore")));

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors =
            [
                new WorldRenderState.Cursor(new WorldPosition(2, 0), 1344, Theme.World.SpellCursorInnerColor, Theme.World.SpellCursorOuterColor, 1.0)
            ]
        }));

        world.CancelSelectedActionCommand.Execute(null);

        renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with { Cursors = [] }));
    }

    [Test]
    public void SpellCursorTypeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("description"),
            new GlobalTextId("effect"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(10),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.UpdateHover(new WorldPosition(0, 0), []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.SelectUnit));

        world.SelectAction(new WorldCursorAction.CastSpell(restoreId));

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.CastSpell));

        world.UpdateHover(new WorldPosition(2, 0), []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.CastSpellInvalid));

        world.ClearHover();

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Default));

        world.UpdateHover(new WorldPosition(0, 0), []);
        world.CancelSelectedActionCommand.Execute(null);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.SelectUnit));
    }

    [Test]
    public async Task ClickOnCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player.Id,
            new WorldPosition(5, 5),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(5, 7),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(5, 7), [], 1);
        world.UpdateHover(new WorldPosition(5, 5), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(5, 5), 5440, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(5, 7),
            [
                new WorldRenderState.PathStep(new WorldPosition(4, 6), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(5, 5), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2")
            ])
        }));

        await world.HandleMapClickAsync(new WorldPosition(5, 5), [], 1);

        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(5, 5)));
    }

    [Test]
    public async Task ClickOnCityBodyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player.Id,
            new WorldPosition(5, 5),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(5, 7),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(5, 7), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(3, 3), [], 1);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ManagementScreenViewModel>());
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(5, 7)));
        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [],
            SelectedPath = null
        }));
    }

    [Test]
    public async Task ClickOnEnemyCityBodyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            enemy.Id,
            new WorldPosition(5, 5),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(5, 7),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(5, 7), [], 1);
        world.UpdateHover(new WorldPosition(3, 3), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(5, 5), 5440, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(5, 7),
            [
                new WorldRenderState.PathStep(new WorldPosition(4, 6), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(5, 5), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2")
            ])
        }));

        await world.HandleMapClickAsync(new WorldPosition(3, 3), [], 1);

        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(5, 5)));
    }

    [Test]
    public async Task ClickOnSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(4, 6),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(4, 6), [], 1);
        world.UpdateHover(new WorldPosition(4, 4), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(4, 4), 4416, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(4, 6),
            [
                new WorldRenderState.PathStep(new WorldPosition(4, 5), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(4, 4), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4")
            ])
        }));

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);

        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(4, 4)));
    }

    [Test]
    public async Task ClickOnSiteBodyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(4, 6),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(4, 6), [], 1);
        world.UpdateHover(new WorldPosition(2, 2), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(4, 4), 4416, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(4, 6),
            [
                new WorldRenderState.PathStep(new WorldPosition(4, 5), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(4, 4), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "4")
            ])
        }));

        await world.HandleMapClickAsync(new WorldPosition(2, 2), [], 1);

        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(4, 4)));
    }

    [Test]
    public async Task EventsBeforeGameStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects: [new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(gameViewModel.TopScreen, Is.Null);

        gameViewModel.ShowScreen(world);

        LoadingScreenViewModel loading = Assert.IsOfType<LoadingScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(loading.Message, Is.EqualTo("Loading world…"));
        Assert.That(loading.RevealDuration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(loading.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Wait));

        world.CompleteGameStart();

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(
            popup.Messages,
            Is.EqualTo(new ScenarioEventPopupMessageViewModel[]
            {
                new(new ScenarioEventPopupMessage([new ScenarioTextSpan("Greetings Milord!", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true))
            }));
    }

    [Test]
    public async Task StackResponseSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(1, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        audioPlayer.Actions.Clear();
        world.Update(TimeSpan.FromSeconds(60), TimeSpan.Zero);

        Assert.That(audioPlayer.Actions, Is.Empty);

        await world.HandleMapClickAsync(new WorldPosition(1, 1), [], 1);
        world.SelectStackItemCommand.Execute(stack.Id);

        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:stack-unit-response/leader/3"]));

        audioPlayer.Actions.Clear();
        world.Update(TimeSpan.FromSeconds(180), TimeSpan.Zero);
        world.Update(TimeSpan.FromSeconds(180), TimeSpan.Zero);

        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:stack-unit-idle/leader/0", "Sound:stack-unit-idle/leader/1"]));
    }

    [Test]
    public async Task StackDeathSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition summonDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 1) with
            {
                Category = UnitCategory.Summon
            };

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(summonDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack summonedStack = TestHelpers.SimpleStack(
                new WorldStackId("Summon"),
                player.Id,
                new WorldPosition(2, 0),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(TestHelpers.SimpleUnit(new WorldUnitId("summon"), summonDefinition), GridPosition.FrontBottom)]),
                movementPoints: 6) with
            {
                ExpirationTurn = 1
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithStack(summonedStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        audioPlayer.Actions.Clear();

        await world.EndTurnCommand.ExecuteAsync(null);

        Assert.That(gameModel.Snapshot.WorldState.Stacks.Keys, Is.EqualTo([new WorldStackId("Stack")]));
        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:stack-unit-death/summon"]));
    }

    [Test]
    public async Task StackAttackSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(defenderDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("Defender"),
            enemy.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithStack(defenderStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAudioPlayer audioPlayer = new();
        TestAssetPreloader preloader = new();

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder()
            .WithAssets(assets)
            .WithAssetPreloader(preloader)
            .WithAudioPlayer(audioPlayer)
            .Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        audioPlayer.Actions.Clear();

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);

        Assert.That(gameModel.Snapshot.BattleState, Is.Not.Null);
        Assert.That(audioPlayer.Actions, Is.EqualTo([
            "Sound:stack-unit-attack/leader",
            "Sound:world-battle-start"
        ]));

        audioPlayer.Actions.Clear();
        world.CompleteAnimation();
        preloader.CompleteInitial();

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<BattleScreenViewModel>());
        Assert.That(audioPlayer.Actions, Is.EqualTo([
            "Stop:ambient-music/3,world-music/7,ambient-music/5,world-music/9,ambient-music/7,world-music/8,ambient-music/6,world-music/0,ambient-music/10,world-music/5,ambient-music/1,world-music/2,ambient-music/4,world-music/10,ambient-music/0,world-music/4,ambient-music/2,world-music/6,ambient-music/9,world-music/1,ambient-music/8,world-music/3",
            "Music:battle-music/2,battle-music/3,battle-music/1,battle-music/0"
        ]));
    }

    [Test]
    public async Task ConsecutivePauseAnimationsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = TestHelpers.SimpleUnitDefinition("rod-leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Rod,
            LeaderAbilities = LeaderAbility.Rod,
            Leadership = 1,
            MovementPoints = 20
        };

        WorldPlayer computer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(rodLeaderDefinition)
            .Build();

        WorldStack first = TestHelpers.SimpleStack(
            new WorldStackId("First"),
            computer.Id,
            new WorldPosition(2, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 20);

        WorldStack second = TestHelpers.SimpleStack(
            new WorldStackId("Second"),
            computer.Id,
            new WorldPosition(2, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("rodLeader2"), rodLeaderDefinition.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithTurnInfo(new WorldTurnInfo(1, computer.Id))
            .WithDefinition(TestHelpers.SimpleLord(computer.LordId, computer.Race, LordCategory.Warrior))
            .WithPlayer(computer)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(first)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(second)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldCreateRodCommand(first.Id, new WorldPosition(3, 1)));
        await gameModel.World.DispatchCommandAsync(new WorldCreateRodCommand(second.Id, new WorldPosition(3, 4)));

        Assert.That(world.RenderRequest.Animation, Is.InstanceOf<WorldAnimationState.Pause>());

        WorldRenderComposer composer = new(gameModel.Definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        AnimationPlayback<WorldRenderState.WorldAnimation> playback = new();
        TimeSpan now = TimeSpan.Zero;

        for (int frame = 0; frame < 100; frame++)
        {
            WorldRenderState state = composer.Compose(world.RenderRequest);

            playback.Register(state.Animation, now);

            if (playback.TryComplete(now))
            {
                world.CompleteAnimation();
            }

            now += TimeSpan.FromSeconds(1);
        }

        Assert.That(world.RenderRequest.Animation, Is.Null);
        Assert.That(world.RenderRequest.State, Is.SameAs(gameModel.Snapshot.WorldState));
    }

    [Test]
    public async Task AnimationAcrossRefreshesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition rodLeaderDefinition = TestHelpers.SimpleUnitDefinition("rod-leader", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Rod,
            LeaderAbilities = LeaderAbility.Rod,
            Leadership = 1,
            MovementPoints = 20
        };

        WorldPlayer computer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(rodLeaderDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("First"),
            computer.Id,
            new WorldPosition(2, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("rodLeader"), rodLeaderDefinition.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodLeaderDefinition)
            .WithTurnInfo(new WorldTurnInfo(1, computer.Id))
            .WithDefinition(TestHelpers.SimpleLord(computer.LordId, computer.Race, LordCategory.Warrior))
            .WithPlayer(computer)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldCreateRodCommand(stack.Id, new WorldPosition(3, 1)));

        WorldAnimationState? animation = world.RenderRequest.Animation;

        Assert.That(animation, Is.InstanceOf<WorldAnimationState.Pause>());

        gameViewModel.DevTools.IsFogEnabled = false;

        Assert.That(world.RenderRequest.Animation, Is.SameAs(animation));
    }

    [Test]
    public async Task PickUpBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        ItemDefinition potion = new(
            new ItemDefinitionId("Potion"),
            new GlobalTextId("Healing potion"),
            new GlobalTextId("Heals 50 hit points"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);
        ItemDefinition elixir = new(
            new ItemDefinitionId("Elixir"),
            new GlobalTextId("Elixir of life"),
            new GlobalTextId("Heals 50 hit points"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldInventory items = new(
        [
            new WorldItem(new WorldItemId("Potion"), potion.Id, Charges: null),
            new WorldItem(new WorldItemId("Elixir"), elixir.Id, Charges: null),
            new WorldItem(new WorldItemId("SecondPotion"), potion.Id, Charges: null)
        ]);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(2, 0),
            new WorldObjectVariant(0),
            items,
            AiPriority: 3);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(potion)
            .WithDefinition(elixir)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(2, 0), []);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [new WorldRenderState.Cursor(new WorldPosition(2, 0), 1344, Theme.World.Path.UnreachedInnerColor, Theme.World.Path.UnreachedOuterColor, 1.0)],
            SelectedPath = new WorldRenderState.PathOverlay(new WorldPosition(0, 0),
            [
                new WorldRenderState.PathStep(new WorldPosition(1, 0), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
                new WorldRenderState.PathStep(new WorldPosition(2, 0), Theme.World.Path.UnreachedInnerColor, Theme.World.Path.UnreachedOuterColor, "4")
            ])
        }));

        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(1, 0)));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Inventory, Is.EqualTo(items));
        Assert.That(gameModel.Snapshot.WorldState.Bags, Is.Empty);
        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        world.CompleteAnimation();

        LootScreenViewModel loot = Assert.IsOfType<LootScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(loot.Title, Is.EqualTo("Treasure"));
        Assert.That(loot.Text, Is.EqualTo("You received 3 items."));
        Assert.That(
            loot.Items,
            Is.EqualTo(new LootItem[]
            {
                new("2 × Healing potion", "Heals 50 hit points", new IconViewModel.Item(new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(potion.Id)))),
                new("Elixir of life", "Heals 50 hit points", new IconViewModel.Item(new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(elixir.Id))))
            }));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsActive, Is.True);
    }

    [Test]
    public async Task BagCursorTypeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(2, 0),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("BagItem"), new ItemDefinitionId("Item1"), Charges: null)]),
            AiPriority: 3);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.UpdateHover(new WorldPosition(2, 0), []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Default));

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(2, 0), []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.TakeBag));

        world.UpdateHover(new WorldPosition(1, 0), []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Move));
    }

    [Test]
    public async Task PickUpAdjacentBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        ItemDefinition potion = new(
            new ItemDefinitionId("Potion"),
            new GlobalTextId("Healing potion"),
            new GlobalTextId("Heals 50 hit points"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(1, 0),
            new WorldObjectVariant(0),
            new WorldInventory([potionItem]),
            AiPriority: 3);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithBag(bag)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);

        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 0)));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].MovementPoints, Is.EqualTo(6));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Inventory, Is.EqualTo(new WorldInventory([potionItem])));
        Assert.That(gameModel.Snapshot.WorldState.Bags, Is.Empty);

        LootScreenViewModel loot = Assert.IsOfType<LootScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(loot.Title, Is.EqualTo("Treasure"));
        Assert.That(loot.Text, Is.EqualTo("You received an item."));
        Assert.That(
            loot.Items,
            Is.EqualTo(new LootItem[]
            {
                new("Healing potion", "Heals 50 hit points", new IconViewModel.Item(new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(potion.Id))))
            }));
    }

    [Test]
    public async Task ScenarioLostInBattlePopupTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition escort = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("escort", maxHitpoints: 10, initiative: 10, damage: 1),
            movementPoints: 6, leadership: 6);
        UnitDefinition slayer = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("slayer", maxHitpoints: 100, initiative: 100, damage: 100),
            movementPoints: 6, leadership: 6);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(escort)
            .AddUnit(slayer)
            .Build();

        WorldStack escortStack = TestHelpers.SimpleStack(
            new WorldStackId("escort"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("escort"), escort.Id, 10, 0, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);
        WorldStack slayerStack = TestHelpers.SimpleStack(
            new WorldStackId("slayer"),
            enemy.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("slayer"), slayer.Id, 100, 0, 1, [], null, false), new GridPosition(0, 0))]),
            movementPoints: 6);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Heretic],
            [WorldRace.Heretic],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(escortStack.Id))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("Bethrezen is dead!"),
                    ScenarioEventPopupAudience.AllPlayers,
                    null,
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: true),
                new ScenarioEventWinEffect(WinnerId: null)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(escort)
            .WithDefinition(slayer)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(escortStack)
            .WithStack(slayerStack)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        HotseatTurnScreenViewModel hotseatTurn = Assert.IsOfType<HotseatTurnScreenViewModel>(gameViewModel.TopScreen);

        hotseatTurn.Close();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        world.CompleteAnimation();

        BattleScreenViewModel battle = Assert.IsOfType<BattleScreenViewModel>(gameViewModel.TopScreen);

        await battle.CompleteTransitionAsync();
        battle.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);
        battle.Close();
        await battle.CompleteTransitionAsync();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        world.CompleteAnimation();
        world.CompleteAnimation();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.RenderRequest.Animation, Is.EqualTo(new WorldAnimationState.Pause(TimeSpan.FromSeconds(0.25))));

        world.CompleteAnimation();

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);
        Assert.That(popup.Messages, Is.EqualTo([
            new ScenarioEventPopupMessageViewModel(new ScenarioEventPopupMessage(
                [new ScenarioTextSpan("Bethrezen is dead!", ScenarioTextSpanType.Text)],
                "name",
                new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("slayer")),
                IsPortraitLeft: true))
        ]));

        popup.CloseCommand.Execute(null);

        GameEndedScreenViewModel gameEnded = Assert.IsOfType<GameEndedScreenViewModel>(gameViewModel.TopScreen);
        Assert.That(gameEnded.IsVictory, Is.False);
        Assert.That(gameEnded.Title, Is.EqualTo("Defeat"));
        Assert.That(gameEnded.Debriefing, Is.EqualTo([new ScenarioTextSpan("test-scenario:DEBUNKL:0", ScenarioTextSpanType.Text)]));
    }

    [Test]
    public async Task TurnStartSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer computer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map) with { IsAi = true };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, human.Id))
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(computer.LordId, computer.Race, LordCategory.Warrior))
            .WithPlayer(computer)
            .BuildSession();

        TestAudioPlayer audioPlayer = new();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        audioPlayer.Actions.Clear();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:turn-start"]));

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:turn-start"]));

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:turn-start", "Sound:turn-start"]));
    }

    [Test]
    public async Task CityRiotEndSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("City1"),
                cityDefinition,
                human.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                RiotTurns = 1
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, human.Id))
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithCity(city)
            .BuildSession();

        TestAudioPlayer audioPlayer = new();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        audioPlayer.Actions.Clear();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:city-riot-end", "Sound:turn-start"]));
    }

    [Test]
    public async Task InspectOnSiteSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldMerchantSite site = new(
            Id: new WorldSiteId("merchant"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        TestAudioPlayer audioPlayer = new();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);
        audioPlayer.Actions.Clear();

        world.InspectStackCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ManagementScreenViewModel>());
        Assert.That(audioPlayer.Actions, Is.Empty);
    }

    [Test]
    public async Task EscapeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.SelectAction(new WorldCursorAction.CastSpell(new WorldSpellDefinitionId("restore")));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(world.ActionPromptText, Is.Null);

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { world, menu }));
            Assert.That(menu.SaveTip, Is.Null);
            Assert.That(menu.SaveGameCommand.CanExecute(null), Is.True);
        }

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel reopenedMenu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);
        reopenedMenu.Game.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel settings = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { world, reopenedMenu, settings }));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { world, reopenedMenu }));

        await reopenedMenu.Game.ExitGameCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([gameViewModel.RootScreen]));
        Assert.That(gameViewModel.RootScreen, Is.InstanceOf<MainMenuScreenViewModel>());
    }

    [Test]
    public void CancelSelectedActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.SelectAction(new WorldCursorAction.CastSpell(new WorldSpellDefinitionId("restore")));

        Assert.That(world.ActionPromptText, Is.EqualTo("Select a target for the spell on the map."));

        world.CancelSelectedActionCommand.Execute(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
            Assert.That(world.ActionPromptText, Is.Null);
            Assert.That(gameViewModel.Alert.Current, Is.Null);
        }
    }

    [Test]
    public void CityPartiallyVisibleInfoTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Dwarf, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["?????", "?????", "?????", "???.?"])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Dwarf, 1), WorldResourceSet.Empty))
            .WithCity(new WorldCity(
                Id: new WorldFortificationId("city"),
                NameTextId: new ScenarioTextId("Hemwyk Court"),
                DefinitionId: new WorldCityDefinitionId(WorldRace.Dwarf, 1),
                OwnerId: player.Id,
                SubraceId: TestHelpers.NeutralSubrace.Id,
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
                Garrison: UnitGrid<WorldUnit>.Empty,
                Inventory: WorldInventory.Empty,
                AiPriority: 1,
                RiotTurns: 0,
                GrowthTurn: 0))
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.ShowPositionInfo(new WorldPosition(0, 0), []);

        WorldFortificationInfoViewModel info = Assert.IsOfType<WorldFortificationInfoViewModel>(world.PositionInfo);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.Title, Is.EqualTo("Hemwyk Court"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["City level: 1", "Owner: Mountain Clans", "Hit point regeneration per turn: 10%"]));
            Assert.That(info.Note, Is.Null);
            Assert.That(info.VisitingStackTitle, Is.Null);
            Assert.That(info.VisitingStack.Units, Is.Empty);
            Assert.That(info.Defenses.Units, Is.Empty);
        }
    }

    [Test]
    public void UnexploredCityInfoTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Dwarf, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["?????", "?????", "?????", "?????"])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Dwarf, 1), WorldResourceSet.Empty))
            .WithCity(new WorldCity(
                Id: new WorldFortificationId("city"),
                NameTextId: new ScenarioTextId("Hemwyk Court"),
                DefinitionId: new WorldCityDefinitionId(WorldRace.Dwarf, 1),
                OwnerId: player.Id,
                SubraceId: TestHelpers.NeutralSubrace.Id,
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
                Garrison: UnitGrid<WorldUnit>.Empty,
                Inventory: WorldInventory.Empty,
                AiPriority: 1,
                RiotTurns: 0,
                GrowthTurn: 0))
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.ShowPositionInfo(new WorldPosition(0, 0), []);

        Assert.That(world.PositionInfo, Is.Null);
    }

    [Test]
    public void RuinInfoTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Dwarf, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([".....", ".....", ".....", "....."])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(new WorldRuin(
                Id: new WorldFortificationId("ruin"),
                NameTextId: new ScenarioTextId("Abandoned Farm"),
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
                Variant: new WorldObjectVariant(0),
                Garrison: UnitGrid<WorldUnit>.Empty,
                Resources: WorldResourceSet.Empty,
                Item: null,
                LooterId: null,
                AiPriority: 1))
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.ShowPositionInfo(new WorldPosition(0, 1), []);

        WorldTileInfoViewModel info = Assert.IsOfType<WorldTileInfoViewModel>(world.PositionInfo);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.Title, Is.EqualTo("Abandoned Farm"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["Unexplored"]));
        }
    }

    [Test]
    public void CityVisitingStackInfoTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 4);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Dwarf, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 50, initiative: 40, damage: 5);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Dwarf, 2), WorldResourceSet.Empty))
            .WithCity(new WorldCity(
                Id: new WorldFortificationId("city"),
                NameTextId: new ScenarioTextId("Hemwyk Court"),
                DefinitionId: new WorldCityDefinitionId(WorldRace.Dwarf, 2),
                OwnerId: player.Id,
                SubraceId: TestHelpers.NeutralSubrace.Id,
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
                Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard),
                Inventory: WorldInventory.Empty,
                AiPriority: 1,
                RiotTurns: 0,
                GrowthTurn: 0))
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.ShowPositionInfo(new WorldPosition(3, 3), []);

        WorldFortificationInfoViewModel info = Assert.IsOfType<WorldFortificationInfoViewModel>(world.PositionInfo);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.Title, Is.EqualTo("Hemwyk Court"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["City level: 2", "Owner: Mountain Clans", "Hit point regeneration per turn: 15%"]));
            Assert.That(info.Note, Is.Null);
            Assert.That(info.VisitingStackTitle, Is.EqualTo("name's party"));
            Assert.That(info.VisitingStack.Units, Is.EqualTo(new WorldUnitGroupInfoUnitViewModel[]
            {
                new(new AssetSpriteKey.UnitFace(leader.Id), "name", 100, 100, IsAlive: true, Row: 2, Column: 1, ColumnSpan: 1, Statuses: [])
            }));
            Assert.That(info.Defenses.Units, Is.EqualTo(new WorldUnitGroupInfoUnitViewModel[]
            {
                new(new AssetSpriteKey.UnitFace(guard.Id), "name", 50, 50, IsAlive: true, Row: 2, Column: 1, ColumnSpan: 1, Statuses: [])
            }));
        }
    }

    [Test]
    public async Task ThiefActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemyLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemyLeader"), enemyLeader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(enemyLeader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(thiefStack.Position, [], 1);
        world.UpdateHover(enemyStack.Position, []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Negotiate));

        await world.HandleMapClickAsync(enemyStack.Position, [], 1);
        await world.HandleMapClickAsync(enemyStack.Position, [], 1);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        world.CompleteAnimation();

        ThiefActionScreenViewModel thiefScreen = Assert.IsOfType<ThiefActionScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatThiefActions(thiefScreen), Is.EqualTo([
            WorldThiefActionType.PoisonStack,
            WorldThiefActionType.Spy,
            WorldThiefActionType.Duel,
            WorldThiefActionType.StealItem,
            WorldThiefActionType.Assassinate,
            WorldThiefActionType.CounterfeitOrders
        ]));
        Assert.That(gameModel.Snapshot.BattleState, Is.Null);
        Assert.That(gameModel.Snapshot.WorldState.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            Position = new WorldPosition(1, 0),
            Direction = WorldStackDirection.SouthEast,
            MovementPoints = 18
        }));

        gameViewModel.CloseScreen(thiefScreen);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);

        await world.HandleMapClickAsync(enemyStack.Position, [], 1);
        await world.HandleMapClickAsync(enemyStack.Position, [], 1);

        thiefScreen = Assert.IsOfType<ThiefActionScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatThiefActions(thiefScreen), Is.EqualTo([
            WorldThiefActionType.PoisonStack,
            WorldThiefActionType.Spy,
            WorldThiefActionType.Duel,
            WorldThiefActionType.StealItem,
            WorldThiefActionType.Assassinate,
            WorldThiefActionType.CounterfeitOrders
        ]));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            Position = new WorldPosition(1, 0),
            Direction = WorldStackDirection.SouthEast,
            MovementPoints = 18
        }));

        gameViewModel.CloseScreen(thiefScreen);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task EnterSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldMerchantSite site = new(
            Id: new WorldSiteId("merchant"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(6, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        TestAudioPlayer audioPlayer = new();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(stack.Position, [], 1);
        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);
        audioPlayer.Actions.Clear();

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));

        world.CompleteAnimation();

        ManagementScreenViewModel siteScreen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { world, siteScreen }));
        Assert.That(siteScreen.SelectedTab, Is.InstanceOf<MerchantTabViewModel>());
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(4, 4)));
        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:site-enter"]));

        gameViewModel.CloseScreen(siteScreen);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task EnterSiteOutOfReachTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldMerchantSite site = new(
            Id: new WorldSiteId("merchant"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(6, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        TestAudioPlayer audioPlayer = new();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(stack.Position, [], 1);
        audioPlayer.Actions.Clear();

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);

        Assert.That(audioPlayer.Actions, Is.Empty);
        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(6, 4)));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task ThiefActionSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            new WorldResourceSet(50, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        WorldMerchantSite site = new(
            Id: new WorldSiteId("merchant"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [new WorldMerchantItem(potion.Id, 1)],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player.Id,
            new WorldPosition(6, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMerchant, 60))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(thiefStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(thiefStack.Position, [], 1);
        world.UpdateHover(new WorldPosition(3, 3), []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Negotiate));

        await world.HandleMapClickAsync(new WorldPosition(3, 3), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(3, 3), [], 1);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        world.CompleteAnimation();

        ThiefActionScreenViewModel thiefScreen = Assert.IsOfType<ThiefActionScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatThiefActions(thiefScreen), Is.EqualTo([WorldThiefActionType.StealMerchant]));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            Position = new WorldPosition(5, 4),
            Direction = WorldStackDirection.NorthWest,
            MovementPoints = 18
        }));

        gameViewModel.CloseScreen(thiefScreen);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task ThiefActionUngarrisonedCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 50))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(thiefStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(thiefStack.Position, [], 1);
        world.UpdateHover(new WorldPosition(2, 2), []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Negotiate));

        await world.HandleMapClickAsync(new WorldPosition(2, 2), [], 1);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        world.CompleteAnimation();

        ThiefActionScreenViewModel thiefScreen = Assert.IsOfType<ThiefActionScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatThiefActions(thiefScreen), Is.EqualTo([
            WorldThiefActionType.Spy,
            WorldThiefActionType.PoisonCity,
            WorldThiefActionType.StealGold,
            WorldThiefActionType.StealSpell,
            WorldThiefActionType.Assassinate,
            WorldThiefActionType.CounterfeitOrders,
            WorldThiefActionType.Bribe,
            WorldThiefActionType.RiotCity
        ]));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[thiefStack.Id], Is.EqualTo(thiefStack with
        {
            Position = new WorldPosition(4, 2),
            Direction = WorldStackDirection.North,
            MovementPoints = 18
        }));

        gameViewModel.CloseScreen(thiefScreen);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.EndTurnCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task RuinLootTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 100),
            movementPoints: 10, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("Guard", maxHitpoints: 10, initiative: 1, damage: 1);
        ItemDefinition potion = new(
            new ItemDefinitionId("Potion"),
            new GlobalTextId("Healing potion"),
            new GlobalTextId("Heals 50 hit points"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, guard.IsLarge)
                .Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: potion.Id,
            LooterId: null,
            AiPriority: 3);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(guard)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        TestAssetProviderBuilder assetBuilder = new();
        Sprite unitSprite = assetBuilder.CreateSprite(SKColors.Transparent, 80, 120);
        Animation unitAnimation = assetBuilder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 80, Height: 120, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [unitSprite]);
        using TestAssetProvider assets = assetBuilder
            .ResolveSprite(_ => unitSprite)
            .ResolveAnimation(_ => unitAnimation)
            .ResolveSound(_ => null)
            .Build();

        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAssets(assets).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 2), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 2), [], 1);

        world.CompleteAnimation();

        BattleScreenViewModel battle = Assert.IsOfType<BattleScreenViewModel>(gameViewModel.TopScreen);

        await battle.CompleteTransitionAsync();
        battle.AutoResolveCommand.Execute(null);

        ConfirmPromptViewModel autoResolvePrompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await autoResolvePrompt.ConfirmCommand.ExecuteAsync(null);
        battle.Close();
        await battle.CompleteTransitionAsync();
        world.CompleteAnimation();

        LootScreenViewModel loot = Assert.IsOfType<LootScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(loot.Title, Is.EqualTo("You are victorious!"));
        Assert.That(loot.Text, Is.EqualTo("You found treasure."));
        Assert.That(loot.Resources, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(200, 0, 0, 0, 0, 0), ResourceSetProjection.All)));
        Assert.That(
            loot.Items,
            Is.EqualTo(new LootItem[]
            {
                new("Healing potion", "Heals 50 hit points", new IconViewModel.Item(new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(potion.Id))))
            }));

        loot.Close();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsActive, Is.True);
    }

    [Test]
    public void InfoTilePriorityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Dwarf, map);
        UnitDefinition knight = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                NameTextId = new GlobalTextId("Knight")
            };
        UnitDefinition archer = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("archer", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                NameTextId = new GlobalTextId("Archer")
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(knight)
            .WithDefinition(archer)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(TestHelpers.SimpleStack(new WorldStackId("Knight"), player.Id, new WorldPosition(1, 1), TestHelpers.SingleUnitGrid(new WorldUnitId("knight"), knight), movementPoints: 6))
            .WithStack(TestHelpers.SimpleStack(new WorldStackId("Archer"), player.Id, new WorldPosition(3, 1), TestHelpers.SingleUnitGrid(new WorldUnitId("archer"), archer), movementPoints: 6))
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.ShowPositionInfo(new WorldPosition(1, 1), [new WorldPosition(3, 1)]);

        WorldStackInfoViewModel info = Assert.IsOfType<WorldStackInfoViewModel>(world.PositionInfo);

        Assert.That(info.LeaderName, Is.EqualTo("Knight"));
    }

    [Test]
    public void InfoEmptyTileHitObjectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Dwarf, map);
        UnitDefinition knight = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                NameTextId = new GlobalTextId("Knight")
            };
        UnitDefinition archer = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("archer", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                NameTextId = new GlobalTextId("Archer")
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(knight)
            .WithDefinition(archer)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(TestHelpers.SimpleStack(new WorldStackId("Knight"), player.Id, new WorldPosition(1, 1), TestHelpers.SingleUnitGrid(new WorldUnitId("knight"), knight), movementPoints: 6))
            .WithStack(TestHelpers.SimpleStack(new WorldStackId("Archer"), player.Id, new WorldPosition(3, 1), TestHelpers.SingleUnitGrid(new WorldUnitId("archer"), archer), movementPoints: 6))
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.ShowPositionInfo(new WorldPosition(2, 1), [new WorldPosition(3, 1), new WorldPosition(1, 1)]);

        WorldStackInfoViewModel info = Assert.IsOfType<WorldStackInfoViewModel>(world.PositionInfo);

        Assert.That(info.LeaderName, Is.EqualTo("Archer"));
    }

    [Test]
    public void CollapseObjectiveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioState(WorldScenarioState.Empty with { ObjectiveTextId = new ScenarioTextId("Hold the pass.") })
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(world.ObjectiveText, Is.EqualTo("Hold the pass."));
        Assert.That(world.IsObjectiveExpanded, Is.True);

        world.ToggleObjectiveCommand.Execute(null);

        Assert.That(world.IsObjectiveExpanded, Is.False);

        world.ToggleObjectiveCommand.Execute(null);

        Assert.That(world.IsObjectiveExpanded, Is.True);
    }

    [Test]
    public async Task ObjectiveChangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects:
            [
                new ScenarioEventChangeObjectiveEffect(new ScenarioTextId("Capture the city.")),
                new ScenarioEventPopupEffect(new ScenarioTextId("The city has fallen."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .WithScenarioState(WorldScenarioState.Empty with { ObjectiveTextId = new ScenarioTextId("Hold the pass.") })
            .BuildSession();

        TestAudioPlayer audioPlayer = new();
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        world.ToggleObjectiveCommand.Execute(null);

        List<string?> changes = [];
        world.ObjectiveChanged += () => changes.Add(world.ObjectiveText);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        ScenarioEventPopupViewModel popup = Assert.IsOfType<ScenarioEventPopupViewModel>(gameViewModel.TopScreen);

        Assert.That(
            popup.Messages,
            Is.EqualTo(new ScenarioEventPopupMessageViewModel[]
            {
                new(new ScenarioEventPopupMessage([new ScenarioTextSpan("The city has fallen.", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true))
            }));
        Assert.That(world.ObjectiveText, Is.EqualTo("Hold the pass."));
        Assert.That(world.IsObjectiveExpanded, Is.False);
        Assert.That(changes, Is.Empty);
        Assert.That(audioPlayer.Actions, Is.EqualTo([
            "Music:ambient-music/3,world-music/7,ambient-music/5,world-music/9,ambient-music/7,world-music/8,ambient-music/6,world-music/0,ambient-music/10,world-music/5,ambient-music/1,world-music/2,ambient-music/4,world-music/10,ambient-music/0,world-music/4,ambient-music/2,world-music/6,ambient-music/9,world-music/1,ambient-music/8,world-music/3",
            "Sound:turn-start",
            "Sound:interface-open"
        ]));

        audioPlayer.Actions.Clear();
        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.ObjectiveText, Is.EqualTo("Capture the city."));
        Assert.That(world.IsObjectiveExpanded, Is.True);
        Assert.That(changes, Is.EqualTo(["Capture the city."]));
        Assert.That(audioPlayer.Actions, Is.Empty);
    }

    [Test]
    public void NoObjectiveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(world.ObjectiveText, Is.Null);
    }

    [Test]
    public async Task ThiefActionResultTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemyLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemyLeader"), enemyLeader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(enemyLeader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(thiefStack.Position, [], 1);
        await world.HandleMapClickAsync(enemyStack.Position, [], 1);
        await world.HandleMapClickAsync(enemyStack.Position, [], 1);

        ThiefActionScreenViewModel thiefScreen = Assert.IsOfType<ThiefActionScreenViewModel>(gameViewModel.TopScreen);

        await thiefScreen.Actions[1].SelectCommand.ExecuteAsync(null);

        ThiefActionResultScreenViewModel result = Assert.IsOfType<ThiefActionResultScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(result.Title, Is.EqualTo("Success!"));
        Assert.That(result.Text, Is.EqualTo("Your spy has infiltrated the enemy ranks. Right-click enemy parties and cities to reveal information about them!"));
        Assert.That(result.ImageKey, Is.EqualTo(new AssetSpriteKey.ThiefActionImage(WorldThiefActionType.Spy, IsLarge: true)));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsActive, Is.True);
    }

    [Test]
    public async Task ThiefActionFailedResultTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemyLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("enemyLeader"), enemyLeader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(99, 99))
            .WithDefinition(thief)
            .WithDefinition(enemyLeader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(enemyStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(thiefStack.Position, [], 1);
        await world.HandleMapClickAsync(enemyStack.Position, [], 1);
        await world.HandleMapClickAsync(enemyStack.Position, [], 1);

        ThiefActionScreenViewModel thiefScreen = Assert.IsOfType<ThiefActionScreenViewModel>(gameViewModel.TopScreen);

        await thiefScreen.Actions[0].SelectCommand.ExecuteAsync(null);

        ThiefActionResultScreenViewModel result = Assert.IsOfType<ThiefActionResultScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(result.Title, Is.EqualTo("Failure!"));
        Assert.That(result.Text, Is.EqualTo("Your thief has been captured and executed!"));
        Assert.That(result.ImageKey, Is.EqualTo(new AssetSpriteKey.ThiefActionImage(WorldThiefActionType.PoisonStack, IsLarge: true)));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsActive, Is.True);
    }

    [Test]
    public async Task ActionWheelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.Rod | LeaderAbility.WandScrollUse
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldSpellDefinition healSpell = new(
            new WorldSpellDefinitionId("heal"),
            new GlobalTextId("heal-name"),
            new GlobalTextId("heal-description"),
            new GlobalTextId("heal-effect"),
            WorldRace.Human,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: null,
            new WorldHealSpellEffect(5),
            AiType: WorldAiSpellType.All);
        WorldSpellDefinition areaSpell = new(
            new WorldSpellDefinitionId("area-heal"),
            new GlobalTextId("area-heal-name"),
            new GlobalTextId("area-heal-description"),
            new GlobalTextId("area-heal-effect"),
            WorldRace.Human,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 1,
            new WorldHealSpellEffect(5),
            AiType: WorldAiSpellType.All);

        ItemDefinition scroll = new(
            new ItemDefinitionId("scroll"),
            new GlobalTextId("scroll-name"),
            new GlobalTextId("scroll-description"),
            ItemCategory.Scroll,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new SpellItemEffect(healSpell.Id, WorldResourceSet.Empty),
            AnimationUnitId: null);
        ItemDefinition wand = new(
            new ItemDefinitionId("wand"),
            new GlobalTextId("wand-name"),
            new GlobalTextId("wand-description"),
            ItemCategory.Wand,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new SpellItemEffect(areaSpell.Id, WorldResourceSet.Empty),
            AnimationUnitId: null);

        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: 3);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player.Id,
            new WorldPosition(5, 5),
            UnitGrid<WorldUnit>.Empty);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 30, 0, soldierDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack"),
                player.Id,
                new WorldPosition(5, 5),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (leader, GridPosition.FrontBottom),
                        (soldier, GridPosition.FrontCenter)
                    ]),
                movementPoints: 20) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, scrollItem).With(LeaderEquipmentSlot.Battle2, wandItem),
                PendingLeaderUpgrades = 1
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(healSpell)
            .WithDefinition(areaSpell)
            .WithDefinition(scroll)
            .WithDefinition(wand)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(world.ActionWheel, Is.Null);

        await world.HandleMapClickAsync(new WorldPosition(5, 5), [], 1);

        Assert.That(world.ActionWheel, Is.EqualTo(new WorldActionWheelViewModel(
            new WorldPosition(5, 5),
            [
                new WorldActionWheelItemViewModel("Learn a new ability (1)", IconMoniker.LearnAbility, ItemIconKey: null, IsHighlighted: true, new WorldStackAction.LearnAbility(stack.Id)),
                new WorldActionWheelItemViewModel("Plant rod", IconMoniker.PlantRod, ItemIconKey: null, IsHighlighted: false, new WorldStackAction.CreateRod(stack.Id)),
                new WorldActionWheelItemViewModel("Destroy rod", IconMoniker.DestroyRod, ItemIconKey: null, IsHighlighted: false, new WorldStackAction.DestroyRod(stack.Id)),
                new WorldActionWheelItemViewModel("Use scroll-name", Icon: null, new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Scroll(WorldRace.Human)), IsHighlighted: false, new WorldStackAction.UseItem(stack.Id, scrollItem.Id)),
                new WorldActionWheelItemViewModel("Use wand-name (3)", Icon: null, new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(wand.Id)), IsHighlighted: false, new WorldStackAction.UseItem(stack.Id, wandItem.Id))
            ])));
        Assert.That(world.IsActionWheelVisible, Is.True);
        Assert.That(world.InspectStackCommand.CanExecute(null), Is.True);

        await world.ActivateStackActionCommand.ExecuteAsync(new WorldStackAction.UseItem(stack.Id, scrollItem.Id));

        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id], Is.EqualTo(stack with
        {
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle2, wandItem),
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier with { Hitpoints = 35 }, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(world.Game.Alert.Current, Is.Null);
        Assert.That(world.ActionWheel, Is.EqualTo(new WorldActionWheelViewModel(
            new WorldPosition(5, 5),
            [
                new WorldActionWheelItemViewModel("Learn a new ability (1)", IconMoniker.LearnAbility, ItemIconKey: null, IsHighlighted: true, new WorldStackAction.LearnAbility(stack.Id)),
                new WorldActionWheelItemViewModel("Plant rod", IconMoniker.PlantRod, ItemIconKey: null, IsHighlighted: false, new WorldStackAction.CreateRod(stack.Id)),
                new WorldActionWheelItemViewModel("Destroy rod", IconMoniker.DestroyRod, ItemIconKey: null, IsHighlighted: false, new WorldStackAction.DestroyRod(stack.Id)),
                new WorldActionWheelItemViewModel("Use wand-name (3)", Icon: null, new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(wand.Id)), IsHighlighted: false, new WorldStackAction.UseItem(stack.Id, wandItem.Id))
            ])));

        await world.ActivateStackActionCommand.ExecuteAsync(new WorldStackAction.UseItem(stack.Id, wandItem.Id));

        Assert.That(world.ActionPromptText, Is.EqualTo("Select a target for the item on the map."));

        await world.ActivateStackActionCommand.ExecuteAsync(new WorldStackAction.CreateRod(stack.Id));

        Assert.That(world.ActionPromptText, Is.EqualTo("Select a tile to plant a rod on."));

        await world.ActivateStackActionCommand.ExecuteAsync(new WorldStackAction.DestroyRod(stack.Id));

        Assert.That(world.ActionPromptText, Is.EqualTo("Select a rod to destroy."));

        world.CancelSelectedActionCommand.Execute(null);

        Assert.That(world.ActionPromptText, Is.Null);

        await world.ActivateStackActionCommand.ExecuteAsync(new WorldStackAction.LearnAbility(stack.Id));

        LeaderUpgradeViewModel upgradeScreen = Assert.IsOfType<LeaderUpgradeViewModel>(gameViewModel.TopScreen);

        Assert.That(world.IsActionWheelVisible, Is.False);

        gameViewModel.CloseScreen(upgradeScreen);

        Assert.That(world.IsActionWheelVisible, Is.True);

        world.InspectStackCommand.Execute(null);

        ManagementScreenViewModel cityScreen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        Assert.IsOfType<CityTabViewModel>(cityScreen.Tabs[0]);
        Assert.IsOfType<VisitingStackTabViewModel>(cityScreen.SelectedTab);
        gameViewModel.CloseScreen(cityScreen);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public async Task ActionWheelThiefTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(thiefStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(thiefStack.Position, [], 1);

        Assert.That(world.ActionWheel, Is.Null);
        Assert.That(world.IsActionWheelVisible, Is.False);
        Assert.That(world.InspectStackCommand.CanExecute(null), Is.True);
    }

    [Test]
    public async Task StackItemMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack ready = TestHelpers.SimpleStack(
            new WorldStackId("Ready"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 2);

        WorldStack exhausted = TestHelpers.SimpleStack(
            new WorldStackId("Exhausted"),
            player.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader2"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(ready)
            .WithStack(exhausted)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Exhausted | unit-event-portrait/leader | name | level 1 | 0 / 2 | - | -",
            "Ready | unit-event-portrait/leader | name | level 1 | 2 / 2 | - | -"
        ]));
        Assert.That(world.IsViewPlayerTurn, Is.True);
        Assert.That(world.IsEndTurnHighlighted, Is.False);

        IReadOnlyList<WorldStackItemViewModel> items = [.. world.StackItems];

        world.SelectStackItemCommand.Execute(ready.Id);

        Assert.That(world.ActionWheel, Is.Null);

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Exhausted | unit-event-portrait/leader | name | level 1 | 0 / 2 | - | -",
            "Ready | unit-event-portrait/leader | name | level 1 | 0 / 2 | selected | -"
        ]));
        Assert.That(world.ActionWheel, Is.EqualTo(new WorldActionWheelViewModel(
            new WorldPosition(1, 0),
            [
                new WorldActionWheelItemViewModel("Exchange", IconMoniker.Exchange, ItemIconKey: null, IsHighlighted: false, new WorldStackAction.Exchange(ready.Id))
            ])));
        Assert.That(world.IsActionWheelVisible, Is.False);
        Assert.That(world.StackItems, Is.EqualTo(items).Using<WorldStackItemViewModel>(ReferenceEquals));
        Assert.That(world.IsEndTurnHighlighted, Is.False);

        world.CompleteAnimation();

        Assert.That(world.IsEndTurnHighlighted, Is.True);
        Assert.That(world.IsActionWheelVisible, Is.True);
    }

    [Test]
    public async Task UnreachableTileTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            ".^.",
            ".^."
        ]);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 1), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 1), [], 1);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [],
            SelectedPath = null
        }));
        Assert.That(gameViewModel.Alert.Current, Is.Null);
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 1)));
    }

    [Test]
    public async Task MountainTileTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            ".^.",
            ".^."
        ]);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 1), [], 1);
        world.UpdateHover(new WorldPosition(1, 1), []);
        await world.HandleMapClickAsync(new WorldPosition(1, 1), [], 1);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [],
            SelectedPath = null
        }));
        Assert.That(gameViewModel.Alert.Current, Is.Null);
        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Default));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 1)));
    }

    [Test]
    public async Task LandmarkTileTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithDefinition(new WorldLandmarkDefinition(
                new WorldLandmarkDefinitionId("landmark"),
                new GlobalTextId("Ancient Obelisk"),
                new GlobalTextId("desc"),
                WorldLandmarkType.Structure,
                false,
                1,
                1))
            .WithLandmark(new WorldLandmark(
                Id: new WorldLandmarkId("landmark1"),
                DefinitionId: new WorldLandmarkDefinitionId("landmark"),
                Footprint: new WorldFootprint(new WorldPosition(1, 1), 1, 1)))
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 1), [], 1);
        world.UpdateHover(new WorldPosition(1, 1), []);
        await world.HandleMapClickAsync(new WorldPosition(1, 1), [], 1);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [],
            SelectedPath = null
        }));
        Assert.That(gameViewModel.Alert.Current, Is.Null);
        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Default));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 1)));
    }

    [Test]
    public async Task ResourceNodeTileTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 24, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 24);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithResourceNode(new WorldResourceNode(
                Id: new WorldResourceNodeId("crystal"),
                Position: new WorldPosition(1, 1),
                ResourceType: WorldResourceType.LifeMana,
                AiPriority: 1))
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 1), [], 1);
        world.UpdateHover(new WorldPosition(1, 1), []);
        await world.HandleMapClickAsync(new WorldPosition(1, 1), [], 1);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors = [],
            SelectedPath = null
        }));
        Assert.That(gameViewModel.Alert.Current, Is.Null);
        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Default));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 1)));
    }

    [Test]
    public async Task InspectOnSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldMerchantSite site = new(
            Id: new WorldSiteId("merchant"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.SelectStackItemCommand.Execute(stack.Id);

        Assert.That(world.ActionWheel, Is.Null);

        world.InspectStackCommand.Execute(null);

        ManagementScreenViewModel siteScreen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { world, siteScreen }));
        Assert.IsOfType<MerchantTabViewModel>(siteScreen.Tabs[0]);
        Assert.IsOfType<MapStackTabViewModel>(siteScreen.SelectedTab);
    }

    [Test]
    public async Task ClickSelectedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.UpdateHover(new WorldPosition(1, 0), []);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.SelectUnit));

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);
        world.UpdateHover(new WorldPosition(1, 0), []);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Stack | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -"
        ]));
        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Default));

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Stack | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -"
        ]));

        world.InspectStackCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { world, screen }));
        Assert.IsOfType<MapStackTabViewModel>(screen.SelectedTab);
    }

    [Test]
    public async Task ClickCityShieldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player.Id,
            new WorldPosition(5, 5),
            UnitGrid<WorldUnit>.Empty);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(5, 5), [], 1);
        world.UpdateHover(new WorldPosition(5, 5), []);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.ViewCity));

        await world.HandleMapClickAsync(new WorldPosition(5, 5), [], 1);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.Screens, Is.EqualTo(new AbstractScreenViewModel[] { world, screen }));
        Assert.IsOfType<CityTabViewModel>(screen.Tabs[0]);
    }

    [Test]
    public async Task ClickStackFigureTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack first = TestHelpers.SimpleStack(
            new WorldStackId("First"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldStack second = TestHelpers.SimpleStack(
            new WorldStackId("Second"),
            player.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(first)
            .WithStack(second)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.UpdateHover(new WorldPosition(1, 0), [new WorldPosition(2, 0)]);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.SelectUnit));

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [new WorldPosition(2, 0)], 1);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "First | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -",
            "Second | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -"
        ]));

        world.UpdateHover(new WorldPosition(1, 0), [new WorldPosition(0, 0)]);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.SelectUnit));

        world.UpdateHover(new WorldPosition(1, 0), [new WorldPosition(2, 0)]);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Move));

        world.UpdateHover(new WorldPosition(1, 0), [new WorldPosition(0, 0), new WorldPosition(2, 0)]);

        Assert.That(world.CursorType, Is.EqualTo(AssetAnimationKey.CursorType.Move));

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [new WorldPosition(0, 0)], 1);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "First | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -",
            "Second | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[second.Id].Position, Is.EqualTo(new WorldPosition(2, 0)));
    }

    [Test]
    public async Task HoverStackMarkerOutOfReachTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack first = TestHelpers.SimpleStack(
            new WorldStackId("First"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldStack second = TestHelpers.SimpleStack(
            new WorldStackId("Second"),
            player.Id,
            new WorldPosition(4, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(first)
            .WithStack(second)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        world.UpdateHover(new WorldPosition(3, 0), [new WorldPosition(4, 0)]);

        WorldRenderState renderState = BuildWorldState(world);

        Assert.That(renderState, Is.EqualTo(renderState with
        {
            Cursors =
            [
                new WorldRenderState.Cursor(new WorldPosition(4, 0), 2078, Theme.World.HoverCursorInnerColor, Theme.World.HoverCursorOuterColor, 1.0)
            ],
            Reach = new WorldRenderState.ReachOverlay([new WorldPosition(0, 0), new WorldPosition(1, 0)])
        }));
    }

    [Test]
    public async Task AttackPromptTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);
        WorldStack enemy = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemy)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        AttackPromptViewModel prompt = Assert.IsOfType<AttackPromptViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prompt.PortraitKey, Is.EqualTo(new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leader.Id))));
            Assert.That(prompt.Message.Text, Is.EqualTo("The path to the destination leads into battle with name."));
            Assert.That(prompt.IsTargetKnown, Is.True);
        }

        prompt.CloseCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(0, 0)));

        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        AttackPromptViewModel reopenedPrompt = Assert.IsOfType<AttackPromptViewModel>(gameViewModel.TopScreen);

        await reopenedPrompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(2, 0)));
        Assert.That(gameModel.ActiveBattle, Is.Not.Null);
    }

    [Test]
    public async Task AttackPromptFoggedEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["...??"])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);
        WorldStack enemy = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemy)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        AttackPromptViewModel prompt = Assert.IsOfType<AttackPromptViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prompt.PortraitKey, Is.Null);
            Assert.That(prompt.Message.Text, Is.EqualTo("The path to the destination leads into battle with an unknown enemy."));
            Assert.That(prompt.IsTargetKnown, Is.False);
        }
    }

    [Test]
    public async Task AttackPromptConcealedEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);
        WorldStack enemy = TestHelpers.SimpleStack(
                new WorldStackId("Enemy"),
                player2.Id,
                new WorldPosition(3, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 10) with
            {
                Concealed = true
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemy)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(2, 0), [], 1);

        AttackPromptViewModel prompt = Assert.IsOfType<AttackPromptViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prompt.PortraitKey, Is.Null);
            Assert.That(prompt.Message.Text, Is.EqualTo("The path to the destination leads into battle with an unknown enemy."));
            Assert.That(prompt.IsTargetKnown, Is.False);
        }
    }

    [Test]
    public async Task AttackClickedEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);
        WorldStack enemy = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(3, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithStack(enemy)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(3, 0), [], 1);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(gameModel.Snapshot.WorldState.Stacks[stack.Id].Position, Is.EqualTo(new WorldPosition(2, 0)));
        Assert.That(gameModel.ActiveBattle, Is.Not.Null);
    }

    [Test]
    public async Task EscapeDeselectsStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Stack | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));

        gameViewModel.EscapeCommand.Execute(null);

        GameMenuViewModel menu = Assert.IsOfType<GameMenuViewModel>(gameViewModel.TopScreen);
        gameViewModel.CloseScreen(menu);

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);
        world.SelectAction(new WorldCursorAction.CastSpell(new WorldSpellDefinitionId("restore")));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([world]));
        Assert.That(world.ActionPromptText, Is.Null);
        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Stack | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));
    }

    [Test]
    public void TabSelectsStacksTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack alpha = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldStack bravo = TestHelpers.SimpleStack(
            new WorldStackId("Bravo"),
            player.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);
        WorldStack charlie = TestHelpers.SimpleStack(
            new WorldStackId("Charlie"),
            player.Id,
            new WorldPosition(4, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(alpha)
            .WithStack(bravo)
            .WithStack(charlie)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        List<WorldPosition> cameraFocusRequests = [];
        world.CameraFocusRequested += (position, _) => cameraFocusRequests.Add(position);

        world.SelectNextStackCommand.Execute(null);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -",
            "Bravo | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -",
            "Charlie | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));

        world.SelectNextStackCommand.Execute(null);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -",
            "Bravo | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -",
            "Charlie | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));

        world.SelectPreviousStackCommand.Execute(null);
        world.SelectPreviousStackCommand.Execute(null);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -",
            "Bravo | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -",
            "Charlie | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -"
        ]));

        world.SelectNextStackCommand.Execute(null);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -",
            "Bravo | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -",
            "Charlie | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));
        Assert.That(cameraFocusRequests, Is.EqualTo([
            new WorldPosition(0, 0),
            new WorldPosition(2, 0),
            new WorldPosition(0, 0),
            new WorldPosition(4, 0),
            new WorldPosition(0, 0)
        ]));
    }

    [Test]
    public async Task FinishStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack alpha = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);
        WorldStack bravo = TestHelpers.SimpleStack(
            new WorldStackId("Bravo"),
            player.Id,
            new WorldPosition(2, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 0);
        WorldStack charlie = TestHelpers.SimpleStack(
            new WorldStackId("Charlie"),
            player.Id,
            new WorldPosition(4, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(alpha)
            .WithStack(bravo)
            .WithStack(charlie)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        List<WorldPosition> cameraFocusRequests = [];
        world.CameraFocusRequested += (position, _) => cameraFocusRequests.Add(position);

        Assert.That(world.FinishStackCommand.CanExecute(null), Is.False);

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);

        Assert.That(world.FinishStackCommand.CanExecute(null), Is.True);

        world.FinishStackCommand.Execute(null);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | finished",
            "Bravo | unit-event-portrait/leader | name | level 1 | 0 / 6 | - | -",
            "Charlie | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -"
        ]));
        Assert.That(cameraFocusRequests, Is.EqualTo([new WorldPosition(4, 0)]));
        Assert.That(world.IsEndTurnHighlighted, Is.False);

        world.FinishStackCommand.Execute(null);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | finished",
            "Bravo | unit-event-portrait/leader | name | level 1 | 0 / 6 | - | -",
            "Charlie | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | finished"
        ]));
        Assert.That(cameraFocusRequests, Is.EqualTo([new WorldPosition(4, 0)]));
        Assert.That(world.IsEndTurnHighlighted, Is.True);
        Assert.That(world.FinishStackCommand.CanExecute(null), Is.False);

        world.SelectStackItemCommand.Execute(alpha.Id);

        Assert.That(world.FinishStackCommand.CanExecute(null), Is.False);

        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);
        world.CompleteAnimation();

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 4 / 6 | selected | -",
            "Bravo | unit-event-portrait/leader | name | level 1 | 0 / 6 | - | -",
            "Charlie | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | finished"
        ]));
        Assert.That(world.IsEndTurnHighlighted, Is.False);
        Assert.That(world.FinishStackCommand.CanExecute(null), Is.True);

        await world.EndTurnCommand.ExecuteAsync(null);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -",
            "Bravo | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -",
            "Charlie | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));
    }

    [Test]
    public async Task ExhaustedStackFinishesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldStack alpha = TestHelpers.SimpleStack(
            new WorldStackId("Alpha"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldStack bravo = TestHelpers.SimpleStack(
            new WorldStackId("Bravo"),
            player.Id,
            new WorldPosition(4, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 6);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(alpha)
            .WithStack(bravo)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        List<WorldPosition> cameraFocusRequests = [];
        world.CameraFocusRequested += (position, _) => cameraFocusRequests.Add(position);

        await world.HandleMapClickAsync(new WorldPosition(0, 0), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(1, 0), [], 1);
        world.CompleteAnimation();

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 0 / 6 | - | -",
            "Bravo | unit-event-portrait/leader | name | level 1 | 6 / 6 | selected | -"
        ]));
        Assert.That(cameraFocusRequests, Is.EqualTo([new WorldPosition(1, 0), new WorldPosition(4, 0)]));
        Assert.That(world.FinishStackCommand.CanExecute(null), Is.True);

        world.SelectStackItemCommand.Execute(alpha.Id);

        Assert.That(FormatStackItems(world.StackItems), Is.EqualTo([
            "Alpha | unit-event-portrait/leader | name | level 1 | 0 / 6 | selected | -",
            "Bravo | unit-event-portrait/leader | name | level 1 | 6 / 6 | - | -"
        ]));
        Assert.That(world.FinishStackCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task HotseatTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer first = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer second = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(first.LordId, first.Race, LordCategory.Warrior))
            .WithPlayer(first)
            .WithDefinition(TestHelpers.SimpleLord(second.LordId, second.Race, LordCategory.Warrior))
            .WithPlayer(second)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        HotseatTurnScreenViewModel firstTurn = Assert.IsOfType<HotseatTurnScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(firstTurn.LordName, Is.EqualTo("Player1"));
        Assert.That(firstTurn.RaceTitle, Is.EqualTo("Lord of the Empire"));
        Assert.That(firstTurn.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Human, 0, IsLarge: true)));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        await gameModel.World.DispatchCommandAsync(new WorldEndTurnCommand());

        HotseatTurnScreenViewModel secondTurn = Assert.IsOfType<HotseatTurnScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(secondTurn.LordName, Is.EqualTo("Player2"));
        Assert.That(secondTurn.RaceTitle, Is.EqualTo("Lord of the Undead Hordes"));
        Assert.That(secondTurn.PortraitKey, Is.EqualTo(new AssetSpriteKey.LordFace(WorldRace.Undead, 0, IsLarge: true)));

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }

    [Test]
    public async Task HotseatTurnSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCapital capital1 = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital1"),
            player1.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldCapital capital2 = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital2"),
            player2.Id,
            new WorldPosition(9, 9),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCapital(capital1)
            .WithCapital(capital2)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        audioPlayer.Actions.Clear();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<HotseatTurnScreenViewModel>());
        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:interface-open"]));
    }

    [Test]
    public async Task StackNavigationResponseSoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack first = TestHelpers.SimpleStack(
            new WorldStackId("First"),
            player.Id,
            new WorldPosition(1, 1),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("first"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);
        WorldStack second = TestHelpers.SimpleStack(
            new WorldStackId("Second"),
            player.Id,
            new WorldPosition(3, 3),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("second"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(first)
            .WithStack(second)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        TestAudioPlayer audioPlayer = new();
        GameViewModel gameViewModel = new TestGameViewModelBuilder().WithAudioPlayer(audioPlayer).Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        audioPlayer.Actions.Clear();

        world.SelectNextStackCommand.Execute(null);
        world.SelectNextStackCommand.Execute(null);
        world.SelectPreviousStackCommand.Execute(null);

        Assert.That(audioPlayer.Actions, Is.Empty);

        world.SelectStackItemCommand.Execute(second.Id);
        await world.HandleMapClickAsync(new WorldPosition(1, 1), [], 1);

        Assert.That(audioPlayer.Actions, Is.EqualTo(["Sound:stack-unit-response/leader/3", "Sound:stack-unit-response/leader/0"]));
    }

    [Test]
    public async Task DoubleClickInspectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 2);

        Assert.That(gameViewModel.TopScreen, Is.InstanceOf<ManagementScreenViewModel>());
    }

    [Test]
    public async Task DoubleClickInspectInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithCapital(capital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 2);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        Assert.IsOfType<CityTabViewModel>(screen.SelectedTab);
    }

    [Test]
    public async Task ScenarioGiveItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("Capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);
        ItemDefinition potion = new(
            new ItemDefinitionId("Potion"),
            new GlobalTextId("Healing potion"),
            new GlobalTextId("Heals 50 hit points"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects: [new ScenarioEventGiveItemEffect(potion.Id, ScenarioEventGiveItemTarget.TriggererCapital)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        LootScreenViewModel loot = Assert.IsOfType<LootScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(loot.Title, Is.EqualTo("Treasure"));
        Assert.That(loot.Text, Is.EqualTo("You received an item."));
        Assert.That(loot.Resources, Is.Null);
        Assert.That(
            loot.Items,
            Is.EqualTo(new LootItem[]
            {
                new("Healing potion", "Heals 50 hit points", new IconViewModel.Item(new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(potion.Id))))
            }));

        loot.Close();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsActive, Is.True);
    }

    [Test]
    public async Task ScenarioGiveSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("Spell"), new WorldRestoreMovementSpellEffect(50)) with
        {
            NameTextId = new GlobalTextId("Haste"),
            DescriptionTextId = new GlobalTextId("Restores movement")
        };

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects: [new ScenarioEventGiveSpellEffect(spell.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        LootScreenViewModel loot = Assert.IsOfType<LootScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(loot.Title, Is.EqualTo("New spells"));
        Assert.That(loot.Text, Is.EqualTo("You received a spell."));
        Assert.That(loot.Resources, Is.Null);
        Assert.That(
            loot.Items,
            Is.EqualTo(new LootItem[]
            {
                new("Haste", "Restores movement", new IconViewModel.Spell(new AssetSpriteKey.SpellIcon(spell.Id), IconViewModel.Spell.BackgroundType.Light))
            }));

        loot.Close();

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.IsActive, Is.True);
    }

    [Test]
    public async Task InspectStackInCapturedCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("EnemyCapital"),
            enemy.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty) with
        {
            CapturedById = player.Id
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithPlayer(enemy)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);
        await world.HandleMapClickAsync(new WorldPosition(4, 4), [], 1);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));

        world.InspectStackCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        Assert.IsOfType<StackTabViewModel>(screen.SelectedTab);
    }
}
