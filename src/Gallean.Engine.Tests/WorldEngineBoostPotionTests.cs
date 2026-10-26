namespace Gallean.Engine.Tests;

public sealed class WorldEngineBoostPotionTests
{
    private static UnitModifierDefinition DamageModifier(string id, int percent)
    {
        return TestHelpers.SimpleModifier(id) with { Damage = new UnitStatModifier(0, percent) };
    }

    private static ItemDefinition Potion(string id, ItemCategory category, UnitModifierDefinitionId modifier)
    {
        ItemEffect effect;
        if (category == ItemCategory.PotionPermanentBoost)
        {
            effect = new PermanentBoostItemEffect(modifier);
        }
        else
        {
            effect = new TemporaryBoostItemEffect(modifier);
        }

        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), category) with
        {
            Effect = effect
        };
    }

    private static WorldStack Stack(WorldUnitId leaderId, UnitDefinition leaderDefinition, WorldUnit soldier, WorldPlayerId owner, IEnumerable<WorldItem> inventory)
    {
        return TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                owner,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(leaderId, leaderDefinition), GridPosition.BackBottom, false)
                    .Place(soldier, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([.. inventory])
            };
    }

    [Test]
    public void BoostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitModifierDefinition modifier = DamageModifier("boost", percent: 30);
        ItemDefinition potion = Potion("potion1", ItemCategory.PotionTemporaryBoost, modifier.Id);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition),
            player.Id,
            [potionItem]);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .BuildEngineWithContext();

        engine.CheckCommand(
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("soldier"),
                    new UnitModifier(modifier.Id, 1))
            ]);

        WorldUnit boosted = new(new WorldUnitId("soldier"), soldierDefinition.Id, 100, 0, 1, [new UnitModifier(modifier.Id, 1)], null, false);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(boosted, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));

        Assert.That(context.UnitStats.GetWorldUnitStats(boosted), Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 40,
            Damage: 13,
            Heal: 0,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void PermanentBoostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitModifierDefinition modifier = DamageModifier("permanent", percent: 10);
        ItemDefinition potion = Potion("potion1", ItemCategory.PotionPermanentBoost, modifier.Id);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition),
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("soldier"),
                    UnitModifier.Permanent(modifier.Id))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Modifiers = [UnitModifier.Permanent(modifier.Id)] }, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void BoostExpiresNextTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitModifierDefinition boost = DamageModifier("boost", percent: 30);
        UnitModifierDefinition permanent = DamageModifier("permanent", percent: 10);
        ItemDefinition boostPotion = Potion("potion1", ItemCategory.PotionTemporaryBoost, boost.Id);
        ItemDefinition permanentPotion = Potion("potion2", ItemCategory.PotionPermanentBoost, permanent.Id);
        WorldItem boostPotionItem = new(new WorldItemId("BoostPotion"), boostPotion.Id, Charges: null);
        WorldItem permanentPotionItem = new(new WorldItemId("PermanentPotion"), permanentPotion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition),
            player.Id,
            [boostPotionItem, permanentPotionItem]);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(boost)
            .WithDefinition(permanent)
            .WithDefinition(boostPotion)
            .WithDefinition(permanentPotion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .BuildEngineWithContext();

        _ = engine.DispatchCommand(new WorldUseItemCommand(stack.Id, boostPotionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))));
        _ = engine.DispatchCommand(new WorldUseItemCommand(stack.Id, permanentPotionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))));

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stack.Id, 6),
            new WorldUnitModifierExpiredEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("soldier"), boost.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        WorldUnit soldier = TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Modifiers = [UnitModifier.Permanent(permanent.Id)] };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(100, 25, 0, 0, 0, 0)
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(soldier, GridPosition.FrontBottom, false)
                        .Build()
                })
            }));

        Assert.That(context.UnitStats.GetWorldUnitStats(soldier), Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 40,
            Damage: 11,
            Heal: 0,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void SamePotionTwiceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitModifierDefinition modifier = DamageModifier("boost", percent: 30);
        ItemDefinition potion = Potion("potion1", ItemCategory.PotionTemporaryBoost, modifier.Id);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);
        WorldItem secondPotionItem = new(new WorldItemId("SecondPotion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition),
            player.Id,
            [potionItem, secondPotionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))));

        engine.CheckCommandError(
            new WorldUseItemCommand(stack.Id, secondPotionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            WorldCommandError.UnitModifierAlreadyApplied);

        engine.CheckCommand(
            new WorldUseItemCommand(stack.Id, secondPotionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("leader"))),
            [
                new WorldItemUsedEvent(stack.Id, secondPotionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("leader"),
                    new UnitModifier(modifier.Id, 1))
            ]);
    }

    [Test]
    public void DeadTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitModifierDefinition modifier = DamageModifier("boost", percent: 30);
        ItemDefinition potion = Potion("potion1", ItemCategory.PotionTemporaryBoost, modifier.Id);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 0 },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            WorldCommandError.UnitDead);
    }

    [Test]
    public void ModifierLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        UnitModifierDefinition modifier = DamageModifier("boost", percent: 30);
        ItemDefinition potion = Potion("potion1", ItemCategory.PotionTemporaryBoost, modifier.Id);
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("leader2"),
            leaderDefinition,
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with
            {
                Modifiers =
                [
                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried1")),
                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried2")),
                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried3")),
                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried4")),
                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried5")),
                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried6")),
                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried7")),
                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried8"))
                ]
            },
            player.Id,
            [potionItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(modifier)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("soldier"))),
            [
                new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("soldier"),
                    new UnitModifier(modifier.Id, 1))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leaderDefinition), GridPosition.BackBottom, false)
                        .Place(
                            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with
                            {
                                Modifiers =
                                [
                                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried1")),
                                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried2")),
                                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried3")),
                                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried4")),
                                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried5")),
                                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried6")),
                                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried7")),
                                    UnitModifier.Permanent(new UnitModifierDefinitionId("carried8")),
                                    new UnitModifier(modifier.Id, 1)
                                ]
                            },
                            GridPosition.FrontBottom,
                            false)
                        .Build()
                })
            }));
    }
}
