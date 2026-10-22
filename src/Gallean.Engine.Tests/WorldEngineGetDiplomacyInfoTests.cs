namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetDiplomacyInfoTests
{
    [Test]
    public void PlayersTest()
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
            PurchaseCost: new WorldResourceSet(500, 0, 0, 0, 0, 0),
            AreaRadius: 1,
            new WorldSummonStackSpellEffect(new UnitDefinitionId("imp")),
            AiType: WorldAiSpellType.All);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([], [heal.Id, summon.Id])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };
        WorldPlayer player3 = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map) with
        {
            IsAi = true,
            Spellbook = new WorldPlayerSpellbook([], [heal.Id])
        };
        WorldPlayer player4 = TestHelpers.SimplePlayer(new WorldPlayerId(4), WorldRace.Neutral, map);

        WorldRelation hereticRelation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 70,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        WorldRelation humanRelation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Human),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: true);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(new WorldFortificationId("city"), cityDefinition, player1.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty) with
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
            .WithDefinition(heal)
            .WithDefinition(summon)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(player3.LordId, player3.Race, LordCategory.Warrior))
            .WithPlayer(player3)
            .WithDefinition(TestHelpers.SimpleLord(player4.LordId, player4.Race, LordCategory.Warrior))
            .WithPlayer(player4)
            .WithRelation(hereticRelation)
            .WithRelation(humanRelation)
            .WithCity(city)
            .WithCapital(capital)
            .Build();

        WorldGetDiplomacyInfoCommand.Result result = engine.CheckCommand(new WorldGetDiplomacyInfoCommand(), []);

        Assert.That(result, Is.EqualTo(new WorldGetDiplomacyInfoCommand.Result(
        [
            new WorldGetDiplomacyInfoCommand.Player(
                player2.Id,
                hereticRelation,
                WorldRelationLevel.Peace,
                GiveGoldError: null,
                ProposeAllianceError: null,
                BreakAllianceError: WorldCommandError.DiplomacyAllianceRequired,
                ExchangeMapError: null,
                [new WorldGetDiplomacyInfoCommand.TradeItem(city.Id, swordItem.DefinitionId, 200, Error: null)],
                [
                    new WorldGetDiplomacyInfoCommand.TradeSpell(heal.Id, 300, Error: null),
                    new WorldGetDiplomacyInfoCommand.TradeSpell(summon.Id, 500, WorldCommandError.DiplomacySpellNotTradable)
                ]),
            new WorldGetDiplomacyInfoCommand.Player(
                player3.Id,
                humanRelation,
                WorldRelationLevel.Peace,
                GiveGoldError: null,
                ProposeAllianceError: WorldCommandError.DiplomacyAlreadyAllied,
                BreakAllianceError: null,
                ExchangeMapError: null,
                [new WorldGetDiplomacyInfoCommand.TradeItem(city.Id, swordItem.DefinitionId, 200, WorldCommandError.CapitalNotFound)],
                [
                    new WorldGetDiplomacyInfoCommand.TradeSpell(heal.Id, 300, WorldCommandError.SpellAlreadyKnown),
                    new WorldGetDiplomacyInfoCommand.TradeSpell(summon.Id, 500, WorldCommandError.DiplomacySpellNotTradable)
                ])
        ])));
    }
}
