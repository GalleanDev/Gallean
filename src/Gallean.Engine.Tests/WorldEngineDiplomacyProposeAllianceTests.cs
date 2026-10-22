namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyProposeAllianceTests
{
    [Test]
    public void AiAcceptsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 80,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(69))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyProposeAllianceAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: true),
                new WorldAllianceStartedEvent(player1.Id, player2.Id, Permanent: false),
                new WorldSpyAddedEvent(player1.Id, player2.Id),
                new WorldSpyAddedEvent(player2.Id, player1.Id),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 100)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { SpiedPlayers = [player2.Id] })
                    .SetItem(player2.Id, player2 with { SpiedPlayers = [player1.Id] }),
                Relations = before.Relations.SetItem(
                    relation.Id,
                    relation with { Current = 100, Alliance = true, AllianceTurn = 1 })
            }));
    }

    [Test]
    public void AiRefusesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 80,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(70))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyProposeAllianceAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 50)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 50 })
            }));
    }

    [Test]
    public void OwnedLandTest()
    {
        WorldMap map = TestHelpers.BuildMap(
        [
            "UUUUUU",
            "UUUUUU",
            "UUUUUU",
            "......",
            "......",
            "......"
        ]);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 80,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(20))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyProposeAllianceAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 50)
            ]);
    }

    [Test]
    public void HumanAcceptsTest()
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

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyProposeAllianceAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            [new WorldDiplomacyProposalSentEvent(proposal)]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player2.Id, player2 with { IncomingDiplomacyProposals = [proposal] })
            }));

        engine.CheckCommand(
            new WorldAnswerDiplomacyProposalCommand(proposal, Accepted: true),
            [
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: true),
                new WorldAllianceStartedEvent(player1.Id, player2.Id, Permanent: false),
                new WorldSpyAddedEvent(player1.Id, player2.Id),
                new WorldSpyAddedEvent(player2.Id, player1.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { SpiedPlayers = [player2.Id] })
                    .SetItem(player2.Id, player2 with { SpiedPlayers = [player1.Id] }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Alliance = true, AllianceTurn = 1 })
            }));
    }

    [Test]
    public void HumanRefusesTest()
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

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyProposeAllianceAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            [new WorldDiplomacyProposalSentEvent(proposal)]);

        engine.CheckCommand(
            new WorldAnswerDiplomacyProposalCommand(proposal, Accepted: false),
            [new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false)]);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void ExpiresTest()
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

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyProposeAllianceAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            [new WorldDiplomacyProposalSentEvent(proposal)]);

        Assert.That(engine.State.Players[player2.Id].IncomingDiplomacyProposals, Is.EqualTo([proposal]));

        _ = engine.DispatchCommand(new WorldEndTurnCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State.TurnInfo, Is.EqualTo(new WorldTurnInfo(2, player1.Id)));
        Assert.That(engine.State.Players[player2.Id].IncomingDiplomacyProposals, Is.Empty);

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            [new WorldDiplomacyProposalSentEvent(proposal)]);
    }

    [Test]
    public void PeaceRequiredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 66,
            Previous: 66,
            Alliance: false,
            AllianceTurn: 0,
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

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            WorldCommandError.DiplomacyPeaceRequired);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void AlreadyAlliedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 0,
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

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            WorldCommandError.DiplomacyAlreadyAllied);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void DuplicateTest()
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

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyProposeAllianceAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            [new WorldDiplomacyProposalSentEvent(proposal)]);

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyProposeAllianceAction()),
            WorldCommandError.DiplomacyAlreadyProposed);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void UnknownProposalTest()
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

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyProposeAllianceAction());

        engine.CheckCommandError(
            new WorldAnswerDiplomacyProposalCommand(proposal, Accepted: true),
            WorldCommandError.DiplomacyProposalNotFound);

        Assert.That(engine.State, Is.EqualTo(before));
    }
}
