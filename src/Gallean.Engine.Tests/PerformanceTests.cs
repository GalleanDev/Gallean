using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Entities.GameData;
using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

[Explicit]
[NonParallelizable]
public sealed class PerformanceTests
{
    private static readonly UnitDefinition StackLeaderDefinition =
        TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 6);

    private static void RunBenchmark(string name, int iterations, Action action)
    {
        PerformanceMeasurement measurement = PerformanceTestHelpers.Measure(action, iterations, warmupIterations: 0);
        long elapsedPerIteration = (long)Math.Round(measurement.ElapsedTime.TotalMilliseconds);
        long cpuPerIteration = (long)Math.Round(measurement.CpuTime.TotalMilliseconds);
        long allocatedPerIteration = measurement.AllocatedBytes / 1024;

        TestContext.Out.WriteLine($"{name}: Time = {elapsedPerIteration:N0} ms, CPU = {cpuPerIteration:N0} ms, Allocated={allocatedPerIteration:N0} KB");
    }

    private static WorldMap CreateWorldMap(int width, int height)
    {
        ImmutableArray<WorldMapCell>.Builder cells = ImmutableArray.CreateBuilder<WorldMapCell>(width * height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                WorldMapFeature feature = (x + y) % 11 == 0 ? WorldMapFeature.Forest : WorldMapFeature.None;

                if ((x + y) % 17 == 0)
                {
                    feature = WorldMapFeature.Road;
                }

                cells.Add(new WorldMapCell(WorldMapTerrain.Neutral, feature, new WorldObjectVariant(0)));
            }
        }

        return new WorldMap(width, height, cells.MoveToImmutable());
    }

    private static WorldMap CreateSegmentedWorldMap(int width, int height)
    {
        ImmutableArray<WorldMapCell>.Builder cells = ImmutableArray.CreateBuilder<WorldMapCell>(width * height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                WorldMapTerrain terrain = WorldMapTerrain.Neutral;
                WorldMapFeature feature = (x + y) % 11 == 0 ? WorldMapFeature.Forest : WorldMapFeature.None;

                if ((x % 31 == 0 && y % 5 != 0) || (y % 37 == 0 && x % 7 != 0))
                {
                    feature = WorldMapFeature.Mountain;
                }
                else if ((x + 3 * y) % 43 == 0)
                {
                    terrain = WorldMapTerrain.Water;
                    feature = WorldMapFeature.None;
                }

                cells.Add(new WorldMapCell(terrain, feature, new WorldObjectVariant(0)));
            }
        }

        return new WorldMap(width, height, cells.MoveToImmutable());
    }

    private static IGameDefinitionProvider CreateDefinitions(int playerCount)
    {
        TestGameDefinitionProviderBuilder builder = new();
        builder.AddUnit(StackLeaderDefinition);

        for (int i = 0; i < playerCount; i++)
        {
            WorldPlayerId playerId = new(i + 1);
            builder.AddLord(TestHelpers.SimpleLord(new LordDefinitionId($"Lord{playerId.Value}"), WorldRace.Human, LordCategory.Warrior));
        }

        return builder.Build();
    }

    private static WorldState CreateWorldState(
        int width,
        int height,
        int playerCount,
        int stackCount,
        int resourceNodeCount,
        IGameDefinitionProvider definitions)
    {
        WorldMap map = CreateWorldMap(width, height);
        ImmutableBitmap fogOfWar = ImmutableBitmap.Create(width, height, initialValue: true);
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer>.Builder players = ImmutableSortedDictionary.CreateBuilder<WorldPlayerId, WorldPlayer>();

        for (int i = 0; i < playerCount; i++)
        {
            WorldPlayerId playerId = new(i + 1);
            WorldRace race = i == playerCount - 1 ? WorldRace.Neutral : WorldRace.Human;
            WorldPlayer player = new(
                playerId,
                new ScenarioTextId("Name"),
                new LordDefinitionId($"Lord{playerId.Value}"),
                race,
                new WorldObjectVariant(0),
                WorldAiAttitudeType.Medium,
                true,
                WorldResourceSet.Empty,
                new WorldPlayerBuildings([], []), new WorldPlayerSpellbook([], []),
                fogOfWar,
                [],
                [],
                [],
                new WorldPlayerTurnActions());

            players.Add(playerId, player);
        }

        ImmutableSortedDictionary<WorldStackId, WorldStack>.Builder stacks =
            ImmutableSortedDictionary.CreateBuilder<WorldStackId, WorldStack>();
        HashSet<WorldPosition> stackPositions = new(stackCount);

        for (int i = 0; i < stackCount; i++)
        {
            WorldStackId stackId = new($"Stack{i}");
            WorldPlayerId ownerId = new((i % Math.Max(1, playerCount - 1)) + 1);
            WorldUnitId leaderId = new($"Leader{i}");
            WorldPosition position = new(i % width, i / width % height);
            WorldStack stack = TestHelpers.SimpleStack(stackId, ownerId, position, TestHelpers.SingleUnitGrid(leaderId, StackLeaderDefinition), movementPoints: 20);
            stacks.Add(stackId, stack);
            stackPositions.Add(position);
        }

        ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Builder resourceNodes =
            ImmutableSortedDictionary.CreateBuilder<WorldResourceNodeId, WorldResourceNode>();
        HashSet<WorldPosition> nodePositions = new(resourceNodeCount);

        for (int i = 0; i < resourceNodeCount; i++)
        {
            int x = 1 + (i * 37) % (width - 2);
            int y = 1 + (i * 53) % (height - 2);
            WorldPosition position = new(x, y);

            while (stackPositions.Contains(position) || !nodePositions.Add(position))
            {
                x = x + 1 < width - 1 ? x + 1 : 1;

                if (x == 1)
                {
                    y = y + 1 < height - 1 ? y + 1 : 1;
                }

                position = new WorldPosition(x, y);
            }

            WorldResourceNodeId nodeId = new($"Node{i}");
            WorldPlayerId ownerId = new(i % Math.Max(1, playerCount - 1) + 1);
            WorldResourceNode node = new(nodeId, ownerId, position, WorldResourceType.Gold, AiPriority: 3);
            resourceNodes.Add(nodeId, node);
        }

        WorldState state = new(
            map,
            new WorldTurnInfo(1, new WorldPlayerId(1)),
            WorldDifficulty.Average,
            players.ToImmutable(),
            ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            stacks.ToImmutable(),
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            resourceNodes.ToImmutable(),
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            WorldScenarioState.Empty,
            ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty);

        TestWorldStateValidator.Validate(state, definitions);

        return state;
    }

    private static ImmutableSortedDictionary<WorldFortificationId, WorldCity> CreateWorldCities(int width, int height, int playerCount, int cityCount)
    {
        ImmutableSortedDictionary<WorldFortificationId, WorldCity>.Builder cities = ImmutableSortedDictionary.CreateBuilder<WorldFortificationId, WorldCity>();

        for (int i = 0; i < cityCount; i++)
        {
            WorldFortificationId cityId = new($"City{i}");
            WorldPlayerId ownerId = new(i % Math.Max(1, playerCount - 1) + 1);
            int x = 3 + i * 13 % (width - 6);
            int y = 3 + i / Math.Max(1, width / 13) * 17 % (height - 6);
            WorldPosition position = new(x, y);
            WorldCity city = new(
                cityId,
                new ScenarioTextId("Name"),
                new WorldCityDefinitionId(WorldRace.Human, 1),
                ownerId,
                TestHelpers.NeutralSubrace.Id,
                FortificationCalculator.CreateFootprint(position, FortificationCalculator.CityFootprint),
                UnitGrid<WorldUnit>.Empty,
                WorldInventory.Empty,
                AiPriority: 3,
                RiotTurns: 0,
                GrowthTurn: 0);
            cities.Add(cityId, city);
        }

        return cities.ToImmutable();
    }

    [Test]
    public void PathfindingTest()
    {
        WorldState state = CreateWorldState(80, 80, 4, 0, 500, CreateDefinitions(4));
        ImmutableBitmap fogOfWar = ImmutableBitmap.Create(state.Map.Width, state.Map.Height, initialValue: true);
        WorldPosition start = new(0, 0);
        WorldPosition end = new(79, 79);
        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);

        RunBenchmark("Pathfinding", 10, () =>
        {
            WorldPlan.For(state).GetPath(start, end, fogOfWar, profile, WorldMovementLimit.Unlimited);
        });
    }

    [Test]
    public void PathMapTest()
    {
        WorldState state = CreateWorldState(80, 80, 4, 0, 500, CreateDefinitions(4));
        WorldMap map = state.Map;
        ImmutableBitmap fogOfWar = ImmutableBitmap.Create(map.Width, map.Height, initialValue: true);
        ImmutableBitmap.Builder blockedCells = ImmutableBitmap.Create(map.Width, map.Height).ToBuilder();

        foreach (WorldResourceNode resourceNode in state.ResourceNodes.Values)
        {
            blockedCells.Set(resourceNode.Position.X, resourceNode.Position.Y);
        }

        ImmutableBitmap blocked = blockedCells.ToImmutable();
        ImmutableBitmap terminalCells = ImmutableBitmap.Create(map.Width, map.Height);
        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);
        WorldPosition start = new(0, 0);
        Func<WorldPosition, WorldPosition, int> stepCost = (from, to) => WorldMovementCalculator.GetMovementCost(map, to, profile);

        RunBenchmark("Path map", 200, () =>
        {
            _ = PathMapCalculator.Calculate(map, start, fogOfWar, blocked, terminalCells, stepCost);
        });
    }

    [Test]
    public void MoveInfoTest()
    {
        IGameDefinitionProvider definitions = CreateDefinitions(4);
        WorldState state = CreateWorldState(80, 80, 4, 200, 500, definitions);
        GlobalVariables globals = new();
        WorldEngine engine = new(new EngineContext(new TestSeedRandomProvider(), definitions, new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition), GameScenarioProvider.Empty, globals), state);
        WorldStack stack = state.Stacks.Values.First(item => item.OwnerId == state.TurnInfo.CurrentPlayerId);
        WorldGetMoveInfoCommand command = new(stack.Id);

        RunBenchmark("Move info", 10, () =>
        {
            engine.DispatchCommand(command, dryRun: true);
        });
    }

    [Test]
    public void EndTurnTest()
    {
        IGameDefinitionProvider definitions = CreateDefinitions(6);
        WorldState state = CreateWorldState(80, 80, 6, 1000, 2000, definitions);
        GlobalVariables globals = new();
        WorldEngine engine = new(new EngineContext(new TestSeedRandomProvider(), definitions, new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition), GameScenarioProvider.Empty, globals), state);
        WorldEndTurnCommand command = new();

        RunBenchmark("End turn", 10, () =>
        {
            engine.DispatchCommand(command, dryRun: true);
        });
    }

    [Test]
    public void TerritoryExpansionTest()
    {
        IGameDefinitionProvider definitions = CreateDefinitions(6);
        WorldState state = CreateWorldState(200, 200, 6, 0, 0, definitions);
        WorldPlayerId playerId = new(1);
        GlobalVariables globals = new();
        EngineContext context = new(
            new TestSeedRandomProvider(),
            definitions,
            new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition),
            GameScenarioProvider.Empty,
            globals);

        state = state with
        {
            Map = CreateSegmentedWorldMap(200, 200),
            Fortifications = CreateWorldCities(200, 200, 6, 500).ToImmutableSortedDictionary(entry => entry.Key, entry => (WorldFortification)entry.Value)
        };

        for (int i = 0; i < 12; i++)
        {
            foreach (WorldPlayer player in state.Players.Values)
            {
                state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, player.Id, context) };
            }
        }

        WorldState settledState = state;

        RunBenchmark("Territory expansion", 10, () =>
        {
            ExpansionCalculator.ExpandPlayerTerrain(settledState, playerId, context);
        });
    }

    [Test]
    public void ScenarioDatabaseLoadTest()
    {
        RunBenchmark("Scenario database load (all scenarios)", 3, () =>
        {
            _ = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        });

        RunBenchmark("Scenario database load (single scenario)", 10, () =>
        {
            _ = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath, new Db.ScenarioId("S095SC0000"));
        });

        RunBenchmark("Scenario database index load", 10, () =>
        {
            _ = ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath);
        });
    }

    [Test]
    public void CampaignCatalogLoadTest()
    {
        RunBenchmark("Global database load", 5, () =>
        {
            _ = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        });

        RunBenchmark("Global database index load", 10, () =>
        {
            _ = GlobalDatabaseLoader.LoadIndex(RiseOfTheElves.GlobalDatabasePath);
        });

        RunBenchmark("Campaign database load", 10, () =>
        {
            _ = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        });

        GlobalDatabaseIndex globalIndex = GlobalDatabaseLoader.LoadIndex(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabaseIndex scenarioIndex = ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath);

        RunBenchmark("Enumerate campaigns", 10, () =>
        {
            _ = ScenarioLoader.EnumerateCampaigns(globalIndex, scenarioIndex, campaignDb);
        });

        RunBenchmark("Campaign select (cold)", 5, () =>
        {
            GlobalDatabaseIndex globals = GlobalDatabaseLoader.LoadIndex(RiseOfTheElves.GlobalDatabasePath);
            CampaignDatabase campaigns = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
            ScenarioDatabaseIndex scenarios = ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath);

            _ = ScenarioLoader.EnumerateCampaigns(globals, scenarios, campaigns);
        });
    }

    [Test]
    [TestCase("S095SC0000", 5)]
    [TestCase("S099SC0000", 8)]
    public void WorldAiTest(string scenarioId, int turns)
    {
        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        GlobalVariables globals = new();
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId(scenarioId), definitions, globals);

        EngineContext context = new(new TestSeedRandomProvider(), definitions, new UnitStatsProvider(definitions, globals, scenario.Provider.Definition), scenario.Provider, globals);
        GameSession session = new(context, scenario.State);
        _ = session.World.DispatchCommand(new WorldStartGameCommand());

        RunBenchmark($"World AI ({scenarioId})", turns, () =>
        {
            TestWorldAiRunner runner = new(session, context);
            _ = runner.PlayTurn();
        });
    }
}
