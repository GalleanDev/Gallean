using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineCastSpellTests
{
    private static readonly WorldSpellDefinitionId RestoreSpellId = new("restore");
    private static readonly WorldSpellDefinitionId RestoreSpellId2 = new("restore2");

    private static WorldSpellDefinition RestoreSpell(WorldSpellDefinitionId id, int percent, WorldResourceSet castingCost)
    {
        return TestHelpers.SimpleSpellDefinition(id, new WorldRestoreMovementSpellEffect(percent)) with
        {
            CastingCost = castingCost
        };
    }

    private static WorldStack LeaderStack(WorldUnitId leaderId, WorldPlayerId ownerId, UnitDefinition leader, WorldPosition position, int movementPoints)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(leaderId, leader), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: movementPoints);
    }

    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: new WorldResourceSet(0, 0, 0, 40, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 100, 0, 0),
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), spell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 7)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(0, 0, 0, 60, 0, 0),
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RestoreSpellId, 1)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 7
                })
            }));
    }

    [Test]
    public void RestoreAtMaxMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 100, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 8);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), spell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 10
                })
            }));
    }

    [Test]
    public void NotKnownTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellNotKnown);
    }

    [Test]
    public void TargetWithoutOwnedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: new WorldResourceSet(0, 0, 0, 40, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 10, 0, 0),
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.InsufficientResources);
    }

    [Test]
    public void RestoredPercentRoundingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 30, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), spell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 1)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 1
                })
            }));
    }

    [Test]
    public void AlreadyCastThisTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), spell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 7)
            ]);

        Assert.That(engine.State.Players[player.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions
        {
            SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RestoreSpellId, 1)
        }));

        engine.CheckCommandError(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellAlreadyCastThisTurn);
    }

    [Test]
    public void DifferentSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: WorldResourceSet.Empty);
        WorldSpellDefinition otherSpell = RestoreSpell(RestoreSpellId2, percent: 50, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId, RestoreSpellId2])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(otherSpell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId2, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId2, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), otherSpell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty
                            .Add(RestoreSpellId, 1)
                            .Add(RestoreSpellId2, 1)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 10
                })
            }));
    }

    [Test]
    public void TurnStartResetsCastsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        Assert.That(engine.State.Players[player.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions
        {
            SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RestoreSpellId, 1)
        }));

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State.Players[player.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions()));

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), spell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 10)
            ]);
    }

    [Test]
    public void MageLordCastsTwiceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), spell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 7)
            ]);

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), spell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RestoreSpellId, 2)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    MovementPoints = 10
                })
            }));

        engine.CheckCommandError(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellAlreadyCastThisTurn);
    }

    [Test]
    public void GuildmasterLordCastsOnceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        Assert.That(engine.State.Players[player.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions
        {
            SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RestoreSpellId, 1)
        }));

        engine.CheckCommandError(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellAlreadyCastThisTurn);
    }

    [Test]
    public void MageLordTurnStartResetsCastsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = RestoreSpell(RestoreSpellId, percent: 50, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldStack stack = LeaderStack(new WorldUnitId("leader"), player.Id, leader, new WorldPosition(0, 0), movementPoints: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        _ = engine.DispatchCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        _ = engine.DispatchCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        Assert.That(engine.State.Players[player.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions
        {
            SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RestoreSpellId, 2)
        }));

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State.Players[player.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions()));

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0), spell.CastingCost, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(stack.Id, 10)
            ]);
    }

    [Test]
    public void GlobalRestoreMovementTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 10, leadership: 3);
        WorldSpellDefinition spell = TestHelpers.SimpleSpellDefinition(RestoreSpellId, new WorldRestoreMovementSpellEffect(100)) with
        {
            AreaRadius = null
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack near = TestHelpers.SimpleStack(
            new WorldStackId("Near"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 2);
        WorldStack far = TestHelpers.SimpleStack(
            new WorldStackId("Far"),
            player.Id,
            new WorldPosition(2, 2),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 0);
        WorldStack theirs = TestHelpers.SimpleStack(
            new WorldStackId("Theirs"),
            enemy.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader3"), leader), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(near)
            .WithStack(far)
            .WithStack(theirs)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RestoreSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, RestoreSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldStackMovementRestoredEvent(near.Id, 10),
                new WorldStackMovementRestoredEvent(far.Id, 10)
            ]);
    }
}
