using System.Collections.Specialized;
using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class StackTabViewModelTests
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

    private static LeaderEquipmentSlotViewModel FindEquipmentSlot(StackTabViewModel screen, LeaderEquipmentSlot slot)
    {
        foreach (LeaderEquipmentSlotViewModel candidate in screen.Equipment.Slots)
        {
            if (candidate.Slot == slot)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Equipment slot {slot} was not found.");
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

    private static UnitModifierDefinition WeaponModifier()
    {
        return TestHelpers.SimpleModifier("weapon-mod") with { Damage = new UnitStatModifier(20, 0) };
    }

    private static ItemDefinition Weapon(UnitModifierDefinitionId modifierId)
    {
        return new ItemDefinition(
            new ItemDefinitionId("weapon1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Weapon,
            WorldResourceSet.Empty,
            EquipmentModifierId: modifierId,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);
    }

    private static ItemDefinition HealingPotion(ItemDefinitionId id, int hitpoints)
    {
        return new ItemDefinition(
            id,
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(hitpoints),
            AnimationUnitId: null);
    }

    [Test]
    public async Task RepositionsUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
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

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel source = FindSlot(screen.UnitGrid, GridPosition.FrontCenter);
        UnitGridSlotViewModel target = FindSlot(screen.UnitGrid, GridPosition.BackCenter);

        screen.DragDrop.TryBeginDrag(source.Reference);

        Assert.That(screen.UnitGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([true, false, true, true, true, true]));

        await screen.DragDrop.DropAsync(target.Reference);

        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontCenter), Is.Not.SameAs(source));
        Assert.That(FindSlot(screen.UnitGrid, GridPosition.BackCenter), Is.Not.SameAs(target));

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
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task SwapUnitsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
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

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel source = FindSlot(screen.UnitGrid, GridPosition.FrontBottom);
        UnitGridSlotViewModel target = FindSlot(screen.UnitGrid, GridPosition.FrontCenter);

        screen.DragDrop.TryBeginDrag(source.Reference);
        await screen.DragDrop.DropAsync(target.Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (soldier, GridPosition.FrontBottom),
                    (leader, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public void LargeUnitGridTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 80, initiative: 40, damage: 20) with
        {
            IsLarge = true
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit large = new(new WorldUnitId("large"), largeDefinition.Id, 80, 0, largeDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(largeDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (large, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(largeDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        Assert.That(screen.UnitGrid.Slots.Select(slot => (slot.Position, slot.Row, slot.Column, slot.ColumnSpan, slot.HasUnit, slot.IsVisible)), Is.EqualTo([
            (GridPosition.FrontBottom, Row: 2, Column: 1, ColumnSpan: 1, HasUnit: true, IsVisible: true),
            (GridPosition.FrontCenter, Row: 1, Column: 0, ColumnSpan: 2, HasUnit: true, IsVisible: true),
            (GridPosition.FrontTop, Row: 0, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackBottom, Row: 2, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackCenter, Row: 1, Column: 0, ColumnSpan: 2, HasUnit: false, IsVisible: false),
            (GridPosition.BackTop, Row: 0, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true)
        ]));
    }

    [Test]
    public async Task EquipItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };

        UnitModifierDefinition weaponModifier = WeaponModifier();
        ItemDefinition weapon = Weapon(weaponModifier.Id);
        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([weaponItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(weapon)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        Assert.That(screen.Equipment.Slots.Select(slot => (slot.Slot, HasItem: slot.ItemId is not null, slot.IsHighlighted)), Is.EqualTo([
            (LeaderEquipmentSlot.Banner, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Tome, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Battle1, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Battle2, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Artifact1, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Artifact2, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Boots, HasItem: false, IsHighlighted: false)
        ]));

        InventoryItemViewModel item = screen.Inventory.Items[0];
        screen.DragDrop.TryBeginDrag(item.Reference);

        Assert.That(screen.Equipment.Slots.Select(slot => (slot.Slot, HasItem: slot.ItemId is not null, slot.IsHighlighted)), Is.EqualTo([
            (LeaderEquipmentSlot.Banner, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Tome, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Battle1, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Battle2, HasItem: false, IsHighlighted: false),
            (LeaderEquipmentSlot.Artifact1, HasItem: false, IsHighlighted: true),
            (LeaderEquipmentSlot.Artifact2, HasItem: false, IsHighlighted: true),
            (LeaderEquipmentSlot.Boots, HasItem: false, IsHighlighted: false)
        ]));

        LeaderEquipmentSlotViewModel artifactSlot = FindEquipmentSlot(screen, LeaderEquipmentSlot.Artifact1);
        await screen.DragDrop.DropAsync(artifactSlot.Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = WorldInventory.Empty,
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, weaponItem),
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(leader with { Modifiers = [UnitModifier.FromEquipment(weaponModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build()
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task UnequipItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };

        UnitModifierDefinition weaponModifier = WeaponModifier();
        ItemDefinition weapon = Weapon(weaponModifier.Id);
        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([weaponItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(weapon)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);
        await screen.DragDrop.DropAsync(FindEquipmentSlot(screen, LeaderEquipmentSlot.Artifact2).Reference);

        Assert.That(session.World.State.Stacks[stack.Id].Equipment, Is.EqualTo(
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact2, weaponItem)));
        Assert.That(screen.Inventory.IsHighlighted, Is.False);

        screen.DragDrop.TryBeginDrag(FindEquipmentSlot(screen, LeaderEquipmentSlot.Artifact2).Reference);

        Assert.That(screen.Inventory.IsHighlighted, Is.True);

        await screen.DragDrop.DropAsync(screen.Inventory.DropReference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task ItemOnUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        ItemDefinition potion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 30, 0, soldierDefinition.Level, [], null, false);
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
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).IsHighlighted, Is.False);
        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).IsHighlighted, Is.False);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);

        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).IsHighlighted, Is.False);
        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).IsHighlighted, Is.True);

        await screen.DragDrop.DropAsync(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = WorldInventory.Empty,
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier with { Hitpoints = 35 }, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task SpellItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.WandScrollUse
            };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldSpellDefinition spell = new(
            new WorldSpellDefinitionId("heal"),
            new GlobalTextId("name"),
            new GlobalTextId("description"),
            new GlobalTextId("effect"),
            WorldRace.Elf,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: null,
            new WorldHealSpellEffect(5),
            AiType: WorldAiSpellType.All);

        ItemDefinition scroll = new(
            new ItemDefinitionId("scroll1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Scroll,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new SpellItemEffect(spell.Id, WorldResourceSet.Empty),
            AnimationUnitId: null);

        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 30, 0, soldierDefinition.Level, [], null, false);
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
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([scrollItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        Assert.That(screen.SpellDropZone.IsVisible, Is.False);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);

        Assert.That(screen.SpellDropZone.IsVisible, Is.True);
        Assert.That(screen.SpellDropZone.Content, Is.EqualTo("Use name"));
        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).IsHighlighted, Is.False);

        screen.DragDrop.EndDrag();

        Assert.That(screen.SpellDropZone.IsVisible, Is.False);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);
        await screen.DragDrop.DropAsync(screen.SpellDropZone.DropReference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = WorldInventory.Empty,
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier with { Hitpoints = 35 }, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(screen.SpellDropZone.IsVisible, Is.False);
    }

    [Test]
    public async Task ItemOnUnwoundedUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        ItemDefinition potion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
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
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);

        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).IsHighlighted, Is.False);

        await screen.DragDrop.DropAsync(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Reference);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack));
        Alert alert = Assert.NotNull(screen.Game.Alert.Current);
        Assert.That(alert, Is.EqualTo(new Alert(AlertType.Error, IconMoniker.Warning, "The unit is not wounded", Details: null)));
    }

    [Test]
    public void LeaderSummaryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
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

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.Leader, Is.EqualTo(new StackLeaderViewModel(
                new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("leader")),
                "name",
                [
                    new StatRowViewModel(IconMoniker.Hitpoint, "Hit points", new StatValueViewModel("100 / 100", 0)),
                    new StatRowViewModel(IconMoniker.Movement, "Movement", new StatValueViewModel("20 / 20", 0)),
                    new StatRowViewModel(IconMoniker.Stack, "Units", new StatValueViewModel("2 / 6", 0)),
                    new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
                ],
                [],
                new UnitDetailsViewModel(
                    new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("leader")),
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
                    "Damage (Weapon)",
                    new StatValueViewModel("10", 0),
                    null,
                    [
                        new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                        new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                        new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                        new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                        new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                        new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0)),
                        new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
                    ],
                    [],
                    []))));
            Assert.That(screen.StackGridTitle, Is.EqualTo("name's party"));
            Assert.That(screen.StackUnitCount, Is.EqualTo("2 / 3"));
        }
    }

    [Test]
    public void LeaderAbilitiesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse | LeaderAbility.BannerUse
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
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

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        Assert.That(screen.Leader, Is.EqualTo(new StackLeaderViewModel(
            new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("leader")),
            "name",
            [
                new StatRowViewModel(IconMoniker.Hitpoint, "Hit points", new StatValueViewModel("100 / 100", 0)),
                new StatRowViewModel(IconMoniker.Movement, "Movement", new StatValueViewModel("20 / 20", 0)),
                new StatRowViewModel(IconMoniker.Stack, "Units", new StatValueViewModel("2 / 6", 0)),
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
            ],
            [
                new LeaderAbilityViewModel(IconMoniker.AbilityWeaponArmorUse, "Weapon and armor use"),
                new LeaderAbilityViewModel(IconMoniker.AbilityBannerUse, "Banner use")
            ],
            new UnitDetailsViewModel(
                new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("leader")),
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
                "Damage (Weapon)",
                new StatValueViewModel("10", 0),
                null,
                [
                    new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                    new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                    new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                    new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                    new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                    new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0)),
                    new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
                ],
                [],
                []))));
    }

    [Test]
    public void NamedLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], Name: "Ardobert", IsTypeLocked: false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
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
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);
        UnitDetailsViewModel leaderDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).Details);
        UnitDetailsViewModel soldierDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Details);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.StackGridTitle, Is.EqualTo("Ardobert's party"));
            Assert.That(leaderDetails.Name, Is.EqualTo("Ardobert"));
            Assert.That(soldierDetails.Name, Is.EqualTo("name"));
        }
    }

    [Test]
    public void VisitingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new VisitingStackTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        VisitingStackTabViewModel screen = Assert.IsOfType<VisitingStackTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.HasStack, Is.True);
            Assert.That(screen.StackGridTitle, Is.EqualTo("name's party"));
        }
        Assert.That(screen.UnitGrid.Slots.Select(slot => (slot.Position, slot.Row, slot.Column, slot.ColumnSpan, slot.HasUnit, slot.IsVisible)), Is.EqualTo([
            (GridPosition.FrontBottom, Row: 2, Column: 1, ColumnSpan: 1, HasUnit: true, IsVisible: true),
            (GridPosition.FrontCenter, Row: 1, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.FrontTop, Row: 0, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackBottom, Row: 2, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackCenter, Row: 1, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackTop, Row: 0, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true)
        ]));
    }

    [Test]
    public void NoVisitingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
                new WorldCityDefinitionId(WorldRace.Human, 3),
                WorldResourceSet.Empty) with
            {
                VisionRadius = 5
            };

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(cityDefinition)
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new VisitingStackTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        VisitingStackTabViewModel screen = Assert.IsOfType<VisitingStackTabViewModel>(management.Tabs[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(screen.HasStack, Is.False);
            Assert.That(screen.Leader, Is.Null);
            Assert.That(screen.StackGridTitle, Is.Empty);
            Assert.That(screen.StackUnitCount, Is.Empty);
        }

        Assert.That(screen.Equipment.Slots, Is.Empty);
        Assert.That(screen.UnitGrid.Slots.Select(slot => (slot.Position, slot.Row, slot.Column, slot.ColumnSpan, slot.HasUnit, slot.IsVisible)), Is.EqualTo([
            (GridPosition.FrontBottom, Row: 2, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.FrontCenter, Row: 1, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.FrontTop, Row: 0, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackBottom, Row: 2, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackCenter, Row: 1, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackTop, Row: 0, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true)
        ]));
    }

    [Test]
    public void InspectUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 999, soldierDefinition.Level, [], null, false);
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

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel emptySlot = FindSlot(screen.UnitGrid, GridPosition.BackTop);
        UnitDetailsViewModel leaderDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).Details);
        UnitDetailsViewModel soldierDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Details);

        gameViewModel.ShowDetails(leaderDetails);
        DetailsViewModel details = Assert.IsOfType<DetailsViewModel>(gameViewModel.TopScreen);

        gameViewModel.HideDetails();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(emptySlot.Details, Is.Null);
            Assert.That(leaderDetails, Is.EqualTo(new UnitDetailsViewModel(
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
                "Damage (Weapon)",
                new StatValueViewModel("10", 0),
                null,
                [
                    new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                    new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                    new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                    new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                    new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                    new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0)),
                    new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
                ],
                [],
                [])));
            Assert.That(soldierDetails, Is.EqualTo(new UnitDetailsViewModel(
                new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(soldierDefinition.Id)),
                "name",
                "description",
                1,
                35,
                35,
                999,
                1000,
                false,
                true,
                false,
                false,
                "Damage (Weapon)",
                new StatValueViewModel("10", 0),
                null,
                [
                    new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                    new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                    new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                    new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                    new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                    new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0))
                ],
                [],
                [])));
            Assert.That(details.Content, Is.EqualTo(leaderDetails));
            Assert.That(gameViewModel.TopScreen, Is.Null);
        }
    }

    [Test]
    public async Task LeaderUpgradeSelectionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        UnitModifierDefinition bannerModifier = new(
            new UnitModifierDefinitionId("banner-mod"),
            DescriptionTextId: new GlobalTextId("description"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.BannerUse,
            Wards: []);

        UnitModifierDefinition orbModifier = new(
            new UnitModifierDefinitionId("orb-mod"),
            DescriptionTextId: new GlobalTextId("description"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 0,
            LeaderAbilities: LeaderAbility.OrbUse,
            Wards: []);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder().Place(leader, GridPosition.FrontBottom, false).Build(),
                movementPoints: 20) with
            {
                PendingLeaderUpgrades = 1
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(bannerModifier)
            .WithDefinition(orbModifier)
            .WithLeaderUpgrade(new LeaderUpgradeDefinition(
                player.LordId,
                bannerModifier.Id,
                MinLevel: 1,
                Priority: 80,
                new GlobalTextId("banner-name"),
                new GlobalTextId("banner-description")))
            .WithLeaderUpgrade(new LeaderUpgradeDefinition(
                player.LordId,
                orbModifier.Id,
                MinLevel: 5,
                Priority: 95,
                new GlobalTextId("orb-name"),
                new GlobalTextId("orb-description")))
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        gameViewModel.OpenScreen(new LeaderUpgradeViewModel(gameModel, gameViewModel, stack.Id));

        LeaderUpgradeViewModel upgrades = Assert.IsOfType<LeaderUpgradeViewModel>(gameViewModel.TopScreen);

        Assert.That(upgrades.Options.Select(option => (option.Name, option.Description, option.Status.UnavailableReason)), Is.EqualTo<(string, string, string?)[]>([
            ("banner-name", "banner-description", null)
        ]));

        Assert.That(upgrades.LearnCommand.CanExecute(null), Is.False);

        upgrades.Options[0].SelectCommand.Execute(null);

        Assert.That(upgrades.Options.Select(option => (option.Name, option.IsSelected)), Is.EqualTo([
            ("banner-name", true)
        ]));
        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(upgrades));
        Assert.That(upgrades.LearnCommand.CanExecute(null), Is.True);

        await upgrades.LearnCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(leader with { Modifiers = [UnitModifier.Permanent(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
                .Build(),
            PendingLeaderUpgrades = 0
        }));
        Assert.That(gameViewModel.TopScreen, Is.Null);
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

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        Assert.That(screen.DismissDropZone.IsVisible, Is.False);

        screen.DragDrop.TryBeginDrag(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).Reference);

        Assert.That(screen.DismissDropZone.IsVisible, Is.False);

        screen.DragDrop.EndDrag();
        screen.DragDrop.TryBeginDrag(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Reference);

        Assert.That(screen.DismissDropZone.IsVisible, Is.True);
        Assert.That(screen.DismissDropZone.Content, Is.EqualTo("Dismiss name"));

        await screen.DragDrop.DropAsync(screen.DismissDropZone.DropReference);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prompt.Title, Is.EqualTo("Dismiss unit"));
            Assert.That(prompt.Message, Is.EqualTo(new TextSpans([new TextSpan("Are you sure you want to dismiss ", TextStyle.None), new TextSpan("name", TextStyle.Highlight), new TextSpan("?", TextStyle.None)])));
            Assert.That(prompt.ConfirmLabel, Is.EqualTo("Dismiss"));
        }

        await prompt.ConfirmCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Stacks[stack.Id], Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)])
        }));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.TopScreen, Is.Null);
            Assert.That(screen.DismissDropZone.IsVisible, Is.False);
            Assert.That(screen.Game.Alert.Current, Is.Null);
        }
    }

    [Test]
    public async Task DisbandCancelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
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

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());
        WorldState initialState = session.World.State;

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Reference);
        await screen.DragDrop.DropAsync(screen.DismissDropZone.DropReference);

        ConfirmPromptViewModel prompt = Assert.IsOfType<ConfirmPromptViewModel>(gameViewModel.TopScreen);
        prompt.CloseCommand.Execute(null);

        Assert.That(session.World.State, Is.EqualTo(initialState));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.TopScreen, Is.Null);
            Assert.That(screen.DismissDropZone.IsVisible, Is.False);
        }
    }

    [Test]
    public void TalismanChargesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.TalismanUse
            };

        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman1"),
            new GlobalTextId("Talisman of Fire"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        ItemDefinition potion = HealingPotion(new ItemDefinitionId("potion1"), 20);
        WorldItem talismanItem = new(new WorldItemId("Talisman"), talisman.Id, Charges: 3);
        WorldItem equippedTalismanItem = new(new WorldItemId("EquippedTalisman"), talisman.Id, Charges: 1);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([talismanItem, potionItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, equippedTalismanItem)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(talisman)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        Assert.That(screen.Inventory.Items.Select(item => item.Name), Is.EqualTo([
            "Talisman of Fire (3)",
            "name"
        ]));

        Assert.That(screen.Equipment.Slots.Select(slot => slot.Details?.Name), Is.EqualTo([
            null,
            null,
            "Talisman of Fire (1)",
            null,
            null,
            null,
            null
        ]));
    }

    [Test]
    public async Task SlotRefreshTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
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

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        List<(NotifyCollectionChangedAction Action, int Index)> changes = [];
        screen.UnitGrid.Slots.CollectionChanged += (sender, e) => changes.Add((e.Action, e.NewStartingIndex));

        await FindSlot(screen.UnitGrid, GridPosition.FrontCenter).SelectCommand.ExecuteAsync(null);

        Assert.That(screen.UnitGrid.Slots.Select(slot => slot.IsSelected), Is.EqualTo([false, true, false, false, false, false]));
        Assert.That(changes, Is.Empty);

        await FindSlot(screen.UnitGrid, GridPosition.FrontCenter).SelectCommand.ExecuteAsync(null);

        Assert.That(screen.UnitGrid.Slots.Select(slot => slot.IsSelected), Is.EqualTo([false, true, false, false, false, false]));
        Assert.That(changes, Is.Empty);
    }

    [Test]
    public async Task SwapEquippedItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        ItemDefinition equippedPotion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        ItemDefinition carriedPotion = HealingPotion(new ItemDefinitionId("potion2"), 20);
        WorldItem equippedPotionItem = new(new WorldItemId("EquippedPotion"), equippedPotion.Id, Charges: null);
        WorldItem carriedPotionItem = new(new WorldItemId("CarriedPotion"), carriedPotion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([carriedPotionItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, equippedPotionItem)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(equippedPotion)
            .WithDefinition(carriedPotion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);

        Assert.That(screen.Equipment.Slots.Select(slot => (slot.Slot, slot.ItemId, slot.IsHighlighted)), Is.EqualTo([
            (LeaderEquipmentSlot.Banner, ItemId: (ItemDefinitionId?)null, IsHighlighted: false),
            (LeaderEquipmentSlot.Tome, ItemId: null, IsHighlighted: false),
            (LeaderEquipmentSlot.Battle1, ItemId: equippedPotion.Id, IsHighlighted: true),
            (LeaderEquipmentSlot.Battle2, ItemId: null, IsHighlighted: true),
            (LeaderEquipmentSlot.Artifact1, ItemId: null, IsHighlighted: false),
            (LeaderEquipmentSlot.Artifact2, ItemId: null, IsHighlighted: false),
            (LeaderEquipmentSlot.Boots, ItemId: null, IsHighlighted: false)
        ]));

        await screen.DragDrop.DropAsync(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle1).Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = new WorldInventory([equippedPotionItem]),
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, carriedPotionItem)
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task DragItemToEquipmentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        ItemDefinition potion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);
        await screen.DragDrop.DropAsync(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle2).Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = WorldInventory.Empty,
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle2, potionItem)
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task DragEquipmentToInventoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        ItemDefinition potion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, potionItem)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle1).Reference);
        await screen.DragDrop.DropAsync(screen.Inventory.DropReference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = new WorldInventory([potionItem]),
            Equipment = LeaderEquipment.Empty
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task DragItemOntoOccupiedSlotTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        ItemDefinition equippedPotion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        ItemDefinition carriedPotion = HealingPotion(new ItemDefinitionId("potion2"), 20);
        WorldItem equippedPotionItem = new(new WorldItemId("EquippedPotion"), equippedPotion.Id, Charges: null);
        WorldItem carriedPotionItem = new(new WorldItemId("CarriedPotion"), carriedPotion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([carriedPotionItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, equippedPotionItem)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(equippedPotion)
            .WithDefinition(carriedPotion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);
        await screen.DragDrop.DropAsync(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle1).Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = new WorldInventory([equippedPotionItem]),
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, carriedPotionItem)
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task DragBetweenEquipmentSlotsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        ItemDefinition firstPotion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        ItemDefinition secondPotion = HealingPotion(new ItemDefinitionId("potion2"), 20);
        WorldItem firstPotionItem = new(new WorldItemId("FirstPotion"), firstPotion.Id, Charges: null);
        WorldItem secondPotionItem = new(new WorldItemId("SecondPotion"), secondPotion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Equipment = LeaderEquipment.Empty
                    .With(LeaderEquipmentSlot.Battle1, firstPotionItem)
                    .With(LeaderEquipmentSlot.Battle2, secondPotionItem)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(firstPotion)
            .WithDefinition(secondPotion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle1).Reference);

        Assert.That(screen.Equipment.Slots.Select(slot => (slot.Slot, slot.IsHighlighted)), Is.EqualTo([
            (LeaderEquipmentSlot.Banner, IsHighlighted: false),
            (LeaderEquipmentSlot.Tome, IsHighlighted: false),
            (LeaderEquipmentSlot.Battle1, IsHighlighted: false),
            (LeaderEquipmentSlot.Battle2, IsHighlighted: true),
            (LeaderEquipmentSlot.Artifact1, IsHighlighted: false),
            (LeaderEquipmentSlot.Artifact2, IsHighlighted: false),
            (LeaderEquipmentSlot.Boots, IsHighlighted: false)
        ]));

        await screen.DragDrop.DropAsync(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle2).Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = new WorldInventory([secondPotionItem]),
            Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle2, firstPotionItem)
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task DragItemToUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        ItemDefinition potion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 30, 0, soldierDefinition.Level, [], null, false);
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
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);
        await screen.DragDrop.DropAsync(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = WorldInventory.Empty,
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier with { Hitpoints = 35 }, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public async Task DragEquippedItemToUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10);

        ItemDefinition potion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 10, 0, soldierDefinition.Level, [], null, false);
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
                movementPoints: 20) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, potionItem)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        screen.DragDrop.TryBeginDrag(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle1).Reference);
        await screen.DragDrop.DropAsync(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Reference);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Equipment = LeaderEquipment.Empty,
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier with { Hitpoints = 80 }, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(screen.Game.Alert.Current, Is.Null);
    }

    [Test]
    public void InspectMaximumLevelUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit summon = new(new WorldUnitId("summon"), summonDefinition.Id, 35, 0, summonDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(summonDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (summon, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitDetailsViewModel summonDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontCenter).Details);

        Assert.That(summonDetails, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(summonDefinition.Id)),
            "name",
            "description",
            1,
            35,
            35,
            1000,
            1000,
            true,
            false,
            false,
            false,
            "Damage (Weapon)",
            new StatValueViewModel("10", 0),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0))
            ],
            [],
            [])));
    }

    [Test]
    public async Task HireLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = WorldResourceSet.Empty with { Gold = 150 }
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldRaceDefinition raceDefinition = new(
            Race: WorldRace.Human,
            NameTextId: new GlobalTextId("Human"),
            Income: WorldResourceSet.Empty,
            NativeTerrain: WorldMapTerrain.Human,
            IsPlayable: true,
            CapitalRegeneration: 35,
            Cities: [],
            Leaders: [leaderDefinition.Id],
            Units: [],
            MaleLeaderNames: ["Konrad"],
            FemaleLeaderNames: ["Zolian"]);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 3),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("Milonia"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldSubrace subrace = new(
            Id: new WorldSubraceId("subrace"),
            PlayerId: player.Id,
            Subrace: WorldSubraceType.Human,
            BannerVariant: new WorldObjectVariant(1));

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(cityDefinition)
            .WithRace(raceDefinition)
            .WithCity(city)
            .WithSubrace(subrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new VisitingStackTabViewModel(gameModel, gameViewModel, owner, city.Id)]);
        VisitingStackTabViewModel screen = Assert.IsOfType<VisitingStackTabViewModel>(management.Tabs[0]);
        LeaderBlankslateViewModel blankslate = Assert.NotNull(screen.LeaderBlankslate);

        Assert.That(screen.HasStack, Is.False);

        blankslate.HireCommand.Execute(null);

        LeaderPurchaseViewModel purchase = Assert.IsOfType<LeaderPurchaseViewModel>(gameViewModel.TopScreen);

        await purchase.Options[0].PurchaseCommand.ExecuteAsync(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(gameViewModel.TopScreen, Is.Null);
            Assert.That(screen.HasStack, Is.True);
            Assert.That(screen.StackGridTitle, Is.EqualTo("Konrad's party"));
        }

        Assert.That(screen.UnitGrid.Slots.Select(slot => (slot.Position, slot.Row, slot.Column, slot.ColumnSpan, slot.HasUnit, slot.IsVisible)), Is.EqualTo([
            (GridPosition.FrontBottom, Row: 2, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.FrontCenter, Row: 1, Column: 1, ColumnSpan: 1, HasUnit: true, IsVisible: true),
            (GridPosition.FrontTop, Row: 0, Column: 1, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackBottom, Row: 2, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackCenter, Row: 1, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true),
            (GridPosition.BackTop, Row: 0, Column: 0, ColumnSpan: 1, HasUnit: false, IsVisible: true)
        ]));
    }

    [Test]
    public void InspectModifiedUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("modifier") with
        {
            Hitpoints = new UnitStatModifier(20, 0),
            Armor = new UnitStatModifier(15, 0),
            Initiative = new UnitStatModifier(0, 10),
            Damage = new UnitStatModifier(0, 50),
            Accuracy = new UnitStatModifier(-10, 0)
        };
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 120, 0, leaderDefinition.Level, [UnitModifier.Permanent(modifier.Id)], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(modifier)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(modifier)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitDetailsViewModel leaderDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).Details);

        Assert.That(leaderDetails, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
            "name",
            "description",
            1,
            120,
            120,
            0,
            1000,
            false,
            false,
            false,
            false,
            "Damage (Weapon)",
            new StatValueViewModel("10", 5),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 15)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", -10)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 5)),
                new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0)),
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
            ],
            [],
            [new UnitPermanentModifierViewModel(IconMoniker.Hitpoint, "+20 hit points, +15 armor, +50% damage, -10 accuracy, +10% initiative", "Permanent", null, 1)])));
    }

    [Test]
    public void InspectUpgradedLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitModifierDefinition leadership = TestHelpers.SimpleModifier("leadership") with { Leadership = new UnitStatModifier(1, 0) };
        UnitModifierDefinition moreLeadership = TestHelpers.SimpleModifier("more-leadership") with { Leadership = new UnitStatModifier(1, 0) };
        UnitModifierDefinition pathfinding = TestHelpers.SimpleModifier("pathfinding") with { MaxMovement = new UnitStatModifier(0, 20) };
        UnitModifierDefinition armor = TestHelpers.SimpleModifier("armor") with { Armor = new UnitStatModifier(10, 0) };
        UnitModifierDefinition swiftness = TestHelpers.SimpleModifier("swiftness") with { Initiative = new UnitStatModifier(0, 20) };
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(
            new WorldUnitId("leader"),
            leaderDefinition.Id,
            100,
            0,
            leaderDefinition.Level,
            [
                UnitModifier.Permanent(leadership.Id),
                UnitModifier.Permanent(pathfinding.Id),
                UnitModifier.Permanent(armor.Id),
                new UnitModifier(swiftness.Id, RemainingTurns: 2),
                UnitModifier.Permanent(moreLeadership.Id)
            ],
            null,
            false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(leadership)
            .AddUnitModifier(moreLeadership)
            .AddUnitModifier(pathfinding)
            .AddUnitModifier(armor)
            .AddUnitModifier(swiftness)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(leadership)
            .WithDefinition(moreLeadership)
            .WithDefinition(pathfinding)
            .WithDefinition(armor)
            .WithDefinition(swiftness)
            .WithLeaderUpgrade(new LeaderUpgradeDefinition(
                player.LordId,
                leadership.Id,
                MinLevel: 3,
                Priority: 99,
                new GlobalTextId("leadership-name"),
                new GlobalTextId("leadership-description")))
            .WithLeaderUpgrade(new LeaderUpgradeDefinition(
                player.LordId,
                moreLeadership.Id,
                MinLevel: 6,
                Priority: 99,
                new GlobalTextId("leadership-name"),
                new GlobalTextId("leadership-description")))
            .WithLeaderUpgrade(new LeaderUpgradeDefinition(
                player.LordId,
                pathfinding.Id,
                MinLevel: 2,
                Priority: 80,
                new GlobalTextId("pathfinding-name"),
                new GlobalTextId("pathfinding-description")))
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitDetailsViewModel leaderDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).Details);

        Assert.That(leaderDetails, Is.EqualTo(new UnitDetailsViewModel(
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
            "Damage (Weapon)",
            new StatValueViewModel("10", 0),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 10)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 10)),
                new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0)),
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("5", 0))
            ],
            [new UnitModifierStatusViewModel(UnitModifierType.Boost, [new UnitModifierViewModel("+20% initiative", TrSpans("Turns left: {0:*}", 1), UnitModifierType.Boost)])],
            [
                new UnitPermanentModifierViewModel(IconMoniker.Leadership, "leadership-name", "leadership-description", "+1 leadership", 2),
                new UnitPermanentModifierViewModel(IconMoniker.Movement, "pathfinding-name", "pathfinding-description", "+20% movement", 1),
                new UnitPermanentModifierViewModel(IconMoniker.Armor, "+10 armor", "Permanent", null, 1)
            ])));
    }

    [Test]
    public async Task PotionHitpointsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        ItemDefinition potion = HealingPotion(new ItemDefinitionId("potion1"), 50);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 10, 0, soldierDefinition.Level, [], null, false);
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
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel soldierSlot = FindSlot(screen.UnitGrid, GridPosition.FrontCenter);

        Assert.That(soldierSlot.Hitpoints, Is.EqualTo(11));

        screen.DragDrop.TryBeginDrag(screen.Inventory.Items[0].Reference);
        await screen.DragDrop.DropAsync(soldierSlot.Reference);

        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontCenter), Is.SameAs(soldierSlot));
        Assert.That(soldierSlot.Hitpoints, Is.EqualTo(35));
    }

    [Test]
    public void InspectItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse | LeaderAbility.TalismanUse
            };

        UnitModifierDefinition weaponModifier = WeaponModifier();
        ItemDefinition weapon = Weapon(weaponModifier.Id) with { Value = WorldResourceSet.Empty with { Gold = 150 } };

        ItemDefinition talisman = new(
            new ItemDefinitionId("talisman1"),
            new GlobalTextId("Talisman of Fire"),
            new GlobalTextId("desc"),
            ItemCategory.Talisman,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem weaponItem = new(new WorldItemId("Weapon"), weapon.Id, Charges: null);
        WorldItem talismanItem = new(new WorldItemId("Talisman"), talisman.Id, Charges: 2);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Inventory = new WorldInventory([weaponItem]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, talismanItem)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(weaponModifier)
            .WithDefinition(weapon)
            .WithDefinition(talisman)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        ItemDetailsViewModel weaponDetails = screen.Inventory.Items[0].Details;
        ItemDetailsViewModel talismanDetails = Assert.NotNull(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle1).Details);

        gameViewModel.ShowDetails(weaponDetails);
        DetailsViewModel details = Assert.IsOfType<DetailsViewModel>(gameViewModel.TopScreen);

        gameViewModel.HideDetails();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle2).Details, Is.Null);
            Assert.That(weaponDetails, Is.EqualTo(new ItemDetailsViewModel(
                new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(weapon.Id)),
                "name",
                "Weapon",
                "desc",
                ["+20 damage"],
                CastingCost: null,
                new ResourceSetViewModel(WorldResourceSet.Empty with { Gold = 150 }, ResourceSetProjection.All))));
            Assert.That(talismanDetails, Is.EqualTo(new ItemDetailsViewModel(
                new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(talisman.Id)),
                "Talisman of Fire (2)",
                "Talisman",
                "desc",
                [],
                CastingCost: null,
                new ResourceSetViewModel(WorldResourceSet.Empty, ResourceSetProjection.All))));
            Assert.That(details.Content, Is.EqualTo(weaponDetails));
            Assert.That(gameViewModel.TopScreen, Is.Null);
        }
    }

    [Test]
    public void InspectStaffTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = WorldResourceSet.Empty with { LifeMana = 150, RuneMana = 20 }
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.WandScrollUse
            };

        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId("Lightning"), new WorldRestoreMovementSpellEffect(50));

        ItemDefinition staff = new(
            new ItemDefinitionId("staff1"),
            new GlobalTextId("Staff of Thunder"),
            new GlobalTextId("desc"),
            ItemCategory.Wand,
            WorldResourceSet.Empty with { Gold = 600 },
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new SpellItemEffect(spell.Id, WorldResourceSet.Empty with { LifeMana = 100, RuneMana = 50 }),
            AnimationUnitId: null);

        WorldItem staffItem = new(new WorldItemId("Staff"), staff.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Virgile"),
                player.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, staffItem)
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(staff)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        ItemDetailsViewModel details = Assert.NotNull(FindEquipmentSlot(screen, LeaderEquipmentSlot.Battle1).Details);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(details, Is.EqualTo(new ItemDetailsViewModel(
                new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(staff.Id)),
                "Staff of Thunder",
                "Staff",
                "desc",
                [],
                new OfferPriceViewModel(WorldResourceSet.Empty with { LifeMana = 100, RuneMana = 50 }, WorldResourceSet.Empty with { LifeMana = 150, RuneMana = 20 }),
                new ResourceSetViewModel(WorldResourceSet.Empty with { Gold = 600 }, ResourceSetProjection.All))));
            Assert.That(FormatPrice(Assert.NotNull(details.CastingCost)), Is.EqualTo("100 life mana, 50 rune mana (unaffordable)"));
        }
    }

    [Test]
    public void InspectScenarioModifiedUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("modifier") with
        {
            Hitpoints = new UnitStatModifier(20, 0),
            Armor = new UnitStatModifier(15, 0),
            Initiative = new UnitStatModifier(0, 10),
            Damage = new UnitStatModifier(0, 50),
            Accuracy = new UnitStatModifier(-10, 0)
        };
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 120, 0, leaderDefinition.Level, [UnitModifier.FromScenario(modifier.Id)], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(modifier)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(modifier)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitDetailsViewModel leaderDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).Details);

        Assert.That(leaderDetails, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id)),
            "name",
            "description",
            1,
            120,
            120,
            0,
            1000,
            false,
            false,
            false,
            false,
            "Damage (Weapon)",
            new StatValueViewModel("15", 0),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("15", 0)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("90%", 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("55", 0)),
                new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0)),
                new StatRowViewModel(IconMoniker.Leadership, "Leadership", new StatValueViewModel("3", 0))
            ],
            [],
            [])));
    }

    [Test]
    public void InspectSummonerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition summonerTemplate = TestHelpers.SimpleUnitDefinition("summoner", maxHitpoints: 35, initiative: 50, damage: 0);
        UnitDefinition summonerDefinition = summonerTemplate with
        {
            Attack = summonerTemplate.Attack with
            {
                Type = UnitAttackType.Summon,
                Source = UnitAttackSource.Death
            }
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit summoner = new(new WorldUnitId("summoner"), summonerDefinition.Id, 35, 0, summonerDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(summonerDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (summoner, GridPosition.BackCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonerDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitDetailsViewModel summonerDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.BackCenter).Details);

        Assert.That(summonerDetails, Is.EqualTo(new UnitDetailsViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(summonerDefinition.Id)),
            "name",
            "description",
            1,
            35,
            35,
            0,
            1000,
            false,
            false,
            false,
            false,
            "Summon (Death)",
            new StatValueViewModel("—", 0),
            null,
            [
                new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0))
            ],
            [],
            [])));
    }

    [Test]
    public void InspectBlockedUpgradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 35, initiative: 50, damage: 10);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 50, initiative: 50, damage: 15);
        UnitDefinition paladinDefinition = TestHelpers.SimpleUnitDefinition("paladin", maxHitpoints: 60, initiative: 50, damage: 12);
        WorldPlayerBuildingDefinition barracks = TestHelpers.SimpleBuildingDefinition(new WorldPlayerBuildingDefinitionId("barracks"), WorldPlayerBuildingCategory.Unit) with
        {
            NameTextId = new GlobalTextId("Barracks")
        };
        WorldPlayerBuildingDefinition temple = TestHelpers.SimpleBuildingDefinition(new WorldPlayerBuildingDefinitionId("temple"), WorldPlayerBuildingCategory.Unit) with
        {
            NameTextId = new GlobalTextId("Temple")
        };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Buildings = new WorldPlayerBuildings([barracks.Id, temple.Id], [])
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit squire = new(new WorldUnitId("squire"), squireDefinition.Id, 35, 999, squireDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(squireDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (squire, GridPosition.FrontCenter)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithDefinition(paladinDefinition)
            .WithDefinition(barracks)
            .WithDefinition(temple)
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, knightDefinition.Id, barracks.Id))
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, paladinDefinition.Id, temple.Id))
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel squireSlot = FindSlot(screen.UnitGrid, GridPosition.FrontCenter);
        UnitDetailsViewModel leaderDetails = Assert.NotNull(FindSlot(screen.UnitGrid, GridPosition.FrontBottom).Details);
        UnitDetailsViewModel squireDetails = Assert.NotNull(squireSlot.Details);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(leaderDetails.IsUpgradeBlocked, Is.False);
            Assert.That(squireDetails, Is.EqualTo(new UnitDetailsViewModel(
                new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(squireDefinition.Id)),
                "name",
                "description",
                1,
                35,
                35,
                999,
                1000,
                false,
                false,
                true,
                false,
                "Damage (Weapon)",
                new StatValueViewModel("10", 0),
                null,
                [
                    new StatRowViewModel(IconMoniker.Target, "Targets", new StatValueViewModel("1", 0)),
                    new StatRowViewModel(IconMoniker.Armor, "Armor", new StatValueViewModel("0", 0)),
                    new StatRowViewModel(IconMoniker.Accuracy, "Accuracy", new StatValueViewModel("100%", 0)),
                    new StatRowViewModel(IconMoniker.Initiative, "Initiative", new StatValueViewModel("50", 0)),
                    new StatRowViewModel(IconMoniker.Immunity, "Immunities", new StatValueViewModel("None", 0)),
                    new StatRowViewModel(IconMoniker.Ward, "Wards", new StatValueViewModel("None", 0))
                ],
                [],
                [])));
            Assert.That(squireSlot.IsPendingUpgrade, Is.True);
        }
    }

    [Test]
    public async Task SwapWoundedUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit woundedSoldier = new(new WorldUnitId("woundedSoldier"), soldierDefinition.Id, 10, 0, soldierDefinition.Level, [], null, false);
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
                    (woundedSoldier, GridPosition.FrontCenter),
                    (soldier, GridPosition.FrontTop)
                ]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MapStackTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        MapStackTabViewModel screen = Assert.IsOfType<MapStackTabViewModel>(management.Tabs[0]);

        UnitGridSlotViewModel source = FindSlot(screen.UnitGrid, GridPosition.FrontCenter);
        UnitGridSlotViewModel target = FindSlot(screen.UnitGrid, GridPosition.FrontTop);

        screen.DragDrop.TryBeginDrag(source.Reference);
        await screen.DragDrop.DropAsync(target.Reference);

        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontCenter), Is.Not.SameAs(source));
        Assert.That(FindSlot(screen.UnitGrid, GridPosition.FrontTop), Is.Not.SameAs(target));
        Assert.That(screen.UnitGrid.Slots.Select(slot => (slot.Position, slot.Hitpoints)), Is.EqualTo([
            (GridPosition.FrontBottom, 100),
            (GridPosition.FrontCenter, 35),
            (GridPosition.FrontTop, 17),
            (GridPosition.BackBottom, 0),
            (GridPosition.BackCenter, 0),
            (GridPosition.BackTop, 0)
        ]));
    }
}
