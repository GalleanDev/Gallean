using System.Collections.Concurrent;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class WorldScreenSnapshotWorkerTests
{
    private static WorldState BuildWorldState(string[] mapRows)
    {
        WorldMap map = TestHelpers.BuildMap(mapRows);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        return new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build()
            .State;
    }

    private static WorldRenderRequest BuildRequest(WorldState state)
    {
        return new WorldRenderRequest(
            state,
            state.TurnInfo.CurrentPlayerId,
            Cursor: WorldCursor.Empty,
            FogEnabled: false,
            Animation: null);
    }

    [Test]
    public void FirstRequestTest()
    {
        int callingThread = Environment.CurrentManagedThreadId;
        ConcurrentQueue<int> buildThreads = new();

        TestAssetProviderBuilder builder = new();
        Sprite ground = builder.CreateSprite(SKColors.Gray, 64, 32);
        Sprite feature = builder.CreateSprite(SKColors.Red, 8, 8);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 8, Height: 8, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [feature]);
        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? ground : feature)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .OnRequest(_ => buildThreads.Enqueue(Environment.CurrentManagedThreadId))
            .Build();

        WorldState state = BuildWorldState(["."]);
        WorldRenderRequest request = BuildRequest(state);

        using WorldScreenSnapshotWorker worker = new(assets, new TestGameDefinitionProviderBuilder().Build(), GameScenarioProvider.Empty, new DiagnosticsRecorder());

        worker.Request(request);

        SpinWait.SpinUntil(() => worker.Current is not null, TimeSpan.FromSeconds(30));

        WorldScreenSnapshot snapshot = Assert.NotNull(worker.Current);

        Assert.That(snapshot.RenderRequest, Is.SameAs(request));
        Assert.That(snapshot.WorldState, Is.Not.Null);
        Assert.That(snapshot.WorldContent, Is.Not.Null);
        Assert.That(snapshot.WorldContent.MinimapImage, Is.Not.Null);
        Assert.That(buildThreads, Is.Not.Empty);
        Assert.That(buildThreads, Has.All.Not.EqualTo(callingThread));
    }

    [Test]
    public void LatestRequestTest()
    {
        int callingThread = Environment.CurrentManagedThreadId;
        ConcurrentQueue<int> buildThreads = new();

        TestAssetProviderBuilder builder = new();
        Sprite ground = builder.CreateSprite(SKColors.Gray, 64, 32);
        Sprite feature = builder.CreateSprite(SKColors.Red, 8, 8);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 8, Height: 8, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [feature]);
        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? ground : feature)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .OnRequest(_ => buildThreads.Enqueue(Environment.CurrentManagedThreadId))
            .Build();

        WorldRenderRequest first = BuildRequest(BuildWorldState(["."]));
        WorldRenderRequest second = BuildRequest(BuildWorldState([".."]));
        WorldRenderRequest third = BuildRequest(BuildWorldState(["..."]));
        WorldRenderRequest fourth = BuildRequest(BuildWorldState(["...."]));

        using WorldScreenSnapshotWorker worker = new(assets, new TestGameDefinitionProviderBuilder().Build(), GameScenarioProvider.Empty, new DiagnosticsRecorder());

        worker.Request(first);
        worker.Request(second);
        worker.Request(third);
        worker.Request(fourth);

        SpinWait.SpinUntil(() => ReferenceEquals(worker.Current?.RenderRequest, fourth), TimeSpan.FromSeconds(30));

        Assert.That(worker.Current?.RenderRequest, Is.SameAs(fourth));
        Assert.That(buildThreads, Has.Some.Not.EqualTo(callingThread));
    }

    [Test]
    public void BuildFailureTest()
    {
        int assetRequests = 0;

        TestAssetProviderBuilder builder = new();
        Sprite ground = builder.CreateSprite(SKColors.Gray, 64, 32);
        Sprite feature = builder.CreateSprite(SKColors.Red, 8, 8);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 8, Height: 8, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [feature]);
        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? ground : feature)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .OnRequest(_ =>
            {
                Interlocked.Increment(ref assetRequests);
                throw new InvalidOperationException("Asset load failed.");
            })
            .Build();

        WorldState state = BuildWorldState(["."]);
        WorldRenderRequest request = BuildRequest(state);

        using WorldScreenSnapshotWorker worker = new(assets, new TestGameDefinitionProviderBuilder().Build(), GameScenarioProvider.Empty, new DiagnosticsRecorder());

        worker.Request(request);

        SpinWait.SpinUntil(() => worker.Exception is not null, TimeSpan.FromSeconds(30));

        Assert.That(worker.Current, Is.Null);
        Assert.That(worker.Exception, Is.InstanceOf<InvalidOperationException>());

        int requestsAfterFailure = Volatile.Read(ref assetRequests);

        worker.Request(BuildRequest(state with { }));

        Assert.That(worker.Current, Is.Null);
        Assert.That(Volatile.Read(ref assetRequests), Is.EqualTo(requestsAfterFailure));
    }

    [Test]
    public void BuildFailureAfterSnapshotTest()
    {
        bool failed = false;

        TestAssetProviderBuilder builder = new();
        Sprite ground = builder.CreateSprite(SKColors.Gray, 64, 32);
        Sprite feature = builder.CreateSprite(SKColors.Red, 8, 8);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 8, Height: 8, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [feature]);
        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? ground : feature)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .OnRequest(_ =>
            {
                if (Volatile.Read(ref failed))
                {
                    throw new InvalidOperationException("Asset load failed.");
                }
            })
            .Build();

        WorldRenderRequest first = BuildRequest(BuildWorldState(["~"]));
        WorldRenderRequest second = BuildRequest(BuildWorldState(["~~"]));
        WorldRenderRequest third = BuildRequest(BuildWorldState(["~~~"]));

        using WorldScreenSnapshotWorker worker = new(assets, new TestGameDefinitionProviderBuilder().Build(), GameScenarioProvider.Empty, new DiagnosticsRecorder());

        worker.Request(first);

        SpinWait.SpinUntil(() => ReferenceEquals(worker.Current?.RenderRequest, first), TimeSpan.FromSeconds(30));

        Assert.That(worker.Current?.RenderRequest, Is.SameAs(first));
        Assert.That(worker.Exception, Is.Null);

        Volatile.Write(ref failed, true);
        worker.Request(second);

        SpinWait.SpinUntil(() => worker.Exception is not null, TimeSpan.FromSeconds(30));

        Assert.That(worker.Exception, Is.InstanceOf<InvalidOperationException>());
        Assert.That(worker.Current?.RenderRequest, Is.SameAs(first));

        worker.Request(third);

        Assert.That(worker.Current?.RenderRequest, Is.SameAs(first));
        Assert.That(worker.Exception, Is.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void RequestAfterDisposeTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite ground = builder.CreateSprite(SKColors.Gray, 64, 32);
        Sprite feature = builder.CreateSprite(SKColors.Red, 8, 8);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 8, Height: 8, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [feature]);
        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? ground : feature)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .OnRequest(_ => { })
            .Build();

        WorldRenderRequest first = BuildRequest(BuildWorldState(["."]));
        WorldRenderRequest second = BuildRequest(BuildWorldState([".."]));

        WorldScreenSnapshotWorker worker = new(assets, new TestGameDefinitionProviderBuilder().Build(), GameScenarioProvider.Empty, new DiagnosticsRecorder());

        worker.Request(first);

        SpinWait.SpinUntil(() => worker.Current is not null, TimeSpan.FromSeconds(30));

        worker.Dispose();

        worker.Request(second);

        Assert.That(worker.Current?.RenderRequest, Is.SameAs(first));
        Assert.That(worker.Exception, Is.Null);
        Assert.DoesNotThrow(worker.Dispose);
    }

    [Test]
    public void DisposeDuringBuildTest()
    {
        using ManualResetEventSlim buildBlocked = new();
        using ManualResetEventSlim buildReleased = new();

        bool block = false;

        TestAssetProviderBuilder builder = new();
        Sprite ground = builder.CreateSprite(SKColors.Gray, 64, 32);
        Sprite feature = builder.CreateSprite(SKColors.Red, 8, 8);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 8, Height: 8, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [feature]);
        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? ground : feature)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .OnRequest(_ =>
            {
                if (Volatile.Read(ref block))
                {
                    buildBlocked.Set();
                    buildReleased.Wait();
                }
            })
            .Build();

        WorldRenderRequest first = BuildRequest(BuildWorldState(["~"]));
        WorldRenderRequest second = BuildRequest(BuildWorldState(["~~"]));

        WorldScreenSnapshotWorker worker = new(assets, new TestGameDefinitionProviderBuilder().Build(), GameScenarioProvider.Empty, new DiagnosticsRecorder());

        worker.Request(first);

        SpinWait.SpinUntil(() => worker.Current is not null, TimeSpan.FromSeconds(30));

        Volatile.Write(ref block, true);
        worker.Request(second);

        Assert.That(buildBlocked.Wait(TimeSpan.FromSeconds(30)), Is.True);

        Task dispose = Task.Run(worker.Dispose);

        Assert.That(dispose.Wait(TimeSpan.FromSeconds(1)), Is.False);

        buildReleased.Set();

        Assert.That(dispose.Wait(TimeSpan.FromSeconds(30)), Is.True);
        Assert.That(worker.Exception, Is.Null);
        Assert.That(worker.Current?.RenderRequest, Is.SameAs(second));
    }

    [Test]
    public void CachedAnimationsTest()
    {
        List<AssetKey> animationLoads = [];

        TestAssetProviderBuilder builder = new();
        Sprite ground = builder.CreateSprite(SKColors.Gray, 64, 32);
        Sprite feature = builder.CreateSprite(SKColors.Red, 8, 8);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 8, Height: 8, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [feature]);
        using TestAssetProvider assets = builder
            .ResolveSprite(key => key is AssetSpriteKey.Ground ? ground : feature)
            .ResolveAnimation(key =>
            {
                lock (animationLoads)
                {
                    animationLoads.Add(key);
                }

                return animation;
            })
            .ResolveSound(_ => null)
            .OnRequest(_ => { })
            .Build();

        UnitDefinition firstLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader1", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        UnitDefinition secondLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader2", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(firstLeader)
            .AddUnit(secondLeader)
            .Build();

        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack firstStack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("firstLeader"), firstLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldStack secondStack = TestHelpers.SimpleStack(
            new WorldStackId("s2"),
            human.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("secondLeader"), secondLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(firstLeader)
            .WithDefinition(secondLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(firstStack)
            .WithStack(secondStack)
            .Build()
            .State;

        WorldRenderRequest request = BuildRequest(state);

        using WorldScreenSnapshotWorker worker = new(assets, definitions, GameScenarioProvider.Empty, new DiagnosticsRecorder());

        worker.Request(request);

        SpinWait.SpinUntil(() => worker.Current is not null, TimeSpan.FromSeconds(30));

        Assert.That(worker.Current, Is.Not.Null);

        lock (animationLoads)
        {
            animationLoads.Clear();
        }

        using AssetCache<Animation>.Lease firstLease = assets.GetRequiredAnimation(new AssetAnimationKey.StackUnit(firstLeader.Id, AssetAnimationKey.StackUnitPose.Stop, (int)WorldStackDirection.South));
        using AssetCache<Animation>.Lease secondLease = assets.GetRequiredAnimation(new AssetAnimationKey.StackUnit(secondLeader.Id, AssetAnimationKey.StackUnitPose.Stop, (int)WorldStackDirection.South));

        Assert.That(animationLoads, Is.Empty);
    }
}
