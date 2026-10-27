using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineTombTests
{
    [Test]
    public void StackBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker) with { Name = "Morgana" }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender) with { Name = "Aldric" }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 10) with { Name = "Morgana" }, GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defender.Id, BattleSide.Defender, hitpoints: 0) with { Name = "Aldric" }, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker) with { Name = "Morgana" }, GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender) with { Hitpoints = 0, Name = "Aldric" }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), player1.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(
                    new WorldTombId("Tomb0001"),
                    new WorldTomb(
                        new WorldTombId("Tomb0001"),
                        new WorldPosition(1, 0),
                        player2.Id, defender.Id, "Aldric", player1.Id, attacker.Id, "Morgana", Turn: 1))
            }));
    }

    [Test]
    public void SecondTombTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("attacker"), attacker),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("defender"), defender),
            movementPoints: 6);

        WorldTomb tomb = new(new WorldTombId("Tomb0001"), new WorldPosition(1, 0), player1.Id, attacker.Id, "Morwen", player2.Id, defender.Id, DestroyerLeaderName: null, Turn: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(4, player1.Id))
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithTomb(tomb)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 10), GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defender.Id, BattleSide.Defender, hitpoints: 0), GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), player1.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty
                    .Add(tomb.Id, tomb)
                    .Add(
                        new WorldTombId("Tomb0002"),
                        new WorldTomb(
                            new WorldTombId("Tomb0002"),
                            new WorldPosition(1, 0),
                            player2.Id,
                            defender.Id,
                            LeaderName: null,
                            player1.Id,
                            attacker.Id,
                            DestroyerLeaderName: null,
                            Turn: 4))
            }));
    }

    [Test]
    public void CityBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("attacker"), attacker),
            movementPoints: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender), new GridPosition(0, 0), false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack)
            .WithCity(city)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 0), GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("defender"), defender.Id, BattleSide.Defender, hitpoints: 10), new GridPosition(0, 0), false)
                .Build(),
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
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender), new GridPosition(0, 0), false)
            .Build();

        UnitGrid<WorldUnit> destroyedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(stack.Id, city.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(stack.Id, city.Id, BattleSide.Defender, destroyedAttackerGrid, expectedDefenderGrid),
            new WorldStackDestroyedEvent(stack.Id, new WorldUnitGroup.Fortification(city.Id), player2.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with { Garrison = expectedDefenderGrid }),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(
                    new WorldTombId("Tomb0001"),
                    new WorldTomb(
                        new WorldTombId("Tomb0001"),
                        new WorldPosition(4, 4),
                        player1.Id, attacker.Id, LeaderName: null, player2.Id, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 1))
            }));
    }

    [Test]
    public void RuinBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 10, leadership: 6);
        UnitDefinition guard = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("attacker"), attacker),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard),
            Resources: WorldResourceSet.Empty,
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 0), GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("guard"), guard.Id, BattleSide.Defender, hitpoints: 10), GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Defender),
            Modifiers: new BattleModifiers(
                Attacker: BattleSideModifiers.None,
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false)));

        UnitGrid<WorldUnit> expectedGuardGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(stack.Id, ruin.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(stack.Id, ruin.Id, BattleSide.Defender, destroyedAttackerGrid, expectedGuardGrid),
            new WorldStackDestroyedEvent(stack.Id, Destroyer: null, DestroyerPlayerId: null)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Fortifications = engine.State.Fortifications.SetItem(ruin.Id, ruin with { Garrison = expectedGuardGrid }),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(
                    new WorldTombId("Tomb0001"),
                    new WorldTomb(
                        new WorldTombId("Tomb0001"),
                        new WorldPosition(5, 5),
                        player.Id, attacker.Id, LeaderName: null, DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 1))
            }));
    }

    [Test]
    public void AttackSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldSpellDefinition spell = new(
            new WorldSpellDefinitionId("attack"),
            new GlobalTextId("name"),
            new GlobalTextId("description"),
            new GlobalTextId("effect"),
            WorldRace.Human,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldAttackSpellEffect(UnitAttackSource.Fire, 125),
            AiType: WorldAiSpellType.All);

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
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
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
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(
                    new WorldTombId("Tomb0001"),
                    new WorldTomb(
                        new WorldTombId("Tomb0001"),
                        new WorldPosition(0, 0),
                        enemy.Id, leader.Id, LeaderName: null, player.Id, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 1))
            }));
    }

    [Test]
    public void InsideCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("attacker"), attacker),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("defender"), defender),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .WithCity(city)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 10), GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defender.Id, BattleSide.Defender, hitpoints: 0), GridPosition.FrontBottom, false)
                .Build(),
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
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), player1.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty
            }));
    }

    [Test]
    public void ForestTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+"
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("attacker"), attacker),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("defender"), defender),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 10), GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defender.Id, BattleSide.Defender, hitpoints: 0), GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), player1.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty
            }));
    }

    [Test]
    public void WaterTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".~"
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("attacker"), attacker),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("defender"), defender),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 10), GridPosition.FrontBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defender.Id, BattleSide.Defender, hitpoints: 0), GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> expectedAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
            .Build();

        UnitGrid<WorldUnit> destroyedDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, expectedAttackerGrid, destroyedDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), player1.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(stack1.Id, stack1 with { Units = expectedAttackerGrid, BattlesWon = 1 })
                    .Remove(stack2.Id),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty
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

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Summon"),
                player.Id,
                new WorldPosition(1, 1),
                TestHelpers.SingleUnitGrid(new WorldUnitId("summon"), summon),
                movementPoints: 20) with
            {
                ExpirationTurn = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 20),
            new WorldStackDestroyedEvent(stack.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty
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

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thief),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("warlord"), warlord),
            movementPoints: 10);

        WorldThiefActionDefinition poisonStackAction = new(WorldThiefActionType.PoisonStack, 90, new GlobalTextId("poison-stack"), new GlobalTextId("poison-stack-description"));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(83, 99))
            .WithDefinition(thief)
            .WithDefinition(warlord)
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
                new WorldStackDestroyedEvent(thiefStack.Id, Destroyer: null, DestroyerPlayerId: null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(targetStack.Id, targetStack),
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty
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

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

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
                Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty
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
            movementPoints: 2);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            defender.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
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
            new WorldFortificationEnteredEvent(attackerStack.Id, capital.Id),
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
            Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(
                new WorldTombId("Tomb0001"),
                new WorldTomb(
                    new WorldTombId("Tomb0001"),
                    new WorldPosition(7, 4),
                    defender.Id, leader.Id, LeaderName: null, DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 1))
        }));
    }
}
