namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetThiefStealInfoTests
{
    private static ItemDefinition Item(string id)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), ItemCategory.PotionHeal) with
        {
            NameTextId = new GlobalTextId($"{id}-name"),
            DescriptionTextId = new GlobalTextId($"{id}-description"),
            Value = new WorldResourceSet(50, 0, 0, 0, 0, 0),
            Effect = new HealItemEffect(50)
        };
    }

    private static WorldSpellDefinition Spell(string id)
    {
        return TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId(id), new WorldFogSpellEffect()) with
        {
            NameTextId = new GlobalTextId($"{id}-name"),
            DescriptionTextId = new GlobalTextId($"{id}-description"),
            CastingCost = new WorldResourceSet(10, 0, 0, 0, 0, 0),
            PurchaseCost = new WorldResourceSet(200, 0, 0, 0, 0, 0)
        };
    }

    [Test]
    public void StackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 2);
        ItemDefinition potion = Item("potion");
        ItemDefinition elixir = Item("elixir");
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);
        WorldItem elixirItem = new(new WorldItemId("Elixir"), elixir.Id, Charges: null);

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
                Inventory = new WorldInventory([potionItem, elixirItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(potion)
            .WithDefinition(elixir)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        WorldGetThiefStealInfoCommand.Result result = engine.CheckCommand(
            new WorldGetThiefStealInfoCommand(thiefStack.Id, new WorldPosition(3, 2)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefStealInfoCommand.Result(
            [
                new WorldGetThiefStealInfoCommand.Item(new WorldThiefStealItemAction(new WorldPosition(3, 2), potion.Id), Error: null),
                new WorldGetThiefStealInfoCommand.Item(new WorldThiefStealItemAction(new WorldPosition(3, 2), elixir.Id), Error: null)
            ],
            [])));
    }

    [Test]
    public void MerchantTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        ItemDefinition potion = Item("potion");
        ItemDefinition elixir = Item("elixir");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(potion.Id, 2), new WorldMerchantItem(elixir.Id, 1)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(potion)
            .WithDefinition(elixir)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMerchant, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        WorldGetThiefStealInfoCommand.Result result = engine.CheckCommand(
            new WorldGetThiefStealInfoCommand(thiefStack.Id, new WorldPosition(4, 4)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefStealInfoCommand.Result(
            [
                new WorldGetThiefStealInfoCommand.Item(new WorldThiefStealItemAction(new WorldPosition(4, 4), potion.Id), Error: null),
                new WorldGetThiefStealInfoCommand.Item(new WorldThiefStealItemAction(new WorldPosition(4, 4), elixir.Id), Error: null)
            ],
            [])));
    }

    [Test]
    public void CityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldSpellDefinition fog = Spell("fog");
        WorldSpellDefinition storm = Spell("storm");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [fog.Id])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [fog.Id, storm.Id])
        };

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
            .WithDefinition(fog)
            .WithDefinition(storm)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 70))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldGetThiefStealInfoCommand.Result result = engine.CheckCommand(
            new WorldGetThiefStealInfoCommand(thiefStack.Id, new WorldPosition(3, 3)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefStealInfoCommand.Result(
            [],
            [
                new WorldGetThiefStealInfoCommand.Spell(new WorldThiefStealSpellAction(new WorldPosition(3, 3), fog.Id), WorldCommandError.SpellAlreadyKnown),
                new WorldGetThiefStealInfoCommand.Spell(new WorldThiefStealSpellAction(new WorldPosition(3, 3), storm.Id), Error: null)
            ])));
    }

    [Test]
    public void MageTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldSpellDefinition fog = Spell("fog");
        WorldSpellDefinition storm = Spell("storm");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [storm.Id])
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldMageSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [fog.Id, storm.Id],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(fog)
            .WithDefinition(storm)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMage, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        WorldGetThiefStealInfoCommand.Result result = engine.CheckCommand(
            new WorldGetThiefStealInfoCommand(thiefStack.Id, new WorldPosition(4, 4)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefStealInfoCommand.Result(
            [],
            [
                new WorldGetThiefStealInfoCommand.Spell(new WorldThiefStealSpellAction(new WorldPosition(4, 4), fog.Id), Error: null),
                new WorldGetThiefStealInfoCommand.Spell(new WorldThiefStealSpellAction(new WorldPosition(4, 4), storm.Id), WorldCommandError.SpellAlreadyKnown)
            ])));
    }

    [Test]
    public void NoMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldSpellDefinition fog = Spell("fog");
        WorldSpellDefinition storm = Spell("storm");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [storm.Id])
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        WorldMageSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [fog.Id, storm.Id],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(fog)
            .WithDefinition(storm)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMage, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        WorldGetThiefStealInfoCommand.Result result = engine.CheckCommand(
            new WorldGetThiefStealInfoCommand(thiefStack.Id, new WorldPosition(4, 4)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefStealInfoCommand.Result(
            [],
            [
                new WorldGetThiefStealInfoCommand.Spell(new WorldThiefStealSpellAction(new WorldPosition(4, 4), fog.Id), WorldCommandError.InsufficientMovement),
                new WorldGetThiefStealInfoCommand.Spell(new WorldThiefStealSpellAction(new WorldPosition(4, 4), storm.Id), WorldCommandError.InsufficientMovement)
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

        WorldGetThiefStealInfoCommand.Result result = engine.CheckCommand(
            new WorldGetThiefStealInfoCommand(thiefStack.Id, new WorldPosition(3, 2)),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetThiefStealInfoCommand.Result([], [])));
    }
}
