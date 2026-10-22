namespace Gallean.Engine.Tests;

public sealed class WorldEngineLockUnitTypeTests
{
    [Test]
    public void GarrisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 200, initiative: 50, damage: 20);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("squire"), squireDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, knightDefinition.Id, RequiredBuildingId: null))
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        engine.CheckCommand(
            new WorldLockUnitTypeCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("squire")),
            [
                new WorldUnitTypeLockedEvent(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("squire"))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("squire"), squireDefinition) with { IsTypeLocked = true }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));

        engine.CheckCommandError(
            new WorldLockUnitTypeCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("squire")),
            WorldCommandError.UnitTypeLockForbidden);
    }

    [Test]
    public void NoUpgradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 200, initiative: 50, damage: 20);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("knight"), knightDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(knightDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldLockUnitTypeCommand(new WorldUnitGroup.Fortification(city.Id), new WorldUnitId("knight")),
            WorldCommandError.UnitTypeLockForbidden);
    }

    [Test]
    public void StackOutsideCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 200, initiative: 50, damage: 20);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("squire"), squireDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, knightDefinition.Id, RequiredBuildingId: null))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldLockUnitTypeCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("squire")),
            WorldCommandError.StackNotInCity);
    }
}
