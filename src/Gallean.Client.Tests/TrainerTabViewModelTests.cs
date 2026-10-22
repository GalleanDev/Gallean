using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class TrainerTabViewModelTests
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

    private static string FormatPrice(OfferPriceViewModel price)
    {
        List<string> items = [];

        foreach (OfferPriceItemViewModel item in price.Items)
        {
            if (item.IsShort)
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()} (short)");
            }
            else
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()}");
            }
        }

        return string.Join(", ", items);
    }

    private static string FormatOffer(SiteOfferViewModel offer)
    {
        return $"{offer.Title} | {offer.Subtitle.Text} | {offer.Description} | {FormatPrice(Assert.NotNull(offer.Status.Price))} | {offer.ActionText} | {FormatAvailability(offer.Status.UnavailableReason)} | {FormatEnabled(offer.ActCommand.CanExecute(null))}";
    }

    private static string FormatInspectedUnit(TrainerInspectedUnitViewModel unit)
    {
        return $"{unit.PortraitKey.CanonicalId} | {unit.Name} | {unit.GainText} | {unit.Experience} / {unit.NextLevelExperience} + {unit.TrainableExperience} | {unit.ExperienceText} | maximum {unit.IsMaximumLevel} | pending {unit.IsPendingUpgrade}";
    }

    private static string FormatEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            return "enabled";
        }
        else
        {
            return "disabled";
        }
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
    public async Task SelectedUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 35, 900, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            site.Entrance,
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
            .WithSite(site)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, ResourceSetProjection.Gold, owner => [new TrainerTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        TrainerTabViewModel screen = Assert.IsOfType<TrainerTabViewModel>(management.Tabs[0]);

        Assert.That(screen.Offer, Is.Null);
        Assert.That(screen.Stack.UnitGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, true, false, false, false, false]));

        await FindSlot(screen.Stack.UnitGrid, GridPosition.FrontCenter).SelectCommand.ExecuteAsync(null);

        Assert.That(FormatInspectedUnit(Assert.NotNull(screen.InspectedUnit)), Is.EqualTo("unit-event-portrait/soldier | name | Gains 99 XP, enough to reach the next level. | 900 / 1000 + 99 | 900/1000 +99 | maximum False | pending False"));
        Assert.That(management.PlayerResources, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(1100, 0, 0, 25, 0, 0), ResourceSetProjection.Gold)));
        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 1 |  | 495 gold | Train | available | enabled"));

        await Assert.NotNull(screen.Offer).ActCommand.ExecuteAsync(null);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(
                stackDefinitions,
                [
                    (leader, GridPosition.FrontBottom),
                    (soldier with { Experience = 999 }, GridPosition.FrontCenter)
                ])
        }));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(605, 0, 0, 25, 0, 0)));
        Assert.That(FormatInspectedUnit(Assert.NotNull(screen.InspectedUnit)), Is.EqualTo("unit-event-portrait/soldier | name |  | 999 / 1000 + 0 | 999/1000 | maximum False | pending True"));
        Assert.That(management.PlayerResources, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(605, 0, 0, 25, 0, 0), new WorldResourceSet(1100, 0, 0, 25, 0, 0), ResourceSetProjection.Gold)));
        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 1 |  | 0 gold | Train | unavailable: This unit is already trained and will upgrade at the end of its next battle. | disabled"));
        Assert.That(screen.Stack.UnitGrid.Slots.Select(slot => slot.IsHighlighted), Is.EqualTo([false, false, false, false, false, false]));
        Assert.That(screen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Experience, "Unit trained", "name")));
    }

    [Test]
    public async Task PartialTrainingSummaryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(3, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 900, leaderDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            site.Entrance,
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithSite(site)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, ResourceSetProjection.Gold, owner => [new TrainerTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        TrainerTabViewModel screen = Assert.IsOfType<TrainerTabViewModel>(management.Tabs[0]);

        await FindSlot(screen.Stack.UnitGrid, GridPosition.FrontBottom).SelectCommand.ExecuteAsync(null);

        Assert.That(management.PlayerResources, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(103, 0, 0, 25, 0, 0), ResourceSetProjection.Gold)));
        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 1 |  | 100 gold | Train | available | enabled"));
        Assert.That(FormatInspectedUnit(Assert.NotNull(screen.InspectedUnit)), Is.EqualTo("unit-event-portrait/leader | name | Gains 20 XP, the most you can afford. | 900 / 1000 + 20 | 900/1000 +20 | maximum False | pending False"));

        await Assert.NotNull(screen.Offer).ActCommand.ExecuteAsync(null);

        WorldStack updatedStack = session.World.State.Stacks[stack.Id];

        Assert.That(updatedStack, Is.EqualTo(stack with
        {
            Units = TestHelpers.StackUnitGrid(stackDefinitions, [(leader with { Experience = 920 }, GridPosition.FrontBottom)])
        }));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(3, 0, 0, 25, 0, 0)));
        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 1 |  | 0 gold | Train | unavailable: Training costs 5 gold per experience point. You need more gold. | disabled"));
        Assert.That(FormatInspectedUnit(Assert.NotNull(screen.InspectedUnit)), Is.EqualTo("unit-event-portrait/leader | name |  | 920 / 1000 + 0 | 920/1000 | maximum False | pending False"));
    }

    [Test]
    public async Task SummonedUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 3);
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], null, false);
        WorldUnit summon = new(new WorldUnitId("summon"), summonDefinition.Id, 35, 0, summonDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(summonDefinition)
            .Build();

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            site.Entrance,
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
            .WithSite(site)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, ResourceSetProjection.Gold, owner => [new TrainerTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        TrainerTabViewModel screen = Assert.IsOfType<TrainerTabViewModel>(management.Tabs[0]);

        await FindSlot(screen.Stack.UnitGrid, GridPosition.FrontCenter).SelectCommand.ExecuteAsync(null);

        Assert.That(FormatInspectedUnit(Assert.NotNull(screen.InspectedUnit)), Is.EqualTo("unit-event-portrait/summon | name |  | 1000 / 1000 + 0 | Max | maximum True | pending False"));
        Assert.That(Assert.NotNull(screen.Offer).ActCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task MaximumLeaderLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };

        UnitDynamicUpgradeDefinition leaderDynamicUpgrade = new(
            leaderDefinition.Id,
            UpgradeChangeLevel: 5,
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 3, ExperienceKilled = 2, ExperienceNextLevel = 500 },
            UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 1, ExperienceKilled = 1, ExperienceNextLevel = 250 });

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 110, 900, 2, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitDynamicUpgrade(leaderDynamicUpgrade)
            .Build();

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            site.Entrance,
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithMaxLeaderLevel(2)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithUnitDynamicUpgrade(leaderDynamicUpgrade)
            .WithSite(site)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, ResourceSetProjection.Gold, owner => [new TrainerTabViewModel(gameModel, gameViewModel, owner, stack.Id)]);
        TrainerTabViewModel screen = Assert.IsOfType<TrainerTabViewModel>(management.Tabs[0]);

        await FindSlot(screen.Stack.UnitGrid, GridPosition.FrontBottom).SelectCommand.ExecuteAsync(null);

        Assert.That(FormatInspectedUnit(Assert.NotNull(screen.InspectedUnit)), Is.EqualTo("unit-event-portrait/leader | name |  | 1500 / 1500 + 0 | Max | maximum True | pending False"));
        Assert.That(Assert.NotNull(screen.Offer).ActCommand.CanExecute(null), Is.False);
    }
}
