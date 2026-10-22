namespace Gallean.Engine.Tests;

public sealed class WorldEngineHealSpellTests
{
    private static readonly WorldSpellDefinitionId HealSpellId = new("heal");

    private static WorldSpellDefinition HealSpell(int hitpoints, int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(HealSpellId, new WorldHealSpellEffect(hitpoints)) with
        {
            AreaRadius = areaRadius
        };
    }

    private static WorldStack WoundedStack(
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
    public void HealsWoundedUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = HealSpell(hitpoints: 30, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldStack stack = WoundedStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0), hitpoints: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(HealSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    70,
                    WorldResourceSet.Empty)
            ]);
    }

    [Test]
    public void CapsAtMaxHitpointsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = HealSpell(hitpoints: 150, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldStack stack = WoundedStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0), hitpoints: 40);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(HealSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    100,
                    WorldResourceSet.Empty)
            ]);
    }

    [Test]
    public void EnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = HealSpell(hitpoints: 30, areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack own = WoundedStack(new WorldUnitId("ownLeader"), new WorldStackId("Own"), player.Id, leader, new WorldPosition(0, 0), hitpoints: 40);
        WorldStack theirs = WoundedStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(1, 1), hitpoints: 40);

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
            new WorldCastSpellCommand(HealSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(own.Id),
                    new WorldUnitId("ownLeader"),
                    70,
                    WorldResourceSet.Empty),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(theirs.Id),
                    new WorldUnitId("theirsLeader"),
                    64,
                    WorldResourceSet.Empty)
            ]);
    }

    [Test]
    public void GlobalCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = HealSpell(hitpoints: 80, areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldStack near = WoundedStack(new WorldUnitId("nearLeader"), new WorldStackId("Near"), player.Id, leader, new WorldPosition(0, 0), hitpoints: 10);
        WorldStack far = WoundedStack(new WorldUnitId("farLeader"), new WorldStackId("Far"), player.Id, leader, new WorldPosition(2, 2), hitpoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(near)
            .WithStack(far)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(HealSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, HealSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(near.Id), new WorldUnitId("nearLeader"), 90, WorldResourceSet.Empty),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(far.Id), new WorldUnitId("farLeader"), 90, WorldResourceSet.Empty)
            ]);
    }

    [Test]
    public void NothingWoundedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = HealSpell(hitpoints: 30, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldStack stack = WoundedStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0), hitpoints: 100);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(HealSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void LargeUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                IsLarge = true
            };

        WorldSpellDefinition spell = HealSpell(hitpoints: 30, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Hitpoints = 40 }, GridPosition.FrontBottom, true)
                .Build(),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(HealSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("leader"),
                    70,
                    WorldResourceSet.Empty)
            ]);
    }

    [Test]
    public void AreaFalloffTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = HealSpell(hitpoints: 50, areaRadius: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldStack ring0 = WoundedStack(new WorldUnitId("ring0Leader"), new WorldStackId("Ring0"), player.Id, leader, new WorldPosition(4, 0), hitpoints: 10);
        WorldStack ring1 = WoundedStack(new WorldUnitId("ring1Leader"), new WorldStackId("Ring1"), player.Id, leader, new WorldPosition(5, 0), hitpoints: 10);
        WorldStack ring2 = WoundedStack(new WorldUnitId("ring2Leader"), new WorldStackId("Ring2"), player.Id, leader, new WorldPosition(6, 0), hitpoints: 10);
        WorldStack ring3 = WoundedStack(new WorldUnitId("ring3Leader"), new WorldStackId("Ring3"), player.Id, leader, new WorldPosition(7, 0), hitpoints: 10);
        WorldStack ring4 = WoundedStack(new WorldUnitId("ring4Leader"), new WorldStackId("Ring4"), player.Id, leader, new WorldPosition(8, 0), hitpoints: 10);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(ring0)
            .WithStack(ring1)
            .WithStack(ring2)
            .WithStack(ring3)
            .WithStack(ring4)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(HealSpellId, new WorldSpellTarget(new WorldPosition(4, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(4, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(ring0.Id),
                    new WorldUnitId("ring0Leader"),
                    60,
                    WorldResourceSet.Empty),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(ring1.Id),
                    new WorldUnitId("ring1Leader"),
                    50,
                    WorldResourceSet.Empty),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(ring2.Id),
                    new WorldUnitId("ring2Leader"),
                    40,
                    WorldResourceSet.Empty),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(ring3.Id),
                    new WorldUnitId("ring3Leader"),
                    30,
                    WorldResourceSet.Empty),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(ring4.Id),
                    new WorldUnitId("ring4Leader"),
                    25,
                    WorldResourceSet.Empty)
            ]);
    }

    [Test]
    public void StackInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = HealSpell(hitpoints: 30, areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldStack inside = WoundedStack(new WorldUnitId("insideLeader"), new WorldStackId("Inside"), player.Id, leader, new WorldPosition(5, 5), hitpoints: 40);

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
            new WorldCastSpellCommand(HealSpellId, new WorldSpellTarget(new WorldPosition(5, 5), Radius: 0)),
            WorldCommandError.SpellTargetInCity);
    }

    [Test]
    public void GlobalCastStackInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = HealSpell(hitpoints: 30, areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [HealSpellId])
        };

        WorldStack inside = WoundedStack(new WorldUnitId("insideLeader"), new WorldStackId("Inside"), player.Id, leader, new WorldPosition(5, 5), hitpoints: 40);

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
            new WorldCastSpellCommand(HealSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, HealSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldUnitHealedEvent(new WorldUnitGroup.Stack(inside.Id), new WorldUnitId("insideLeader"), 70, WorldResourceSet.Empty)
            ]);
    }
}
