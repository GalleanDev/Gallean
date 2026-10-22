namespace Gallean.Engine.Tests;

public sealed class WorldEngineResurrectUnitGarrisonTests
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

        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 0, 0, 1, [], null, false);
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
            new WorldResurrectUnitCommand(new WorldUnitGroup.Fortification(capital.Id), new WorldUnitId("soldier")),
            [
                new WorldUnitResurrectedEvent(
                    new WorldUnitGroup.Fortification(capital.Id),
                    new WorldUnitId("soldier"),
                    1,
                    new WorldResourceSet(50, 0, 0, 0, 0, 0))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
                }),
                Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with
                {
                    Garrison = new UnitGrid<WorldUnit>.Builder()
                        .Place(soldier with { Hitpoints = 1 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }
}
