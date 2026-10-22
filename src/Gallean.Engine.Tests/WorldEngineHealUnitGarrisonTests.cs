namespace Gallean.Engine.Tests;

public sealed class WorldEngineHealUnitGarrisonTests
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

        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 30, 0, 1, [], null, false);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(soldier, GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        engine.CheckCommand(
            new WorldHealUnitCommand(new WorldUnitGroup.Fortification(capital.Id), new WorldUnitId("soldier")),
            [
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Fortification(capital.Id),
                    new WorldUnitId("soldier"),
                    100,
                    new WorldResourceSet(70, 0, 0, 0, 0, 0))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(30, 0, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Garrison = new UnitGrid<WorldUnit>.Builder()
                        .Place(soldier with { Hitpoints = 100 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void OtherPlayerCapitalTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

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

        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 30, 0, 1, [], null, false);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            other.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(soldier, GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldierDefinition)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithCapital(capital)
            .Build();

        engine.CheckCommandError(
            new WorldHealUnitCommand(new WorldUnitGroup.Fortification(capital.Id), new WorldUnitId("soldier")),
            WorldCommandError.GarrisonNotFound);
    }

    [Test]
    public void CityInfoTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

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
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id])
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(temple)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        WorldGetCityInfoCommand.Result result = engine.CheckCommand(
            new WorldGetCityInfoCommand(capital.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetCityInfoCommand.Result(Growth: null, HealError: null)));
    }
}
