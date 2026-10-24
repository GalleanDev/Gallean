using System.Collections.Immutable;
using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventRuinTests
{
    [Test]
    public void LootingRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventLootingRuinCondition(ruin.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The ruin gives up its treasure."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: true)],
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
            .WithRuin(ruin)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(stack.Id, ruin.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(
                stack.Id,
                ruin.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldRuinLootedEvent(ruin.Id, stack.Id, new WorldResourceSet(200, 0, 0, 0, 0, 0), null),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id)),
            new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("The ruin gives up its treasure."), ScenarioEventPortraitId.Unit(leader.Id), PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(5, 5), [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackMovedTrigger(stack.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("The ruin gives up its treasure."),
                            ScenarioEventPortraitId.Unit(leader.Id),
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void LootingRuinOtherPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer rival = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldStack rivalStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            rival.Id,
            new WorldPosition(0, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventLootingRuinCondition(ruin.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The ruin gives up its treasure."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(rival.LordId, rival.Race, LordCategory.Warrior))
            .WithPlayer(rival)
            .WithStack(stack)
            .WithStack(rivalStack)
            .WithRuin(ruin)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        _ = engine.DispatchCommand(new WorldResolveFortificationBattleCommand(rivalStack.Id, ruin.Id, finalBattleState));
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(Assert.IsOfType<WorldRuin>(engine.State.Fortifications[ruin.Id]).LooterId, Is.EqualTo(rival.Id));
        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void LootingRuinUnverifiedLooterTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer rival = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldStack rivalStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            rival.Id,
            new WorldPosition(0, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human, WorldRace.Undead],
            [WorldRace.Human],
            [new ScenarioEventLootingRuinCondition(ruin.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The ruin gives up its treasure."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(rival.LordId, rival.Race, LordCategory.Warrior))
            .WithPlayer(rival)
            .WithStack(stack)
            .WithStack(rivalStack)
            .WithRuin(ruin)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        _ = engine.DispatchCommand(new WorldResolveFortificationBattleCommand(rivalStack.Id, ruin.Id, finalBattleState));
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(Assert.IsOfType<WorldRuin>(engine.State.Fortifications[ruin.Id]).LooterId, Is.EqualTo(rival.Id));
        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 2,
                            scenarioEvent.Id,
                            new ScenarioTextId("The ruin gives up its treasure."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void LootingSeveralRuinsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldRuin firstRuin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldRuin secondRuin = new(
            Id: new WorldFortificationId("Ruin2"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(5, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [
                new ScenarioEventLootingRuinCondition(firstRuin.Id),
                new ScenarioEventLootingRuinCondition(secondRuin.Id)
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Both ruins are plundered."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
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
            .WithRuin(firstRuin)
            .WithRuin(secondRuin)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        _ = engine.DispatchCommand(new WorldResolveFortificationBattleCommand(stack.Id, firstRuin.Id, finalBattleState));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));

        _ = engine.DispatchCommand(new WorldResolveFortificationBattleCommand(stack.Id, secondRuin.Id, finalBattleState));

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 1,
                            scenarioEvent.Id,
                            new ScenarioTextId("Both ruins are plundered."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }

    [Test]
    public void LootingRuinAffectedRaceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer ally = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 6);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 10);

        WorldStack allyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            ally.Id,
            new WorldPosition(0, 5),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leader),
            movementPoints: 10);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("leader3"), leader),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human, WorldRace.Dwarf],
            [WorldRace.Human, WorldRace.Dwarf],
            [
                new ScenarioEventLootingRuinCondition(ruin.Id),
                new ScenarioEventFrequencyCondition(1)
            ],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The ruin gives up its treasure."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(ally.LordId, ally.Race, LordCategory.Warrior))
            .WithPlayer(ally)
            .WithStack(stack)
            .WithStack(allyStack)
            .WithRuin(ruin)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        BattleState finalBattleState = new BattleStateBuilder(new TestGameDefinitionProviderBuilder().AddUnit(leader).Build())
                .Add(TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("leader"), leader.Id, BattleSide.Attacker, hitpoints: leader.MaxHitpoints), GridPosition.FrontBottom)
                .Build(BattleModifiers.None) with
            {
                Outcome = new BattleOutcome(BattleSide.Attacker)
            };

        engine.CheckCommand(new WorldResolveFortificationBattleCommand(allyStack.Id, ruin.Id, finalBattleState), [
            new WorldFortificationBattleEndedEvent(
                allyStack.Id,
                ruin.Id,
                BattleSide.Attacker,
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
                UnitGrid<WorldUnit>.Empty),
            new WorldRuinLootedEvent(ruin.Id, allyStack.Id, new WorldResourceSet(200, 0, 0, 0, 0, 0), null)
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(ally.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 10),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(
                scenarioEvent.Id,
                new ScenarioTextId("The ruin gives up its treasure."),
                PortraitId: null,
                PortraitName: null,
                IsPortraitLeft: true,
                player.Id,
                TriggererPosition: null,
                [player.Id]),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Fortifications = engine.State.Fortifications.SetItem(ruin.Id, ruin with
                {
                    Garrison = UnitGrid<WorldUnit>.Empty,
                    LooterId = ally.Id
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false),
                    Messages =
                    [
                        new WorldScenarioMessage(
                            TurnNumber: 2,
                            scenarioEvent.Id,
                            new ScenarioTextId("The ruin gives up its treasure."),
                            PortraitId: null,
                            PortraitName: null,
                            IsPortraitLeft: true,
                            player.Id,
                            [player.Id])
                    ]
                }
            }));
    }
}
