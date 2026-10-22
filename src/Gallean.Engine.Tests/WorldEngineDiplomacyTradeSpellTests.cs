namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyTradeSpellTests
{
    [Test]
    public void AiAcceptsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldSpellDefinition heal = new(
            new WorldSpellDefinitionId("heal"),
            new GlobalTextId("heal-name"),
            new GlobalTextId("heal-description"),
            new GlobalTextId("heal-effect"),
            WorldRace.Undead,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            AreaRadius: null,
            new WorldHealSpellEffect(10),
            AiType: WorldAiSpellType.All);

        WorldSpellDefinition summon = new(
            new WorldSpellDefinitionId("summon"),
            new GlobalTextId("summon-name"),
            new GlobalTextId("summon-description"),
            new GlobalTextId("summon-effect"),
            WorldRace.Undead,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            AreaRadius: 1,
            new WorldSummonStackSpellEffect(new UnitDefinitionId("imp")),
            AiType: WorldAiSpellType.All);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [heal.Id, summon.Id])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(74))
            .WithDefinition(heal)
            .WithDefinition(summon)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyTradeSpellAction action = new(heal.Id, Price: 300);
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, action);

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, action),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: true),
                new WorldDiplomacySpellTradedEvent(player1.Id, player2.Id, heal.Id, 300)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0) })
                    .SetItem(player2.Id, player2 with
                    {
                        Resources = new WorldResourceSet(200, 0, 0, 0, 0, 0),
                        Spellbook = new WorldPlayerSpellbook([], [heal.Id])
                    })
            }));
    }

    [Test]
    public void OverpricedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldSpellDefinition heal = new(
            new WorldSpellDefinitionId("heal"),
            new GlobalTextId("heal-name"),
            new GlobalTextId("heal-description"),
            new GlobalTextId("heal-effect"),
            WorldRace.Undead,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            AreaRadius: null,
            new WorldHealSpellEffect(10),
            AiType: WorldAiSpellType.All);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [heal.Id])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(2000, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(25))
            .WithDefinition(heal)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyTradeSpellAction action = new(heal.Id, Price: 1200);
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, action);

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, action),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false)
            ]);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void SummonSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldSpellDefinition heal = new(
            new WorldSpellDefinitionId("heal"),
            new GlobalTextId("heal-name"),
            new GlobalTextId("heal-description"),
            new GlobalTextId("heal-effect"),
            WorldRace.Undead,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            AreaRadius: null,
            new WorldHealSpellEffect(10),
            AiType: WorldAiSpellType.All);

        WorldSpellDefinition summon = new(
            new WorldSpellDefinitionId("summon"),
            new GlobalTextId("summon-name"),
            new GlobalTextId("summon-description"),
            new GlobalTextId("summon-effect"),
            WorldRace.Undead,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            AreaRadius: 1,
            new WorldSummonStackSpellEffect(new UnitDefinitionId("imp")),
            AiType: WorldAiSpellType.All);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [heal.Id, summon.Id])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(heal)
            .WithDefinition(summon)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyTradeSpellAction(summon.Id, Price: 100)),
            WorldCommandError.DiplomacySpellNotTradable);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void AlreadyKnownTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldSpellDefinition heal = new(
            new WorldSpellDefinitionId("heal"),
            new GlobalTextId("heal-name"),
            new GlobalTextId("heal-description"),
            new GlobalTextId("heal-effect"),
            WorldRace.Undead,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            AreaRadius: null,
            new WorldHealSpellEffect(10),
            AiType: WorldAiSpellType.All);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [heal.Id])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            Spellbook = new WorldPlayerSpellbook([], [heal.Id])
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(heal)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyTradeSpellAction(heal.Id, Price: 100)),
            WorldCommandError.SpellAlreadyKnown);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void NotKnownTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldSpellDefinition heal = new(
            new WorldSpellDefinitionId("heal"),
            new GlobalTextId("heal-name"),
            new GlobalTextId("heal-description"),
            new GlobalTextId("heal-effect"),
            WorldRace.Undead,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            AreaRadius: null,
            new WorldHealSpellEffect(10),
            AiType: WorldAiSpellType.All);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(heal)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyTradeSpellAction(heal.Id, Price: 100)),
            WorldCommandError.SpellNotKnown);

        Assert.That(engine.State, Is.EqualTo(before));
    }
}
