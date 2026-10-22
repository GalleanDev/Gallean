using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineScenarioEventDiplomacyTests
{
    private static ScenarioEventDefinition BuildEvent(ScenarioEventCondition condition)
    {
        return new ScenarioEventDefinition(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [condition, new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("The envoys have arrived."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);
    }

    [Test]
    public void DiplomacyRelationsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
            Current: 80,
            Previous: 80,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        ScenarioEventDefinition scenarioEvent = BuildEvent(
            new ScenarioEventDiplomacyRelationsCondition(player.Id, otherPlayer.Id, WorldRelationLevel.Peace));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(relation)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(
                scenarioEvent.Id,
                new ScenarioTextId("The envoys have arrived."),
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
                ScenarioState = WorldScenarioState.Empty with { EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false) }
            }));
    }

    [Test]
    public void DiplomacyRelationsMismatchTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
            Current: 66,
            Previous: 66,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        ScenarioEventDefinition scenarioEvent = BuildEvent(
            new ScenarioEventDiplomacyRelationsCondition(player.Id, otherPlayer.Id, WorldRelationLevel.Peace));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(relation)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void MissingRelationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        ScenarioEventDefinition scenarioEvent = BuildEvent(
            new ScenarioEventDiplomacyRelationsCondition(player.Id, otherPlayer.Id, WorldRelationLevel.War));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(
                scenarioEvent.Id,
                new ScenarioTextId("The envoys have arrived."),
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
                ScenarioState = WorldScenarioState.Empty with { EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false) }
            }));
    }

    [Test]
    public void AllianceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: false);
        ScenarioEventDefinition scenarioEvent = BuildEvent(new ScenarioEventAllianceCondition(player.Id, otherPlayer.Id));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(relation)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioPopupEvent(
                scenarioEvent.Id,
                new ScenarioTextId("The envoys have arrived."),
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
                ScenarioState = WorldScenarioState.Empty with { EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false) }
            }));
    }

    [Test]
    public void NoAllianceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
            Current: 100,
            Previous: 100,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        ScenarioEventDefinition scenarioEvent = BuildEvent(new ScenarioEventAllianceCondition(player.Id, otherPlayer.Id));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(relation)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                ScenarioState = WorldScenarioState.Empty
            }));
    }

    [Test]
    public void AllyEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
            Current: 45,
            Previous: 20,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventAllyEffect(player.Id, otherPlayer.Id, Permanent: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(relation)
            .WithScenarioEvent(scenarioEvent)
            .WithTurnInfo(new WorldTurnInfo(7, player.Id))
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldAllianceStartedEvent(player.Id, otherPlayer.Id, Permanent: true),
            new WorldSpyAddedEvent(player.Id, otherPlayer.Id),
            new WorldSpyAddedEvent(otherPlayer.Id, player.Id),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State.Relations.Values, Is.EqualTo([
            new WorldRelation(
                new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
                Current: 100,
                Previous: 20,
                Alliance: true,
                AllianceTurn: 7,
                PermanentWar: false,
                PermanentAlliance: true)
        ]));

        Assert.That(engine.State.Players[player.Id].SpiedPlayers, Is.EqualTo([otherPlayer.Id]));
        Assert.That(engine.State.Players[otherPlayer.Id].SpiedPlayers, Is.EqualTo([player.Id]));
    }

    [Test]
    public void AllyEffectPermanentWarTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
            Current: 10,
            Previous: 30,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: true,
            PermanentAlliance: false);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventAllyEffect(player.Id, otherPlayer.Id, Permanent: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(relation)
            .WithScenarioEvent(scenarioEvent)
            .WithTurnInfo(new WorldTurnInfo(4, player.Id))
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldAllianceStartedEvent(player.Id, otherPlayer.Id, Permanent: false),
            new WorldSpyAddedEvent(player.Id, otherPlayer.Id),
            new WorldSpyAddedEvent(otherPlayer.Id, player.Id),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State.Relations.Values, Is.EqualTo([
            new WorldRelation(
                new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
                Current: 100,
                Previous: 30,
                Alliance: true,
                AllianceTurn: 4,
                PermanentWar: true,
                PermanentAlliance: false)
        ]));
    }

    [Test]
    public void DiplomacyEffectTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
            Current: 45,
            Previous: 20,
            Alliance: true,
            AllianceTurn: 3,
            PermanentWar: true,
            PermanentAlliance: false);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventDiplomacyEffect(player.Id, otherPlayer.Id, Relation: 100, PermanentWar: null)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithRelation(relation)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State.Relations.Values, Is.EqualTo([
            new WorldRelation(
                new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
                Current: 100,
                Previous: 20,
                Alliance: true,
                AllianceTurn: 3,
                PermanentWar: true,
                PermanentAlliance: false)
        ]));
    }

    [Test]
    public void DiplomacyEffectPermanentWarTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer otherPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);
        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Human],
            [WorldRace.Human],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventDiplomacyEffect(player.Id, otherPlayer.Id, Relation: 49, PermanentWar: true)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(otherPlayer.LordId, otherPlayer.Race, LordCategory.Warrior))
            .WithPlayer(otherPlayer)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 25, 0, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        Assert.That(engine.State.Relations.Values, Is.EqualTo([
            new WorldRelation(
                new WorldRelationId(WorldRace.Human, WorldRace.Dwarf),
                Current: 49,
                Previous: 0,
                Alliance: false,
                AllianceTurn: 0,
                PermanentWar: true,
                PermanentAlliance: false)
        ]));
    }
}
