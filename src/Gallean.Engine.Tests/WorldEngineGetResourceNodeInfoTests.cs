namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetResourceNodeInfoTests
{
    [Test]
    public void SimpleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResourceNode(new WorldResourceNode(
                Id: new WorldResourceNodeId("mine"),
                Position: new WorldPosition(1, 1),
                ResourceType: WorldResourceType.Gold,
                AiPriority: 1))
            .WithResourceNode(new WorldResourceNode(
                Id: new WorldResourceNodeId("crystal"),
                Position: new WorldPosition(2, 0),
                ResourceType: WorldResourceType.InfernalMana,
                AiPriority: 1))
            .Build();

        WorldGetResourceNodeInfoCommand.Result mine = engine.CheckCommand(new WorldGetResourceNodeInfoCommand(new WorldPosition(1, 1)), []);
        WorldGetResourceNodeInfoCommand.Result crystal = engine.CheckCommand(new WorldGetResourceNodeInfoCommand(new WorldPosition(2, 0)), []);

        Assert.That(mine, Is.EqualTo(new WorldGetResourceNodeInfoCommand.Result(new WorldResourceSet(50, 0, 0, 0, 0, 0))));
        Assert.That(crystal, Is.EqualTo(new WorldGetResourceNodeInfoCommand.Result(new WorldResourceSet(0, 0, 0, 0, 50, 0))));
    }

    [Test]
    public void NotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldGetResourceNodeInfoCommand(new WorldPosition(1, 1)),
            WorldCommandError.ResourceNodeNotFound);
    }
}
