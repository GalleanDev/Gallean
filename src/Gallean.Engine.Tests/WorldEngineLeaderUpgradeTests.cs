namespace Gallean.Engine.Tests;

public sealed class WorldEngineLeaderUpgradeTests
{
    private static UnitModifierDefinition AbilityModifier(string id, LeaderAbility abilities)
    {
        return TestHelpers.SimpleModifier(id) with { LeaderAbilities = abilities };
    }

    private static LeaderUpgradeDefinition Upgrade(LordDefinitionId lordId, string modifierId, int minLevel, int priority)
    {
        return new LeaderUpgradeDefinition(
            lordId,
            new UnitModifierDefinitionId(modifierId),
            minLevel,
            priority,
            new GlobalTextId($"name{modifierId}"),
            new GlobalTextId($"description{modifierId}"));
    }

    [Test]
    public void GrantedOnLevelUpTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 4, leadership: 6) with
            {
                ExperienceNextLevel = 10
            };
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Experience = 7 }, GridPosition.FrontBottom, false)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            units,
            movementPoints: 4);
        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit upgradedLeader = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("leader"),
            DefinitionId: leaderDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 110,
            Experience: 0,
            ExperienceGained: 0,
            Level: 2,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defenderDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(upgradedLeader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> survivingDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();
        UnitGrid<WorldUnit> survivingAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 110, Level = 2 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, survivingAttackerGrid, survivingDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId),
            new WorldLeaderUpgradeGrantedEvent(stack1.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with { Units = survivingAttackerGrid, PendingLeaderUpgrades = 1, BattlesWon = 1 })
                .Remove(stack2.Id)
        }));
    }

    [Test]
    public void SelectUpgradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 2 }, GridPosition.FrontBottom, false)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                units,
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(AbilityModifier("banner", LeaderAbility.BannerUse))
            .WithLeaderUpgrade(Upgrade(player.LordId, "banner", minLevel: 2, priority: 80))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(new WorldSelectLeaderUpgradeCommand(stack.Id, new UnitModifierDefinitionId("banner")), [
            new WorldLeaderUpgradeSelectedEvent(stack.Id, new UnitModifierDefinitionId("banner")),
            new WorldUnitModifierAppliedEvent(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitId("leader"),
                UnitModifier.Permanent(new UnitModifierDefinitionId("banner")))
        ]);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(
                TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 2, Modifiers = [UnitModifier.Permanent(new UnitModifierDefinitionId("banner"))] },
                GridPosition.FrontBottom,
                false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Units = expectedUnits, PendingLeaderUpgrades = 0 })
        }));
    }

    [Test]
    public void PostponeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(AbilityModifier("banner", LeaderAbility.BannerUse))
            .WithLeaderUpgrade(Upgrade(player.LordId, "banner", minLevel: 1, priority: 80))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(new WorldPostponeLeaderUpgradeCommand(stack.Id), [
            new WorldLeaderUpgradePostponedEvent(stack.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { LeaderUpgradePostponed = true })
        }));
    }

    [Test]
    public void PostponeNoPendingUpgradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(new WorldPostponeLeaderUpgradeCommand(stack.Id), WorldCommandError.LeaderUpgradeNotPending);
    }

    [Test]
    public void PostponeUpgradeAfterTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 4, leadership: 6) with
            {
                ExperienceNextLevel = 10
            };
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Experience = 7 }, GridPosition.FrontBottom, false)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                units,
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 1,
                LeaderUpgradePostponed = true
            };
        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit upgradedLeader = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("leader"),
            DefinitionId: leaderDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 110,
            Experience: 0,
            ExperienceGained: 0,
            Level: 2,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defenderDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(upgradedLeader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> survivingDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();
        UnitGrid<WorldUnit> survivingAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 110, Level = 2 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, survivingAttackerGrid, survivingDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId),
            new WorldLeaderUpgradeGrantedEvent(stack1.Id)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with { Units = survivingAttackerGrid, PendingLeaderUpgrades = 2, LeaderUpgradePostponed = false, BattlesWon = 1 })
                .Remove(stack2.Id)
        }));
    }

    [Test]
    public void NoPendingUpgradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(AbilityModifier("banner", LeaderAbility.BannerUse))
            .WithLeaderUpgrade(Upgrade(player.LordId, "banner", minLevel: 1, priority: 80))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldSelectLeaderUpgradeCommand(stack.Id, new UnitModifierDefinitionId("banner")),
            WorldCommandError.LeaderUpgradeNotPending);
    }

    [Test]
    public void UpgradeAboveLeaderLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(AbilityModifier("banner", LeaderAbility.BannerUse))
            .WithLeaderUpgrade(Upgrade(player.LordId, "banner", minLevel: 5, priority: 80))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldSelectLeaderUpgradeCommand(stack.Id, new UnitModifierDefinitionId("banner")),
            WorldCommandError.LeaderUpgradeNotAvailable);
    }

    [Test]
    public void UpgradeAlreadyLearnedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(
                TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 3, Modifiers = [UnitModifier.Permanent(new UnitModifierDefinitionId("banner"))] },
                GridPosition.FrontBottom,
                false)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                units,
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(AbilityModifier("banner", LeaderAbility.BannerUse))
            .WithLeaderUpgrade(Upgrade(player.LordId, "banner", minLevel: 2, priority: 80))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldSelectLeaderUpgradeCommand(stack.Id, new UnitModifierDefinitionId("banner")),
            WorldCommandError.LeaderUpgradeNotAvailable);
    }

    [Test]
    public void AiUpgradesOnTurnStartTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with { IsAi = true };

        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader2"), leaderDefinition),
            movementPoints: 4);

        WorldStack stack2 = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player2.Id,
                new WorldPosition(2, 0),
                units,
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 2
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(AbilityModifier("banner", LeaderAbility.BannerUse))
            .WithDefinition(AbilityModifier("orb", LeaderAbility.OrbUse))
            .WithDefinition(AbilityModifier("talisman", LeaderAbility.TalismanUse))
            .WithLeaderUpgrade(Upgrade(player2.LordId, "banner", minLevel: 1, priority: 80))
            .WithLeaderUpgrade(Upgrade(player2.LordId, "orb", minLevel: 1, priority: 99))
            .WithLeaderUpgrade(Upgrade(player2.LordId, "talisman", minLevel: 9, priority: 95))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(player2.Id),
            new WorldStackMovementRestoredEvent(stack2.Id, 4),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldLeaderUpgradeSelectedEvent(stack2.Id, new UnitModifierDefinitionId("orb")),
            new WorldUnitModifierAppliedEvent(
                new WorldUnitGroup.Stack(stack2.Id),
                new WorldUnitId("leader"),
                UnitModifier.Permanent(new UnitModifierDefinitionId("orb"))),
            new WorldLeaderUpgradeSelectedEvent(stack2.Id, new UnitModifierDefinitionId("banner")),
            new WorldUnitModifierAppliedEvent(
                new WorldUnitGroup.Stack(stack2.Id),
                new WorldUnitId("leader"),
                UnitModifier.Permanent(new UnitModifierDefinitionId("banner")))
        ]);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(
                TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with
                {
                    Modifiers =
                    [
                        UnitModifier.Permanent(new UnitModifierDefinitionId("orb")),
                        UnitModifier.Permanent(new UnitModifierDefinitionId("banner"))
                    ]
                },
                GridPosition.FrontBottom,
                false)
            .Build();

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks.SetItem(stack2.Id, stack2 with { Units = expectedUnits, PendingLeaderUpgrades = 0 })
        }));
    }

    [Test]
    public void LevelLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 15 }, GridPosition.FrontBottom, false)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            units,
            movementPoints: 4);
        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit upgradedLeader = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("leader"),
            DefinitionId: leaderDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 100,
            Experience: 0,
            ExperienceGained: 0,
            Level: 16,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defenderDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(upgradedLeader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> survivingDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();
        UnitGrid<WorldUnit> survivingAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 16 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, survivingAttackerGrid, survivingDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with { Units = survivingAttackerGrid, BattlesWon = 1 })
                .Remove(stack2.Id)
        }));
    }

    [Test]
    public void PendingLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 3 }, GridPosition.FrontBottom, false)
            .Build();

        WorldStack stack1 = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player1.Id,
                new WorldPosition(0, 0),
                units,
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 15
            };
        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .Build();

        BattleUnit upgradedLeader = new(
            Id: new BattleUnitId(0),
            WorldUnitId: new WorldUnitId("leader"),
            DefinitionId: leaderDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: 100,
            Experience: 0,
            ExperienceGained: 0,
            Level: 4,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: null,
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        BattleUnit deadDefender = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defenderDefinition.Id, BattleSide.Defender, hitpoints: 0);

        BattleState finalBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder().Place(upgradedLeader, GridPosition.FrontBottom, false).Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder().Place(deadDefender, GridPosition.FrontBottom, false).Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: new BattleOutcome(BattleSide.Attacker),
            Modifiers: BattleModifiers.None);

        UnitGrid<WorldUnit> survivingDefenderGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
            .Build();
        UnitGrid<WorldUnit> survivingAttackerGrid = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 4 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(new WorldResolveStackBattleCommand(stack1.Id, stack2.Id, finalBattleState, IsDuel: false), [
            new WorldStackBattleEndedEvent(stack1.Id, stack2.Id, BattleSide.Attacker, survivingAttackerGrid, survivingDefenderGrid),
            new WorldStackDestroyedEvent(stack2.Id, new WorldUnitGroup.Stack(stack1.Id), stack1.OwnerId)
        ]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Stacks = engine.State.Stacks
                .SetItem(stack1.Id, stack1 with { Units = survivingAttackerGrid, BattlesWon = 1 })
                .Remove(stack2.Id)
        }));
    }

    [Test]
    public void InfoTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(
                TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Level = 2, Modifiers = [UnitModifier.Permanent(new UnitModifierDefinitionId("wand"))] },
                GridPosition.FrontBottom,
                false)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                units,
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(AbilityModifier("banner", LeaderAbility.BannerUse))
            .WithDefinition(AbilityModifier("orb", LeaderAbility.OrbUse))
            .WithDefinition(AbilityModifier("talisman", LeaderAbility.TalismanUse))
            .WithDefinition(AbilityModifier("wand", LeaderAbility.WandScrollUse))
            .WithLeaderUpgrade(Upgrade(player.LordId, "banner", minLevel: 1, priority: 80))
            .WithLeaderUpgrade(Upgrade(player.LordId, "orb", minLevel: 2, priority: 99))
            .WithLeaderUpgrade(Upgrade(player.LordId, "talisman", minLevel: 9, priority: 95))
            .WithLeaderUpgrade(Upgrade(player.LordId, "wand", minLevel: 1, priority: 99))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldGetLeaderUpgradeInfoCommand.Result result = engine.CheckCommand(new WorldGetLeaderUpgradeInfoCommand(stack.Id), []);

        Assert.That(result, Is.EqualTo(new WorldGetLeaderUpgradeInfoCommand.Result(
        [
            new WorldGetLeaderUpgradeInfoCommand.Upgrade(Upgrade(player.LordId, "banner", minLevel: 1, priority: 80), IsPreferred: false, Error: null),
            new WorldGetLeaderUpgradeInfoCommand.Upgrade(Upgrade(player.LordId, "orb", minLevel: 2, priority: 99), IsPreferred: true, Error: null)
        ])));
    }

    [Test]
    public void KnownAbilityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 4, leadership: 4) with
            {
                LeaderAbilities = LeaderAbility.BannerUse
            };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
                movementPoints: 4) with
            {
                PendingLeaderUpgrades = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(AbilityModifier("banner", LeaderAbility.BannerUse))
            .WithDefinition(AbilityModifier("orb", LeaderAbility.OrbUse))
            .WithLeaderUpgrade(Upgrade(player.LordId, "banner", minLevel: 1, priority: 99))
            .WithLeaderUpgrade(Upgrade(player.LordId, "orb", minLevel: 1, priority: 80))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldGetLeaderUpgradeInfoCommand.Result result = engine.CheckCommand(new WorldGetLeaderUpgradeInfoCommand(stack.Id), []);

        Assert.That(result, Is.EqualTo(new WorldGetLeaderUpgradeInfoCommand.Result(
        [
            new WorldGetLeaderUpgradeInfoCommand.Upgrade(Upgrade(player.LordId, "orb", minLevel: 1, priority: 80), IsPreferred: true, Error: null)
        ])));
    }
}
