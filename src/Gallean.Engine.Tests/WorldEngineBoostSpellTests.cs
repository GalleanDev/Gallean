using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineBoostSpellTests
{
    private static readonly WorldSpellDefinitionId BoostSpellId = new("boost");
    private static readonly UnitModifierDefinitionId ModifierId = new("modifier");

    private static WorldSpellDefinition BoostSpell(int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(BoostSpellId, new WorldBoostSpellEffect(ModifierId)) with
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
    public void EnchantsOwnUnitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = BoostSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
        };

        WorldStack stack = UnitStack(new WorldUnitId("stack1Leader"), new WorldStackId("Stack1"), player.Id, leader, new WorldPosition(0, 0), []);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(BoostSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    BoostSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);
    }

    [Test]
    public void AlreadyBoostedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = BoostSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
        };

        WorldStack stack = UnitStack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leader,
            new WorldPosition(0, 0),
            [new UnitModifier(ModifierId, RemainingTurns: 1)]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(BoostSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void EnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = BoostSpell(areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
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
            new WorldCastSpellCommand(BoostSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    BoostSpellId,
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
    }

    [Test]
    public void GlobalCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = BoostSpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
        };

        WorldStack near = UnitStack(new WorldUnitId("nearLeader"), new WorldStackId("Near"), player.Id, leader, new WorldPosition(0, 0), []);
        WorldStack far = UnitStack(new WorldUnitId("farLeader"), new WorldStackId("Far"), player.Id, leader, new WorldPosition(2, 2), []);

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
            new WorldCastSpellCommand(BoostSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, BoostSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(near.Id),
                    new WorldUnitId("nearLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1)),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(far.Id),
                    new WorldUnitId("farLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);
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

        WorldSpellDefinition spell = BoostSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
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
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(BoostSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    BoostSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("leader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(BoostSpellId, 1)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Modifiers = [new UnitModifier(ModifierId, RemainingTurns: 1)] }, GridPosition.FrontBottom, true)
                        .Build()
                })
            }));
    }

    [Test]
    public void ModifierLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = BoostSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
        };

        WorldStack stack = UnitStack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leader,
            new WorldPosition(0, 0),
            [
                UnitModifier.Permanent(new UnitModifierDefinitionId("carried1")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("carried2")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("carried3")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("carried4")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("carried5")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("carried6")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("carried7")),
                UnitModifier.Permanent(new UnitModifierDefinitionId("carried8"))
            ]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(BoostSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    BoostSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(BoostSpellId, 1)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(
                            TestHelpers.SimpleUnit(new WorldUnitId("stack1Leader"), leader) with
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
                                    new UnitModifier(ModifierId, RemainingTurns: 1)
                                ]
                            },
                            GridPosition.BackBottom,
                            false)
                        .Build()
                })
            }));
    }

    [Test]
    public void AlliedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = BoostSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
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

        WorldStack own = UnitStack(new WorldUnitId("ownLeader"), new WorldStackId("Own"), player.Id, leader, new WorldPosition(0, 0), []);
        WorldStack allied = UnitStack(new WorldUnitId("alliedLeader"), new WorldStackId("Allied"), ally.Id, leader, new WorldPosition(1, 1), []);

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

        engine.CheckCommand(
            new WorldCastSpellCommand(BoostSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    BoostSpellId,
                    new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(allied.Id),
                    new WorldUnitId("alliedLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);
    }

    [Test]
    public void GlobalCastAlliedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = BoostSpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
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

        WorldStack own = UnitStack(new WorldUnitId("ownLeader"), new WorldStackId("Own"), player.Id, leader, new WorldPosition(0, 0), []);
        WorldStack allied = UnitStack(new WorldUnitId("alliedLeader"), new WorldStackId("Allied"), ally.Id, leader, new WorldPosition(1, 1), []);

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

        engine.CheckCommand(
            new WorldCastSpellCommand(BoostSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, BoostSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(own.Id),
                    new WorldUnitId("ownLeader"),
                    new UnitModifier(ModifierId, RemainingTurns: 1))
            ]);
    }

    [Test]
    public void StackInCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition spell = BoostSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [BoostSpellId])
        };

        WorldStack inside = UnitStack(new WorldUnitId("insideLeader"), new WorldStackId("Inside"), player.Id, leader, new WorldPosition(5, 5), []);

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
            new WorldCastSpellCommand(BoostSpellId, new WorldSpellTarget(new WorldPosition(5, 5), Radius: 0)),
            WorldCommandError.SpellTargetInCity);
    }
}
