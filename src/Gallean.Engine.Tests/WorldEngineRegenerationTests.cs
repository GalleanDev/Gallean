namespace Gallean.Engine.Tests;

public sealed class WorldEngineRegenerationTests
{
    [TestCase(1, 65)]
    [TestCase(2, 70)]
    [TestCase(3, 75)]
    [TestCase(4, 80)]
    [TestCase(5, 85)]
    public void CityGarrisonHealsByLevelTest(int cityLevel, int healedHitpoints)
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };
        WorldCityDefinition cityDefinition = new(new WorldCityDefinitionId(WorldRace.Human, cityLevel), cityLevel, 4, 5 * (cityLevel + 1), WorldResourceSet.Empty, ProtectionModifierId: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = healedHitpoints }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with { Garrison = healedGarrison }));
    }

    [Test]
    public void CapitalGarrisonHealsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player1.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCapital(capital)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 90 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Fortifications[capital.Id], Is.EqualTo(capital with { Garrison = healedGarrison }));
    }

    [Test]
    public void StackHealsInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 65 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void StackHealsInFieldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 55 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void ModifierRaisesRateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        UnitModifierDefinition bannerModifier = TestHelpers.SimpleModifier("banner-of-health") with
        {
            RegenerationPercent = 10
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50, Modifiers = [UnitModifier.Permanent(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(bannerModifier)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 60, Modifiers = [UnitModifier.Permanent(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void ModifierRaisesCityRateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        UnitModifierDefinition bannerModifier = TestHelpers.SimpleModifier("banner-of-health") with
        {
            RegenerationPercent = 10
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50, Modifiers = [UnitModifier.Permanent(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(bannerModifier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 70, Modifiers = [UnitModifier.Permanent(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void BannerEquipmentHealsLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5,
                LeaderAbilities = LeaderAbility.BannerUse
            };

        UnitModifierDefinition bannerModifier = TestHelpers.SimpleModifier("banner-of-health") with
        {
            RegenerationPercent = 10
        };

        ItemDefinition banner = new(
            new ItemDefinitionId("banner-of-health"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Banner,
            WorldResourceSet.Empty,
            EquipmentModifierId: bannerModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([bannerItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(bannerModifier)
            .WithDefinition(banner)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEquipItemCommand(stack.Id, bannerItem.Id, LeaderEquipmentSlot.Banner));
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        WorldStack equippedStack = stack with
        {
            Inventory = WorldInventory.Empty,
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem),
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 65, Modifiers = [UnitModifier.FromEquipment(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build()
        };

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(equippedStack));
    }

    [Test]
    public void WarriorLordHealsStackInFieldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 70 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void WarriorLordHealsGarrisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithCity(city)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with { Garrison = healedGarrison }));
    }

    [Test]
    public void WarriorLordDoesNotHealOtherPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldStack warriorStack = TestHelpers.SimpleStack(
            new WorldStackId("warrior"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack mageStack = TestHelpers.SimpleStack(
            new WorldStackId("mage"),
            player2.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Mage))
            .WithPlayer(player2)
            .WithStack(warriorStack)
            .WithStack(mageStack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedWarriorUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 70 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[warriorStack.Id], Is.EqualTo(warriorStack with { Units = healedWarriorUnits }));
        Assert.That(engine.State.Stacks[mageStack.Id], Is.EqualTo(mageStack));
    }

    [Test]
    public void RuinGarrisonHealsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier) with { Hitpoints = 0 }, GridPosition.FrontTop, false)
                .Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        UnitGrid<WorldUnit> healedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 70 }, GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier) with { Hitpoints = 0 }, GridPosition.FrontTop, false)
            .Build();

        Assert.That(engine.State.Fortifications[ruin.Id], Is.EqualTo(ruin with { Garrison = healedGarrison }));
    }

    [Test]
    public void RuinGarrisonHealsOncePerRoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            Regeneration = 5
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Mage))
            .WithPlayer(player2)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        UnitGrid<WorldUnit> healedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 70 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.TurnInfo, Is.EqualTo(new WorldTurnInfo(2, new WorldPlayerId(1))));
        Assert.That(engine.State.Fortifications[ruin.Id], Is.EqualTo(ruin with { Garrison = healedGarrison }));
    }

    [Test]
    public void NativeTerrainHealsStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3, 'H');

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 65 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void ForeignTerrainTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3, 'U');

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 55 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void NativeTerrainWithWarriorLordTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3, 'H');

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void NativeTerrainInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 65 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void HealToMaxHitpointsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 98 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void HealRoundingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 10, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 5 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack));
    }

    [Test]
    public void NeutralCityHealsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map) with { IsAi = true };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("garrisonLeader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            neutral.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("stackLeader"), leader) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city));
        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack));

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        UnitGrid<WorldUnit> healedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("garrisonLeader"), leader) with { Hitpoints = 65 }, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("stackLeader"), leader) with { Hitpoints = 65 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Fortifications[city.Id], Is.EqualTo(city with { Garrison = healedGarrison }));
        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void HighestModifierRateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 5
            };

        UnitModifierDefinition lowerModifier = TestHelpers.SimpleModifier("lower-regeneration") with
        {
            RegenerationPercent = 10
        };

        UnitModifierDefinition higherModifier = TestHelpers.SimpleModifier("higher-regeneration") with
        {
            RegenerationPercent = 15
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(
                    TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50, Modifiers = [UnitModifier.Permanent(lowerModifier.Id), UnitModifier.Permanent(higherModifier.Id)] },
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(lowerModifier)
            .WithDefinition(higherModifier)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(
                TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 65, Modifiers = [UnitModifier.Permanent(lowerModifier.Id), UnitModifier.Permanent(higherModifier.Id)] },
                GridPosition.FrontBottom,
                false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }

    [Test]
    public void OwnRateAboveModifierTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 6) with
            {
                Regeneration = 20
            };

        UnitModifierDefinition bannerModifier = TestHelpers.SimpleModifier("banner-of-health") with
        {
            RegenerationPercent = 10
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 50, Modifiers = [UnitModifier.Permanent(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(bannerModifier)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Mage))
            .WithPlayer(player1)
            .WithStack(stack)
            .Build();

        engine.DispatchCommand(new WorldStartGameCommand());

        UnitGrid<WorldUnit> healedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 70, Modifiers = [UnitModifier.Permanent(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[stack.Id], Is.EqualTo(stack with { Units = healedUnits }));
    }
}
