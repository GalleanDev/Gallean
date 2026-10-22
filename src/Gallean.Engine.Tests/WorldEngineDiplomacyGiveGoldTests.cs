namespace Gallean.Engine.Tests;

public sealed class WorldEngineDiplomacyGiveGoldTests
{
    [Test]
    public void ImprovesRelationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 0,
            Previous: 0,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(250)),
            [
                new WorldGoldGivenEvent(player1.Id, player2.Id, 250),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 5)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0) })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(350, 0, 0, 0, 0, 0) }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 5 })
            }));
    }

    [Test]
    public void UnauthoredRacePairTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(250)),
            [new WorldGoldGivenEvent(player1.Id, player2.Id, 250)]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0) })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(350, 0, 0, 0, 0, 0) })
            }));
    }

    [Test]
    public void AddsToExistingRelationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(5000, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 40,
            Previous: 20,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(5000)),
            [
                new WorldGoldGivenEvent(player1.Id, player2.Id, 5000),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 100)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = WorldResourceSet.Empty })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(5000, 0, 0, 0, 0, 0) }),
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 100 })
            }));
    }

    [Test]
    public void PermanentWarFreezesRelationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 20,
            Previous: 20,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: true,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(1000)),
            [new WorldGoldGivenEvent(player1.Id, player2.Id, 1000)]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = WorldResourceSet.Empty })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(1000, 0, 0, 0, 0, 0) })
            }));
    }

    [Test]
    public void GiftBelowRelationStepTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 10,
            Previous: 10,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(30)),
            [new WorldGoldGivenEvent(player1.Id, player2.Id, 30)]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = new WorldResourceSet(270, 0, 0, 0, 0, 0) })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(30, 0, 0, 0, 0, 0) })
            }));
    }

    [Test]
    public void InsufficientGoldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(101)),
            WorldCommandError.InsufficientResources);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void EmptyGiftTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommandError(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(0)),
            WorldCommandError.DiplomacyGiftInvalid);

        Assert.That(engine.State, Is.EqualTo(before));
    }

    [Test]
    public void EnemiesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);
        WorldPlayer friend = TestHelpers.SimplePlayer(new WorldPlayerId(4), WorldRace.Dwarf, map);
        WorldPlayer lockedEnemy = TestHelpers.SimplePlayer(new WorldPlayerId(5), WorldRace.Elf, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 0,
            Previous: 0,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        WorldRelation enemyRelation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Human),
            Current: 50,
            Previous: 50,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        WorldRelation friendRelation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Dwarf),
            Current: 50,
            Previous: 50,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        WorldRelation lockedEnemyRelation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Elf),
            Current: 50,
            Previous: 50,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        WorldRelation targetEnemyRelation = new(
            new WorldRelationId(WorldRace.Human, WorldRace.Heretic),
            Current: 33,
            Previous: 33,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        WorldRelation targetFriendRelation = new(
            new WorldRelationId(WorldRace.Dwarf, WorldRace.Heretic),
            Current: 34,
            Previous: 34,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);
        WorldRelation targetLockedEnemyRelation = new(
            new WorldRelationId(WorldRace.Elf, WorldRace.Heretic),
            Current: 0,
            Previous: 0,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: true,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(TestHelpers.SimpleLord(friend.LordId, friend.Race, LordCategory.Warrior))
            .WithPlayer(friend)
            .WithDefinition(TestHelpers.SimpleLord(lockedEnemy.LordId, lockedEnemy.Race, LordCategory.Warrior))
            .WithPlayer(lockedEnemy)
            .WithRelation(relation)
            .WithRelation(enemyRelation)
            .WithRelation(friendRelation)
            .WithRelation(lockedEnemyRelation)
            .WithRelation(targetEnemyRelation)
            .WithRelation(targetFriendRelation)
            .WithRelation(targetLockedEnemyRelation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(500)),
            [
                new WorldGoldGivenEvent(player1.Id, player2.Id, 500),
                new WorldRelationChangedEvent(player1.Id, player2.Id, 10),
                new WorldRelationChangedEvent(player1.Id, enemy.Id, 45)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = WorldResourceSet.Empty })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0) }),
                Relations = before.Relations
                    .SetItem(relation.Id, relation with { Current = 10 })
                    .SetItem(enemyRelation.Id, enemyRelation with { Current = 45 })
            }));
    }

    [Test]
    public void UnauthoredRacePairEnemiesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);

        WorldRelation enemyRelation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Human),
            Current: 50,
            Previous: 50,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithRelation(enemyRelation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldDiplomacyActionCommand(player2.Id, new WorldDiplomacyGiveGoldAction(500)),
            [
                new WorldGoldGivenEvent(player1.Id, player2.Id, 500),
                new WorldRelationChangedEvent(player1.Id, enemy.Id, 45)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players
                    .SetItem(player1.Id, player1 with { Resources = WorldResourceSet.Empty })
                    .SetItem(player2.Id, player2 with { Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0) }),
                Relations = before.Relations.SetItem(enemyRelation.Id, enemyRelation with { Current = 45 })
            }));
    }
}
