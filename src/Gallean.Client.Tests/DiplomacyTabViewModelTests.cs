using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class DiplomacyTabViewModelTests
{
    private static IReadOnlyList<string> FormatPlayers(IReadOnlyList<DiplomacyPlayerViewModel> players)
    {
        List<string> rows = [];

        foreach (DiplomacyPlayerViewModel player in players)
        {
            rows.Add($"{player.Name} | {player.RaceName} | {player.PortraitKey.CanonicalId} | {player.LargePortraitKey.CanonicalId} | {player.RelationText} | {player.StatusText} | {FormatSelected(player.IsSelected)}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatActions(IReadOnlyList<DiplomacyActionViewModel> actions)
    {
        List<string> rows = [];

        foreach (DiplomacyActionViewModel action in actions)
        {
            rows.Add($"{action.Name} | {FormatAvailability(action.Status.UnavailableReason)}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatOffers(IReadOnlyList<DiplomacyOfferViewModel> offers)
    {
        List<string> rows = [];

        foreach (DiplomacyOfferViewModel offer in offers)
        {
            rows.Add($"{offer.Name} | {offer.Description} | Cost: {WorldResourceFormatter.Format(offer.Cost.Resources)} | {FormatAvailability(offer.Status.UnavailableReason)}");
        }

        return rows;
    }

    private static string FormatSelected(bool isSelected)
    {
        return isSelected ? "selected" : "not selected";
    }

    private static string FormatAvailability(string? unavailableReason)
    {
        if (unavailableReason is null)
        {
            return "available";
        }
        else
        {
            return $"unavailable: {unavailableReason}";
        }
    }

    [Test]
    public void ActionsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("Sword"),
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
            new GlobalTextId("Heal"),
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
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([], [heal.Id])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            NameTextId = new ScenarioTextId("Ydrazel"),
            IsAi = true,
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 70,
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

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0))
            .WithDefinition(sword)
            .WithDefinition(heal)
            .WithDefinition(cityDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Heretic, new GlobalTextId("Legions of the Damned"), WorldMapTerrain.Heretic))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        DiplomacyTabViewModel diplomacy = new(gameModel, gameViewModel);

        Assert.That(diplomacy.TabHeader, Is.EqualTo("Diplomacy"));
        Assert.That(diplomacy.Step, Is.EqualTo(DiplomacyStep.Actions));
        Assert.That(FormatPlayers(diplomacy.Players), Is.EqualTo([
            "Ydrazel | Legions of the Damned | lord-face/heretic/0/small | lord-face/heretic/0/large | Peace (80) |  | selected"
        ]));
        Assert.That(FormatActions(diplomacy.Actions), Is.EqualTo([
            "Sell item | available",
            "Sell spell | available",
            "Give gold | available",
            "Offer alliance | available",
            "Exchange map information | available",
            "Break alliance | unavailable: You are not allied with that race"
        ]));
    }

    [Test]
    public async Task OfferAllianceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            NameTextId = new ScenarioTextId("Ydrazel"),
            IsAi = true,
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 70,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Heretic, new GlobalTextId("Legions of the Damned"), WorldMapTerrain.Heretic))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        DiplomacyTabViewModel diplomacy = new(gameModel, gameViewModel);

        diplomacy.SelectAction(diplomacy.Actions[1]);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Do you want to ask the Legions of the Damned for an alliance?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Alert alert = Assert.NotNull(diplomacy.Game.Alert.Current);
        Assert.That(alert, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Exchange, "Offer accepted", "The Legions of the Damned is now your ally")));
        Assert.That(session.World.State.Relations[new WorldRelationId(WorldRace.Undead, WorldRace.Heretic)].Alliance, Is.True);
        Assert.That(session.World.State.Players[player1.Id].SpiedPlayers, Is.EqualTo([player2.Id]));
        Assert.That(FormatPlayers(diplomacy.Players), Is.EqualTo([
            "Ydrazel | Legions of the Damned | lord-face/heretic/0/small | lord-face/heretic/0/large | Peace (100) | Allied | selected"
        ]));
        Assert.That(FormatActions(diplomacy.Actions), Is.EqualTo([
            "Give gold | available",
            "Break alliance | available",
            "Exchange map information | available",
            "Sell item | unavailable: You have no items to trade",
            "Sell spell | unavailable: You have no spells available to sell.",
            "Offer alliance | unavailable: You are already allied with that race"
        ]));
    }

    [Test]
    public async Task TradeItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        ItemDefinition sword = new(
            new ItemDefinitionId("sword"),
            new GlobalTextId("Sword"),
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
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            NameTextId = new ScenarioTextId("Ydrazel"),
            IsAi = true,
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 70,
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

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(74))
            .WithDefinition(sword)
            .WithDefinition(cityDefinition)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Heretic, new GlobalTextId("Legions of the Damned"), WorldMapTerrain.Heretic))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithCity(city)
            .WithCapital(capital)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        DiplomacyTabViewModel diplomacy = new(gameModel, gameViewModel);

        diplomacy.SelectAction(diplomacy.Actions[0]);

        Assert.That(diplomacy.Step, Is.EqualTo(DiplomacyStep.Offer));
        Assert.That(FormatOffers(diplomacy.Offers), Is.EqualTo([
            "Sword | sword-description | Cost: 200 gold | available"
        ]));

        diplomacy.SelectOffer(diplomacy.Offers[0]);

        Assert.That(diplomacy.Step, Is.EqualTo(DiplomacyStep.Amount));
        Assert.That(diplomacy.PromptText, Is.EqualTo("Name a price for Sword. It is worth 200 gold."));
        Assert.That(diplomacy.AmountText, Is.EqualTo("200"));

        diplomacy.AmountText = "100";
        await diplomacy.ConfirmAmountCommand.ExecuteAsync(null);

        Assert.That(diplomacy.Step, Is.EqualTo(DiplomacyStep.Actions));
        Alert alert = Assert.NotNull(diplomacy.Game.Alert.Current);
        Assert.That(alert, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Exchange, "Offer accepted", "The Legions of the Damned paid 100 gold")));
        Assert.That(session.World.State.Fortifications[capital.Id].Inventory, Is.EqualTo(new WorldInventory([swordItem])));
        Assert.That(session.World.State.Fortifications[city.Id].Inventory, Is.EqualTo(WorldInventory.Empty));
        Assert.That(session.World.State.Players[player1.Id].Resources, Is.EqualTo(new WorldResourceSet(200, 0, 0, 0, 0, 0)));
        Assert.That(session.World.State.Players[player2.Id].Resources, Is.EqualTo(new WorldResourceSet(400, 0, 0, 0, 0, 0)));
        Assert.That(FormatActions(diplomacy.Actions), Is.EqualTo([
            "Give gold | available",
            "Offer alliance | available",
            "Exchange map information | available",
            "Sell item | unavailable: You have no items to trade",
            "Sell spell | unavailable: You have no spells available to sell.",
            "Break alliance | unavailable: You are not allied with that race"
        ]));
    }

    [Test]
    public async Task GiveGoldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            NameTextId = new ScenarioTextId("Ydrazel"),
            IsAi = true,
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 80,
            Previous: 70,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Heretic, new GlobalTextId("Legions of the Damned"), WorldMapTerrain.Heretic))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        DiplomacyTabViewModel diplomacy = new(gameModel, gameViewModel);

        diplomacy.SelectAction(diplomacy.Actions[0]);

        Assert.That(diplomacy.Step, Is.EqualTo(DiplomacyStep.Amount));
        Assert.That(diplomacy.PromptText, Is.EqualTo("How much gold do you want to give the Legions of the Damned?"));
        Assert.That(diplomacy.AmountText, Is.EqualTo("100"));

        diplomacy.AmountText = "abc";
        await diplomacy.ConfirmAmountCommand.ExecuteAsync(null);

        Assert.That(diplomacy.Step, Is.EqualTo(DiplomacyStep.Amount));
        Alert alert = Assert.NotNull(diplomacy.Game.Alert.Current);
        Assert.That(alert, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "Please enter a valid amount of gold", Details: null)));

        diplomacy.AmountText = "50";
        await diplomacy.ConfirmAmountCommand.ExecuteAsync(null);

        Assert.That(diplomacy.Step, Is.EqualTo(DiplomacyStep.Actions));
        Alert info = Assert.NotNull(diplomacy.Game.Alert.Current);
        Assert.That(info, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Gold, "Gold given", "50 gold to the Legions of the Damned")));
        Assert.That(session.World.State.Players[player1.Id].Resources, Is.EqualTo(new WorldResourceSet(50, 0, 0, 0, 0, 0)));
        Assert.That(session.World.State.Players[player2.Id].Resources, Is.EqualTo(new WorldResourceSet(550, 0, 0, 0, 0, 0)));
    }

    [Test]
    public async Task BreakPermanentAllianceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            NameTextId = new ScenarioTextId("Ydrazel"),
            IsAi = true
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: true);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithRace(TestHelpers.SimpleRace(WorldRace.Heretic, new GlobalTextId("Legions of the Damned"), WorldMapTerrain.Heretic))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        DiplomacyTabViewModel diplomacy = new(gameModel, gameViewModel);

        Assert.That(FormatPlayers(diplomacy.Players), Is.EqualTo([
            "Ydrazel | Legions of the Damned | lord-face/heretic/0/small | lord-face/heretic/0/large | Peace (100) | Allied | selected"
        ]));

        Assert.That(FormatActions(diplomacy.Actions), Is.EqualTo([
            "Break alliance | available",
            "Exchange map information | available",
            "Sell item | unavailable: You have no items to trade",
            "Sell spell | unavailable: You have no spells available to sell.",
            "Give gold | unavailable: Not enough resources",
            "Offer alliance | unavailable: You are already allied with that race"
        ]));

        diplomacy.SelectAction(diplomacy.Actions[0]);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        Assert.That(prompt.Message, Is.EqualTo(TextSpans.FromText("Breaking the alliance will greatly offend the Legions of the Damned. Do you want to end the alliance and go to war?")));

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Alert alert = Assert.NotNull(diplomacy.Game.Alert.Current);
        Assert.That(alert, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Banner, "Alliance broken", "At war with the Legions of the Damned")));
        Assert.That(FormatPlayers(diplomacy.Players), Is.EqualTo([
            "Ydrazel | Legions of the Damned | lord-face/heretic/0/small | lord-face/heretic/0/large | War (10) |  | selected"
        ]));
    }
}
