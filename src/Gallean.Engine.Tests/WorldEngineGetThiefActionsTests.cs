namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetThiefActionsTests
{
    private static UnitDefinition Warlord()
    {
        return TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 2) with
            {
                Negotiate = 20
            };
    }

    [Test]
    public void StackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = Warlord();
        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            new WorldResourceSet(50, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(3, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Potion"), potion.Id, Charges: null)])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, new WorldPosition(3, 2)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result(
        [
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.PoisonStack, 70, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Spy, 100, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealItem, 60, WorldCommandError.ThiefActionNotAllowed),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Assassinate, 30, WorldCommandError.ThiefActionNotAllowed),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.CounterfeitOrders, 40, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Duel, 100, Error: null)
        ])));
    }

    [Test]
    public void GuildmasterTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = Warlord();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, new WorldPosition(3, 2)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result(
        [
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.PoisonStack, 70, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Spy, 100, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealItem, 60, WorldCommandError.ThiefNothingToSteal),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Assassinate, 30, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.CounterfeitOrders, 40, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Duel, 100, Error: null)
        ])));
    }

    [Test]
    public void NoMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = Warlord();

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, new WorldPosition(3, 2)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result(
        [
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.PoisonStack, 70, WorldCommandError.InsufficientMovement),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Spy, 100, WorldCommandError.InsufficientMovement),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealItem, 60, WorldCommandError.ThiefNothingToSteal),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Assassinate, 30, WorldCommandError.InsufficientMovement),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.CounterfeitOrders, 40, WorldCommandError.InsufficientMovement),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Duel, 100, WorldCommandError.InsufficientMovement)
        ])));
    }

    [Test]
    public void CityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result(
        [
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Spy, 100, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.PoisonCity, 80, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealGold, 70, WorldCommandError.ThiefActionNotAllowed),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealSpell, 60, WorldCommandError.ThiefNothingToSteal),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Assassinate, 40, WorldCommandError.ThiefActionNotAllowed),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.CounterfeitOrders, 50, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Bribe, 100, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.RiotCity, 100, WorldCommandError.ThiefActionNotAllowed)
        ])));
    }

    [Test]
    public void NoTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, new WorldPosition(3, 2)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result([])));
    }

    [Test]
    public void AlreadySpiedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = Warlord();

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [new WorldPlayerId(2)],
            SpiedRuins = []
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, new WorldPosition(3, 2)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result(
        [
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.PoisonStack, 70, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Spy, 100, WorldCommandError.ThiefTargetAlreadyKnown),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealItem, 60, WorldCommandError.ThiefNothingToSteal),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Assassinate, 30, WorldCommandError.ThiefActionNotAllowed),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.CounterfeitOrders, 40, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Duel, 100, Error: null)
        ])));
    }

    [Test]
    public void RuinAlreadySpiedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            SpiedPlayers = [],
            SpiedRuins = [new WorldFortificationId("ruin")]
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldRuin ruin = new(
            new WorldFortificationId("ruin"),
            new ScenarioTextId("Ruin"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            new WorldObjectVariant(0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard),
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.SpyRuin, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithRuin(ruin)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, new WorldPosition(4, 4)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result(
        [
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.SpyRuin, 90, WorldCommandError.ThiefTargetAlreadyKnown)
        ])));
    }

    [Test]
    public void NeutralCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            ExperienceKilled = 50
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result(
        [
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Spy, 100, WorldCommandError.ThiefTargetAlreadyKnown),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.PoisonCity, 80, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealGold, 70, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealSpell, 60, WorldCommandError.ThiefNothingToSteal),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Assassinate, 40, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.CounterfeitOrders, 50, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Bribe, 100, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.RiotCity, 100, Error: null)
        ])));
    }

    [Test]
    public void InsufficientGoldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            ExperienceKilled = 50
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(149, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result(
        [
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Spy, 100, WorldCommandError.ThiefTargetAlreadyKnown),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.PoisonCity, 80, Error: null),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealGold, 70, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.StealSpell, 60, WorldCommandError.ThiefNothingToSteal),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Assassinate, 40, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.CounterfeitOrders, 50, WorldCommandError.ThiefTargetInvalid),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.Bribe, 100, WorldCommandError.InsufficientResources),
            new WorldGetThiefActionsCommand.ThiefAction(WorldThiefActionType.RiotCity, 100, Error: null)
        ])));
    }

    [Test]
    public void AlliedCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldRelation alliance = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .WithRelation(alliance)
            .Build();

        WorldGetThiefActionsCommand.Result result = engine.CheckCommand(
            new WorldGetThiefActionsCommand(thiefStack.Id, city.Entrance),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefActionsCommand.Result([])));
    }
}
