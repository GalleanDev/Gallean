namespace Gallean.Engine.Tests;

public sealed class WorldEngineAttackSpellTests
{
    private static readonly WorldSpellDefinitionId AttackSpellId = new("attack");

    private static WorldSpellDefinition AttackSpell(int damage, int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(AttackSpellId, new WorldAttackSpellEffect(UnitAttackSource.Fire, damage)) with
        {
            AreaRadius = areaRadius
        };
    }

    private static WorldStack UnitStack(
        WorldUnitId leaderId,
        WorldStackId id,
        WorldPlayerId ownerId,
        UnitDefinition leader,
        WorldPosition position,
        int hitpoints)
    {
        return TestHelpers.SimpleStack(
            id,
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(leaderId, leader) with { Hitpoints = hitpoints }, GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);
    }

    [Test]
    public void DamagesEnemyUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldStack theirs = UnitStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(0, 0), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("theirsLeader"), 60, player.Id)
            ]);
    }

    [Test]
    public void OwnStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack own = UnitStack(new WorldUnitId("ownLeader"), new WorldStackId("Own"), player.Id, leader, new WorldPosition(0, 0), hitpoints: 100);
        WorldStack theirs = UnitStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(1, 1), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(own)
            .WithStack(theirs)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(own.Id), new WorldUnitId("ownLeader"), 60, player.Id),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("theirsLeader"), 68, player.Id)
            ]);
    }

    [Test]
    public void AreaFalloffTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = AttackSpell(damage: 50, areaRadius: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack ring0 = UnitStack(new WorldUnitId("ring0Leader"), new WorldStackId("Ring0"), enemy.Id, leader, new WorldPosition(4, 0), hitpoints: 100);
        WorldStack ring1 = UnitStack(new WorldUnitId("ring1Leader"), new WorldStackId("Ring1"), enemy.Id, leader, new WorldPosition(5, 0), hitpoints: 100);
        WorldStack ring2 = UnitStack(new WorldUnitId("ring2Leader"), new WorldStackId("Ring2"), enemy.Id, leader, new WorldPosition(6, 0), hitpoints: 100);
        WorldStack ring3 = UnitStack(new WorldUnitId("ring3Leader"), new WorldStackId("Ring3"), enemy.Id, leader, new WorldPosition(7, 0), hitpoints: 100);
        WorldStack ring4 = UnitStack(new WorldUnitId("ring4Leader"), new WorldStackId("Ring4"), enemy.Id, leader, new WorldPosition(8, 0), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(ring0)
            .WithStack(ring1)
            .WithStack(ring2)
            .WithStack(ring3)
            .WithStack(ring4)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(4, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(4, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(ring0.Id), new WorldUnitId("ring0Leader"), 50, player.Id),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(ring1.Id), new WorldUnitId("ring1Leader"), 60, player.Id),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(ring2.Id), new WorldUnitId("ring2Leader"), 70, player.Id),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(ring3.Id), new WorldUnitId("ring3Leader"), 80, player.Id),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(ring4.Id), new WorldUnitId("ring4Leader"), 85, player.Id)
            ]);
    }

    [Test]
    public void KillingEveryUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = AttackSpell(damage: 125, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldStack theirs = UnitStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(0, 0), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("theirsLeader"), 0, player.Id),
                new WorldStackDestroyedEvent(theirs.Id, Destroyer: null, player.Id)
            ]);

        Assert.That(engine.State.Stacks, Is.Empty);
    }

    [Test]
    public void ImmuneUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always)]
            };

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 50, damage: 10);
        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack theirs = TestHelpers.SimpleStack(
            new WorldStackId("Theirs"),
            enemy.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("soldier"), 60, player.Id)
            ]);
    }

    [Test]
    public void ImmuneTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always)]
            };

        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldStack theirs = UnitStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(0, 0), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetImmune);
    }

    [Test]
    public void ImmuneStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        UnitDefinition warded = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("warded", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                Wards = [new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Always)]
            };

        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack targeted = UnitStack(new WorldUnitId("targetedLeader"), new WorldStackId("Targeted"), enemy.Id, leader, new WorldPosition(0, 0), hitpoints: 100);
        WorldStack immune = UnitStack(new WorldUnitId("immuneLeader"), new WorldStackId("Immune"), enemy.Id, warded, new WorldPosition(1, 1), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(warded)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(targeted)
            .WithStack(immune)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(targeted.Id), new WorldUnitId("targetedLeader"), 60, player.Id)
            ]);
    }

    [Test]
    public void KillingLargeUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                IsLarge = true
            };

        WorldSpellDefinition spell = AttackSpell(damage: 125, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack theirs = TestHelpers.SimpleStack(
            new WorldStackId("Theirs"),
            enemy.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, true)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("leader"), 0, player.Id),
                new WorldStackDestroyedEvent(theirs.Id, Destroyer: null, player.Id)
            ]);

        Assert.That(engine.State.Stacks, Is.Empty);
    }

    [Test]
    public void NoEnemyInAreaTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer ally = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldRelation alliance = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Human),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldStack own = UnitStack(new WorldUnitId("ownLeader"), new WorldStackId("Own"), player.Id, leader, new WorldPosition(0, 0), hitpoints: 100);
        WorldStack allied = UnitStack(new WorldUnitId("alliedLeader"), new WorldStackId("Allied"), ally.Id, leader, new WorldPosition(1, 1), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(ally.LordId, ally.Race, LordCategory.Warrior))
            .WithPlayer(ally)
            .WithRelation(alliance)
            .WithStack(own)
            .WithStack(allied)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void StackInCityInAreaTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack inside = UnitStack(new WorldUnitId("insideLeader"), new WorldStackId("Inside"), enemy.Id, leader, new WorldPosition(5, 5), hitpoints: 100);
        WorldStack outside = UnitStack(new WorldUnitId("outsideLeader"), new WorldStackId("Outside"), enemy.Id, leader, new WorldPosition(6, 6), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), enemy.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(inside)
            .WithStack(outside)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(6, 6), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(6, 6), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(outside.Id), new WorldUnitId("outsideLeader"), 60, player.Id)
            ]);
    }

    [Test]
    public void StackInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldStack inside = UnitStack(new WorldUnitId("insideLeader"), new WorldStackId("Inside"), enemy.Id, leader, new WorldPosition(5, 5), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), enemy.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(inside)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(5, 5), Radius: 0)),
            WorldCommandError.SpellTargetInCity);
    }

    [Test]
    public void IllusionStackInAreaTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition illusion = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                Category = UnitCategory.Illusion
            };

        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack theirs = UnitStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(0, 0), hitpoints: 100);
        WorldStack mirage = UnitStack(new WorldUnitId("mirageLeader"), new WorldStackId("Mirage"), enemy.Id, illusion, new WorldPosition(1, 1), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(illusion)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(theirs)
            .WithStack(mirage)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    AttackSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(theirs.Id), new WorldUnitId("theirsLeader"), 60, player.Id)
            ]);
    }

    [Test]
    public void IllusionStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition illusion = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("illusion", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                Category = UnitCategory.Illusion
            };

        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldStack mirage = UnitStack(new WorldUnitId("mirageLeader"), new WorldStackId("Mirage"), enemy.Id, illusion, new WorldPosition(0, 0), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(illusion)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(mirage)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetImmune);
    }

    [Test]
    public void AreaCenteredOnCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = AttackSpell(damage: 40, areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [AttackSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldStack outside = UnitStack(new WorldUnitId("outsideLeader"), new WorldStackId("Outside"), enemy.Id, leader, new WorldPosition(6, 6), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), enemy.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(outside)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(AttackSpellId, new WorldSpellTarget(new WorldPosition(5, 5), Radius: 0)),
            WorldCommandError.SpellTargetInCity);
    }
}
