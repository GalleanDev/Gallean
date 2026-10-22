using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class DiplomacyProposalViewModelTests
{
    [Test]
    public async Task AcceptTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 80,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, player2.Id))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Heretic, new GlobalTextId("Legions of the Damned"), WorldMapTerrain.Heretic))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .BuildSession();

        WorldDiplomacyProposal proposal = new(player2.Id, player1.Id, new WorldDiplomacyProposeAllianceAction());

        session.World.DispatchCommand(new WorldDiplomacyActionCommand(player1.Id, new WorldDiplomacyProposeAllianceAction()));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        DiplomacyProposalViewModel prompt = new(gameModel, gameViewModel, proposal);
        gameViewModel.OpenScreen(prompt);

        Assert.That(prompt.Message, Is.EqualTo("The Legions of the Damned offers to ally with you."));

        await prompt.AcceptCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.TopScreen, Is.Null);
        Assert.That(session.World.State.Players[player1.Id].IncomingDiplomacyProposals, Is.Empty);
        Assert.That(session.World.State.Relations[new WorldRelationId(WorldRace.Undead, WorldRace.Heretic)].Alliance, Is.True);
    }

    [Test]
    public async Task DeclineTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 80,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, player2.Id))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Heretic, new GlobalTextId("Legions of the Damned"), WorldMapTerrain.Heretic))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .BuildSession();

        WorldDiplomacyProposal proposal = new(player2.Id, player1.Id, new WorldDiplomacyExchangeMapAction());

        session.World.DispatchCommand(new WorldDiplomacyActionCommand(player1.Id, new WorldDiplomacyExchangeMapAction()));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        DiplomacyProposalViewModel prompt = new(gameModel, gameViewModel, proposal);
        gameViewModel.OpenScreen(prompt);

        Assert.That(prompt.Message, Is.EqualTo("The Legions of the Damned offers to exchange map information with you."));

        await prompt.DeclineCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.TopScreen, Is.Null);
        Assert.That(session.World.State.Players[player1.Id].IncomingDiplomacyProposals, Is.Empty);
        Assert.That(session.World.State.Relations[new WorldRelationId(WorldRace.Undead, WorldRace.Heretic)].Alliance, Is.False);
    }
}
