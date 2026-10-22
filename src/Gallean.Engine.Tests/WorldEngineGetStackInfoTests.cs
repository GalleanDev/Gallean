namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetStackInfoTests
{
    [Test]
    public void UnitsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            HealCost = new WorldResourceSet(1, 0, 0, 0, 0, 0),
            ReviveCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldPlayerBuildingDefinition temple = new(
            Id: new WorldPlayerBuildingDefinitionId("temple"),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id]),
            Spellbook = new WorldPlayerSpellbook([], [])
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        WorldGetStackInfoCommand.Result result = engine.CheckCommand(
            new WorldGetStackInfoCommand(stack.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetStackInfoCommand.Result(
            [
                new WorldUnitActionInfo(
                    new WorldUnitId("soldier"),
                    HealError: null,
                    ResurrectError: WorldCommandError.UnitNotDead,
                    DisbandError: null,
                    LockTypeError: WorldCommandError.UnitTypeLockForbidden),
                new WorldUnitActionInfo(
                    new WorldUnitId("soldier2"),
                    HealError: WorldCommandError.UnitDead,
                    ResurrectError: null,
                    DisbandError: null,
                    LockTypeError: WorldCommandError.UnitTypeLockForbidden),
                new WorldUnitActionInfo(
                    new WorldUnitId("leader"),
                    HealError: WorldCommandError.UnitNotWounded,
                    ResurrectError: WorldCommandError.UnitNotDead,
                    DisbandError: WorldCommandError.LeaderDisbandForbidden,
                    LockTypeError: WorldCommandError.UnitTypeLockForbidden)
            ],
            OccupiedLeadership: 3,
            TotalLeadership: 3)));
    }

    [Test]
    public void OtherPlayerStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            otherPlayer.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithPlayer(otherPlayer)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(new WorldGetStackInfoCommand(stack.Id), WorldCommandError.StackNotFound);
    }
}
