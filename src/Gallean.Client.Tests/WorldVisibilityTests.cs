using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class WorldVisibilityTests
{
    [Test]
    public void FogEnabledTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                ".?",
                "??"
            ])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build()
            .State;

        WorldVisibility visibility = WorldVisibility.Create(state, player.Id, fogEnabled: true);

        WorldPosition explored = new(0, 0);
        WorldPosition unexplored = new(1, 0);

        Assert.That(visibility.IsExplored(explored), Is.True);
        Assert.That(visibility.IsRevealed(explored), Is.True);
        Assert.That(visibility.IsExplored(unexplored), Is.False);
        Assert.That(visibility.IsRevealed(unexplored), Is.False);

        WorldFootprint partiallyExplored = new(new WorldPosition(0, 0), 2, 2);
        WorldFootprint unexploredOnly = new(unexplored, 1, 1);

        Assert.That(visibility.IsAnyRevealed(partiallyExplored), Is.True);
        Assert.That(visibility.IsAnyRevealed(unexploredOnly), Is.False);
    }

    [Test]
    public void FogDisabledTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["??"])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build()
            .State;

        WorldVisibility visibility = WorldVisibility.Create(state, player.Id, fogEnabled: false);

        Assert.That(visibility.IsRevealed(new WorldPosition(0, 0)), Is.True);
        Assert.That(visibility.IsRevealed(new WorldPosition(1, 0)), Is.True);
        Assert.That(visibility.IsAnyRevealed(new WorldFootprint(new WorldPosition(0, 0), 2, 1)), Is.True);
    }
}
