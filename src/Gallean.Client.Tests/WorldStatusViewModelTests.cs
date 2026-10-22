using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class WorldStatusViewModelTests
{
    [Test]
    public void CreatePlayerStatusTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "H.",
            ".."
        ]);

        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(Gold: 100, DeathMana: 5, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0)
        };

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(Gold: 250, DeathMana: 1, RuneMana: 2, LifeMana: 3, InfernalMana: 4, GroveMana: 5)
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithTurnInfo(new WorldTurnInfo(TurnNumber: 7, CurrentPlayerId: human.Id))
            .Build()
            .State;

        WorldStatusViewModel status = WorldStatusViewModel.Create(state, human.Id, null);

        WorldStatusViewModel expected = new(
            human.Id,
            7,
            new AssetSpriteKey.LordFace(WorldRace.Human, 0, IsLarge: false),
            new ResourceSetViewModel(new WorldResourceSet(250, 1, 2, 3, 4, 5), ResourceSetProjection.All));

        Assert.That(status, Is.EqualTo(expected));
    }
}
