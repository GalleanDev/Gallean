using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class PathMapCalculatorTests
{
    private static readonly WorldMovementCalculator.MovementProfile DefaultProfile =
        new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);

    private static WorldState CreateEmptyState(WorldMap map)
    {
        return new WorldState(
            map,
            new WorldTurnInfo(1, new WorldPlayerId(0)),
            WorldDifficulty.Average,
            ImmutableSortedDictionary<WorldPlayerId, WorldPlayer>.Empty,
            ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            WorldScenarioState.Empty,
            ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty);
    }

    private static ImmutableBitmap CreateFullFog(WorldMap map)
    {
        return ImmutableBitmap.Create(map.Width, map.Height, true);
    }

    private static string[] SerializePathMap(WorldMap map, WorldPath path)
    {
        WorldPosition start = path.Segments[0].Start;
        HashSet<WorldPosition> pathPositions = [start, .. path.Positions];
        WorldPosition end = path.Positions[^1];

        return SerializeMap(map, position =>
        {
            if (position == start)
            {
                return 'S';
            }
            else if (position == end)
            {
                return 'E';
            }
            else if (pathPositions.Contains(position))
            {
                return '*';
            }
            else
            {
                return null;
            }
        });
    }

    private static string[] SerializeMap(WorldMap map, Func<WorldPosition, char?> getOverlay)
    {
        List<string> rows = [];

        for (int y = 0; y < map.Height; y++)
        {
            List<char> cells = [];

            for (int x = 0; x < map.Width; x++)
            {
                WorldPosition position = new(x, y);
                char? overlay = getOverlay(position);
                cells.Add(overlay ?? GetTerrainChar(map.GetCellAt(position)));
            }

            rows.Add(string.Join("", cells));
        }

        return [.. rows];
    }

    private static char GetTerrainChar(WorldMapCell cell)
    {
        if (cell.Terrain == WorldMapTerrain.Water)
        {
            return '~';
        }

        return cell.Feature switch
        {
            WorldMapFeature.None => '.',
            WorldMapFeature.Forest => 'T',
            WorldMapFeature.Mountain => '^',
            WorldMapFeature.Road => '='
        };
    }

    [Test]
    public void StraightLinePathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(4, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S***E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(8));
    }

    [Test]
    public void VerticalLinePathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 5);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(0, 4), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S",
            "*",
            "*",
            "*",
            "E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(8));
    }

    [Test]
    public void DiagonalPathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 2), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S..",
            ".*.",
            "..E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void PathAroundMountainTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            "...",
            "..."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S^E",
            ".*.",
            "..."
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void PathAroundWaterTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".~.",
            "...",
            "..."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S~E",
            ".*.",
            "..."
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void MountainBlockedNoPathTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^^."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath? path = plan.GetPath(new WorldPosition(0, 0), new WorldPosition(3, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void SameStartAndEndTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath? path = plan.GetPath(new WorldPosition(1, 0), new WorldPosition(1, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited);

        Assert.That(path, Is.EqualTo(new WorldPath([])));
    }

    [Test]
    public void BlockedStartPositionTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath? path = plan.GetPath(new WorldPosition(0, 0), new WorldPosition(1, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void BlockedEndPositionTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^"
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath? path = plan.GetPath(new WorldPosition(0, 0), new WorldPosition(1, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void HiddenEndPositionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        ImmutableBitmap fogOfWar = TestHelpers.BuildFogOfWar([
            ".?"
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath? path = plan.GetPath(new WorldPosition(0, 0), new WorldPosition(1, 0), fogOfWar, DefaultProfile, WorldMovementLimit.Unlimited);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void ExploredCellsConstrainPathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);
        ImmutableBitmap fogOfWar = TestHelpers.BuildFogOfWar([
            "?.?",
            ".?."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 1), new WorldPosition(2, 1), fogOfWar, DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            ".*.",
            "S.E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void NoFogTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), fogOfWar: null, DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S*E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void PathCostWithForestTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S*E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(6));
    }

    [Test]
    public void PrefersRoadTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".===.",
            ".+++.",
            ".^^^."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 1), new WorldPosition(4, 1), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            ".***.",
            "STTTE",
            ".^^^."
        ]));
        Assert.That(path.Cost, Is.EqualTo(5));
    }

    [Test]
    public void AvoidsImpassableTerrainTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            ".^.",
            "..."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 2), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S^.",
            "*^.",
            ".*E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(6));
    }

    [Test]
    public void AvoidsResourceNodesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldResourceNode node1 = new(new WorldResourceNodeId("0"), new WorldPosition(1, 0), default, AiPriority: 3);
        WorldResourceNode node2 = new(new WorldResourceNodeId("1"), new WorldPosition(1, 1), default, AiPriority: 3);
        WorldState state = CreateEmptyState(map) with
        {
            ResourceNodes = ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty
                .Add(node1.Id, node1)
                .Add(node2.Id, node2)
        };

        WorldPlan plan = WorldPlan.For(state);
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S.E",
            "*.*",
            ".*."
        ]));
        Assert.That(path.Cost, Is.EqualTo(8));
    }

    [Test]
    public void LandmarkImpassableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldLandmark landmark = new(new WorldLandmarkId("0"), new WorldLandmarkDefinitionId("def"), new WorldFootprint(new WorldPosition(1, 0), 1, 1));
        WorldState state = CreateEmptyState(map) with
        {
            Landmarks = ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty
                .Add(landmark.Id, landmark)
        };

        WorldPlan plan = WorldPlan.For(state);
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S.E",
            ".*.",
            "..."
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void LandmarkBlocksEndPositionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldLandmark landmark = new(new WorldLandmarkId("0"), new WorldLandmarkDefinitionId("def"), new WorldFootprint(new WorldPosition(1, 0), 1, 1));
        WorldState state = CreateEmptyState(map) with
        {
            Landmarks = ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty
                .Add(landmark.Id, landmark)
        };

        WorldPlan plan = WorldPlan.For(state);
        WorldPath? path = plan.GetPath(new WorldPosition(0, 0), new WorldPosition(1, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void LandmarkBlocksPathTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 1);

        WorldLandmark landmark = new(new WorldLandmarkId("0"), new WorldLandmarkDefinitionId("def"), new WorldFootprint(new WorldPosition(1, 0), 2, 1));
        WorldState state = CreateEmptyState(map) with
        {
            Landmarks = ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty
                .Add(landmark.Id, landmark)
        };

        WorldPlan plan = WorldPlan.For(state);
        WorldPath? path = plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void PrefersStraightHorizontalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 1), new WorldPosition(3, 1), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "....",
            "S**E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(6));
    }

    [Test]
    public void PrefersStraightVerticalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 4);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(0, 3), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S.",
            "*.",
            "*.",
            "E."
        ]));
        Assert.That(path.Cost, Is.EqualTo(6));
    }

    [Test]
    public void DiagonalBetweenBlocksTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".^.",
            "^.^",
            ".^."
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(1, 1), new WorldPosition(0, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "E^.",
            "^S^",
            ".^."
        ]));
        Assert.That(path.Cost, Is.EqualTo(2));
    }

    [Test]
    public void DiagonalForestCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..",
            ".+"
        ]);

        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(1, 1), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S.",
            ".E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void WaterBonusCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".~."
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.Water, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);
        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), profile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S*E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void ForestBonusCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+."
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.Forest, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);
        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), profile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S*E"
        ]));
        Assert.That(path.Cost, Is.EqualTo(4));
    }

    [Test]
    public void WaterOnlyDeepCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "~~~~",
            "~~~~",
            "~~~."
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: true, IsLeaderDead: false, IsThief: false);
        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(3, 0), CreateFullFog(map), profile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "S**E",
            "~~~~",
            "~~~."
        ]));
        Assert.That(path.Cost, Is.EqualTo(6));
    }

    [Test]
    public void WaterOnlyCoastalCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "~~."
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: true, IsLeaderDead: false, IsThief: false);
        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 0), new WorldPosition(1, 0), CreateFullFog(map), profile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "SE."
        ]));
        Assert.That(path.Cost, Is.EqualTo(6));
    }

    [Test]
    public void WaterOnlyLandBlockedTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "~~."
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: true, IsLeaderDead: false, IsThief: false);
        WorldPlan plan = WorldPlan.For(CreateEmptyState(map));
        WorldPath? path = plan.GetPath(new WorldPosition(0, 0), new WorldPosition(2, 0), CreateFullFog(map), profile, WorldMovementLimit.Unlimited);

        Assert.That(path, Is.Null);
    }

    [Test]
    public void CityEntranceNoDetourTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "....",
            "....",
            "..+."
        ]);

        WorldCity city = new(
            new WorldFortificationId("city"),
            new ScenarioTextId("Name"),
            new WorldCityDefinitionId(WorldRace.Human, 1),
            new WorldPlayerId(0),
            TestHelpers.NeutralSubrace.Id,
            FortificationCalculator.CreateFootprint(new WorldPosition(2, 1), 1),
            UnitGrid<WorldUnit>.Empty,
            WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);
        WorldState state = CreateEmptyState(map) with
        {
            Fortifications = ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty.Add(city.Id, city)
        };

        WorldPlan plan = WorldPlan.For(state);
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(0, 2), new WorldPosition(2, 2), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            "....",
            "....",
            "S*E."
        ]));
        Assert.That(path.Cost, Is.EqualTo(6));
    }

    [Test]
    public void CityEntranceFreeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldCity city = new(
            new WorldFortificationId("city"),
            new ScenarioTextId("Name"),
            new WorldCityDefinitionId(WorldRace.Human, 1),
            new WorldPlayerId(0),
            TestHelpers.NeutralSubrace.Id,
            FortificationCalculator.CreateFootprint(new WorldPosition(2, 0), 1),
            UnitGrid<WorldUnit>.Empty,
            WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);
        WorldState state = CreateEmptyState(map) with
        {
            Fortifications = ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty.Add(city.Id, city)
        };

        WorldPlan plan = WorldPlan.For(state);
        WorldPath path = Assert.NotNull(plan.GetPath(new WorldPosition(1, 0), new WorldPosition(2, 0), CreateFullFog(map), DefaultProfile, WorldMovementLimit.Unlimited));

        Assert.That(SerializePathMap(map, path), Is.EqualTo([
            ".SE"
        ]));
        Assert.That(path.Cost, Is.Zero);
    }
}
