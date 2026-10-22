namespace Gallean.Engine.Tests;

public sealed class WorldEngineHealUnitStackTests
{
    [Test]
    public void SuccessTest()
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
            HealCost = new WorldResourceSet(1, 0, 0, 0, 0, 0)
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

        engine.CheckCommand(
            new WorldHealUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier")),
            [
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 100, new WorldResourceSet(70, 0, 0, 0, 0, 0))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with { Resources = new WorldResourceSet(30, 0, 0, 0, 0, 0) }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void LeveledUnitCostTest()
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
            HealCost = new WorldResourceSet(1, 0, 0, 0, 0, 0)
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
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0),
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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30, Level = 3 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(temple)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                soldierDefinition.Id,
                UpgradeChangeLevel: 10,
                LowLevelUpgrade: UnitDynamicUpgrade.Empty with
                {
                    MaxHitpoints = 10,
                    HealCost = new WorldResourceSet(1, 0, 0, 0, 0, 0)
                },
                HighLevelUpgrade: UnitDynamicUpgrade.Empty))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldHealUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier")),
            [
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 120, new WorldResourceSet(270, 0, 0, 0, 0, 0))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with { Resources = new WorldResourceSet(30, 0, 0, 0, 0, 0) }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 120, Level = 3 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void StackNotInCityTest()
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
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

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
            new WorldPosition(5, 5),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 30 }, GridPosition.FrontBottom, false)
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

        engine.CheckCommandError(
            new WorldHealUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier")),
            WorldCommandError.StackNotInCity);
    }

    [Test]
    public void BuildingRequiredTest()
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
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
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
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldHealUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier")),
            WorldCommandError.HealBuildingRequired);
    }

    [Test]
    public void NotWoundedTest()
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
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
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

        engine.CheckCommandError(
            new WorldHealUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier")),
            WorldCommandError.UnitNotWounded);
    }

    [Test]
    public void DeadUnitTest()
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
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

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
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
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

        engine.CheckCommandError(
            new WorldHealUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier")),
            WorldCommandError.UnitDead);
    }

    [Test]
    public void InsufficientResourcesTest()
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
            HealCost = new WorldResourceSet(1, 0, 0, 0, 0, 0)
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
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0),
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

        engine.CheckCommandError(
            new WorldHealUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier")),
            WorldCommandError.InsufficientResources);
    }
}
