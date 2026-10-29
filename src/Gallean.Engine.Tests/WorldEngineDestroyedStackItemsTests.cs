using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineDestroyedStackItemsTests
{
    private static ItemDefinition Item(string id, ItemCategory category)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), category);
    }

    [Test]
    public void StackBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition carried = Item("carried", ItemCategory.Valuable);
        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        ItemDefinition banner = Item("banner", ItemCategory.Banner);
        ItemDefinition talisman = Item("talisman", ItemCategory.Talisman);
        ItemDefinition potion = Item("potion", ItemCategory.PotionHeal);

        WorldItem carriedItem = new(new WorldItemId("Carried"), carried.Id, Charges: null);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);
        WorldItem talismanItem = new(new WorldItemId("Talisman"), talisman.Id, Charges: 5);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([carriedItem])
            };

        WorldStack stack2 = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player2.Id,
                new WorldPosition(1, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([valuableItem]),
                Equipment = LeaderEquipment.Empty
                    .With(LeaderEquipmentSlot.Banner, bannerItem)
                    .With(LeaderEquipmentSlot.Battle1, talismanItem)
                    .With(LeaderEquipmentSlot.Battle2, potionItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(carried)
            .WithDefinition(valuable)
            .WithDefinition(banner)
            .WithDefinition(talisman)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items:
            [
                new BattleItem(BattleSide.Defender, LeaderEquipmentSlot.Battle1, talisman.Id, Charges: 5, Used: true),
                new BattleItem(BattleSide.Defender, LeaderEquipmentSlot.Battle2, potion.Id, Charges: null, Used: true)
            ],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), new GridPosition(0, 0), false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldItemUsedEvent(stack2.Id, talismanItem, RemainingCharges: 4, Consumed: false),
            new WorldItemUsedEvent(stack2.Id, potionItem, RemainingCharges: null, Consumed: true),
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with
                    {
                        Units = expectedAttackerGrid,
                        Inventory = new WorldInventory([
                            carriedItem,
                            valuableItem,
                            bannerItem,
                            new WorldItem(new WorldItemId("Talisman"), talisman.Id, Charges: 4)
                        ]),
                        BattlesWon = 1
                    })
                    .Remove(stack2.Id),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void NoItemsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), new GridPosition(0, 0), false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void ExistingBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("attack"), new WorldAttackSpellEffect(UnitAttackSource.Fire, 125));

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        ItemDefinition lying = Item("lying", ItemCategory.Valuable);

        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);
        WorldItem lyingItem = new(new WorldItemId("Lying"), lying.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack theirs = TestHelpers.SimpleStack(
                new WorldStackId("Theirs"),
                enemy.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([valuableItem])
            };

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(0, 0),
            new WorldObjectVariant(2),
            new WorldInventory([lyingItem]),
            AiPriority: 5);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(valuable)
            .WithDefinition(lying)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .WithBag(bag)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(spell.Id, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    spell.Id,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("leader"), 0, player.Id),
                new WorldStackDestroyedEvent(theirs.Id, Destroyer: null, player.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(
                    bag.Id,
                    new WorldBag(
                        new WorldBagId("Bag1"),
                        new WorldPosition(0, 0),
                        new WorldObjectVariant(2),
                        new WorldInventory([lyingItem, valuableItem]),
                        AiPriority: 5))
            }));
    }

    [Test]
    public void InsideCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("attack"), new WorldAttackSpellEffect(UnitAttackSource.Fire, 125)) with
        {
            AreaRadius = null
        };

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        ItemDefinition banner = Item("banner", ItemCategory.Banner);
        ItemDefinition stored = Item("stored", ItemCategory.Valuable);

        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);
        WorldItem storedItem = new(new WorldItemId("Stored"), stored.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("City1"),
                cityDefinition,
                enemy.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([storedItem])
            };

        WorldStack theirs = TestHelpers.SimpleStack(
                new WorldStackId("Theirs"),
                enemy.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([valuableItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(cityDefinition)
            .WithDefinition(valuable)
            .WithDefinition(banner)
            .WithDefinition(stored)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .WithCity(city)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(spell.Id, Target: null),
            [
                new WorldSpellCastEvent(player.Id, spell.Id, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("leader"), 0, player.Id),
                new WorldStackDestroyedEvent(theirs.Id, Destroyer: null, player.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Inventory = new WorldInventory([storedItem, valuableItem, bannerItem])
                }),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void FortificationBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([valuableItem])
            };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), new GridPosition(0, 0), false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(valuable)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithCity(city)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleUnit deadAttacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(deadAttacker, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: new BattleModifiers(
                Attacker: BattleSideModifiers.None,
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false)));

        UnitGrid<WorldUnit> expectedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), new GridPosition(0, 0), false)
            .Build();

        UnitGrid<WorldUnit> destroyedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(stack1.Id, city.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(stack1.Id, city.Id, BattleSide.Defender, destroyedAttackerGrid, expectedDefenderGrid),
            new WorldStackDestroyedEvent(stack1.Id, new WorldUnitGroup.Fortification(city.Id), city.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = expectedDefenderGrid,
                    Inventory = new WorldInventory([valuableItem])
                }),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void AttackSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("attack"), new WorldAttackSpellEffect(UnitAttackSource.Fire, 125));

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack theirs = TestHelpers.SimpleStack(
                new WorldStackId("Theirs"),
                enemy.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([valuableItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(valuable)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(spell.Id, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    spell.Id,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("leader"), 0, player.Id),
                new WorldStackDestroyedEvent(theirs.Id, Destroyer: null, player.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(
                    new WorldBagId("Bag0001"),
                    new WorldBag(
                        new WorldBagId("Bag0001"),
                        new WorldPosition(0, 0),
                        new WorldObjectVariant(0),
                        new WorldInventory([valuableItem]),
                        AiPriority: 3))
            }));
    }

    [Test]
    public void SummonLifetimeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition summon = TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = 1
        };

        ItemDefinition first = Item("first", ItemCategory.Valuable);
        ItemDefinition second = Item("second", ItemCategory.Valuable);
        ItemDefinition lying = Item("lying", ItemCategory.Valuable);

        WorldItem firstItem = new(new WorldItemId("First"), first.Id, Charges: null);
        WorldItem secondItem = new(new WorldItemId("Second"), second.Id, Charges: null);
        WorldItem lyingItem = new(new WorldItemId("Lying"), lying.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack firstStack = TestHelpers.SimpleStack(
                new WorldStackId("FirstSummon"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("summon"), summon),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([firstItem]),
                ExpirationTurn = 1
            };

        WorldStack secondStack = TestHelpers.SimpleStack(
                new WorldStackId("SecondSummon"),
                player.Id,
                new WorldPosition(2, 2),
                TestHelpers.SingleUnitGrid(new WorldUnitId("summon2"), summon),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([secondItem]),
                ExpirationTurn = 1
            };

        WorldBag bag = new(
            new WorldBagId("Bag0001"),
            new WorldPosition(2, 0),
            new WorldObjectVariant(0),
            new WorldInventory([lyingItem]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(first)
            .WithDefinition(second)
            .WithDefinition(lying)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(firstStack)
            .WithStack(secondStack)
            .WithBag(bag)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(firstStack.Id, 20),
            new WorldStackMovementRestoredEvent(secondStack.Id, 20),
            new WorldStackDestroyedEvent(firstStack.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldStackDestroyedEvent(secondStack.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
                    .Add(bag.Id, bag)
                    .Add(
                        new WorldBagId("Bag0002"),
                        new WorldBag(
                            new WorldBagId("Bag0002"),
                            new WorldPosition(0, 0),
                            new WorldObjectVariant(0),
                            new WorldInventory([firstItem]),
                            AiPriority: 3))
                    .Add(
                        new WorldBagId("Bag0003"),
                        new WorldBag(
                            new WorldBagId("Bag0003"),
                            new WorldPosition(2, 2),
                            new WorldObjectVariant(0),
                            new WorldInventory([secondItem]),
                            AiPriority: 3))
            }));
    }

    [Test]
    public void RemoveStackEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([valuableItem])
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(stack.Id), FirstOnly: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(valuable)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldStackRemovedEvent(stack.Id),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(
                    new WorldBagId("Bag0001"),
                    new WorldBag(
                        new WorldBagId("Bag0001"),
                        new WorldPosition(0, 0),
                        new WorldObjectVariant(0),
                        new WorldInventory([valuableItem]),
                        AiPriority: 3)),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    DestroyedStacks = [new ScenarioDestroyedStack(stack.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void FailedThiefActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
                new WorldStackId("thief"),
                player1.Id,
                new WorldPosition(2, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([valuableItem])
            };

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldThiefActionDefinition poisonStackAction = TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(83, 99))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(valuable)
            .WithDefinition(poisonStackAction)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefPoisonAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionFailedEvent(thiefStack.Id, WorldThiefActionType.PoisonStack, player2.Id),
                new WorldStackDestroyedEvent(thiefStack.Id, null, null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(targetStack.Id, targetStack),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(
                    new WorldBagId("Bag0001"),
                    new WorldBag(
                        new WorldBagId("Bag0001"),
                        new WorldPosition(2, 2),
                        new WorldObjectVariant(0),
                        new WorldInventory([valuableItem]),
                        AiPriority: 3))
            }));
    }

    [Test]
    public void PlayerDefeatedTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^...."
        ]);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 2, leadership: 6);

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);

        WorldStack attackerStack = TestHelpers.SimpleStack(
            new WorldStackId("attacker"),
            attacker.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 2);
        WorldStack defenderStack = TestHelpers.SimpleStack(
                new WorldStackId("defender"),
                defender.Id,
                new WorldPosition(7, 4),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
                movementPoints: 2) with
            {
                Inventory = new WorldInventory([valuableItem])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(valuable)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .WithCapital(capital)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(new WorldMoveStackCommand(attackerStack.Id, capital.Entrance), [
            new WorldStackMovingEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null),
            new WorldFortificationEnteredEvent(new WorldStackId("attacker"), new WorldFortificationId("capital")),
            new WorldFortificationCapturingEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldPlayerDefeatedEvent(defender.Id),
            new WorldStackDestroyedEvent(defenderStack.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldScenarioEndedEvent(attacker.Id),
            new WorldFortificationCapturedEvent(capital.Id, defender.Id, attacker.Id, attackerStack.Id),
            new WorldStackMovedEvent(attackerStack.Id, new WorldPosition(5, 4), capital.Entrance, WorldStackDirection.NorthWest, 2, [], null)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(attackerStack.Id, attackerStack with
                {
                    Position = capital.Entrance,
                    Direction = WorldStackDirection.NorthWest,
                    MovementPoints = 2
                })
                .Remove(defenderStack.Id),
            Fortifications = engine.State.Fortifications.SetItem(capital.Id, capital with { CapturedById = attacker.Id }),
            Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(
                new WorldBagId("Bag0001"),
                new WorldBag(
                    new WorldBagId("Bag0001"),
                    new WorldPosition(7, 4),
                    new WorldObjectVariant(0),
                    new WorldInventory([valuableItem]),
                    AiPriority: 3))
        }));
    }

    [Test]
    public void StackBattleInsideCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        ItemDefinition banner = Item("banner", ItemCategory.Banner);
        ItemDefinition stored = Item("stored", ItemCategory.Valuable);

        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);
        WorldItem storedItem = new(new WorldItemId("Stored"), stored.Id, Charges: null);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("City1"),
                cityDefinition,
                player2.Id,
                new WorldPosition(3, 3),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([storedItem])
            };

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player2.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([valuableItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(valuable)
            .WithDefinition(banner)
            .WithDefinition(stored)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithCity(city)
            .Build();

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 10);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, new GridPosition(0, 0), false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: new BattleModifiers(
                Attacker: BattleSideModifiers.None,
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false)));

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), new GridPosition(0, 0), false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with
                    {
                        Units = expectedAttackerGrid,
                        Inventory = new WorldInventory([valuableItem, bannerItem]),
                        BattlesWon = 1
                    })
                    .Remove(stack2.Id),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void StackBattleDefenderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition dummyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        ItemDefinition banner = Item("banner", ItemCategory.Banner);

        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([valuableItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem)
            };

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy2"), dummyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(dummyDefinition)
            .WithDefinition(valuable)
            .WithDefinition(banner)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit deadAttacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Attacker, hitpoints: 0);

        BattleUnit survivingUnit = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("dummy"), dummyDefinition.Id, BattleSide.Defender, hitpoints: 10);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(deadAttacker, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(survivingUnit, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> destroyedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> expectedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dummy"), dummyDefinition), GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Defender, destroyedAttackerGrid, expectedDefenderGrid),
            new WorldStackDestroyedEvent(stack1.Id, new WorldUnitGroup.Stack(stack2.Id), stack2.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .Remove(stack1.Id)
                    .SetItem(stack2.Id, stack2 with
                    {
                        Units = expectedDefenderGrid,
                        Inventory = new WorldInventory([valuableItem, bannerItem]),
                        BattlesWon = 1
                    }),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void RuinBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(5, 5),
                TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([valuableItem])
            };

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("unit2"), unitDefinition),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(valuable)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10) with { Experience = 3, ExperienceGained = 4 }, new GridPosition(0, 0))
                .Build(new BattleModifiers(
                    Attacker: BattleSideModifiers.None,
                    Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false))) with
            {
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 7 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(stack.Id, ruin.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(stack.Id, ruin.Id, BattleSide.Defender, UnitGrid<WorldUnit>.Empty, expectedDefenderUnits),
            new WorldStackDestroyedEvent(stack.Id, Destroyer: null, DestroyerPlayerId: null)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Fortifications = engine.State.Fortifications.SetItem(ruin.Id, ruin with { Garrison = expectedDefenderUnits }),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(
                    new WorldBagId("Bag0001"),
                    new WorldBag(
                        new WorldBagId("Bag0001"),
                        new WorldPosition(5, 5),
                        new WorldObjectVariant(0),
                        new WorldInventory([valuableItem]),
                        AiPriority: 3))
            }));
    }

    [Test]
    public void AssassinateTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemyLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 5);

        ItemDefinition carried = Item("carried", ItemCategory.Valuable);
        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        ItemDefinition banner = Item("banner", ItemCategory.Banner);

        WorldItem carriedItem = new(new WorldItemId("Carried"), carried.Id, Charges: null);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);
        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
                new WorldStackId("thief"),
                player1.Id,
                new WorldPosition(2, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([carriedItem])
            };

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(3, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemyLeader"), enemyLeader), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([valuableItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(enemyLeader)
            .WithDefinition(carried)
            .WithDefinition(valuable)
            .WithDefinition(banner)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 70))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Guildmaster))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefAssassinateAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Assassinate, player2.Id),
                new WorldStackDestroyedEvent(targetStack.Id, new WorldUnitGroup.Stack(thiefStack.Id), thiefStack.OwnerId),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .Remove(targetStack.Id)
                    .SetItem(thiefStack.Id, thiefStack with
                    {
                        Inventory = new WorldInventory([carriedItem, valuableItem, bannerItem]),
                        MovementPoints = 10
                    }),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void BribeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6, 'H');

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            ExperienceKilled = 50
        };
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 40, damage: 5) with
            {
                ExperienceKilled = 200
            },
            movementPoints: 6, leadership: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);
        ItemDefinition stored = Item("stored", ItemCategory.Valuable);

        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);
        WorldItem storedItem = new(new WorldItemId("Stored"), stored.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player.Id,
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
                    .Build()) with
            {
                Inventory = new WorldInventory([storedItem])
            };

        WorldStack occupant = TestHelpers.SimpleStack(
                new WorldStackId("occupant"),
                neutral.Id,
                city.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Inventory = new WorldInventory([valuableItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0, 2))
            .WithDefinition(thief)
            .WithDefinition(guard)
            .WithDefinition(defender)
            .WithDefinition(cityDefinition)
            .WithDefinition(valuable)
            .WithDefinition(stored)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(thiefStack)
            .WithStack(occupant)
            .WithCity(city)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefBribeAction(new WorldPosition(2, 2))),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.Bribe, neutral.Id),
                new WorldStackDestroyedEvent(occupant.Id, new WorldUnitGroup.Stack(thiefStack.Id), thiefStack.OwnerId),
                new WorldCityBribedEvent(thiefStack.Id, city.Id, new WorldResourceSet(750, 0, 0, 0, 0, 0)),
                new WorldFortificationCapturingEvent(city.Id, neutral.Id, player.Id, thiefStack.Id),
                new WorldFortificationCapturedEvent(city.Id, neutral.Id, player.Id, thiefStack.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .Remove(occupant.Id)
                    .SetItem(thiefStack.Id, thiefStack with
                    {
                        Inventory = new WorldInventory([valuableItem]),
                        MovementPoints = 10
                    }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    OwnerId = player.Id,
                    Garrison = UnitGrid<WorldUnit>.Empty
                }),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }

    [Test]
    public void IncorruptibleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 2) with
            {
                LeaderAbilities = LeaderAbility.Incorruptible
            };

        ItemDefinition carried = Item("carried", ItemCategory.Valuable);
        ItemDefinition valuable = Item("valuable", ItemCategory.Valuable);

        WorldItem carriedItem = new(new WorldItemId("Carried"), carried.Id, Charges: null);
        WorldItem valuableItem = new(new WorldItemId("Valuable"), valuable.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
                new WorldStackId("thief"),
                player1.Id,
                new WorldPosition(2, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([valuableItem])
            };

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(3, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([carriedItem])
            };

        WorldThiefActionDefinition poisonStackAction = TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(carried)
            .WithDefinition(valuable)
            .WithDefinition(poisonStackAction)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefPoisonAction(new WorldPosition(3, 2))),
            [
                new WorldThiefActionFailedEvent(thiefStack.Id, WorldThiefActionType.PoisonStack, player2.Id),
                new WorldStackDestroyedEvent(thiefStack.Id, new WorldUnitGroup.Stack(targetStack.Id), targetStack.OwnerId)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(targetStack.Id, targetStack with
                {
                    Inventory = new WorldInventory([carriedItem, valuableItem])
                }),
                Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty
            }));
    }
}
