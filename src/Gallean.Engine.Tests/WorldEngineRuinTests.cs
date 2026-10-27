namespace Gallean.Engine.Tests;

public sealed class WorldEngineRuinTests
{
    [Test]
    public void MoveOntoGuardedRuinTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^...",
            "^^...",
            "^^..U"
        ]);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("Unit1", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10,
                VisionRadius = 3
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("stack1"),
            new WorldPlayerId(1),
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), new GridPosition(0, 0), unitDefinition.IsLarge)
                .Build(),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit2"), unitDefinition), new GridPosition(0, 0), unitDefinition.IsLarge)
                .Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10), new GridPosition(0, 0))
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit2"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10), new GridPosition(0, 0))
            .Build(new BattleModifiers(
                Attacker: BattleSideModifiers.None,
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false)));

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("stack1"), new WorldPosition(2, 2)),
            [
                new WorldStackMovingEvent(new WorldStackId("stack1"), new WorldPosition(4, 4), new WorldPosition(3, 3), WorldStackDirection.North, 8, [], ruin.Entrance),
                new WorldStackMovedEvent(new WorldStackId("stack1"), new WorldPosition(4, 4), new WorldPosition(3, 3), WorldStackDirection.North, 8, [], ruin.Entrance),
                new WorldStackMovementConsumedEvent(new WorldStackId("stack1"), 3),
                new WorldFortificationBattleStartedEvent(new WorldStackId("stack1"), new WorldFortificationId("ruin1"), new WorldPosition(2, 2), expectedBattleState)
            ]);

        WorldStack expectedStack = stack with
        {
            Position = new WorldPosition(3, 3),
            Direction = WorldStackDirection.North,
            MovementPoints = 3
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(expectedStack.Id, expectedStack)
            }));
    }

    [Test]
    public void ResolveRuinBattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10) with { Experience = 3, ExperienceGained = 4 }, new GridPosition(0, 0))
                .Build(new BattleModifiers(
                    Attacker: BattleSideModifiers.None,
                    Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false))) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 7 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("Ruin1"),
                    BattleSide.Attacker,
                    expectedAttackerUnits,
                    UnitGrid<WorldUnit>.Empty),
                new WorldRuinLootedEvent(new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), new WorldResourceSet(200, 0, 0, 0, 0, 0), null)
            ]
        );

        WorldPlayer expectedPlayer = player with
        {
            Resources = new WorldResourceSet(300, 25, 0, 0, 0, 0)
        };
        WorldStack expectedStack = stack with
        {
            Units = expectedAttackerUnits,
            BattlesWon = 1
        };
        WorldRuin expectedRuin = ruin with
        {
            Garrison = UnitGrid<WorldUnit>.Empty,
            LooterId = new WorldPlayerId(1)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(expectedPlayer.Id, expectedPlayer),
                Stacks = engine.State.Stacks.SetItem(expectedStack.Id, expectedStack),
                Fortifications = engine.State.Fortifications.SetItem(expectedRuin.Id, expectedRuin)
            }));
    }

    [Test]
    public void ResolveRuinBattleLossTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("unit2"), unitDefinition),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: new ItemDefinitionId("Item1"),
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
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

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("Ruin1"),
                    BattleSide.Defender,
                    UnitGrid<WorldUnit>.Empty,
                    expectedDefenderUnits),
                new WorldStackDestroyedEvent(new WorldStackId("Stack1"), Destroyer: null, DestroyerPlayerId: null)
            ]
        );

        WorldRuin expectedRuin = ruin with
        {
            Garrison = expectedDefenderUnits
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(new WorldStackId("Stack1")),
                Fortifications = engine.State.Fortifications.SetItem(expectedRuin.Id, expectedRuin)
            }));
    }

    [Test]
    public void ResolveRuinBattleItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        ItemDefinition itemDefinition = new(
            new ItemDefinitionId("Item1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Valuable,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: itemDefinition.Id,
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10) with { Experience = 3, ExperienceGained = 4 }, new GridPosition(0, 0))
                .Build(new BattleModifiers(
                    Attacker: BattleSideModifiers.None,
                    Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false))) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 7 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("Ruin1"),
                    BattleSide.Attacker,
                    expectedAttackerUnits,
                    UnitGrid<WorldUnit>.Empty),
                new WorldRuinLootedEvent(new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), new WorldResourceSet(200, 0, 0, 0, 0, 0), new WorldItem(new WorldItemId("Item0001"), new ItemDefinitionId("Item1"), Charges: null))
            ]
        );

        WorldPlayer expectedPlayer = player with
        {
            Resources = new WorldResourceSet(300, 25, 0, 0, 0, 0)
        };
        WorldStack expectedStack = stack with
        {
            Units = expectedAttackerUnits,
            BattlesWon = 1,
            Inventory = new WorldInventory([new WorldItem(new WorldItemId("Item0001"), itemDefinition.Id, Charges: null)])
        };
        WorldRuin expectedRuin = ruin with
        {
            Garrison = UnitGrid<WorldUnit>.Empty,
            LooterId = new WorldPlayerId(1)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(expectedPlayer.Id, expectedPlayer),
                Stacks = engine.State.Stacks.SetItem(expectedStack.Id, expectedStack),
                Fortifications = engine.State.Fortifications.SetItem(expectedRuin.Id, expectedRuin)
            }));
    }

    [Test]
    public void ResolveRuinBattleClearGarrisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("unit2"), unitDefinition),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10) with { Experience = 3, ExperienceGained = 4 }, new GridPosition(0, 0))
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Defender, hitpoints: 0), new GridPosition(0, 0))
                .Build(new BattleModifiers(
                    Attacker: BattleSideModifiers.None,
                    Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false))) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 7 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Build();

        UnitGrid<WorldUnit> deadDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Hitpoints = 0 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("Ruin1"),
                    BattleSide.Attacker,
                    expectedAttackerUnits,
                    deadDefenderUnits),
                new WorldRuinLootedEvent(new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), new WorldResourceSet(200, 0, 0, 0, 0, 0), null)
            ]
        );

        WorldPlayer expectedPlayer = player with
        {
            Resources = new WorldResourceSet(300, 25, 0, 0, 0, 0)
        };
        WorldStack expectedStack = stack with
        {
            Units = expectedAttackerUnits,
            BattlesWon = 1
        };
        WorldRuin expectedRuin = ruin with
        {
            Garrison = UnitGrid<WorldUnit>.Empty,
            LooterId = new WorldPlayerId(1)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(expectedPlayer.Id, expectedPlayer),
                Stacks = engine.State.Stacks.SetItem(expectedStack.Id, expectedStack),
                Fortifications = engine.State.Fortifications.SetItem(expectedRuin.Id, expectedRuin)
            }));
    }

    [Test]
    public void ResolveRuinBattleDefenderWinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("unit2"), unitDefinition),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: new ItemDefinitionId("Item1"),
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10) with { Experience = 3, ExperienceGained = 4 }, new GridPosition(0, 0))
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(2), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Defender, hitpoints: 0), new GridPosition(0, 1))
                .Build(new BattleModifiers(
                    Attacker: BattleSideModifiers.None,
                    Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false))) with
            {
                Outcome = new BattleOutcome(BattleSide.Defender)
            };

        UnitGrid<WorldUnit> expectedDefenderUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 7 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Hitpoints = 0 }, new GridPosition(0, 1), unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("Ruin1"),
                    BattleSide.Defender,
                    UnitGrid<WorldUnit>.Empty,
                    expectedDefenderUnits),
                new WorldStackDestroyedEvent(new WorldStackId("Stack1"), Destroyer: null, DestroyerPlayerId: null)
            ]
        );

        WorldRuin expectedRuin = ruin with
        {
            Garrison = expectedDefenderUnits
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(new WorldStackId("Stack1")),
                Fortifications = engine.State.Fortifications.SetItem(expectedRuin.Id, expectedRuin)
            }));
    }

    [Test]
    public void EnterGuardedRuinFromEntranceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(3, 3),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("unit2"), unitDefinition),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState expectedBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10), new GridPosition(0, 0))
            .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("unit2"), unitDefinition.Id, BattleSide.Defender, hitpoints: 10), new GridPosition(0, 0))
            .Build(new BattleModifiers(
                Attacker: BattleSideModifiers.None,
                Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false)));

        engine.CheckCommand(
            new WorldMoveStackCommand(new WorldStackId("Stack1"), new WorldPosition(2, 2)),
            [
                new WorldStackMovementConsumedEvent(new WorldStackId("Stack1"), 5),
                new WorldFortificationBattleStartedEvent(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1"), new WorldPosition(2, 2), expectedBattleState)
            ]);

        WorldStack expectedStack = stack with
        {
            Direction = WorldStackDirection.North,
            MovementPoints = 5
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(expectedStack.Id, expectedStack)
            }));
    }

    [Test]
    public void ResolveRuinBattleNoTreasureTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("unit2"), unitDefinition),
            Resources: WorldResourceSet.Empty,
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 10) with { Experience = 3, ExperienceGained = 4 }, new GridPosition(0, 0))
                .Build(new BattleModifiers(
                    Attacker: BattleSideModifiers.None,
                    Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false))) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        UnitGrid<WorldUnit> expectedAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Experience = 7 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("Ruin1"),
                    BattleSide.Attacker,
                    expectedAttackerUnits,
                    UnitGrid<WorldUnit>.Empty),
                new WorldRuinLootedEvent(new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), WorldResourceSet.Empty, null)
            ]
        );

        WorldPlayer expectedPlayer = player with
        {
            Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0)
        };
        WorldStack expectedStack = stack with
        {
            Units = expectedAttackerUnits,
            BattlesWon = 1
        };
        WorldRuin expectedRuin = ruin with
        {
            Garrison = UnitGrid<WorldUnit>.Empty,
            LooterId = new WorldPlayerId(1)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(expectedPlayer.Id, expectedPlayer),
                Stacks = engine.State.Stacks.SetItem(expectedStack.Id, expectedStack),
                Fortifications = engine.State.Fortifications.SetItem(expectedRuin.Id, expectedRuin)
            }));
    }

    [Test]
    public void ResolveRuinBattleWinnerDestroyedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition unitDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("dummy", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                MaxHitpoints = 10
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            new WorldPlayerId(1),
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("unit"), unitDefinition),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("unit2"), unitDefinition),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: new ItemDefinitionId("Item1"),
            LooterId: null,
            AiPriority: 3
        );

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(unitDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(unitDefinition).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("unit"), unitDefinition.Id, BattleSide.Attacker, hitpoints: 0), new GridPosition(0, 0))
                .Build(new BattleModifiers(
                    Attacker: BattleSideModifiers.None,
                    Defender: new BattleSideModifiers(ExperiencePercent: 0, AccuracyBonus: 0, RandomDamageBonus: 0, CanRetreat: false))) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        UnitGrid<WorldUnit> deadAttackerUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition) with { Hitpoints = 0 }, new GridPosition(0, 0), unitDefinition.IsLarge)
            .Build();

        engine.CheckCommand(
            new WorldResolveFortificationBattleCommand(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1"), finalBattleState),
            [
                new WorldFortificationBattleEndedEvent(
                    new WorldStackId("Stack1"),
                    new WorldFortificationId("Ruin1"),
                    BattleSide.Attacker,
                    deadAttackerUnits,
                    UnitGrid<WorldUnit>.Empty),
                new WorldRuinLootedEvent(new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), new WorldResourceSet(200, 0, 0, 0, 0, 0), null),
                new WorldStackDestroyedEvent(new WorldStackId("Stack1"), Destroyer: null, DestroyerPlayerId: null)
            ]
        );

        WorldPlayer expectedPlayer = player with
        {
            Resources = new WorldResourceSet(300, 25, 0, 0, 0, 0)
        };
        WorldRuin expectedRuin = ruin with
        {
            Garrison = UnitGrid<WorldUnit>.Empty,
            LooterId = new WorldPlayerId(1)
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(expectedPlayer.Id, expectedPlayer),
                Stacks = engine.State.Stacks.Remove(new WorldStackId("Stack1")),
                Fortifications = engine.State.Fortifications.SetItem(expectedRuin.Id, expectedRuin)
            }));
    }
}
