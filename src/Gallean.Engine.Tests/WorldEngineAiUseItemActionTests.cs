using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiUseItemActionTests
{
    private static WorldPlayerBuildingDefinition Temple()
    {
        return TestHelpers.SimpleBuildingDefinition(new WorldPlayerBuildingDefinitionId("temple"), WorldPlayerBuildingCategory.Heal);
    }

    private static ItemDefinition HealingPotion()
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId("potion"), ItemCategory.PotionHeal) with
        {
            Effect = new HealItemEffect(50)
        };
    }

    [Test]
    public void HealItemBeforeCityTest()
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
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        ItemDefinition potion = HealingPotion();
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([Temple().Id], [Temple().Id])
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 60 }, GridPosition.BackBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 20 }, GridPosition.BackCenter, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(Temple())
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("cap"), aiPlayer.Id, new WorldPosition(4, 4), UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Heal.Instance, null));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(6, 800),
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
            new WorldUnitHealedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 70, WorldResourceSet.Empty),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void ReviveItemBeforeCityTest()
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
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);

        ItemDefinition potion = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("revive"), ItemCategory.PotionRevive) with
        {
            Effect = new ReviveItemEffect()
        };
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([Temple().Id], [Temple().Id])
        };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(4, 4),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 60 }, GridPosition.BackBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 }, GridPosition.BackCenter, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(Temple())
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("cap"), aiPlayer.Id, new WorldPosition(4, 4), UnitGrid<WorldUnit>.Empty))
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Heal.Instance, null));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(6, 1000),
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
            new WorldUnitResurrectedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 1, WorldResourceSet.Empty),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }
}
