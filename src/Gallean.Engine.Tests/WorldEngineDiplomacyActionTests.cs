namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyActionTests
{
    [Test]
    public void InvalidTargetsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player3 = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map);
        WorldCapital capturedCapital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital"),
                player2.Id,
                new WorldPosition(4, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = player1.Id
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(player3.LordId, player3.Race, LordCategory.Warrior))
            .WithPlayer(player3)
            .WithCapital(capturedCapital)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player1.Id, new WorldDiplomacyGiveGoldAction(100)),
            WorldCommandError.DiplomacyTargetInvalid);
        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(100)),
            WorldCommandError.DiplomacyTargetInvalid);
        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player3.Id, new WorldDiplomacyGiveGoldAction(100)),
            WorldCommandError.DiplomacyTargetInvalid);
        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(new WorldPlayerId(4), new WorldDiplomacyGiveGoldAction(100)),
            WorldCommandError.DiplomacyTargetInvalid);

        Assert.That(engine.State, Is.EqualTo(before));
    }
}
