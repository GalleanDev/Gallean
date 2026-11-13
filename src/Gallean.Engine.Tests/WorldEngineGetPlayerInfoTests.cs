namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetPlayerInfoTests
{
    [Test]
    public void SimpleTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".UU",
            "...",
            ".R."
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithResourceNode(new WorldResourceNode(
                new WorldResourceNodeId("node1"),
                new WorldPosition(1, 0),
                WorldResourceType.Gold,
                AiPriority: 3))
            .WithResourceNode(new WorldResourceNode(
                new WorldResourceNodeId("node2"),
                new WorldPosition(2, 0),
                WorldResourceType.RuneMana,
                AiPriority: 3))
            .WithResourceNode(new WorldResourceNode(
                new WorldResourceNodeId("node3"),
                new WorldPosition(1, 2),
                WorldResourceType.InfernalMana,
                AiPriority: 3))
            .WithResourceNode(new WorldResourceNode(
                new WorldResourceNodeId("node4"),
                new WorldPosition(0, 0),
                WorldResourceType.LifeMana,
                AiPriority: 3))
            .Build();

        WorldGetPlayerInfoCommand.Result result = engine.CheckCommand(new WorldGetPlayerInfoCommand(player1.Id), []);

        Assert.That(result, Is.EqualTo(new WorldGetPlayerInfoCommand.Result(
            TotalIncome: new WorldResourceSet(150, 25, 50, 0, 0, 0),
            CapitalIncome: new WorldResourceSet(100, 25, 0, 0, 0, 0),
            ResourceNodesIncome: new WorldResourceSet(50, 0, 50, 0, 0, 0))));
    }

    [Test]
    public void EasyTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".U.",
            "...",
            ".R."
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Easy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithResourceNode(new WorldResourceNode(
                new WorldResourceNodeId("node1"),
                new WorldPosition(1, 0),
                WorldResourceType.Gold,
                AiPriority: 3))
            .Build();

        WorldGetPlayerInfoCommand.Result result = engine.CheckCommand(new WorldGetPlayerInfoCommand(player1.Id), []);

        Assert.That(result, Is.EqualTo(new WorldGetPlayerInfoCommand.Result(
            TotalIncome: new WorldResourceSet(195, 32, 0, 0, 0, 0),
            CapitalIncome: new WorldResourceSet(130, 32, 0, 0, 0, 0),
            ResourceNodesIncome: new WorldResourceSet(65, 0, 0, 0, 0, 0))));
    }
}
