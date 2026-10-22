using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineFogSpellTests
{
    private static readonly WorldSpellDefinitionId FogSpellId = new("fog");

    private static WorldSpellDefinition FogSpell(int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(FogSpellId, new WorldFogSpellEffect()) with
        {
            AreaRadius = areaRadius
        };
    }

    [Test]
    public void ConcealsFootprintFromOtherPlayersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldSpellDefinition spell = FogSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [FogSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(FogSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    FogSpellId,
                    new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldFogConcealedEvent(enemy.Id, [new WorldPosition(1, 1)])
            ]);

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "...",
            "...",
            "..."
        ]));
        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[enemy.Id].FogOfWar), Is.EqualTo([
            "...",
            ".?.",
            "..."
        ]));
    }

    [Test]
    public void GlobalCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldSpellDefinition spell = FogSpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [FogSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(FogSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, FogSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldFogConcealedEvent(enemy.Id, [
                    new WorldPosition(0, 0),
                    new WorldPosition(1, 0),
                    new WorldPosition(0, 1),
                    new WorldPosition(1, 1)
                ])
            ]);

        Assert.That(engine.State.Players[enemy.Id].FogOfWar, Is.EqualTo(ImmutableBitmap.Create(map.Width, map.Height)));
        Assert.That(engine.State.Players[player.Id].FogOfWar, Is.EqualTo(ImmutableBitmap.Create(map.Width, map.Height, initialValue: true)));
    }

    [Test]
    public void StackVisionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldSpellDefinition spell = FogSpell(areaRadius: null);
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 3,
            leadership: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [FogSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("EnemyStack"),
            enemy.Id,
            new WorldPosition(1, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(leaderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(enemyStack)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(FogSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, FogSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldFogConcealedEvent(enemy.Id, [
                    new WorldPosition(0, 0),
                    new WorldPosition(1, 0),
                    new WorldPosition(2, 0),
                    new WorldPosition(3, 0),
                    new WorldPosition(4, 0),
                    new WorldPosition(0, 1),
                    new WorldPosition(1, 1),
                    new WorldPosition(2, 1),
                    new WorldPosition(3, 1),
                    new WorldPosition(4, 1),
                    new WorldPosition(0, 2),
                    new WorldPosition(1, 2),
                    new WorldPosition(2, 2),
                    new WorldPosition(3, 2),
                    new WorldPosition(4, 2)
                ]),
                new WorldFogRevealedEvent(enemy.Id, [
                    new WorldPosition(0, 0),
                    new WorldPosition(1, 0),
                    new WorldPosition(2, 0),
                    new WorldPosition(0, 1),
                    new WorldPosition(1, 1),
                    new WorldPosition(2, 1),
                    new WorldPosition(0, 2),
                    new WorldPosition(1, 2),
                    new WorldPosition(2, 2)
                ])
            ]);

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[enemy.Id].FogOfWar), Is.EqualTo([
            "...??",
            "...??",
            "...??"
        ]));
    }

    [Test]
    public void AlliedPlayerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldSpellDefinition spell = FogSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [FogSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        WorldPlayer ally = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Dwarf, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Dwarf),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(TestHelpers.SimpleLord(ally.LordId, ally.Race, LordCategory.Warrior))
            .WithPlayer(ally)
            .WithRelation(relation)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(FogSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    FogSpellId,
                    new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldFogConcealedEvent(enemy.Id, [new WorldPosition(1, 1)])
            ]);

        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[player.Id].FogOfWar), Is.EqualTo([
            "...",
            "...",
            "..."
        ]));
        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[enemy.Id].FogOfWar), Is.EqualTo([
            "...",
            ".?.",
            "..."
        ]));
        Assert.That(TestHelpers.SerializeFogOfWar(engine.State.Players[ally.Id].FogOfWar), Is.EqualTo([
            "...",
            "...",
            "..."
        ]));
    }

    [Test]
    public void AlreadyConcealedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldSpellDefinition spell = FogSpell(areaRadius: 0);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [FogSpellId])
        };

        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map) with
        {
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height)
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(FogSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }
}
