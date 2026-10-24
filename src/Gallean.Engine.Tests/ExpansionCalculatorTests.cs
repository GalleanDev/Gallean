using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class ExpansionCalculatorTests
{
    private static ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> CreatePlayers(
        params (WorldPlayerId OwnerId, WorldRace Race)[] playerData)
    {
        Dictionary<WorldPlayerId, WorldPlayer> players = [];

        foreach ((WorldPlayerId ownerId, WorldRace race) in playerData)
        {
            players[ownerId] = new WorldPlayer(
                ownerId,
                new ScenarioTextId($"Player{ownerId.Value}"),
                new LordDefinitionId($"Lord{ownerId.Value}"),
                race,
                new WorldObjectVariant(0),
                WorldAiAttitudeType.Medium,
                true,
                WorldResourceSet.Empty,
                new WorldPlayerBuildings([], []), new WorldPlayerSpellbook([], []),
                ImmutableBitmap.Create(10, 10),
                [],
                [],
                [],
                new WorldPlayerTurnActions());
        }

        return players.ToImmutableSortedDictionary();
    }

    private static WorldCity CreateCity(WorldFortificationId cityId, WorldPlayerId ownerId, WorldPosition entrance, int level)
    {
        return new WorldCity(
            cityId,
            new ScenarioTextId("Name"),
            new WorldCityDefinitionId(WorldRace.Human, level),
            ownerId,
            TestHelpers.NeutralSubrace.Id,
            FortificationCalculator.CreateFootprint(entrance, FortificationCalculator.CityFootprint),
            UnitGrid<WorldUnit>.Empty,
            WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);
    }

    private static ImmutableSortedDictionary<WorldFortificationId, WorldFortification> CreateFortifications(
        params WorldFortification[] fortifications)
    {
        Dictionary<WorldFortificationId, WorldFortification> result = [];

        foreach (WorldFortification fortification in fortifications)
        {
            result[fortification.Id] = fortification;
        }

        return result.ToImmutableSortedDictionary();
    }

    private static ImmutableSortedDictionary<WorldRodId, WorldRod> CreateRods(params WorldRod[] rods)
    {
        Dictionary<WorldRodId, WorldRod> result = [];

        foreach (WorldRod rod in rods)
        {
            result[rod.Id] = rod;
        }

        return result.ToImmutableSortedDictionary();
    }

    private static ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> CreateResourceNodes(
        params WorldResourceNode[] nodes)
    {
        Dictionary<WorldResourceNodeId, WorldResourceNode> result = [];

        foreach (WorldResourceNode node in nodes)
        {
            result[node.Id] = node;
        }

        return result.ToImmutableSortedDictionary();
    }

    private static ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark> CreateLandmarks(params WorldLandmark[] landmarks)
    {
        Dictionary<WorldLandmarkId, WorldLandmark> result = [];

        foreach (WorldLandmark landmark in landmarks)
        {
            result[landmark.Id] = landmark;
        }

        return result.ToImmutableSortedDictionary();
    }

    private static WorldState CreateState(
        WorldMap map,
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players,
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications,
        ImmutableSortedDictionary<WorldRodId, WorldRod> rods,
        ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> resourceNodes,
        ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark> landmarks)
    {
        return new WorldState(
            Map: map,
            TurnInfo: new WorldTurnInfo(1, players.Keys.First()),
            Difficulty: WorldDifficulty.Average,
            Players: players,
            Relations: ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            Stacks: ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
            Fortifications: fortifications,
            ResourceNodes: resourceNodes,
            Rods: rods,
            Bags: ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            Tombs: ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            Landmarks: landmarks,
            Mountains: ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            Sites: ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            Subraces: ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            ScenarioState: WorldScenarioState.Empty,
            AiMemories: ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty);
    }

    private static WorldState CreateState(
        WorldMap map,
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players,
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications)
    {
        return CreateState(
            map,
            players,
            fortifications,
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
    }

    private static IEngineContext CreateContext(GlobalVariables globals, params WorldLandmarkDefinition[] landmarks)
    {
        TestGameDefinitionProviderBuilder builder = new();

        foreach (WorldLandmarkDefinition landmark in landmarks)
        {
            builder.AddLandmark(landmark);
        }

        IGameDefinitionProvider definitions = builder.Build();

        return new EngineContext(
            new TestSeedRandomProvider(),
            definitions,
            new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition),
            GameScenarioProvider.Empty,
            globals);
    }

    [Test]
    public void ExpandsUpToBudgetTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "........",
            "........",
            "........",
            "........",
            "....U...",
            "........",
            "........",
            "........"
        ]);

        WorldPlayerId ownerId = new(1);
        WorldState state = CreateState(
            map,
            CreatePlayers((ownerId, WorldRace.Undead)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), ownerId, new WorldPosition(5, 5), 1)));
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 12 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "........",
            "........",
            "....U...",
            "...UUU..",
            "..UUUUU.",
            "...UUU..",
            "....U...",
            "........"
        ]));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "........",
            "....U...",
            "...UUU..",
            "..UUUUU.",
            ".UUUUUUU",
            "..UUUUU.",
            "...UUU..",
            "....U..."
        ]));
    }

    [Test]
    public void ExpandsAroundFootprintCenterTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 9);

        WorldPlayerId ownerId = new(1);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            ownerId,
            new WorldPosition(6, 6),
            UnitGrid<WorldUnit>.Empty);
        WorldState state = CreateState(map, CreatePlayers((ownerId, WorldRace.Undead)), CreateFortifications(capital));
        IEngineContext context = CreateContext(new GlobalVariables { CapitalTerrainTilesPerTurn = 4 });

        Assert.That(capital.Footprint, Is.EqualTo(new WorldFootprint(new WorldPosition(2, 2), 5, 5)));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".........",
            ".........",
            ".........",
            "....U....",
            "...U.U...",
            "....U....",
            ".........",
            ".........",
            "........."
        ]));
    }

    [Test]
    public void CityLevelTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..............",
            "..............",
            "..U.......U...",
            "..............",
            ".............."
        ]);

        WorldPlayerId ownerId = new(1);
        WorldState state = CreateState(
            map,
            CreatePlayers((ownerId, WorldRace.Undead)),
            CreateFortifications(
                CreateCity(new WorldFortificationId("small"), ownerId, new WorldPosition(3, 3), 1),
                CreateCity(new WorldFortificationId("large"), ownerId, new WorldPosition(11, 3), 2)));
        IEngineContext context = CreateContext(new GlobalVariables
        {
            CityTerrainTilesPerTurnLevel1 = 2,
            CityTerrainTilesPerTurnLevel2 = 4
        });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "..............",
            "..U.......U...",
            "..U......UUU..",
            "..U.......U...",
            ".............."
        ]));
    }

    [Test]
    public void BlocksAtMountainsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "........",
            "........",
            "..U..^..",
            ".....^..",
            ".....^..",
            "........"
        ]);

        WorldPlayerId ownerId = new(1);
        WorldState state = CreateState(
            map,
            CreatePlayers((ownerId, WorldRace.Undead)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), ownerId, new WorldPosition(3, 3), 1)));
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 6 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "........",
            "..U.....",
            ".UUU.^..",
            "..UU.^..",
            "..U..^..",
            "........"
        ]));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".UUU....",
            "UUUUU...",
            "UUUUU^..",
            "UUUUU^..",
            ".UUUU^..",
            ".UUU...."
        ]));
    }

    [Test]
    public void BlocksAtWaterTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "........",
            "........",
            "..U..~..",
            ".....~..",
            ".....~..",
            "........"
        ]);

        WorldPlayerId ownerId = new(1);
        WorldState state = CreateState(
            map,
            CreatePlayers((ownerId, WorldRace.Undead)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), ownerId, new WorldPosition(3, 3), 1)));
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 6 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".UUU....",
            "UUUUU...",
            "UUUUU~..",
            "UUUUU~..",
            ".UUUU~..",
            ".UUU...."
        ]));
    }

    [Test]
    public void SpendsBudgetOnForestTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".......",
            ".......",
            "..U+...",
            ".......",
            "......."
        ]);

        WorldPlayerId ownerId = new(1);
        WorldState state = CreateState(
            map,
            CreatePlayers((ownerId, WorldRace.Undead)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), ownerId, new WorldPosition(3, 3), 1)));
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 3 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(state.Map.GetCellAt(new WorldPosition(3, 2)).Terrain, Is.EqualTo(WorldMapTerrain.Undead));
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".......",
            "..U....",
            "..U+...",
            "..U....",
            "......."
        ]));
    }

    [Test]
    public void ConvertsEnemyTerrainTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^UHHHH",
            "^^^^^^^",
            "^^^^^^^"
        ]);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), undeadPlayerId, new WorldPosition(3, 3), 1)));
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 1 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "^^^^^^^",
            "^^^^^^^",
            "^^UUHHH",
            "^^^^^^^",
            "^^^^^^^"
        ]));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "^^^^^^^",
            "^^^^^^^",
            "^^UUUHH",
            "^^^^^^^",
            "^^^^^^^"
        ]));
    }

    [Test]
    public void SeparateBudgetsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..........",
            "..........",
            "..U....H..",
            "..........",
            ".........."
        ]);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            CreateFortifications(
                CreateCity(new WorldFortificationId("undead"), undeadPlayerId, new WorldPosition(3, 3), 1),
                CreateCity(new WorldFortificationId("human"), humanPlayerId, new WorldPosition(8, 3), 1)));
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 3 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "..........",
            "..U.......",
            "..UU...H..",
            "..U.......",
            ".........."
        ]));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, humanPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "..........",
            "..U....H..",
            "..UU...HH.",
            "..U....H..",
            ".........."
        ]));
    }

    [Test]
    public void ExpandsFromRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 9);

        WorldPlayerId ownerId = new(1);
        WorldState state = CreateState(
            map,
            CreatePlayers((ownerId, WorldRace.Undead)),
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            CreateRods(new WorldRod(new WorldRodId("rod"), ownerId, new WorldPosition(4, 4))),
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
        IEngineContext context = CreateContext(new GlobalVariables { RodTerrainTilesPerTurn = 2 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".........",
            ".........",
            ".........",
            "....U....",
            ".........",
            "....U....",
            ".........",
            ".........",
            "........."
        ]));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".........",
            ".........",
            ".........",
            "....U....",
            "...U.U...",
            "....U....",
            ".........",
            ".........",
            "........."
        ]));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".........",
            ".........",
            ".........",
            "....U....",
            "...U.U...",
            "....UU...",
            "....U....",
            ".........",
            "........."
        ]));
    }

    [Test]
    public void ExpandsFromCityAndRodTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".........",
            ".........",
            "..U......",
            ".........",
            "........."
        ]);

        WorldPlayerId ownerId = new(1);
        WorldState state = CreateState(
            map,
            CreatePlayers((ownerId, WorldRace.Undead)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), ownerId, new WorldPosition(3, 3), 1)),
            CreateRods(new WorldRod(new WorldRodId("rod"), ownerId, new WorldPosition(7, 2))),
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
        IEngineContext context = CreateContext(new GlobalVariables
        {
            CityTerrainTilesPerTurnLevel1 = 2,
            RodTerrainTilesPerTurn = 2
        });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".........",
            "..U....U.",
            "..U......",
            "..U....U.",
            "........."
        ]));
    }

    [Test]
    public void DoesNotExpandFromEnemyRodTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".......",
            ".......",
            "..U....",
            "......."
        ]);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            CreateRods(new WorldRod(new WorldRodId("rod"), undeadPlayerId, new WorldPosition(2, 2))),
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
        IEngineContext context = CreateContext(new GlobalVariables { RodTerrainTilesPerTurn = 4 });

        Assert.That(TestHelpers.SerializeMap(ExpansionCalculator.ExpandPlayerTerrain(state, humanPlayerId, context)), Is.EqualTo([
            ".......",
            ".......",
            "..U....",
            "......."
        ]));
    }

    [Test]
    public void NeutralDoesNotExpandTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 4);

        WorldPlayerId neutralPlayerId = new(1);
        WorldState state = CreateState(
            map,
            CreatePlayers((neutralPlayerId, WorldRace.Neutral)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), neutralPlayerId, new WorldPosition(3, 3), 1)));
        IEngineContext context = CreateContext(new GlobalVariables());

        Assert.That(TestHelpers.SerializeMap(ExpansionCalculator.ExpandPlayerTerrain(state, neutralPlayerId, context)), Is.EqualTo([
            ".......",
            ".......",
            ".......",
            "......."
        ]));
    }

    [Test]
    public void BlocksAtForeignRodTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "........",
            "........",
            "...U....",
            "........"
        ]);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), undeadPlayerId, new WorldPosition(4, 3), 1)),
            CreateRods(new WorldRod(new WorldRodId("rod"), humanPlayerId, new WorldPosition(3, 1))),
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 6 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "........",
            "....U...",
            "..UUU...",
            "..UUU..."
        ]));
    }

    [Test]
    public void ReachesUnroddedResourceNodeTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".......",
            ".......",
            "..U....",
            ".......",
            "......."
        ]);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), undeadPlayerId, new WorldPosition(3, 3), 1)),
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            CreateResourceNodes(
                new WorldResourceNode(new WorldResourceNodeId("node"), new WorldPosition(4, 2), WorldResourceType.Gold, AiPriority: 3)),
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 34 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "UUUUUUU",
            "UUUUUUU",
            "UUUUUUU",
            "UUUUUUU",
            "UUUUUUU"
        ]));
    }

    [Test]
    public void BlocksAroundRoddedResourceNodeTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".......",
            ".......",
            "..U....",
            ".......",
            "......."
        ]);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), undeadPlayerId, new WorldPosition(3, 3), 1)),
            CreateRods(new WorldRod(new WorldRodId("rod"), humanPlayerId, new WorldPosition(5, 2))),
            CreateResourceNodes(
                new WorldResourceNode(new WorldResourceNodeId("node"), new WorldPosition(4, 2), WorldResourceType.Gold, AiPriority: 3)),
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 34 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "UUUUUUU",
            "UUU...U",
            "UUU...U",
            "UUU...U",
            "UUUUUUU"
        ]));
    }

    [Test]
    public void ConvertsResourceNodeBlockTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".......",
            ".......",
            "..U....",
            ".......",
            "......."
        ]);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            CreateRods(new WorldRod(new WorldRodId("rod"), undeadPlayerId, new WorldPosition(2, 2))),
            CreateResourceNodes(
                new WorldResourceNode(new WorldResourceNodeId("node"), new WorldPosition(3, 2), WorldResourceType.Gold, AiPriority: 3)),
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
        IEngineContext context = CreateContext(new GlobalVariables { RodTerrainTilesPerTurn = 2 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".......",
            "..UUU..",
            "..UUU..",
            "..UUU..",
            "......."
        ]));
    }

    [Test]
    public void KeepsResourceNodeOutOfBudgetTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".......",
            ".......",
            "..U....",
            ".......",
            "......."
        ]);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            CreateRods(new WorldRod(new WorldRodId("rod"), undeadPlayerId, new WorldPosition(2, 2))),
            CreateResourceNodes(
                new WorldResourceNode(new WorldResourceNodeId("node"), new WorldPosition(2, 3), WorldResourceType.Gold, AiPriority: 3)),
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty);
        IEngineContext context = CreateContext(new GlobalVariables { RodTerrainTilesPerTurn = 1 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".......",
            "..U....",
            "..U....",
            ".......",
            "......."
        ]));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            ".......",
            "..U....",
            ".UUU...",
            ".UUU...",
            ".UUU..."
        ]));
    }

    [Test]
    public void BlocksAtForeignFortificationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 5);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            CreateFortifications(
                CreateCity(new WorldFortificationId("undead"), undeadPlayerId, new WorldPosition(2, 4), 1),
                CreateCity(new WorldFortificationId("human"), humanPlayerId, new WorldPosition(9, 4), 1)));
        IEngineContext context = CreateContext(new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 40 });

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, undeadPlayerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "UUUUUUUUUU",
            "UUUUUU....",
            "UUUUUU....",
            "U.UUUU....",
            "UUUUUU...."
        ]));
    }

    [Test]
    public void BlocksAtMountainLandmarkTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        WorldPlayerId ownerId = new(1);
        WorldLandmarkDefinitionId mountainId = new("mountain");
        WorldLandmarkDefinitionId ruinsId = new("ruins");
        WorldState state = CreateState(
            map,
            CreatePlayers((ownerId, WorldRace.Undead)),
            CreateFortifications(CreateCity(new WorldFortificationId("city"), ownerId, new WorldPosition(3, 3), 1)),
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            CreateLandmarks(
                new WorldLandmark(new WorldLandmarkId("peak"), mountainId, new WorldFootprint(new WorldPosition(3, 1), 1, 1)),
                new WorldLandmark(new WorldLandmarkId("rubble"), ruinsId, new WorldFootprint(new WorldPosition(1, 1), 1, 1))));
        IEngineContext context = CreateContext(
            new GlobalVariables { CityTerrainTilesPerTurnLevel1 = 24 },
            new WorldLandmarkDefinition(mountainId, new GlobalTextId("Name"), new GlobalTextId("Description"), WorldLandmarkType.Terrain, Mountain: true, Width: 1, Height: 1),
            new WorldLandmarkDefinition(ruinsId, new GlobalTextId("Name"), new GlobalTextId("Description"), WorldLandmarkType.Structure, Mountain: false, Width: 1, Height: 1));

        state = state with { Map = ExpansionCalculator.ExpandPlayerTerrain(state, ownerId, context) };
        Assert.That(TestHelpers.SerializeMap(state.Map), Is.EqualTo([
            "UUUUUU",
            "UUU.UU",
            "UU.UUU",
            "UUUUUU"
        ]));
    }

    [Test]
    public void ExpandsCapturedFortificationTerrainTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 5);

        WorldPlayerId undeadPlayerId = new(1);
        WorldPlayerId humanPlayerId = new(2);
        WorldCity city = CreateCity(new WorldFortificationId("city"), humanPlayerId, new WorldPosition(4, 3), 1);
        WorldState state = CreateState(
            map,
            CreatePlayers((undeadPlayerId, WorldRace.Undead), (humanPlayerId, WorldRace.Human)),
            CreateFortifications(city));
        IEngineContext context = CreateContext(new GlobalVariables());

        WorldMap expanded = ExpansionCalculator.ExpandFortificationTerrain(state, context, city, WorldMapTerrain.Undead, 8);

        Assert.That(TestHelpers.SerializeMap(expanded), Is.EqualTo([
            "...U...",
            "...U...",
            "..U.U..",
            "..UUU..",
            "...U..."
        ]));
    }
}
