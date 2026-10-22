namespace Gallean.Engine.Tests;

public sealed class WorldEngineInvisibilitySpellTests
{
    private static readonly WorldSpellDefinitionId InvisibilitySpellId = new("invisibility");

    private static WorldSpellDefinition InvisibilitySpell(int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(InvisibilitySpellId, new WorldInvisibilitySpellEffect()) with
        {
            Level = 3,
            AreaRadius = areaRadius
        };
    }

    private static WorldStack LeaderStack(
        WorldUnitId leaderId,
        WorldStackId id,
        WorldPlayerId ownerId,
        UnitDefinition leaderDefinition,
        WorldPosition position)
    {
        return TestHelpers.SimpleStack(
            id,
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(leaderId, leaderDefinition), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);
    }

    [Test]
    public void ConcealsTargetStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(InvisibilitySpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    InvisibilitySpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldStackConcealedEvent(stack.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with { Concealed = true })
            }));
    }

    [Test]
    public void GlobalCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack first = LeaderStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0));
        WorldStack second = LeaderStack(new WorldUnitId("stack2Leader"), new WorldStackId("Stack2"), player.Id, leader, new WorldPosition(2, 1));
        WorldStack enemyStack = LeaderStack(new WorldUnitId("stack3Leader"), new WorldStackId("Stack3"), enemy.Id, leader, new WorldPosition(2, 0));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(first)
            .WithStack(second)
            .WithStack(enemyStack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(InvisibilitySpellId, Target: null),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    InvisibilitySpellId,
                    Target: null,
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldStackConcealedEvent(first.Id),
                new WorldStackConcealedEvent(second.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(first.Id, first with { Concealed = true })
                    .SetItem(second.Id, second with { Concealed = true })
            }));
    }

    [Test]
    public void EnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack enemyStack = LeaderStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), enemy.Id, leader, new WorldPosition(1, 1));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(enemyStack)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(InvisibilitySpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void AlreadyConcealedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0)) with
        {
            Concealed = true
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(InvisibilitySpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void CastOnInvisibleStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldStack hiddenStack = LeaderStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(1, 1)) with
        {
            Concealed = true
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(1, enemy.Id))
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(hiddenStack)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(InvisibilitySpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            WorldCommandError.SpellTargetConcealed);
    }

    [Test]
    public void MoveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = LeaderStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0)) with
        {
            Concealed = true
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, new WorldPosition(1, 0)),
            [
                new WorldStackMovingEvent(
                    stack.Id,
                    new WorldPosition(0, 0),
                    new WorldPosition(1, 0),
                    WorldStackDirection.SouthEast,
                    4,
                    [], null),
                new WorldStackRevealedEvent(stack.Id),
                new WorldStackMovedEvent(
                    stack.Id,
                    new WorldPosition(0, 0),
                    new WorldPosition(1, 0),
                    WorldStackDirection.SouthEast,
                    4,
                    [], null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Position = new WorldPosition(1, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 4,
                    Concealed = false
                })
            }));
    }

    [Test]
    public void UseItemTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        ItemDefinition potion = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("potion1"), ItemCategory.PotionHeal) with
        {
            Effect = new HealItemEffect(50)
        };
        WorldItem potionItem = new(new WorldItemId("Potion"), potion.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 40 }, GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([potionItem]),
                Concealed = true
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(potion)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(stack.Id, potionItem.Id, new WorldItemTarget.Unit(new WorldUnitId("leader"))),
            [
                new WorldItemUsedEvent(stack.Id, potionItem, RemainingCharges: null, Consumed: true),
                new WorldStackRevealedEvent(stack.Id),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("leader"), 90, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 90 }, GridPosition.BackBottom, false)
                        .Build(),
                    Concealed = false
                })
            }));
    }

    [Test]
    public void ScrollTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.WandScrollUse
            };

        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: 0);

        ItemDefinition scroll = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("scroll1"), ItemCategory.Scroll) with
        {
            Effect = new SpellItemEffect(InvisibilitySpellId, WorldResourceSet.Empty)
        };
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = LeaderStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0)) with
        {
            Inventory = new WorldInventory([scrollItem])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(
                stack.Id,
                scrollItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))),
            [
                new WorldItemUsedEvent(stack.Id, scrollItem, RemainingCharges: null, Consumed: true),
                new WorldSpellCastEvent(
                    player.Id,
                    InvisibilitySpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Item),
                new WorldStackConcealedEvent(stack.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Concealed = true
                })
            }));
    }

    [Test]
    public void AttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Concealed = true
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            enemy.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(stack)
            .WithStack(enemyStack)
            .Build();

        BattleState expectedBattleState = new(
            AttackerGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(0), new WorldUnitId("attacker"), attacker.Id, BattleSide.Attacker, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            DefenderGrid: new UnitGrid<BattleUnit>.Builder()
                .Place(
                    TestHelpers.SimpleBattleUnit(new BattleUnitId(1), new WorldUnitId("defender"), defender.Id, BattleSide.Defender, hitpoints: 10),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            CurrentUnitTurn: new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue: [],
            WaitingQueue: [],
            Items: [],
            CurrentRound: BattleRound.Normal(1),
            Outcome: null,
            Modifiers: BattleModifiers.None);

        engine.CheckCommand(
            new WorldMoveStackCommand(stack.Id, enemyStack.Position),
            [
                new WorldStackMovementConsumedEvent(stack.Id, 3),
                new WorldStackBattleStartedEvent(stack.Id, enemyStack.Id, enemyStack.Position, expectedBattleState, IsDuel: false),
                new WorldStackRevealedEvent(stack.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 3,
                    Concealed = false
                })
            }));
    }

    [Test]
    public void ThiefActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();

        UnitDefinition warlord = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("warlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = TestHelpers.SimpleStack(
                new WorldStackId("thief"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 20) with
            {
                Concealed = true
            };

        WorldStack targetStack = TestHelpers.SimpleStack(
            new WorldStackId("enemy"),
            enemy.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(warlord)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.PoisonStack, 90))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Guildmaster))
            .WithPlayer(enemy)
            .WithStack(thiefStack)
            .WithStack(targetStack)
            .Build();

        UnitGrid<WorldUnit> poisoned = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("warlord"), warlord) with { Hitpoints = 80 }, GridPosition.FrontBottom, false)
            .Build();

        engine.CheckCommand(
            new WorldThiefActionCommand(thiefStack.Id, new WorldThiefPoisonAction(targetStack.Position)),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.PoisonStack, enemy.Id),
                new WorldStackRevealedEvent(thiefStack.Id),
                new WorldStackPoisonedEvent(targetStack.Id, poisoned),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks
                    .SetItem(targetStack.Id, targetStack with { Units = poisoned })
                    .SetItem(thiefStack.Id, thiefStack with
                    {
                        MovementPoints = 10,
                        Concealed = false
                    })
            }));
    }

    [Test]
    public void StackInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldStack inside = LeaderStack(new WorldUnitId("insideLeader"), new WorldStackId("Inside"), player.Id, leader, new WorldPosition(5, 5));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), player.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(inside)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(InvisibilitySpellId, new WorldSpellTarget(new WorldPosition(5, 5), Radius: 0)),
            WorldCommandError.SpellTargetInCity);
    }

    [Test]
    public void GlobalCastStackInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldStack inside = LeaderStack(new WorldUnitId("insideLeader"), new WorldStackId("Inside"), player.Id, leader, new WorldPosition(5, 5));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), player.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(inside)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(InvisibilitySpellId, Target: null),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    InvisibilitySpellId,
                    Target: null,
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldStackConcealedEvent(inside.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(inside.Id, inside with { Concealed = true })
            }));
    }

    [Test]
    public void IllusionStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition illusion = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                Category = UnitCategory.Illusion
            };

        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldStack mirage = LeaderStack(new WorldUnitId("mirageLeader"), new WorldStackId("Mirage"), player.Id, illusion, new WorldPosition(0, 0));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(illusion)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(mirage)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(InvisibilitySpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void GlobalCastIllusionStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition illusion = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                Category = UnitCategory.Illusion
            };

        WorldSpellDefinition spell = InvisibilitySpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [InvisibilitySpellId])
        };

        WorldStack mirage = LeaderStack(new WorldUnitId("mirageLeader"), new WorldStackId("Mirage"), player.Id, illusion, new WorldPosition(0, 0));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(illusion)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(mirage)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(InvisibilitySpellId, Target: null),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    InvisibilitySpellId,
                    Target: null,
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldStackConcealedEvent(mirage.Id)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(mirage.Id, mirage with { Concealed = true })
            }));
    }
}
