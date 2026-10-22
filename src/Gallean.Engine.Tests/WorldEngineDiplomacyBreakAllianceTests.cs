namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyBreakAllianceTests
{
    [Test]
    public void BreaksAllianceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 100,
            Previous: 80,
            Alliance: true,
            AllianceTurn: 3,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyBreakAllianceAction()),
            [
                new WorldAllianceEndedEvent(player1.Id, player2.Id),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Relations = before.Relations.SetItem(
                    relation.Id,
                    relation with { Current = 10, Alliance = false, AllianceTurn = 0 })
            }));
    }

    [Test]
    public void NoAllianceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyBreakAllianceAction()),
            WorldCommandError.DiplomacyAllianceRequired);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void PermanentAllianceBindsAiTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: true);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyBreakAllianceAction()),
            WorldCommandError.DiplomacyAllianceBreakForbidden);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void PermanentAllianceDoesNotBindPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: true);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyBreakAllianceAction()),
            [
                new WorldAllianceEndedEvent(player1.Id, player2.Id),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Relations = before.Relations.SetItem(
                    relation.Id,
                    relation with { Current = 10, Alliance = false, AllianceTurn = 0 })
            }));
    }
}
