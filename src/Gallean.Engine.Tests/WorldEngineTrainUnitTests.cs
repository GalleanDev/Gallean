namespace Gallean.Engine.Tests;

public sealed class WorldEngineTrainUnitTests
{
    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 900 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            [
                new WorldStackUnitTrainedEvent(stack.Id, new WorldUnitId("soldier"), 999, new WorldResourceSet(495, 0, 0, 0, 0, 0))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with { Resources = new WorldResourceSet(505, 0, 0, 0, 0, 0) }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 999 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void PartialTrainingTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(103, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 900 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            [
                new WorldStackUnitTrainedEvent(stack.Id, new WorldUnitId("soldier"), 920, new WorldResourceSet(100, 0, 0, 0, 0, 0))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with { Resources = new WorldResourceSet(3, 0, 0, 0, 0, 0) }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 920 }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void NotAtTrainerTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(5, 5),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 900 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            WorldCommandError.StackNotAtTrainer);
    }

    [Test]
    public void AlreadyTrainedTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 999 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            WorldCommandError.UnitAlreadyTrained);
    }

    [Test]
    public void FullyTrainedTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0),
            ExperienceNextLevel = 0
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            WorldCommandError.UnitFullyTrained);
    }

    [Test]
    public void TrainingForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            WorldCommandError.UnitTrainingForbidden);
    }

    [Test]
    public void DeadUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            WorldCommandError.UnitDead);
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(4, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 900 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("soldier")),
            WorldCommandError.InsufficientResources);
    }

    [Test]
    public void SummonedUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition summonDefinition = TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            Category = UnitCategory.Summon,
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("summon"), summonDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(summonDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("summon")),
            WorldCommandError.UnitFullyTrained);
    }

    [Test]
    public void MaximumLeaderLevelTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 2 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithMaxLeaderLevel(2)
            .WithDefinition(leaderDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                leaderDefinition.Id,
                UpgradeChangeLevel: 10,
                LowLevelUpgrade: UnitDynamicUpgrade.Empty,
                HighLevelUpgrade: UnitDynamicUpgrade.Empty))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("leader")),
            WorldCommandError.UnitFullyTrained);
    }

    [Test]
    public void UnitNotFoundTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^^..",
            "^^^^.."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10) with
        {
            TrainingCost = new WorldResourceSet(5, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };

        WorldTrainerSite site = new(
            Id: new WorldSiteId("trainer"),
            Footprint: new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Experience = 900 }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(site)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldTrainUnitCommand(stack.Id, new WorldUnitId("missing")),
            WorldCommandError.UnitNotFound);
    }
}
