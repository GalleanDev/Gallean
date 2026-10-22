namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyExchangeMapTests
{
    [Test]
    public void AiAcceptsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "..??",
                "????"
            ])
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "??..",
                "..??"
            ])
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
            .WithRandom(new TestSequenceRandomProvider(29))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyExchangeMapAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyExchangeMapAction()),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: true),
                new WorldFogRevealedEvent(player1.Id, [new WorldPosition(2, 0), new WorldPosition(3, 0), new WorldPosition(0, 1), new WorldPosition(1, 1)]),
                new WorldFogRevealedEvent(player2.Id, [new WorldPosition(0, 0), new WorldPosition(1, 0)])
            ]);

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player1.Id].FogOfWar), Is.EqualTo(
        [
            "....",
            "..??"
        ]));
        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player2.Id].FogOfWar), Is.EqualTo(
        [
            "....",
            "..??"
        ]));
        Assert.That(engine.State.Players[player1.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions
        {
            MapExchangeProposals = [player2.Id]
        }));
        Assert.That(engine.State.Players[player2.Id].IncomingDiplomacyProposals, Is.Empty);
        Assert.That(engine.State.Relations[relation.Id], Is.EqualTo(relation));
    }

    [Test]
    public void AiRefusesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "..??",
                "????"
            ])
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "??..",
                "..??"
            ])
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
            .WithRandom(new TestSequenceRandomProvider(30))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyExchangeMapAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyExchangeMapAction()),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player1.Id, player1 with
                {
                    TurnActions = new WorldPlayerTurnActions { MapExchangeProposals = [player2.Id] }
                })
            }));
    }

    [Test]
    public void OncePerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "..??",
                "????"
            ])
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "??..",
                "..??"
            ])
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
            .WithRandom(new TestSequenceRandomProvider(30))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyExchangeMapAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyExchangeMapAction()),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false)
            ]);

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyExchangeMapAction()),
            WorldCommandError.DiplomacyAlreadyProposed);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void NextTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "..??",
                "????"
            ])
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "??..",
                "..??"
            ])
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
            .WithRandom(new TestSequenceRandomProvider(30, 30))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, new WorldDiplomacyExchangeMapAction());

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyExchangeMapAction()),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false)
            ]);

        _ = engine.DispatchCommand(new WorldEndTurnCommand());
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State.Players[player1.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions()));

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyExchangeMapAction()),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false)
            ]);
    }

    [Test]
    public void PeaceRequiredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 2);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "..??",
                "????"
            ])
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            FogOfWar = TestHelpers.BuildFogOfWar(
            [
                "??..",
                "..??"
            ])
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 50,
            Previous: 50,
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
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyExchangeMapAction()),
            WorldCommandError.DiplomacyPeaceRequired);

        Assert.That(engine.State, Is.EqualTo(before));
    }
}
