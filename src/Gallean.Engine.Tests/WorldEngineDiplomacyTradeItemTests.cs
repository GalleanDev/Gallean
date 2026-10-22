namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyTradeItemTests
{
    [Test]
    public void AiAcceptsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(10, 0, 0, 0, 0, 0)
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

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(5, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([swordItem])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(12, 12),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(74))
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyTradeItemAction action = new(city.Id, swordItem.DefinitionId, Price: 100);
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, action);

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, action),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: true),
                new WorldDiplomacyItemTradedEvent(player1.Id, player2.Id, city.Id, swordItem, 100),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 42)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = new WorldResourceSet(110, 0, 0, 0, 0, 0) })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0) }),
                Fortifications = before.Fortifications
                    .SetItem(city.Id, before.Fortifications[city.Id] with { Inventory = WorldInventory.Empty })
                    .SetItem(capital.Id, before.Fortifications[capital.Id] with
                    {
                        Inventory = new WorldInventory([swordItem])
                    }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 42 })
            }));
    }

    [Test]
    public void GiftTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

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

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(5, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([swordItem])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(12, 12),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(99))
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldDiplomacyTradeItemAction action = new(city.Id, swordItem.DefinitionId, Price: 0);
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, action);

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, action),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: true),
                new WorldDiplomacyItemTradedEvent(player1.Id, player2.Id, city.Id, swordItem, 0),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 44)
            ]);
    }

    [Test]
    public void OverpricedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(5, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([swordItem])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(12, 12),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(12))
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyTradeItemAction action = new(city.Id, swordItem.DefinitionId, Price: 800);
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
    public void AiCannotAffordTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(5, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([swordItem])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(12, 12),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0))
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldDiplomacyTradeItemAction action = new(city.Id, swordItem.DefinitionId, Price: 100);
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, action);

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, action),
            [
                new WorldDiplomacyProposalSentEvent(proposal),
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: false)
            ]);
    }

    [Test]
    public void HumanAcceptsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
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

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(5, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([swordItem])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(12, 12),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldState before = engine.State;
        WorldDiplomacyTradeItemAction action = new(city.Id, swordItem.DefinitionId, Price: 100);
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, action);

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, action),
            [new WorldDiplomacyProposalSentEvent(proposal)]);

        engine.CheckCommand(
            new WorldAnswerDiplomacyProposalCommand(proposal, Accepted: true),
            [
                new WorldDiplomacyProposalAnsweredEvent(proposal, Accepted: true),
                new WorldDiplomacyItemTradedEvent(player1.Id, player2.Id, city.Id, swordItem, 100),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 42)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0) })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0) }),
                Fortifications = before.Fortifications
                    .SetItem(city.Id, before.Fortifications[city.Id] with { Inventory = WorldInventory.Empty })
                    .SetItem(capital.Id, before.Fortifications[capital.Id] with
                    {
                        Inventory = new WorldInventory([swordItem])
                    }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 42 })
            }));
    }

    [Test]
    public void HumanCannotAffordTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(5, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([swordItem])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(12, 12),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldDiplomacyTradeItemAction action = new(city.Id, swordItem.DefinitionId, Price: 100);
        WorldDiplomacyProposal proposal = new(player1.Id, player2.Id, action);

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, action),
            [new WorldDiplomacyProposalSentEvent(proposal)]);

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldAnswerDiplomacyProposalCommand(proposal, Accepted: true),
            WorldCommandError.InsufficientResources);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void MissingItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

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

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(5, 5),
            UnitGrid<WorldUnit>.Empty);
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(12, 12),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyTradeItemAction(city.Id, swordItem.DefinitionId, Price: 100)),
            WorldCommandError.ItemNotFound);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void ForeignFortificationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

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

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(5, 5),
            UnitGrid<WorldUnit>.Empty);
        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital"),
                player2.Id,
                new WorldPosition(12, 12),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([swordItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyTradeItemAction(capital.Id, swordItem.DefinitionId, Price: 100)),
            WorldCommandError.FortificationNotFound);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void NegativePriceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("sword-name"),
            new GlobalTextId("sword-description"),
            ItemCategory.Weapon,
            new WorldResourceSet(200, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        WorldItem swordItem = new(new WorldItemId("Sword"), sword.Id, Charges: null);

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

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
                new WorldFortificationId("city"),
                cityDefinition,
                player1.Id,
                new WorldPosition(5, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([swordItem])
            };
        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player2.Id,
            new WorldPosition(12, 12),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyTradeItemAction(city.Id, swordItem.DefinitionId, Price: -1)),
            WorldCommandError.DiplomacyPriceInvalid);

        Assert.That(engine.State, Is.EqualTo(before));
    }
}
