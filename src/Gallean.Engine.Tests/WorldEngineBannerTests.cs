using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineBannerTests
{
    [Test]
    public void EquipTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.BannerUse
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition bannerModifier = TestHelpers.SimpleModifier("banner-of-health") with
        {
            RegenerationPercent = 10,
            Armor = new UnitStatModifier(10, 0)
        };

        ItemDefinition banner = new(
            new ItemDefinitionId("banner1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Banner,
            WorldResourceSet.Empty,
            EquipmentModifierId: bannerModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, 1, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, 1, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Place(soldier, GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([bannerItem])
            };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(bannerModifier)
            .WithDefinition(banner)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .BuildEngineWithContext();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldEquipItemCommand(stack.Id, bannerItem.Id, LeaderEquipmentSlot.Banner),
            [new WorldItemEquippedEvent(stack.Id, bannerItem, LeaderEquipmentSlot.Banner)]);

        WorldUnit bannerLeader = leader with { Modifiers = [UnitModifier.FromEquipment(bannerModifier.Id)] };
        WorldUnit bannerSoldier = soldier with { Modifiers = [UnitModifier.FromEquipment(bannerModifier.Id)] };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(bannerLeader, GridPosition.FrontBottom, false)
                        .Place(bannerSoldier, GridPosition.BackBottom, false)
                        .Build()
                })
            }));

        Assert.That(context.UnitStats.GetWorldUnitStats(bannerLeader), Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 10,
            Initiative: 50,
            Damage: 10,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));

        Assert.That(context.UnitStats.GetWorldUnitStats(bannerSoldier), Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 10,
            Initiative: 40,
            Damage: 5,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));
    }

    [Test]
    public void LeaderScopedEquipmentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.WeaponArmorUse
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition armorModifier = TestHelpers.SimpleModifier("armor-mod") with
        {
            Armor = new UnitStatModifier(10, 0)
        };

        ItemDefinition armor = new(
            new ItemDefinitionId("armor1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Armor,
            WorldResourceSet.Empty,
            EquipmentModifierId: armorModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem armorItem = new(new WorldItemId("Armor"), armor.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, 1, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, 1, [], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Place(soldier, GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([armorItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(armorModifier)
            .WithDefinition(armor)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldEquipItemCommand(stack.Id, armorItem.Id, LeaderEquipmentSlot.Artifact1),
            [new WorldItemEquippedEvent(stack.Id, armorItem, LeaderEquipmentSlot.Artifact1)]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Artifact1, armorItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(leader with { Modifiers = [UnitModifier.FromEquipment(armorModifier.Id)] }, GridPosition.FrontBottom, false)
                        .Place(soldier, GridPosition.BackBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void RoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.BannerUse
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition bannerModifier = TestHelpers.SimpleModifier("banner-mod") with
        {
            Armor = new UnitStatModifier(10, 0)
        };

        ItemDefinition banner = new(
            new ItemDefinitionId("banner1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Banner,
            WorldResourceSet.Empty,
            EquipmentModifierId: bannerModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, 1, [], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, 1, [], null, false);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(soldier, GridPosition.FrontBottom, false)
                .Build());

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(3, 3),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([bannerItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(cityDefinition)
            .WithDefinition(bannerModifier)
            .WithDefinition(banner)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState initialState = engine.State;

        engine.CheckCommand(
            new WorldEquipItemCommand(stack.Id, bannerItem.Id, LeaderEquipmentSlot.Banner),
            [new WorldItemEquippedEvent(stack.Id, bannerItem, LeaderEquipmentSlot.Banner)]);

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("soldier"),
                GridPosition.BackBottom),
            [
                new WorldUnitTransferredEvent(
                    new WorldUnitGroup.Fortification(city.Id),
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("soldier"),
                    GridPosition.BackBottom)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(leader with { Modifiers = [UnitModifier.FromEquipment(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
                        .Place(soldier with { Modifiers = [UnitModifier.FromEquipment(bannerModifier.Id)] }, GridPosition.BackBottom, false)
                        .Build()
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city with
                {
                    Garrison = UnitGrid<WorldUnit>.Empty
                })
            }));

        engine.CheckCommand(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("soldier"),
                GridPosition.FrontBottom),
            [
                new WorldUnitTransferredEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitGroup.Fortification(city.Id),
                    new WorldUnitId("soldier"),
                    GridPosition.FrontBottom)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(leader with { Modifiers = [UnitModifier.FromEquipment(bannerModifier.Id)] }, GridPosition.FrontBottom, false)
                        .Build()
                }),
                Fortifications = engine.State.Fortifications.SetItem(city.Id, city)
            }));

        engine.CheckCommand(
            new WorldUnequipItemCommand(stack.Id, LeaderEquipmentSlot.Banner),
            [new WorldItemUnequippedEvent(stack.Id, LeaderEquipmentSlot.Banner)]);

        Assert.That(engine.State, Is.EqualTo(initialState));
    }

    [Test]
    public void SharedModifierTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.BannerUse
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition sharedModifier = TestHelpers.SimpleModifier("g000um9035") with
        {
            RegenerationPercent = 10
        };

        UnitModifierDefinition ownModifier = TestHelpers.SimpleModifier("own-mod") with
        {
            Armor = new UnitStatModifier(5, 0)
        };

        ItemDefinition banner = new(
            new ItemDefinitionId("banner1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Banner,
            WorldResourceSet.Empty,
            EquipmentModifierId: sharedModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, 1, [], null, false);
        WorldUnit soldier = new(
            new WorldUnitId("soldier"),
            soldierDefinition.Id,
            soldierDefinition.MaxHitpoints,
            0,
            1,
            [UnitModifier.Permanent(sharedModifier.Id), UnitModifier.Permanent(ownModifier.Id)],
            Name: null,
            IsTypeLocked: false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Place(soldier, GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Inventory = new WorldInventory([bannerItem])
            };

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(sharedModifier)
            .WithDefinition(ownModifier)
            .WithDefinition(banner)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .BuildEngineWithContext();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldState initialState = engine.State;

        engine.CheckCommand(
            new WorldEquipItemCommand(stack.Id, bannerItem.Id, LeaderEquipmentSlot.Banner),
            [new WorldItemEquippedEvent(stack.Id, bannerItem, LeaderEquipmentSlot.Banner)]);

        WorldUnit bannerSoldier = soldier with
        {
            Modifiers =
            [
                UnitModifier.Permanent(sharedModifier.Id),
                UnitModifier.Permanent(ownModifier.Id),
                UnitModifier.FromEquipment(sharedModifier.Id)
            ]
        };

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem),
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(leader with { Modifiers = [UnitModifier.FromEquipment(sharedModifier.Id)] }, GridPosition.FrontBottom, false)
                        .Place(bannerSoldier, GridPosition.BackBottom, false)
                        .Build()
                })
            }));

        Assert.That(context.UnitStats.GetWorldUnitStats(bannerSoldier), Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 5,
            Regeneration: 10,
            Initiative: 40,
            Damage: 5,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));

        engine.CheckCommand(
            new WorldUnequipItemCommand(stack.Id, LeaderEquipmentSlot.Banner),
            [new WorldItemUnequippedEvent(stack.Id, LeaderEquipmentSlot.Banner)]);

        Assert.That(engine.State, Is.EqualTo(initialState));
    }

    [Test]
    public void DestroyedBannerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.BannerUse
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        UnitModifierDefinition bannerModifier = TestHelpers.SimpleModifier("banner-mod") with
        {
            Armor = new UnitStatModifier(10, 0)
        };

        ItemDefinition banner = new(
            new ItemDefinitionId("banner1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Banner,
            WorldResourceSet.Empty,
            EquipmentModifierId: bannerModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        UnitModifier carried = UnitModifier.FromEquipment(bannerModifier.Id);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, 1, [carried], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, 1, [carried], null, false);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Place(soldier, GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 10) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem)
            };

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Undead],
            [WorldRace.Undead],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventDestroyItemEffect(banner.Id, TriggererOnly: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(bannerModifier)
            .WithDefinition(banner)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Equipment = LeaderEquipment.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(leader with { Modifiers = [] }, GridPosition.FrontBottom, false)
                        .Place(soldier with { Modifiers = [] }, GridPosition.BackBottom, false)
                        .Build()
                }),
                ScenarioState = WorldScenarioState.Empty with
                {
                    EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
                }
            }));
    }

    [Test]
    public void BattleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer attacker = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer defender = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                LeaderAbilities = LeaderAbility.BannerUse
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 100, initiative: 30, damage: 5),
            movementPoints: 6, leadership: 6);

        UnitModifierDefinition bannerModifier = TestHelpers.SimpleModifier("banner-of-war") with
        {
            Armor = new UnitStatModifier(10, 0),
            Damage = new UnitStatModifier(0, 20)
        };

        ItemDefinition banner = new(
            new ItemDefinitionId("banner1"),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            ItemCategory.Banner,
            WorldResourceSet.Empty,
            EquipmentModifierId: bannerModifier.Id,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldItem bannerItem = new(new WorldItemId("Banner"), banner.Id, Charges: null);

        UnitModifier carried = UnitModifier.FromEquipment(bannerModifier.Id);

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, 1, [carried], null, false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, soldierDefinition.MaxHitpoints, 0, 1, [carried], null, false);
        WorldUnit enemy = new(new WorldUnitId("enemy"), enemyDefinition.Id, enemyDefinition.MaxHitpoints, 0, 1, [], null, false);

        WorldStack attackerStack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                attacker.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(leader, GridPosition.FrontBottom, false)
                    .Place(soldier, GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, bannerItem)
            };

        WorldStack defenderStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            defender.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(enemy, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(bannerModifier)
            .WithDefinition(banner)
            .WithDefinition(TestHelpers.SimpleLord(attacker.LordId, attacker.Race, LordCategory.Warrior))
            .WithPlayer(attacker)
            .WithDefinition(TestHelpers.SimpleLord(defender.LordId, defender.Race, LordCategory.Warrior))
            .WithPlayer(defender)
            .WithStack(attackerStack)
            .WithStack(defenderStack)
            .BuildEngineWithContext();

        BattleUnit battleLeader = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("leader"),
            DefinitionId: leaderDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 100,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [carried],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit battleSoldier = new(
            Id: new BattleUnitId(1),
            WorldUnitId: new WorldUnitId("soldier"),
            DefinitionId: soldierDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 100,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [carried],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit battleEnemy = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), new WorldUnitId("enemy"), enemyDefinition.Id, BattleSide.Defender, hitpoints: 100);

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(battleLeader, GridPosition.FrontBottom, false)
                .Place(battleSoldier, GridPosition.BackBottom, false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(battleEnemy, GridPosition.FrontBottom, false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        engine.CheckCommand(new WorldMoveStackCommand(attackerStack.Id, defenderStack.Position), [
            new WorldStackMovementConsumedEvent(attackerStack.Id, 3),
            new WorldStackBattleStartedEvent(attackerStack.Id, defenderStack.Id, defenderStack.Position, expectedBattleState, IsDuel: false)
        ]);

        Assert.That(context.UnitStats.GetBattleUnitStats(expectedBattleState, battleSoldier.Id), Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 10,
            Regeneration: 5,
            Initiative: 40,
            Damage: 6,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));

        Assert.That(context.UnitStats.GetBattleUnitStats(expectedBattleState, battleEnemy.Id), Is.EqualTo(new UnitStats(
            MaxHitpoints: 100,
            Armor: 0,
            Regeneration: 5,
            Initiative: 30,
            Damage: 5,
            Accuracy: 100,
            ExperienceKilled: 0,
            ExperienceNextLevel: 1000)));

        Assert.That(
            context.UnitStats.GetBattleAttackStats(expectedBattleState, battleSoldier.Id, soldierDefinition.Attack),
            Is.EqualTo(new UnitAttackStats(Accuracy: 100, Damage: 6, DrainPercent: 0)));
    }
}
