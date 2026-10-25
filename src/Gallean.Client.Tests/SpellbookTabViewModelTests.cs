using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class SpellbookTabViewModelTests
{
    private static IReadOnlyList<string> FormatSpellbook(IReadOnlyList<SpellbookLevelViewModel> levels)
    {
        List<string> rows = [];

        foreach (SpellbookLevelViewModel level in levels)
        {
            foreach (SpellbookItemViewModel item in level.Spells)
            {
                rows.Add($"{level.Title} | {item.SpellId.Value} | {item.Details.Name} | {item.Details.Description} | {item.Type} — {FormatPrice(Assert.NotNull(item.Status.Price))} | {FormatAvailability(item.Status.UnavailableReason)}");
            }
        }

        return rows;

        static string FormatAvailability(string? unavailableReason)
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
    }

    private static string FormatPrice(OfferPriceViewModel price)
    {
        List<string> items = [];

        foreach (OfferPriceItemViewModel item in price.Items)
        {
            if (item.IsUnaffordable)
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()} (unaffordable)");
            }
            else
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()}");
            }
        }

        return string.Join(", ", items);
    }

    private static string FormatBuilding(SpellbookBuildingViewModel building)
    {
        if (building.Status.UnavailableReason is null)
        {
            return $"{building.BuildingId.Value} | {building.Name} | {FormatPrice(Assert.NotNull(building.Status.Price))} | available";
        }
        else
        {
            return $"{building.BuildingId.Value} | {building.Name} | {FormatPrice(Assert.NotNull(building.Status.Price))} | unavailable: {building.Status.UnavailableReason}";
        }
    }

    [Test]
    public async Task ResearchSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId restoreId = new("restore");
        WorldSpellDefinitionId hasteId = new("haste");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = new(
            Id: mageGuildId,
            NameTextId: new GlobalTextId("Mage guild"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 2,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        WorldSpellDefinition haste = new(
            hasteId,
            new GlobalTextId("Haste"),
            new GlobalTextId("Hastens the stack"),
            new GlobalTextId("+ 100% Move points"),
            WorldRace.Undead,
            Level: 2,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            Effect: new WorldRestoreMovementSpellEffect(Percent: 50),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([restoreId, hasteId], [])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(mageGuild)
            .WithDefinition(restore)
            .WithDefinition(haste)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, restoreId, new WorldResourceSet(200, 0, 0, 0, 0, 0)))
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, hasteId, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo([
                "Level 1 | restore | Restoration | Restores movement points | Researchable — 200 gold | available",
                "Level 2 | haste | Haste | Hastens the stack | Researchable — 100 gold | available"
            ]));

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);
        await spellbook.ActivateCommand.ExecuteAsync(null);

        WorldPlayer updatedPlayer = session.World.State.Players[player.Id];
        Assert.That(updatedPlayer.Spellbook.Acquired, Is.EqualTo([restoreId]));
        Assert.That(updatedPlayer.Resources, Is.EqualTo(new WorldResourceSet(400, 25, 0, 0, 0, 0)));

        Assert.That(spellbook.Game.Alert.Current, Is.Null);
        Assert.That(spellbook.SelectedSpell, Is.Null);
        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo([
                "Level 1 | restore | Restoration | Restores movement points | Known — 0 gold | available",
                "Level 2 | haste | Haste | Hastens the stack | Researchable — 100 gold | unavailable: You have already researched a spell this turn"
            ]));
    }

    [Test]
    public void ResearchWithoutGuildTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([restoreId], [])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(restore)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, restoreId, new WorldResourceSet(200, 0, 0, 0, 0, 0)))
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo(["Level 1 | restore | Restoration | Restores movement points | Researchable — 200 gold | unavailable: Magic tower must be built in your capital"]));
    }

    [Test]
    public async Task ResearchAfterGuildTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId restoreId = new("restore");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = new(
            Id: mageGuildId,
            NameTextId: new GlobalTextId("Mage guild"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], []),
            Spellbook = new WorldPlayerSpellbook([restoreId], [])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(mageGuild)
            .WithDefinition(restore)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, restoreId, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();

        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);
        SpellbookBuildingViewModel building = Assert.NotNull(spellbook.RequiredBuilding);

        Assert.That(FormatBuilding(building), Is.EqualTo("MageGuild | Mage guild | 200 gold | available"));

        await building.ConstructCommand.ExecuteAsync(null);

        Assert.That(spellbook.RequiredBuilding, Is.Null);
        Assert.That(spellbook.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Capital, "Building constructed", "Mage guild")));
        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo(["Level 1 | restore | Restoration | Restores movement points | Researchable — 100 gold | available"]));

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);
        await spellbook.ActivateCommand.ExecuteAsync(null);

        Assert.That(gameModel.Snapshot.WorldState.Players[new WorldPlayerId(1)].Spellbook.Acquired, Is.EqualTo([restoreId]));
    }

    [Test]
    public async Task CastSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(10),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);

        Assert.That(session.World.State.Stacks[stack.Id].MovementPoints, Is.EqualTo(8));

        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.OpenSpellbookCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo(["Level 1 | restore | Restoration | Restores movement points | Known — 0 gold | available"]));

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);
        await spellbook.ActivateCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.ActionPromptText, Is.EqualTo("Select a target for the spell on the map."));

        await world.HandleMapClickAsync(new WorldPosition(1, 0), null, 1);

        Assert.That(session.World.State.Stacks[stack.Id].MovementPoints, Is.EqualTo(9));
        Assert.That(world.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task CancelTargetingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.OpenSpellbookCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);
        await spellbook.ActivateCommand.ExecuteAsync(null);

        world.CancelSelectedActionCommand.Execute(null);

        Assert.That(gameViewModel.Alert.Current, Is.Null);

        await world.HandleMapClickAsync(new WorldPosition(1, 0), null, 1);

        Assert.That(session.World.State.Stacks[stack.Id].MovementPoints, Is.EqualTo(8));
    }

    [Test]
    public async Task EscapeCancelsTargetingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.OpenSpellbookCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);
        await spellbook.ActivateCommand.ExecuteAsync(null);

        gameViewModel.EscapeCommand.Execute(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.ActionPromptText, Is.Null);

        await world.HandleMapClickAsync(new WorldPosition(1, 0), null, 1);

        Assert.That(session.World.State.Stacks[stack.Id].MovementPoints, Is.EqualTo(8));
    }

    [Test]
    public async Task CastSelectedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.OpenSpellbookCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);
        await spellbook.CastSelectedCommand.ExecuteAsync(null);

        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
        Assert.That(world.ActionPromptText, Is.EqualTo("Select a target for the spell on the map."));
    }

    [Test]
    public void CoveredCardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.OpenSpellbookCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(spellbook.IsInForeground, Is.True);

        gameViewModel.ShowDetails(spellbook.Levels[0].Spells[0].Details);

        Assert.That(spellbook.IsInForeground, Is.False);

        gameViewModel.HideDetails();

        Assert.That(spellbook.IsInForeground, Is.True);

        gameViewModel.Alert.Show(new Alert(AlertType.Error, IconMoniker.Warning, "This spell cannot be cast on that target", Details: null));

        Assert.That(spellbook.IsInForeground, Is.False);

        gameViewModel.Alert.Dismiss();

        Assert.That(spellbook.IsInForeground, Is.True);
    }

    [Test]
    public async Task InvalidTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        using GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();

        world.OpenSpellbookCommand.Execute(null);

        ManagementScreenViewModel screen = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);
        await spellbook.ActivateCommand.ExecuteAsync(null);
        await world.HandleMapClickAsync(new WorldPosition(1, 0), null, 1);

        Alert alert = Assert.NotNull(world.Game.Alert.Current);
        Assert.That(alert, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "This spell cannot be cast on that target", Details: null)));
    }

    [Test]
    public void NotCastableSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId hasteId = new("haste");

        WorldSpellDefinition haste = new(
            hasteId,
            new GlobalTextId("Haste"),
            new GlobalTextId("Hastens the stack"),
            new GlobalTextId("+ 100% Move points"),
            WorldRace.Undead,
            Level: 2,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            Effect: new WorldRestoreMovementSpellEffect(Percent: 50),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [hasteId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(haste)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo(["Level 2 | haste | Haste | Hastens the stack | Known — 0 gold | available"]));
    }

    [Test]
    public void UnaffordableSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            new WorldResourceSet(0, 0, 0, 40, 0, 0),
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 10, 0, 0),
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo(["Level 1 | restore | Restoration | Restores movement points | Known — 40 life mana (unaffordable) | unavailable: Not enough resources"]));
    }

    [Test]
    public async Task MageLordResearchCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId restoreId = new("restore");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = new(
            Id: mageGuildId,
            NameTextId: new GlobalTextId("Mage guild"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 2,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 300, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([restoreId], [])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithDefinition(mageGuild)
            .WithDefinition(restore)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, restoreId, new WorldResourceSet(200, 100, 0, 0, 0, 0)))
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo(["Level 1 | restore | Restoration | Restores movement points | Researchable — 100 gold, 50 death mana | available"]));

        Assert.That(
            session.World.State.Players[player.Id].Resources,
            Is.EqualTo(new WorldResourceSet(600, 325, 0, 0, 0, 0)));

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);
        await spellbook.ActivateCommand.ExecuteAsync(null);

        Assert.That(
            session.World.State.Players[player.Id].Resources,
            Is.EqualTo(new WorldResourceSet(500, 275, 0, 0, 0, 0)));
    }

    [Test]
    public void WarriorLordResearchCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId restoreId = new("restore");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = new(
            Id: mageGuildId,
            NameTextId: new GlobalTextId("Mage guild"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 2,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 300, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([restoreId], [])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(mageGuild)
            .WithDefinition(restore)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, restoreId, new WorldResourceSet(200, 100, 0, 0, 0, 0)))
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo(["Level 1 | restore | Restoration | Restores movement points | Researchable — 200 gold, 100 death mana | available"]));
    }

    [Test]
    public void AlreadyCastSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldSpellDefinitionId restoreId = new("restore");

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(10),
            AiType: WorldAiSpellType.All);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [restoreId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(restore)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        session.World.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)));
        session.World.DispatchCommand(new WorldCastSpellCommand(restoreId, new WorldSpellTarget(new WorldPosition(1, 0), Radius: 0)));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo(["Level 1 | restore | Restoration | Restores movement points | Known — 0 gold | unavailable: This spell has already been cast this turn"]));
    }

    [Test]
    public void SpellBuildingConstructedThisTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");
        WorldPlayerBuildingDefinitionId templeId = new("Temple");

        WorldPlayerBuildingDefinition mageGuild = new(
            Id: mageGuildId,
            NameTextId: new GlobalTextId("Mage guild"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayerBuildingDefinition temple = new(
            Id: templeId,
            NameTextId: new GlobalTextId("Temple"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(100, 0, 0, 0, 0, 0),
            UnitIds: []);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId, templeId], [])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(mageGuild)
            .WithDefinition(temple)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        session.World.DispatchCommand(new WorldConstructBuildingCommand(player.Id, templeId));

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);
        SpellbookBuildingViewModel building = Assert.NotNull(spellbook.RequiredBuilding);

        Assert.That(
            FormatBuilding(building),
            Is.EqualTo("MageGuild | Mage guild | 200 gold | unavailable: You have already constructed a building this turn"));
        Assert.That(building.ConstructCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void LockedSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId restoreId = new("restore");
        WorldSpellDefinitionId hasteId = new("haste");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = new(
            Id: mageGuildId,
            NameTextId: new GlobalTextId("Mage guild"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Spell,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldSpellDefinition restore = new(
            restoreId,
            new GlobalTextId("Restoration"),
            new GlobalTextId("Restores movement points"),
            new GlobalTextId("+ 50% Move points"),
            WorldRace.Undead,
            Level: 1,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);

        WorldSpellDefinition haste = new(
            hasteId,
            new GlobalTextId("Haste"),
            new GlobalTextId("Hastens the stack"),
            new GlobalTextId("+ 100% Move points"),
            WorldRace.Undead,
            Level: 5,
            WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: 0,
            new WorldRestoreMovementSpellEffect(100),
            AiType: WorldAiSpellType.All);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([restoreId], [])
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(mageGuild)
            .WithDefinition(restore)
            .WithDefinition(haste)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, restoreId, new WorldResourceSet(200, 0, 0, 0, 0, 0)))
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, hasteId, new WorldResourceSet(900, 0, 0, 0, 0, 0)))
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForSpellbook(gameModel, gameViewModel);
        SpellbookTabViewModel spellbook = Assert.IsOfType<SpellbookTabViewModel>(screen.Tabs[0]);

        Assert.That(
            FormatSpellbook(spellbook.Levels),
            Is.EqualTo([
                "Level 1 | restore | Restoration | Restores movement points | Researchable — 200 gold | available",
                "Level 5 | haste | Haste | Hastens the stack | Locked — 0 gold | unavailable: Not available in this quest"
            ]));
        Assert.That(spellbook.ResearchedCountText, Is.EqualTo("0 of 2 spells researched"));
        Assert.That(spellbook.SelectedSpell, Is.Null);
        Assert.That(spellbook.ActivateCommand.CanExecute(null), Is.False);

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[0].Spells[0]);

        Assert.That(spellbook.SelectedSpell, Is.SameAs(spellbook.Levels[0].Spells[0]));
        Assert.That(spellbook.ActivateCommand.CanExecute(null), Is.True);
        Assert.That(spellbook.CastSelectedCommand.CanExecute(null), Is.False);

        spellbook.SelectSpellCommand.Execute(spellbook.Levels[1].Spells[0]);

        Assert.That(spellbook.SelectedSpell, Is.SameAs(spellbook.Levels[1].Spells[0]));
        Assert.That(spellbook.ActivateCommand.CanExecute(null), Is.False);

        spellbook.ClearSelectionCommand.Execute(null);

        Assert.That(spellbook.SelectedSpell, Is.Null);
    }
}
