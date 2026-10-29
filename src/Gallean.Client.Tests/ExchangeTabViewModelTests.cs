using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class ExchangeTabViewModelTests
{
    private static UnitGridSlotViewModel FindSlot(UnitGridViewModel grid, GridPosition position)
    {
        foreach (UnitGridSlotViewModel slot in grid.Slots)
        {
            if (slot.Position == position)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"Slot ({position.Line}, {position.Flank}) was not found.");
    }

    [Test]
    public async Task UnitToOtherStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Hugin"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(otherLeader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel source = FindSlot(screen.StackGrid, GridPosition.FrontCenter);
        UnitGridSlotViewModel target = FindSlot(screen.OtherStackGrid, GridPosition.FrontCenter);

        screen.DragDrop.BeginDrag(source.Reference);

        Assert.That(screen.StackGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([true, false, true, true, true, true]));
        Assert.That(screen.OtherStackGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, true, true, true, true, true]));

        await screen.DragDrop.DropAsync(target.Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];
        WorldStack updatedOtherStack = session.World.State.Stacks[otherStack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)])
        }));
        Assert.That(updatedOtherStack, Is.EqualTo(otherStack with
        {
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (otherLeader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task UnitRepositionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Hugin"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(otherLeader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel source = FindSlot(screen.StackGrid, GridPosition.FrontCenter);
        UnitGridSlotViewModel target = FindSlot(screen.StackGrid, GridPosition.BackCenter);

        screen.DragDrop.BeginDrag(source.Reference);
        await screen.DragDrop.DropAsync(target.Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.BackCenter)
                ])
        }));
        Assert.That(session.World.State.Stacks[otherStack.Id], Is.EqualTo(otherStack));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task ItemToOtherStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        ItemDefinition potionDefinition = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Potion"),
            new GlobalTextId("Potion description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potionDefinition.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(leader, GridPosition.FrontBottom)]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Hugin"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(otherLeader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        InventoryItemViewModel source = screen.InventoryTransfer.Inventory.Items[0];
        InventoryViewModel target = screen.InventoryTransfer.OtherInventory;

        screen.DragDrop.BeginDrag(source.Reference);

        Assert.That(target.IsHighlighted, Is.True);
        Assert.That(screen.InventoryTransfer.Inventory.IsHighlighted, Is.False);

        await screen.DragDrop.DropAsync(target.DropReference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with { Inventory = WorldInventory.Empty }));
        Assert.That(session.World.State.Stacks[otherStack.Id], Is.EqualTo(otherStack with { Inventory = new WorldInventory([potionItem]) }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task ItemFromOtherStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        ItemDefinition potionDefinition = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Potion"),
            new GlobalTextId("Potion description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potionDefinition.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        WorldStack otherStack = TestHelpers.SimpleStack(
                new WorldStackId("Hugin"),
                player.Id,
                new WorldPosition(5, 4),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(otherLeader, GridPosition.FrontBottom)]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        InventoryItemViewModel source = screen.InventoryTransfer.OtherInventory.Items[0];
        InventoryViewModel target = screen.InventoryTransfer.Inventory;

        screen.DragDrop.BeginDrag(source.Reference);
        await screen.DragDrop.DropAsync(target.DropReference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with { Inventory = new WorldInventory([potionItem]) }));
        Assert.That(session.World.State.Stacks[otherStack.Id], Is.EqualTo(otherStack with { Inventory = WorldInventory.Empty }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public void BothStacksTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Hugin"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(otherLeader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        Assert.That(screen.TabHeader, Is.EqualTo("Exchange"));
        Assert.That(screen.Leader, Is.EqualTo(new StackLeaderViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
            "name",
            [
                new StatRowViewModel(IconMoniker.Hitpoint, "Hit points", new StatValueViewModel(TextSpans.FromText("100 / 100"), 0)),
                new StatRowViewModel(IconMoniker.Movement, "Movement", new StatValueViewModel(TextSpans.FromText("20 / 20"), 0)),
                new StatRowViewModel(IconMoniker.Stack, "Units", new StatValueViewModel(TextSpans.FromText("2 / 6"), 0)),
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel(TextSpans.FromText("3"), 0))
            ],
            new UnitDetailsViewModel(
                new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
                "name",
                "description",
                1,
                100,
                100,
                0,
                1000,
                false,
                false,
                false,
                false,
                new UnitAttackViewModel("Damage", new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, "Weapon", Theme.AttackSource.WeaponColor)),
                new StatValueViewModel(TextSpans.FromText("10"), 0),
                null,
                null,
                [
                    new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel(new TextSpans([new TextSpan("1", TextStyle.Bold), new TextSpan(" ", TextStyle.None), new TextSpan("(Ranged)", TextStyle.Italic)]), 0)),
                    new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel(TextSpans.FromText("0"), 0)),
                    new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel(TextSpans.FromText("100%"), 0)),
                    new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel(TextSpans.FromText("50"), 0))
                ],
                null,
                null,
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel(TextSpans.FromText("3"), 0)),
                [],
                []))));
        Assert.That(screen.OtherLeader, Is.EqualTo(new StackLeaderViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
            "name",
            [
                new StatRowViewModel(IconMoniker.Hitpoint, "Hit points", new StatValueViewModel(TextSpans.FromText("100 / 100"), 0)),
                new StatRowViewModel(IconMoniker.Movement, "Movement", new StatValueViewModel(TextSpans.FromText("20 / 20"), 0)),
                new StatRowViewModel(IconMoniker.Stack, "Units", new StatValueViewModel(TextSpans.FromText("1 / 6"), 0)),
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel(TextSpans.FromText("3"), 0))
            ],
            new UnitDetailsViewModel(
                new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
                "name",
                "description",
                1,
                100,
                100,
                0,
                1000,
                false,
                false,
                false,
                false,
                new UnitAttackViewModel("Damage", new UnitAttackSourceViewModel(IconMoniker.AttackSourceWeapon, "Weapon", Theme.AttackSource.WeaponColor)),
                new StatValueViewModel(TextSpans.FromText("10"), 0),
                null,
                null,
                [
                    new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel(new TextSpans([new TextSpan("1", TextStyle.Bold), new TextSpan(" ", TextStyle.None), new TextSpan("(Ranged)", TextStyle.Italic)]), 0)),
                    new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel(TextSpans.FromText("0"), 0)),
                    new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel(TextSpans.FromText("100%"), 0)),
                    new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel(TextSpans.FromText("50"), 0))
                ],
                null,
                null,
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel(TextSpans.FromText("3"), 0)),
                [],
                []))));
        Assert.That(FindSlot(screen.StackGrid, GridPosition.FrontBottom).HasUnit, Is.True);
        Assert.That(FindSlot(screen.StackGrid, GridPosition.FrontCenter).HasUnit, Is.True);
        Assert.That(FindSlot(screen.OtherStackGrid, GridPosition.FrontBottom).HasUnit, Is.True);
        Assert.That(FindSlot(screen.OtherStackGrid, GridPosition.FrontCenter).HasUnit, Is.False);
    }

    [Test]
    public async Task DisbandUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Hugin"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (otherLeader, GridPosition.FrontBottom),
                    (soldier, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        screen.DragDrop.BeginDrag(FindSlot(screen.StackGrid, GridPosition.FrontBottom).Reference);

        Assert.That(screen.DismissDropZone.IsVisible, Is.False);

        screen.DragDrop.EndDrag();
        screen.DragDrop.BeginDrag(FindSlot(screen.OtherStackGrid, GridPosition.FrontCenter).Reference);

        Assert.That(screen.DismissDropZone.IsVisible, Is.True);

        await screen.DragDrop.DropAsync(screen.DismissDropZone.DropReference);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack));
        Assert.That(session.World.State.Stacks[otherStack.Id], Is.EqualTo(otherStack with
        {
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(otherLeader, GridPosition.FrontBottom)])
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.TopScreen, Is.Null);
            Assert.That(screen.DismissDropZone.IsVisible, Is.False);
            Assert.That(screen.Game.Alert.Current, Is.Null);
        }
    }

    [Test]
    public async Task ItemGroupToOtherStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        ItemDefinition potionDefinition = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Potion"),
            new GlobalTextId("Potion description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
        ItemDefinition elixirDefinition = new(
            new ItemDefinitionId("elixir"),
            new GlobalTextId("Elixir"),
            new GlobalTextId("Elixir description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem firstPotionItem = new(new WorldItemId("Potion1"), potionDefinition.Id, Charges: null);
        WorldItem elixirItem = new(new WorldItemId("Elixir"), elixirDefinition.Id, Charges: null);
        WorldItem secondPotionItem = new(new WorldItemId("Potion2"), potionDefinition.Id, Charges: null);
        WorldItem thirdPotionItem = new(new WorldItemId("Potion3"), potionDefinition.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(leader, GridPosition.FrontBottom)]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([firstPotionItem, elixirItem, secondPotionItem, thirdPotionItem])
            };

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Hugin"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(otherLeader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithDefinition(elixirDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        screen.DragDrop.BeginDrag(screen.InventoryTransfer.Inventory.Items[0].GroupReference);
        await screen.DragDrop.DropAsync(screen.InventoryTransfer.OtherInventory.DropReference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with { Inventory = new WorldInventory([elixirItem]) }));
        Assert.That(session.World.State.Stacks[otherStack.Id], Is.EqualTo(otherStack with { Inventory = new WorldInventory([firstPotionItem, secondPotionItem, thirdPotionItem]) }));
        Assert.That(screen.InventoryTransfer.Inventory.Items.Select(item => (item.Index, item.ItemId, item.Count)), Is.EqualTo([
            (Index: 0, new WorldItemId("Elixir"), Count: 1)
        ]));
        Assert.That(screen.InventoryTransfer.OtherInventory.Items.Select(item => (item.Index, item.ItemId, item.Count)), Is.EqualTo([
            (Index: 0, new WorldItemId("Potion3"), Count: 3)
        ]));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task TransferAllItemsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        ItemDefinition potionDefinition = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Potion"),
            new GlobalTextId("Potion description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potionDefinition.Id, Charges: null);
        WorldItem secondPotionItem = new(new WorldItemId("SecondPotion"), potionDefinition.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(leader, GridPosition.FrontBottom)]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem, secondPotionItem])
            };

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Hugin"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(otherLeader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        Assert.That(screen.TransferAllCommand.CanExecute(InventoryTransferDirection.ToOther), Is.True);
        Assert.That(screen.TransferAllCommand.CanExecute(InventoryTransferDirection.FromOther), Is.False);

        await screen.TransferAllCommand.ExecuteAsync(InventoryTransferDirection.ToOther);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with { Inventory = WorldInventory.Empty }));
        Assert.That(session.World.State.Stacks[otherStack.Id], Is.EqualTo(otherStack with { Inventory = new WorldInventory([potionItem, secondPotionItem]) }));
        Assert.That(screen.TransferAllCommand.CanExecute(InventoryTransferDirection.ToOther), Is.False);
        Assert.That(screen.TransferAllCommand.CanExecute(InventoryTransferDirection.FromOther), Is.True);
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task TransferItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit otherLeader = new(new WorldUnitId("otherLeader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        ItemDefinition potionDefinition = new(
            new ItemDefinitionId("potion"),
            new GlobalTextId("Potion"),
            new GlobalTextId("Potion description"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem potionItem = new(new WorldItemId("Potion"), potionDefinition.Id, Charges: null);
        WorldItem secondPotionItem = new(new WorldItemId("SecondPotion"), potionDefinition.Id, Charges: null);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [(leader, GridPosition.FrontBottom)]),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem, secondPotionItem])
            };

        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Hugin"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(otherLeader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potionDefinition)
            .WithStack(stack)
            .WithStack(otherStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new ExchangeTabViewModel(gameModel, gameViewModel, owner, stack.Id, otherStack.Id)]);
        ExchangeTabViewModel screen = Assert.IsOfType<ExchangeTabViewModel>(management.Tabs[0]);

        await screen.InventoryTransfer.TransferItemAsync(screen.InventoryTransfer.Inventory.Items[0].GroupReference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with { Inventory = WorldInventory.Empty }));
        Assert.That(session.World.State.Stacks[otherStack.Id], Is.EqualTo(otherStack with { Inventory = new WorldInventory([potionItem, secondPotionItem]) }));

        await screen.InventoryTransfer.TransferItemAsync(screen.InventoryTransfer.OtherInventory.Items[0].GroupReference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with { Inventory = new WorldInventory([potionItem, secondPotionItem]) }));
        Assert.That(session.World.State.Stacks[otherStack.Id], Is.EqualTo(otherStack with { Inventory = WorldInventory.Empty }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }
}
