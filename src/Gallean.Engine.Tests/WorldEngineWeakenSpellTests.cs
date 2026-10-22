using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineWeakenSpellTests
{
    private static readonly WorldSpellDefinitionId WeakenSpellId = new("weaken");
    private static readonly UnitModifierDefinitionId ModifierId = new("modifier");

    private static WorldSpellDefinition WeakenSpell(int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(WeakenSpellId, new WorldWeakenSpellEffect(ModifierId)) with
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
        ImmutableArray<UnitModifier> modifiers)
    {
        return TestHelpers.SimpleStack(
            id,
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(leaderId, leader) with { Modifiers = modifiers }, GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);
    }

    [Test]
    public void EnchantsEnemyUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = WeakenSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [WeakenSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldStack theirs = UnitStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(0, 0), []);

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
            new WorldCastSpellCommand(WeakenSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    WeakenSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(theirs.Id),
                    new WorldUnitId("theirsLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(WeakenSpellId, 1)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(theirs.Id, theirs with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("theirsLeader"), leader) with { Modifiers = [new UnitModifier(ModifierId, RemainingTurns: 1)] }, GridPosition.BackBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void AlreadyWeakenedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = WeakenSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [WeakenSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack theirs = UnitStack(
            new WorldUnitId("theirsLeader"),
            new WorldStackId("Theirs"),
            enemy.Id,
            leader,
            new WorldPosition(0, 0),
            [new UnitModifier(ModifierId, RemainingTurns: 1)]);

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
            new WorldCastSpellCommand(WeakenSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void OwnStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = WeakenSpell(areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [WeakenSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack own = UnitStack(new WorldUnitId("ownLeader"), new WorldStackId("Own"), player.Id, leader, new WorldPosition(0, 0), []);
        WorldStack theirs = UnitStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(1, 1), []);

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
            new WorldCastSpellCommand(WeakenSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    WeakenSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(own.Id),
                    new WorldUnitId("ownLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1)),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(theirs.Id),
                    new WorldUnitId("theirsLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(WeakenSpellId, 1)
                    }
                }),
                Stacks = engine.State.Stacks
                    .SetItem(own.Id, UnitStack(new WorldUnitId("ownLeader"), own.Id, player.Id, leader, new WorldPosition(0, 0), [new UnitModifier(ModifierId, RemainingTurns: 1)]))
                    .SetItem(theirs.Id, UnitStack(new WorldUnitId("theirsLeader"), theirs.Id, enemy.Id, leader, new WorldPosition(1, 1), [new UnitModifier(ModifierId, RemainingTurns: 1)]))
            }));
    }

    [Test]
    public void AlliedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = WeakenSpell(areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [WeakenSpellId])
        };

        WorldPlayer ally = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Heretic, map);

        WorldRelation alliance = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Human),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldStack own = UnitStack(new WorldUnitId("ownLeader"), new WorldStackId("Own"), player.Id, leader, new WorldPosition(0, 0), []);
        WorldStack allied = UnitStack(new WorldUnitId("alliedLeader"), new WorldStackId("Allied"), ally.Id, leader, new WorldPosition(1, 0), []);
        WorldStack theirs = UnitStack(new WorldUnitId("theirsLeader"), new WorldStackId("Theirs"), enemy.Id, leader, new WorldPosition(1, 1), []);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(ally.LordId, ally.Race, LordCategory.Warrior))
            .WithPlayer(ally)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithRelation(alliance)
            .WithStack(own)
            .WithStack(allied)
            .WithStack(theirs)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(WeakenSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    WeakenSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(own.Id),
                    new WorldUnitId("ownLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1)),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(allied.Id),
                    new WorldUnitId("alliedLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1)),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(theirs.Id),
                    new WorldUnitId("theirsLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(WeakenSpellId, 1)
                    }
                }),
                Stacks = engine.State.Stacks
                    .SetItem(own.Id, UnitStack(new WorldUnitId("ownLeader"), own.Id, player.Id, leader, new WorldPosition(0, 0), [new UnitModifier(ModifierId, RemainingTurns: 1)]))
                    .SetItem(allied.Id, UnitStack(new WorldUnitId("alliedLeader"), allied.Id, ally.Id, leader, new WorldPosition(1, 0), [new UnitModifier(ModifierId, RemainingTurns: 1)]))
                    .SetItem(theirs.Id, UnitStack(new WorldUnitId("theirsLeader"), theirs.Id, enemy.Id, leader, new WorldPosition(1, 1), [new UnitModifier(ModifierId, RemainingTurns: 1)]))
            }));
    }

    [Test]
    public void GlobalCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = WeakenSpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [WeakenSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack near = UnitStack(new WorldUnitId("nearLeader"), new WorldStackId("Near"), enemy.Id, leader, new WorldPosition(0, 0), []);
        WorldStack own = UnitStack(new WorldUnitId("ownLeader"), new WorldStackId("Own"), player.Id, leader, new WorldPosition(1, 1), []);
        WorldStack far = UnitStack(new WorldUnitId("farLeader"), new WorldStackId("Far"), enemy.Id, leader, new WorldPosition(2, 2), []);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(near)
            .WithStack(own)
            .WithStack(far)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(WeakenSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, WeakenSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(near.Id),
                    new WorldUnitId("nearLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1)),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(far.Id),
                    new WorldUnitId("farLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(WeakenSpellId, 1)
                    }
                }),
                Stacks = engine.State.Stacks
                    .SetItem(near.Id, near with
                    {
                        Units = new UnitGrid<WorldUnit>.Builder()
                            .Place(TestHelpers.SimpleUnit(new WorldUnitId("nearLeader"), leader) with { Modifiers = [new UnitModifier(ModifierId, RemainingTurns: 1)] }, GridPosition.BackBottom, false)
                            .Build()
                    })
                    .SetItem(far.Id, far with
                    {
                        Units = new UnitGrid<WorldUnit>.Builder()
                            .Place(TestHelpers.SimpleUnit(new WorldUnitId("farLeader"), leader) with { Modifiers = [new UnitModifier(ModifierId, RemainingTurns: 1)] }, GridPosition.BackBottom, false)
                            .Build()
                    })
            }));
    }
}
