namespace Gallean.Engine.Tests;

public sealed class WorldEngineStartGameTests
{
    [Test]
    public void HumanPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);
    }

    [Test]
    public void TwoHumanPlayersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player3 = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(player3.LordId, player3.Race, LordCategory.Warrior))
            .WithPlayer(player3)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);
    }

    [Test]
    public void AllAiPlayersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);
    }

    [Test]
    public void NeutralFirstPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer player3 = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(player3.LordId, player3.Race, LordCategory.Warrior))
            .WithPlayer(player3)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player3.Id),
            new WorldTurnStartedEvent(player3.Id),
            new WorldTurnResourcesGeneratedEvent(player3.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);
    }
}
