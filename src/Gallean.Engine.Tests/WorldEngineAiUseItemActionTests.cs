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

    private static ItemDefinition PermanentPotion(UnitModifierDefinitionId modifierId)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId("elixir"), ItemCategory.PotionPermanentBoost) with
        {
            Effect = new PermanentBoostItemEffect(modifierId)
        };
    }

    private static ItemDefinition BoostPotion(UnitModifierDefinitionId modifierId)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId("tonic"), ItemCategory.PotionTemporaryBoost) with
        {
            Effect = new TemporaryBoostItemEffect(modifierId)
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
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
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
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"))),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
            new WorldUnitResurrectedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), 1, WorldResourceSet.Empty),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void PermanentBoostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("boost") with { Damage = new UnitStatModifier(0, 20) };
        ItemDefinition potion = PermanentPotion(modifier.Id);
        WorldItem potionItem = new(new WorldItemId("Elixir"), potion.Id, Charges: null);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(2, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(stack)
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(6, 1000),
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"))),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
            new WorldUnitModifierAppliedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), new UnitModifier(modifier.Id, null)),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }

    [Test]
    public void BoostBeforeAttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 30);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 20, initiative: 10, damage: 5),
            movementPoints: 6, leadership: 3);
        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("boost") with { Damage = new UnitStatModifier(0, 50) };
        ItemDefinition potion = BoostPotion(modifier.Id);
        WorldItem potionItem = new(new WorldItemId("Tonic"), potion.Id, Charges: null);

        WorldPlayer aiPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer humanPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                aiPlayer.Id,
                new WorldPosition(2, 2),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition), GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([potionItem])
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            humanPlayer.Id,
            new WorldPosition(4, 2),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(aiPlayer.LordId, aiPlayer.Race, LordCategory.Warrior))
            .WithPlayer(aiPlayer)
            .WithDefinition(TestHelpers.SimpleLord(humanPlayer.LordId, humanPlayer.Race, LordCategory.Warrior))
            .WithPlayer(humanPlayer)
            .WithStack(stack)
            .WithStack(enemyStack)
            .WithAiMemory(aiPlayer.Id, WorldAiMemory.Empty.WithStackMemory(
                stack.Id,
                new WorldAiStackMemory(WorldStackOrder.Normal.Instance, new WorldAiTarget.Stack(enemyStack.Id))))
            .Build();

        WorldAiMemory memory = new WorldAiMemory(new WorldAiTurnMemory(1, aiPlayer.Id, 1, 0, 0, []))
            .WithStackMemory(stack.Id, new WorldAiStackMemory(WorldStackOrder.Normal.Instance, new WorldAiTarget.Stack(enemyStack.Id)));
        WorldAiActionPlan plan = new(
            new WorldAiActionScore(6, 1000),
            new WorldUseItemCommand(new WorldUnitGroup.Stack(stack.Id), potionItem.Id, new WorldItemTarget.Unit(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"))),
            memory);

        engine.CheckCommand(new WorldAiActionCommand(), [
            new WorldAiActionStartedEvent(plan),
            new WorldItemUsedEvent(new WorldUnitGroup.Stack(stack.Id), potionItem, RemainingCharges: null, Consumed: true),
            new WorldUnitModifierAppliedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), new UnitModifier(modifier.Id, 1)),
            new WorldAiActionEndedEvent(aiPlayer.Id, memory)
        ]);
    }
}
