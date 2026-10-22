using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineUnfogSpellTests
{
    private static readonly WorldSpellDefinitionId UnfogSpellId = new("unfog");

    private static WorldSpellDefinition UnfogSpell(int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(UnfogSpellId, new WorldUnfogSpellEffect()) with
        {
            AreaRadius = areaRadius
        };
    }

    [Test]
    public void GlobalCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldSpellDefinition spell = UnfogSpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [UnfogSpellId]),
            FogOfWar = TestHelpers.BuildFogOfWar([
                "???",
                "???"
            ])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(UnfogSpellId, Target: null),
            [
                new WorldSpellCastEvent(player.Id, UnfogSpellId, Target: null, WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldFogRevealedEvent(player.Id, [
                    new WorldPosition(0, 0),
                    new WorldPosition(1, 0),
                    new WorldPosition(2, 0),
                    new WorldPosition(0, 1),
                    new WorldPosition(1, 1),
                    new WorldPosition(2, 1)
                ])
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    FogOfWar = TestHelpers.BuildFogOfWar([
                        "...",
                        "..."
                    ]),
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(UnfogSpellId, 1)
                    }
                })
            }));
    }

    [Test]
    public void FootprintCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldSpellDefinition spell = UnfogSpell(areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [UnfogSpellId]),
            FogOfWar = TestHelpers.BuildFogOfWar([
                "?????",
                "?.???",
                "?????"
            ])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(UnfogSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    UnfogSpellId,
                    new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldFogRevealedEvent(player.Id, [
                    new WorldPosition(0, 0),
                    new WorldPosition(1, 0),
                    new WorldPosition(2, 0),
                    new WorldPosition(0, 1),
                    new WorldPosition(2, 1),
                    new WorldPosition(0, 2),
                    new WorldPosition(1, 2),
                    new WorldPosition(2, 2)
                ])
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    FogOfWar = TestHelpers.BuildFogOfWar([
                        "...??",
                        "...??",
                        "...??"
                    ]),
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(UnfogSpellId, 1)
                    }
                })
            }));
    }

    [Test]
    public void GlobalSpellTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldSpellDefinition spell = UnfogSpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [UnfogSpellId]),
            FogOfWar = TestHelpers.BuildFogOfWar([
                "??",
                "??"
            ])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(UnfogSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void FootprintSpellWithoutTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldSpellDefinition spell = UnfogSpell(areaRadius: 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [UnfogSpellId]),
            FogOfWar = TestHelpers.BuildFogOfWar([
                "??",
                "??"
            ])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(UnfogSpellId, Target: null),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void NothingLeftToRevealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldSpellDefinition spell = UnfogSpell(areaRadius: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [UnfogSpellId]),
            FogOfWar = TestHelpers.BuildFogOfWar([
                "..",
                ".."
            ])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(UnfogSpellId, Target: null),
            WorldCommandError.SpellTargetInvalid);
    }
}
