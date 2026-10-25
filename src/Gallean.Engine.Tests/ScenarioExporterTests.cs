namespace Gallean.Engine.Tests;

public sealed class ScenarioExporterTests
{
    private static readonly WorldItem Banner = new(new WorldItemId("banner-1"), new ItemDefinitionId("banner"), Charges: null);
    private static readonly WorldItem Potion = new(new WorldItemId("potion-1"), new ItemDefinitionId("potion"), Charges: null);
    private static readonly WorldItem SecondPotion = new(new WorldItemId("potion-2"), new ItemDefinitionId("potion"), Charges: null);
    private static readonly WorldItem Gem = new(new WorldItemId("gem-1"), new ItemDefinitionId("gem"), Charges: null);
    private static readonly UnitModifierDefinition BannerModifier = TestHelpers.SimpleModifier("banner-modifier");
    private static readonly UnitModifierDefinition UpgradeModifier = TestHelpers.SimpleModifier("upgrade-modifier");

    private static ItemDefinition CreateItem(string id, ItemCategory category, UnitModifierDefinitionId? modifier)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), category) with
        {
            Value = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            EquipmentModifierId = modifier
        };
    }

    private static (WorldState State, IGameDefinitionProvider Definitions) BuildWorld(WorldStack stack)
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^...",
            "^^^^...",
            "^^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6,
            leadership: 3);
        UnitDefinition questLeaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("quest-leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6,
            leadership: 3);
        UnitDefinition thiefDefinition = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

        WorldRaceDefinition race = new(
            WorldRace.Human,
            new GlobalTextId("Human"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Human,
            true,
            35,
            null,
            [],
            [leaderDefinition.Id, thiefDefinition.Id],
            [],
            ["Uther"],
            ["Elena"]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer foreignPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Dwarf, map);

        WorldStack foreignStack = TestHelpers.SimpleStack(
                new WorldStackId("foreign"),
                foreignPlayer.Id,
                new WorldPosition(6, 6),
                TestHelpers.SingleUnitGrid(new WorldUnitId("foreignLeader"), leaderDefinition),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([Banner with { Id = new WorldItemId("banner-2") }])
            };

        WorldStack thiefStack = TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            player.Id,
            new WorldPosition(5, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("thief"), thiefDefinition),
            movementPoints: 20);

        WorldStack questStack = TestHelpers.SimpleStack(
            new WorldStackId("quest"),
            player.Id,
            new WorldPosition(6, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("questLeader"), questLeaderDefinition),
            movementPoints: 6);

        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital"),
                player.Id,
                new WorldPosition(4, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                Inventory = new WorldInventory([Gem])
            };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(questLeaderDefinition)
            .WithDefinition(thiefDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(BannerModifier)
            .WithDefinition(UpgradeModifier)
            .WithDefinition(CreateItem("banner", ItemCategory.Banner, BannerModifier.Id))
            .WithDefinition(CreateItem("potion", ItemCategory.PotionHeal, null))
            .WithDefinition(CreateItem("gem", ItemCategory.Valuable, null))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(TestHelpers.SimpleLord(foreignPlayer.LordId, foreignPlayer.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithPlayer(foreignPlayer)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .WithCapital(capital)
            .WithStack(stack)
            .WithStack(thiefStack)
            .WithStack(questStack)
            .WithStack(foreignStack)
            .BuildEngineWithContext();

        return (engine.State, context.Definitions);
    }

    private static WorldStack CreateHeroStack()
    {
        WorldUnit leader = new(
            new WorldUnitId("leader"),
            new UnitDefinitionId("leader"),
            Hitpoints: 40,
            Experience: 250,
            Level: 3,
            [
                UnitModifier.Permanent(UpgradeModifier.Id),
                UnitModifier.FromEquipment(BannerModifier.Id),
                new UnitModifier(UpgradeModifier.Id, RemainingTurns: 2)
            ],
            Name: "Uther",
            IsTypeLocked: false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), new UnitDefinitionId("soldier"), 100, 0, 1, [UnitModifier.FromEquipment(BannerModifier.Id)], null, false);

        return TestHelpers.SimpleStack(
                new WorldStackId("hero"),
                new WorldPlayerId(1),
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontCenter, false)
                    .Place(soldier, GridPosition.BackCenter, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([Potion, SecondPotion]),
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, Banner),
                PendingLeaderUpgrades = 1,
                BattlesWon = 4
            };
    }

    [Test]
    public void ExportableItemsTest()
    {
        (WorldState state, _) = BuildWorld(CreateHeroStack());

        Assert.That(
            ScenarioExporter.GetExportableItems(state, new WorldPlayerId(1)),
            Is.EqualTo([Banner, Potion, SecondPotion, Gem]));
    }

    [Test]
    public void ExportableStacksTest()
    {
        WorldStack stack = CreateHeroStack();
        (WorldState state, IGameDefinitionProvider definitions) = BuildWorld(stack);

        ScenarioExporter exporter = new(definitions, new GlobalVariables());

        Assert.That(exporter.GetExportableStacks(state, new WorldPlayerId(1)), Is.EqualTo([state.Stacks[stack.Id]]));
    }

    [Test]
    public void ExportTest()
    {
        WorldStack stack = CreateHeroStack();
        (_, IGameDefinitionProvider definitions) = BuildWorld(stack);

        ScenarioExporter exporter = new(definitions, new GlobalVariables());
        ScenarioLeader exported = exporter.ExportLeader(stack, [Potion, Gem, Banner]);

        Assert.That(exported, Is.EqualTo(new ScenarioLeader(
            new WorldUnit(
                new WorldUnitId("leader"),
                new UnitDefinitionId("leader"),
                Hitpoints: 40,
                Experience: 250,
                Level: 3,
                [UnitModifier.Permanent(UpgradeModifier.Id)],
                Name: "Uther",
                IsTypeLocked: false),
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, Banner),
            new WorldInventory([Potion, Gem]),
            PendingLeaderUpgrades: 1,
            BattlesWon: 4)));
    }

    [Test]
    public void ItemLimitTest()
    {
        WorldStack stack = CreateHeroStack();
        (_, IGameDefinitionProvider definitions) = BuildWorld(stack);

        ScenarioExporter exporter = new(definitions, new GlobalVariables());

        Assert.That(exporter.ItemLimit, Is.EqualTo(5));
        Assert.That(
            () => exporter.ExportLeader(stack, [Potion, Potion, Potion, Potion, Potion, Potion]),
            Throws.ArgumentException);
    }
}
