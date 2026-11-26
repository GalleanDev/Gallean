using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class SpellItemsViewModelTests
{
    private static string[] FormatGroups(SpellItemsViewModel screen)
    {
        List<string> rows = [];

        foreach (SpellItemGroupViewModel group in screen.Groups)
        {
            foreach (SpellItemViewModel item in group.Items)
            {
                rows.Add($"{group.Title}: {item.Details.Name} x{item.Count}");
            }
        }

        return [.. rows];
    }

    [Test]
    public async Task UseScrollTest()
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
            new GlobalTextId("Scroll of Healing"),
            new GlobalTextId("desc"),
            ItemCategory.Scroll,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new SpellItemEffect(spell.Id, WorldResourceSet.Empty),
            AnimationUnitId: null);

        ItemDefinition potion = new(
            new ItemDefinitionId("potion1"),
            new GlobalTextId("Potion of Healing"),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);

        WorldItem firstScroll = new(new WorldItemId("Scroll1"), scroll.Id, Charges: null);
        WorldItem secondScroll = new(new WorldItemId("Scroll2"), scroll.Id, Charges: null);
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
                Inventory = new WorldInventory([firstScroll, potionItem, secondScroll])
            };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(potion)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        WorldScreenViewModel world = new(gameModel, gameViewModel, new RandomProvider(0));
        gameViewModel.ShowScreen(world);
        world.CompleteGameStart();
        gameViewModel.DevTools.IsFogEnabled = false;

        world.SelectStackItemCommand.Execute(stack.Id);
        world.OpenStackCommand.Execute(null);

        ManagementScreenViewModel management = Assert.IsOfType<ManagementScreenViewModel>(gameViewModel.TopScreen);
        MapStackTabViewModel tab = Assert.IsOfType<MapStackTabViewModel>(management.SelectedTab);

        Assert.That(tab.OpenSpellItemsCommand.CanExecute(null), Is.True);

        tab.OpenSpellItemsCommand.Execute(null);

        SpellItemsViewModel screen = Assert.IsOfType<SpellItemsViewModel>(gameViewModel.TopScreen);

        Assert.That(FormatGroups(screen), Is.EqualTo(["Scrolls: Scroll of Healing x2"]));
        Assert.That(screen.UseSelectedCommand.CanExecute(null), Is.False);

        screen.SelectItemCommand.Execute(screen.Groups[0].Items[0]);

        Assert.That(screen.UseSelectedCommand.CanExecute(null), Is.True);

        await screen.UseSelectedCommand.ExecuteAsync(null);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Inventory = new WorldInventory([firstScroll, potionItem]),
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier with { Hitpoints = 35 }, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(gameViewModel.TopScreen, Is.SameAs(world));
    }
}
