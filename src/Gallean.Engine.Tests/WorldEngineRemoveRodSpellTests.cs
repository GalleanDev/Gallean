using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineRemoveRodSpellTests
{
    private static readonly WorldSpellDefinitionId RemoveRodSpellId = new("remove-rod");

    private static WorldSpellDefinition RemoveRodSpell(int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(RemoveRodSpellId, new WorldRemoveRodSpellEffect()) with
        {
            Level = 5,
            AreaRadius = areaRadius
        };
    }

    [Test]
    public void DestroysTargetRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldSpellDefinition spell = RemoveRodSpell(areaRadius: 0);

        WorldPlayer caster = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [RemoveRodSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldRod rod = new(new WorldRodId("Rod0001"), enemy.Id, new WorldPosition(2, 2));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(caster.LordId, caster.Race, LordCategory.Warrior))
            .WithPlayer(caster)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithRod(rod)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RemoveRodSpellId, new WorldSpellTarget(new WorldPosition(2, 2), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    caster.Id,
                    RemoveRodSpellId,
                    new WorldSpellTarget(new WorldPosition(2, 2), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldRodDestroyedEvent(rod.Id, caster.Id, enemy.Id, new WorldPosition(2, 2))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
                Players = engine.State.Players.SetItem(caster.Id, caster with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RemoveRodSpellId, 1)
                    }
                })
            }));
    }

    [Test]
    public void FootprintTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldSpellDefinition spell = RemoveRodSpell(areaRadius: 1);

        WorldPlayer caster = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [RemoveRodSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRod inside = new(new WorldRodId("Rod0001"), enemy.Id, new WorldPosition(1, 1));
        WorldRod alsoInside = new(new WorldRodId("Rod0002"), enemy.Id, new WorldPosition(3, 3));
        WorldRod outside = new(new WorldRodId("Rod0003"), enemy.Id, new WorldPosition(4, 4));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(caster.LordId, caster.Race, LordCategory.Warrior))
            .WithPlayer(caster)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithRod(inside)
            .WithRod(alsoInside)
            .WithRod(outside)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RemoveRodSpellId, new WorldSpellTarget(new WorldPosition(2, 2), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    caster.Id,
                    RemoveRodSpellId,
                    new WorldSpellTarget(new WorldPosition(2, 2), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldRodDestroyedEvent(inside.Id, caster.Id, enemy.Id, new WorldPosition(1, 1)),
                new WorldRodDestroyedEvent(alsoInside.Id, caster.Id, enemy.Id, new WorldPosition(3, 3))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty.Add(outside.Id, outside),
                Players = engine.State.Players.SetItem(caster.Id, caster with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RemoveRodSpellId, 1)
                    }
                })
            }));
    }

    [Test]
    public void GlobalCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 4);

        WorldSpellDefinition spell = RemoveRodSpell(areaRadius: null);

        WorldPlayer caster = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [RemoveRodSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRod near = new(new WorldRodId("Rod0001"), enemy.Id, new WorldPosition(0, 0));
        WorldRod far = new(new WorldRodId("Rod0002"), enemy.Id, new WorldPosition(3, 3));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(caster.LordId, caster.Race, LordCategory.Warrior))
            .WithPlayer(caster)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithRod(near)
            .WithRod(far)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RemoveRodSpellId, Target: null),
            [
                new WorldSpellCastEvent(caster.Id, RemoveRodSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldRodDestroyedEvent(near.Id, caster.Id, enemy.Id, new WorldPosition(0, 0)),
                new WorldRodDestroyedEvent(far.Id, caster.Id, enemy.Id, new WorldPosition(3, 3))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
                Players = engine.State.Players.SetItem(caster.Id, caster with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RemoveRodSpellId, 1)
                    }
                })
            }));
    }

    [Test]
    public void OwnRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldSpellDefinition spell = RemoveRodSpell(areaRadius: 0);

        WorldPlayer caster = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [RemoveRodSpellId])
        };

        WorldRod rod = new(new WorldRodId("Rod0001"), caster.Id, new WorldPosition(1, 1));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(caster.LordId, caster.Race, LordCategory.Warrior))
            .WithPlayer(caster)
            .WithRod(rod)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(RemoveRodSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    caster.Id,
                    RemoveRodSpellId,
                    new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldRodDestroyedEvent(rod.Id, caster.Id, caster.Id, new WorldPosition(1, 1))
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
                Players = engine.State.Players.SetItem(caster.Id, caster with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RemoveRodSpellId, 1)
                    }
                })
            }));
    }

    [Test]
    public void NoRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldSpellDefinition spell = RemoveRodSpell(areaRadius: 0);

        WorldPlayer caster = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [RemoveRodSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldRod rod = new(new WorldRodId("Rod0001"), enemy.Id, new WorldPosition(2, 2));

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(caster.LordId, caster.Race, LordCategory.Warrior))
            .WithPlayer(caster)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithRod(rod)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(RemoveRodSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.RodNotFound);
    }

    [Test]
    public void WorsensRelationPerRodTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".....",
            ".....",
            ".....",
            "....."
        ]);

        WorldSpellDefinition spell = RemoveRodSpell(areaRadius: 1);

        WorldPlayer caster = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [RemoveRodSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRod first = new(new WorldRodId("Rod0001"), enemy.Id, new WorldPosition(1, 1));
        WorldRod second = new(new WorldRodId("Rod0002"), enemy.Id, new WorldPosition(3, 3));

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Elf, WorldRace.Heretic),
            Current: 40,
            Previous: 40,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(caster.LordId, caster.Race, LordCategory.Warrior))
            .WithPlayer(caster)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithRod(first)
            .WithRod(second)
            .WithRelation(relation)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldCastSpellCommand(RemoveRodSpellId, new WorldSpellTarget(new WorldPosition(2, 2), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    caster.Id,
                    RemoveRodSpellId,
                    new WorldSpellTarget(new WorldPosition(2, 2), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldRodDestroyedEvent(first.Id, caster.Id, enemy.Id, new WorldPosition(1, 1)),
                new WorldRelationChangedEvent(caster.Id, enemy.Id, 25),
                new WorldRodDestroyedEvent(second.Id, caster.Id, enemy.Id, new WorldPosition(3, 3)),
                new WorldRelationChangedEvent(caster.Id, enemy.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Rods = ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
                Relations = before.Relations.SetItem(relation.Id, relation with { Current = 10 }),
                Players = before.Players.SetItem(caster.Id, caster with
                {
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(RemoveRodSpellId, 1)
                    }
                })
            }));
    }
}
