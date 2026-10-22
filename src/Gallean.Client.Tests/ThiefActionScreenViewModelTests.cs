using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class ThiefActionScreenViewModelTests
{
    private static IReadOnlyList<string> FormatActions(IReadOnlyList<ThiefActionItemViewModel> actions)
    {
        List<string> rows = [];

        foreach (ThiefActionItemViewModel action in actions)
        {
            rows.Add($"{action.Name} | {action.Description} | {action.ChanceText.Text} | {FormatAvailability(action.Status.UnavailableReason)}");
        }

        return rows;
    }

    private static string FormatIcon(IconViewModel icon)
    {
        return icon switch
        {
            IconViewModel.Item item => item.Key.CanonicalId,
            IconViewModel.Unit unit => unit.Key.CanonicalId,
            IconViewModel.Spell spell => $"{spell.Key.CanonicalId} {spell.Background}",
            _ => throw new ArgumentOutOfRangeException(nameof(icon), icon, null)
        };
    }

    private static IReadOnlyList<string> FormatStealTargets(IReadOnlyList<ThiefStealItemViewModel> targets)
    {
        List<string> rows = [];

        foreach (ThiefStealItemViewModel target in targets)
        {
            rows.Add($"{FormatIcon(target.Icon)} | {target.Name} | {target.Description} | {FormatAvailability(target.Status.UnavailableReason)}");
        }

        return rows;
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
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemyLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemyLeader"), enemyLeader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(enemyLeader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ThiefActionScreenViewModel screen = new(gameModel, gameViewModel, thiefStack.Id, new WorldPosition(3, 2));

        Assert.That(FormatActions(screen.Actions), Is.EqualTo([
            "PoisonStack-name | PoisonStack-description | Chance of success: 90% | available",
            "Spy-name | Spy-description | Chance of success: 100% | available",
            "Duel-name | Duel-description | Chance of success: 100% | available",
            "StealItem-name | StealItem-description | Chance of success: 80% | unavailable: There is nothing to steal",
            "Assassinate-name | Assassinate-description | Chance of success: 50% | unavailable: This thief action is not available to your lord",
            "CounterfeitOrders-name | CounterfeitOrders-description | Chance of success: 60% | unavailable: This action cannot be performed on that target"
        ]));
    }

    [Test]
    public void NoTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ThiefActionScreenViewModel screen = new(gameModel, gameViewModel, thiefStack.Id, new WorldPosition(3, 2));

        Assert.That(screen.Actions, Is.Empty);
    }

    [Test]
    public async Task UnavailableActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemyLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            player2.Id,
            new WorldPosition(3, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemyLeader"), enemyLeader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(enemyLeader)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ThiefActionScreenViewModel screen = new(gameModel, gameViewModel, thiefStack.Id, new WorldPosition(3, 2));

        gameViewModel.OpenScreen(screen);

        WorldState before = session.World.State;

        await screen.Actions[3].SelectCommand.ExecuteAsync(null);

        Assert.That(session.World.State, Is.SameAs(before));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(screen));
    }

    [Test]
    public async Task ActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(199, 0, 0))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonCity, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealGold, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 70))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Bribe, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.RiotCity, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ThiefActionScreenViewModel screen = new(gameModel, gameViewModel, thiefStack.Id, city.Entrance);

        gameViewModel.OpenScreen(screen);

        Assert.That(FormatActions(screen.Actions), Is.EqualTo([
            "Spy-name | Spy-description | Chance of success: 100% | available",
            "PoisonCity-name | PoisonCity-description | Chance of success: 80% | available",
            "StealGold-name | StealGold-description | Chance of success: 70% | available",
            "Assassinate-name | Assassinate-description | Chance of success: 40% | available",
            "RiotCity-name | RiotCity-description | Chance of success: 100% | available",
            "StealSpell-name | StealSpell-description | Chance of success: 60% | unavailable: There is nothing to steal",
            "CounterfeitOrders-name | CounterfeitOrders-description | Chance of success: 50% | unavailable: This action cannot be performed on that target",
            "Bribe-name | Bribe-description | Chance of success: 100% | unavailable: This action cannot be performed on that target"
        ]));

        await screen.Actions[2].SelectCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Players[player1.Id].Resources, Is.EqualTo(new WorldResourceSet(300, 25, 0, 0, 0, 0)));
        Assert.That(session.World.State.Players[player2.Id].Resources, Is.EqualTo(new WorldResourceSet(800, 0, 0, 0, 0, 0)));
        Assert.That(gameViewModel.TopScreen, Is.Null);
    }

    [Test]
    public async Task StealItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemyLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);

        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("potion-name"),
            new GlobalTextId("potion-description"),
            ItemCategory.PotionHeal,
            new WorldResourceSet(50, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(3, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemyLeader"), enemyLeader), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(enemyLeader)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ThiefActionScreenViewModel screen = new(gameModel, gameViewModel, thiefStack.Id, new WorldPosition(3, 2));

        gameViewModel.OpenScreen(screen);

        await screen.Actions[2].SelectCommand.ExecuteAsync(null);

        ThiefStealScreenViewModel picker = Assert.IsOfType<ThiefStealScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatStealTargets(picker.Targets), Is.EqualTo([
            "item-icon/potion | potion-name | potion-description | available"
        ]));

        await picker.Targets[0].SelectCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[targetStack.Id].Inventory, Is.EqualTo(WorldInventory.Empty));
        Assert.That(session.World.State.Stacks[thiefStack.Id].Inventory, Is.EqualTo(
            new WorldInventory([potionItem])));
        Assert.That(gameViewModel.TopScreen, Is.Null);
    }

    [Test]
    public async Task StealSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();

        WorldSpellDefinition fog = new(
            new WorldSpellDefinitionId("fog"),
            new GlobalTextId("fog-name"),
            new GlobalTextId("fog-description"),
            new GlobalTextId("fog-effect"),
            WorldRace.Human,
            Level: 1,
            CastingCost: new WorldResourceSet(10, 0, 0, 0, 0, 0),
            PurchaseCost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            AreaRadius: 0,
            Effect: new WorldFogSpellEffect(),
            AiType: WorldAiSpellType.All);

        WorldSpellDefinition storm = new(
            new WorldSpellDefinitionId("storm"),
            new GlobalTextId("storm-name"),
            new GlobalTextId("storm-description"),
            new GlobalTextId("storm-effect"),
            WorldRace.Human,
            Level: 1,
            CastingCost: new WorldResourceSet(10, 0, 0, 0, 0, 0),
            PurchaseCost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            AreaRadius: 0,
            Effect: new WorldFogSpellEffect(),
            AiType: WorldAiSpellType.All);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [storm.Id])
        };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldMageSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [storm.Id, fog.Id],
            VisitorIds: [],
            AiPriority: 3);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(fog)
            .WithDefinition(storm)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMage, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ThiefActionScreenViewModel screen = new(gameModel, gameViewModel, thiefStack.Id, new WorldPosition(4, 4));

        gameViewModel.OpenScreen(screen);

        await screen.Actions[0].SelectCommand.ExecuteAsync(null);

        ThiefStealScreenViewModel picker = Assert.IsOfType<ThiefStealScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatStealTargets(picker.Targets), Is.EqualTo([
            "spell-icon/fog Light | fog-name | fog-description | available",
            "spell-icon/storm Light | storm-name | storm-description | unavailable: This spell is already known"
        ]));

        WorldState before = session.World.State;

        await picker.Targets[1].SelectCommand.ExecuteAsync(null);

        Assert.That(session.World.State, Is.SameAs(before));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(picker));

        await picker.Targets[0].SelectCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Players[player1.Id].Spellbook, Is.EqualTo(new WorldPlayerSpellbook([], [fog.Id, storm.Id])));
        Assert.That(gameViewModel.TopScreen, Is.Null);
    }

    [Test]
    public async Task CancelStealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition enemyLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);

        ItemDefinition potion = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("potion-name"),
            new GlobalTextId("potion-description"),
            ItemCategory.PotionHeal,
            new WorldResourceSet(50, 0, 0, 0, 0, 0),
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player1.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        WorldStack targetStack = TestHelpers.SimpleStack(
                new WorldStackId("enemy"),
                player2.Id,
                new WorldPosition(3, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemyLeader"), enemyLeader), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([new WorldItem(new WorldItemId("Potion"), potion.Id, Charges: null)])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(enemyLeader)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Spy, 100))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealItem, 80))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Assassinate, 50))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.CounterfeitOrders, 60))
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.Duel, 100))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ThiefActionScreenViewModel screen = new(gameModel, gameViewModel, thiefStack.Id, new WorldPosition(3, 2));

        gameViewModel.OpenScreen(screen);

        await screen.Actions[2].SelectCommand.ExecuteAsync(null);

        ThiefStealScreenViewModel picker = Assert.IsOfType<ThiefStealScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatStealTargets(picker.Targets), Is.EqualTo([
            "item-icon/potion | potion-name | potion-description | available"
        ]));

        picker.CloseCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(screen));
    }
}
