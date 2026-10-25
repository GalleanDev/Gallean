namespace Gallean.Engine.Tests;

public sealed class WorldEngineFortificationProtectionTests
{
    [Test]
    public void EnterAndLeaveCityTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^...",
            "^^^...",
            "^^^..."
        ]);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitModifierDefinition protection = TestHelpers.SimpleModifier("protection") with { Armor = new UnitStatModifier(10, 0) };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty) with
        {
            ProtectionModifierId = protection.Id
        };
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), soldier));

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(protection)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        Assert.That(engine.State.Fortifications[city.Id].Garrison, Is.EqualTo(new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), soldier) with { Modifiers = [UnitModifier.FromFortification(protection.Id)] }, GridPosition.FrontBottom, false)
            .Build()));

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(3, 3)));

        Assert.That(engine.State.Stacks[stack.Id].Units, Is.EqualTo(new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Modifiers = [UnitModifier.FromFortification(protection.Id)] }, GridPosition.FrontBottom, false)
            .Build()));

        _ = engine.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(4, 4)));

        Assert.That(engine.State.Stacks[stack.Id].Units, Is.EqualTo(new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
            .Build()));
    }

    [Test]
    public void UpgradeCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);
        UnitModifierDefinition protection1 = TestHelpers.SimpleModifier("protection1") with { Armor = new UnitStatModifier(10, 0) };
        UnitModifierDefinition protection2 = TestHelpers.SimpleModifier("protection2") with { Armor = new UnitStatModifier(15, 0) };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldCityDefinition level1 = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), new WorldResourceSet(150, 0, 0, 0, 0, 0)) with
        {
            ProtectionModifierId = protection1.Id
        };
        WorldCityDefinition level2 = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 2), new WorldResourceSet(150, 0, 0, 0, 0, 0)) with
        {
            ProtectionModifierId = protection2.Id
        };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            level1,
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), soldier));

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(leader)
            .WithDefinition(protection1)
            .WithDefinition(protection2)
            .WithDefinition(level1)
            .WithDefinition(level2)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldUpgradeCityCommand(city.Id), [
            new WorldCityUpgradedEvent(city.Id, level2.Id, new WorldResourceSet(150, 0, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State.Fortifications[city.Id].Garrison, Is.EqualTo(new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), soldier) with { Modifiers = [UnitModifier.FromFortification(protection2.Id)] }, GridPosition.FrontBottom, false)
            .Build()));
        Assert.That(engine.State.Stacks[stack.Id].Units, Is.EqualTo(new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Modifiers = [UnitModifier.FromFortification(protection2.Id)] }, GridPosition.FrontBottom, false)
            .Build()));
    }

    [Test]
    public void PurchaseUnitInCapitalTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("unit"), initiative: 50, damage: 10, accuracy: 80, reach: UnitAttackReach.Adjacent)
        };
        UnitModifierDefinition protection = TestHelpers.SimpleModifier("protection") with { Armor = new UnitStatModifier(50, 0) };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Undead,
            NameTextId: new GlobalTextId("Undead"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Undead,
            IsPlayable: true,
            CapitalRegeneration: 35,
            CapitalProtectionModifierId: protection.Id,
            Cities: [],
            Leaders: [],
            Units: [unitDefinition.Id],
            MaleLeaderNames: [],
            FemaleLeaderNames: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(protection)
            .WithRace(raceDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldPurchaseUnitCommand(capital.Id, unitDefinition.Id, GridPosition.FrontBottom));

        Assert.That(engine.State.Fortifications[capital.Id].Garrison, Is.EqualTo(new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), unitDefinition) with { Modifiers = [UnitModifier.FromFortification(protection.Id)] }, GridPosition.FrontBottom, false)
            .Build()));
    }

    [Test]
    public void TransferBetweenStacksTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);
        UnitModifierDefinition protection = TestHelpers.SimpleModifier("protection") with { Armor = new UnitStatModifier(10, 0) };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty) with
        {
            ProtectionModifierId = protection.Id
        };
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack visitor = TestHelpers.SimpleStack(
            new WorldStackId("visitor"),
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader1"), leader), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 10);

        WorldStack outsider = TestHelpers.SimpleStack(
            new WorldStackId("outsider"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(protection)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(visitor)
            .WithStack(outsider)
            .WithCity(city)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(visitor.Id), new WorldUnitGroup.Stack(outsider.Id), new WorldUnitId("soldier"), GridPosition.FrontCenter),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(visitor.Id), new WorldUnitGroup.Stack(outsider.Id), new WorldUnitId("soldier"), GridPosition.FrontCenter)
            ]);

        Assert.That(engine.State.Stacks[outsider.Id].Units, Is.EqualTo(new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader), GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontCenter, false)
            .Build()));

        engine.CheckCommand(
            new WorldTransferUnitCommand(new WorldUnitGroup.Stack(outsider.Id), new WorldUnitGroup.Stack(visitor.Id), new WorldUnitId("soldier"), GridPosition.FrontCenter),
            [
                new WorldUnitTransferredEvent(new WorldUnitGroup.Stack(outsider.Id), new WorldUnitGroup.Stack(visitor.Id), new WorldUnitId("soldier"), GridPosition.FrontCenter)
            ]);

        Assert.That(engine.State.Stacks[visitor.Id].Units, Is.EqualTo(new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader1"), leader) with { Modifiers = [UnitModifier.FromFortification(protection.Id)] }, GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Modifiers = [UnitModifier.FromFortification(protection.Id)] }, GridPosition.FrontCenter, false)
            .Build()));
    }
}
