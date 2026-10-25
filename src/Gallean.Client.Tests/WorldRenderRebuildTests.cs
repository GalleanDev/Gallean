using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class WorldRenderRebuildTests
{
    private sealed record RebuildState(
        bool NewRequest,
        bool Terrain,
        bool Fog,
        bool Objects,
        bool Stacks,
        bool World,
        bool Minimap);

    private sealed record Fixture(WorldScreenViewModel World, GameModel Model);

    private sealed class RebuildProbe
    {
        private readonly WorldRenderComposer composer;
        private readonly WorldScreenViewModel world;
        private WorldRenderState last;

        public RebuildProbe(WorldScreenViewModel world)
        {
            this.world = world;
            composer = new WorldRenderComposer(world.GameModel.Definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
            last = composer.Compose(world.RenderRequest);
        }

        public RebuildState Measure(Action action)
        {
            WorldRenderRequest before = world.RenderRequest;
            action();

            return Classify(before);
        }

        public async Task<RebuildState> MeasureAsync(Func<Task> action)
        {
            WorldRenderRequest before = world.RenderRequest;
            await action();

            return Classify(before);
        }

        private RebuildState Classify(WorldRenderRequest before)
        {
            WorldRenderRequest after = world.RenderRequest;
            WorldRenderState next = composer.Compose(after);

            RebuildState rebuildState = new(
                !ReferenceEquals(before, after),
                !ReferenceEquals(last.Terrain, next.Terrain),
                !ReferenceEquals(last.FogTiles, next.FogTiles),
                !ReferenceEquals(last.MapObjects, next.MapObjects),
                !ReferenceEquals(last.Stacks, next.Stacks),
                !ReferenceEquals(last, next),
                !ReferenceEquals(last.Minimap, next.Minimap));

            last = next;

            return rebuildState;
        }
    }

    private static readonly WorldPosition AlphaStart = new(0, 0);
    private static readonly WorldPosition BravoStart = new(3, 2);
    private static readonly WorldPosition NearTile = new(2, 0);
    private static readonly WorldPosition FarTile = new(3, 0);
    private static readonly WorldPosition EmptyTile = new(1, 0);
    private static readonly WorldPosition InvalidTile = new(-1, -1);

    private static Fixture CreateWorld()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 99, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack alpha = MakeStack(new WorldUnitId("alphaLeader"), "Alpha", player1.Id, AlphaStart, leader, stackDefinitions);
        WorldStack bravo = MakeStack(new WorldUnitId("bravoLeader"), "Bravo", player1.Id, BravoStart, leader, stackDefinitions);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(alpha)
            .WithStack(bravo)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        return new Fixture(world, gameModel);
    }

    private static WorldStack MakeStack(WorldUnitId leaderId, string id, WorldPlayerId owner, WorldPosition position, UnitDefinition leader, IGameDefinitionProvider definitions)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId(id),
            owner,
            position,
            TestHelpers.StackUnitGrid(
                definitions,
                [(new WorldUnit(leaderId, leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 99);
    }

    [Test]
    public async Task SelectStackTest()
    {
        Fixture fixture = CreateWorld();
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = await probe.MeasureAsync(() => fixture.World.HandleMapClickAsync(AlphaStart, null, 1));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: true,
            Minimap: false)));
    }

    [Test]
    public void SelectStackItemTest()
    {
        Fixture fixture = CreateWorld();
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(() => fixture.World.SelectStackItemCommand.Execute(new WorldStackId("Alpha")));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: true,
            Minimap: false)));
    }

    [Test]
    public async Task HoverPathTest()
    {
        Fixture fixture = CreateWorld();
        await fixture.World.HandleMapClickAsync(AlphaStart, null, 1);
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(() => fixture.World.UpdateHover(NearTile, null));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: true,
            Minimap: false)));
    }

    [Test]
    public async Task MoveStackTest()
    {
        Fixture fixture = CreateWorld();
        await fixture.World.HandleMapClickAsync(AlphaStart, null, 1);
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = await probe.MeasureAsync(() => fixture.World.HandleMapClickAsync(NearTile, null, 1));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: true,
            World: true,
            Minimap: true)));
    }

    [Test]
    public async Task CompleteAnimationTest()
    {
        Fixture fixture = CreateWorld();
        await fixture.World.HandleMapClickAsync(AlphaStart, null, 1);
        await fixture.World.HandleMapClickAsync(NearTile, null, 1);
        await fixture.World.HandleMapClickAsync(NearTile, null, 1);
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(fixture.World.CompleteAnimation);

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: true,
            World: true,
            Minimap: false)));
    }

    [Test]
    public async Task HoverTest()
    {
        Fixture fixture = CreateWorld();
        await fixture.World.HandleMapClickAsync(AlphaStart, null, 1);
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(() => fixture.World.UpdateHover(NearTile, null));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: true,
            Minimap: false)));
    }

    [Test]
    public async Task HoverUnchangedTest()
    {
        Fixture fixture = CreateWorld();
        await fixture.World.HandleMapClickAsync(AlphaStart, null, 1);
        fixture.World.UpdateHover(NearTile, null);
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(() => fixture.World.UpdateHover(NearTile, null));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: false,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: false,
            Minimap: false)));
    }

    [Test]
    public void HoverWithoutSelectionTest()
    {
        Fixture fixture = CreateWorld();
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(() => fixture.World.UpdateHover(NearTile, null));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: false,
            Minimap: false)));
    }

    [Test]
    public async Task ClearHoverTest()
    {
        Fixture fixture = CreateWorld();
        await fixture.World.HandleMapClickAsync(AlphaStart, null, 1);
        fixture.World.UpdateHover(NearTile, null);
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(fixture.World.ClearHover);

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: true,
            Minimap: false)));
    }

    [Test]
    public void ToggleFogTest()
    {
        Fixture fixture = CreateWorld();
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(() => fixture.World.Game.DevTools.IsFogEnabled = false);

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: true,
            Objects: true,
            Stacks: true,
            World: true,
            Minimap: true)));
    }

    [Test]
    public void ShowPositionInfoTest()
    {
        Fixture fixture = CreateWorld();
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(() => fixture.World.ShowPositionInfo(AlphaStart, null));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: false,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: false,
            Minimap: false)));
    }

    [Test]
    public void HidePositionInfoTest()
    {
        Fixture fixture = CreateWorld();
        fixture.World.ShowPositionInfo(AlphaStart, null);
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = probe.Measure(fixture.World.HidePositionInfo);

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: false,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: false,
            Minimap: false)));
    }

    [Test]
    public async Task ClickInvalidPositionTest()
    {
        Fixture fixture = CreateWorld();
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = await probe.MeasureAsync(() => fixture.World.HandleMapClickAsync(InvalidTile, null, 1));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: false,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: false,
            Minimap: false)));
    }

    [Test]
    public async Task ClickEmptyGroundTest()
    {
        Fixture fixture = CreateWorld();
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = await probe.MeasureAsync(() => fixture.World.HandleMapClickAsync(EmptyTile, null, 1));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: false,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: false,
            Minimap: false)));
    }

    [Test]
    public async Task ExternalSnapshotTest()
    {
        Fixture fixture = CreateWorld();
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = await probe.MeasureAsync(() => fixture.Model.World.DispatchCommandAsync(new WorldStartGameCommand()));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: false,
            Minimap: false)));
    }

    [Test]
    public async Task EndTurnTest()
    {
        Fixture fixture = CreateWorld();
        await fixture.Model.World.DispatchCommandAsync(new WorldStartGameCommand());
        RebuildProbe probe = new(fixture.World);

        RebuildState rebuildState = await probe.MeasureAsync(() => fixture.World.EndTurnCommand.ExecuteAsync(null));

        Assert.That(rebuildState, Is.EqualTo(new RebuildState(
            NewRequest: true,
            Terrain: false,
            Fog: false,
            Objects: false,
            Stacks: false,
            World: false,
            Minimap: false)));
    }

    [Test]
    public async Task ActionTest()
    {
        (WorldScreenViewModel world, GameModel gameModel) = CreateWorld();
        RebuildProbe probe = new(world);

        List<(string Action, RebuildState Outcome)> log =
        [
            ("Click empty ground (no selection)", await probe.MeasureAsync(() => world.HandleMapClickAsync(EmptyTile, null, 1))),
            ("Click invalid position", await probe.MeasureAsync(() => world.HandleMapClickAsync(InvalidTile, null, 1))),
            ("Hover tile (no selection)", probe.Measure(() => world.UpdateHover(EmptyTile, null))),
            ("Select own stack", await probe.MeasureAsync(() => world.HandleMapClickAsync(AlphaStart, null, 1))),
            ("Hover reachable tile (selected)", probe.Measure(() => world.UpdateHover(NearTile, null))),
            ("Hover same tile again", probe.Measure(() => world.UpdateHover(NearTile, null))),
            ("Clear hover", probe.Measure(world.ClearHover)),
            ("Click reachable tile (move stack)", await probe.MeasureAsync(() => world.HandleMapClickAsync(FarTile, null, 1))),
            ("Complete movement animation", probe.Measure(world.CompleteAnimation)),
            ("Select stack item", probe.Measure(() => world.SelectStackItemCommand.Execute(new WorldStackId("Bravo")))),
            ("Show info", probe.Measure(() => world.ShowPositionInfo(BravoStart, null))),
            ("Hide info", probe.Measure(world.HidePositionInfo)),
            ("Toggle fog off", probe.Measure(() => world.Game.DevTools.IsFogEnabled = false)),
            ("Toggle fog on", probe.Measure(() => world.Game.DevTools.IsFogEnabled = true)),
            ("External snapshot (start game)", await probe.MeasureAsync(() => gameModel.World.DispatchCommandAsync(new WorldStartGameCommand()))),
            ("End turn", await probe.MeasureAsync(() => world.EndTurnCommand.ExecuteAsync(null)))
        ];

        Assert.That(log, Is.EqualTo([
            ("Click empty ground (no selection)", new RebuildState(
                NewRequest: false,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: false,
                Minimap: false)),
            ("Click invalid position", new RebuildState(
                NewRequest: false,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: false,
                Minimap: false)),
            ("Hover tile (no selection)", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: false,
                Minimap: false)),
            ("Select own stack", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: true,
                Minimap: false)),
            ("Hover reachable tile (selected)", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: true,
                Minimap: false)),
            ("Hover same tile again", new RebuildState(
                NewRequest: false,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: false,
                Minimap: false)),
            ("Clear hover", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: true,
                Minimap: false)),
            ("Click reachable tile (move stack)", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: true,
                World: true,
                Minimap: true)),
            ("Complete movement animation", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: true,
                World: true,
                Minimap: false)),
            ("Select stack item", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: true,
                Minimap: false)),
            ("Show info", new RebuildState(
                NewRequest: false,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: false,
                Minimap: false)),
            ("Hide info", new RebuildState(
                NewRequest: false,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: false,
                Minimap: false)),
            ("Toggle fog off", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: true,
                Objects: true,
                Stacks: true,
                World: true,
                Minimap: true)),
            ("Toggle fog on", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: true,
                Objects: true,
                Stacks: true,
                World: true,
                Minimap: true)),
            ("External snapshot (start game)", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: true,
                World: true,
                Minimap: true)),
            ("End turn", new RebuildState(
                NewRequest: true,
                Terrain: false,
                Fog: false,
                Objects: false,
                Stacks: false,
                World: false,
                Minimap: false))
        ]));
    }
}
