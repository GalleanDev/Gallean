using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiSpellTests
{
    private static WorldSpellDefinition Spell(string id, int level, WorldAbstractSpellEffect effect)
    {
        return TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId(id), effect) with
        {
            Race = WorldRace.Undead,
            Level = level,
            CastingCost = new WorldResourceSet(0, 50, 0, 0, 0, 0)
        };
    }

    private static WorldSpellDefinition GlobalSpell(string id, int level, WorldAbstractSpellEffect effect)
    {
        return Spell(id, level, effect) with
        {
            AreaRadius = null
        };
    }

    private static WorldPlayerBuildingDefinition MageGuild()
    {
        return TestHelpers.SimpleBuildingDefinition(new WorldPlayerBuildingDefinitionId("MageGuild"), WorldPlayerBuildingCategory.Spell);
    }

    private static WorldStack UnitStack(WorldUnitId leaderId, string id, WorldPlayerId ownerId, WorldPosition position, UnitDefinition leader, int hitpoints)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId(id),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(leaderId, leader) with { Hitpoints = hitpoints }, GridPosition.BackCenter, false)
                .Build(),
            movementPoints: 0);
    }

    [Test]
    public void ResearchTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldSpellDefinition heal = Spell("heal", level: 1, new WorldHealSpellEffect(50));
        WorldSpellDefinition boost = Spell("boost", level: 1, new WorldBoostSpellEffect(new UnitModifierDefinitionId("modifier")));
        WorldSpellDefinition attack = Spell("attack", level: 1, new WorldAttackSpellEffect(UnitAttackSource.Fire, 40));
        WorldSpellDefinition strongAttack = Spell("strongAttack", level: 2, new WorldAttackSpellEffect(UnitAttackSource.Fire, 80));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([MageGuild().Id], [MageGuild().Id]),
            Spellbook = new WorldPlayerSpellbook([heal.Id, boost.Id, attack.Id, strongAttack.Id], [])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(5, player.Id))
            .WithDefinition(MageGuild())
            .WithDefinition(heal)
            .WithDefinition(boost)
            .WithDefinition(attack)
            .WithDefinition(strongAttack)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, heal.Id, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, boost.Id, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, attack.Id, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, strongAttack.Id, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P3 1000] Research attack"
        ]));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player with
        {
            Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0),
            Spellbook = player.Spellbook with { Acquired = [attack.Id] },
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        }));
    }

    [Test]
    public void ResearchOffTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldSpellDefinition attack = Spell("attack", level: 1, new WorldAttackSpellEffect(UnitAttackSource.Fire, 40));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([MageGuild().Id], [MageGuild().Id]),
            Spellbook = new WorldPlayerSpellbook([attack.Id], [])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(4, player.Id))
            .WithDefinition(MageGuild())
            .WithDefinition(attack)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, attack.Id, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player));
    }

    [Test]
    public void CastAttackNearCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition attack = Spell("attack", level: 1, new WorldAttackSpellEffect(UnitAttackSource.Fire, 40));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 100, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([attack.Id], [attack.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack near = UnitStack(new WorldUnitId("nearLeader"), "Near", enemy.Id, new WorldPosition(10, 10), leader, hitpoints: 100);
        WorldStack far = UnitStack(new WorldUnitId("farLeader"), "Far", enemy.Id, new WorldPosition(15, 15), leader, hitpoints: 100);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(attack)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), player.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(far)
            .WithStack(near)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P4 1000] Cast attack -> (10, 10)"
        ]));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(far.Id, far with { MovementPoints = 6 }),
            new KeyValuePair<WorldStackId, WorldStack>(near.Id, UnitStack(new WorldUnitId("nearLeader"), "Near", enemy.Id, new WorldPosition(10, 10), leader, hitpoints: 80) with { MovementPoints = 6 })
        ])));
    }

    [Test]
    public void DefendedCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition attack = Spell("attack", level: 1, new WorldAttackSpellEffect(UnitAttackSource.Fire, 40));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 100, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([attack.Id], [attack.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack defender = UnitStack(new WorldUnitId("defenderLeader"), "Defender", player.Id, new WorldPosition(5, 5), leader, hitpoints: 100);
        WorldStack near = UnitStack(new WorldUnitId("nearLeader"), "Near", enemy.Id, new WorldPosition(10, 10), leader, hitpoints: 100);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(attack)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), player.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(defender)
            .WithStack(near)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(defender.Id, defender),
            new KeyValuePair<WorldStackId, WorldStack>(near.Id, near with { MovementPoints = 6 })
        ])));
    }

    [Test]
    public void CastHealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition heal = Spell("heal", level: 1, new WorldHealSpellEffect(30));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 100, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([heal.Id], [heal.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack scratched = UnitStack(new WorldUnitId("scratchedLeader"), "Scratched", player.Id, new WorldPosition(0, 0), leader, hitpoints: 60);
        WorldStack wounded = UnitStack(new WorldUnitId("woundedLeader"), "Wounded", player.Id, new WorldPosition(5, 5), leader, hitpoints: 40);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(heal)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(scratched)
            .WithStack(wounded)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P4 1000] Cast heal -> (5, 5)"
        ]));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(scratched.Id, scratched),
            new KeyValuePair<WorldStackId, WorldStack>(wounded.Id, UnitStack(new WorldUnitId("woundedLeader"), "Wounded", player.Id, new WorldPosition(5, 5), leader, hitpoints: 70))
        ])));
    }

    [Test]
    public void AreaSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(16, 16);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition attack = Spell("attack", level: 1, new WorldAttackSpellEffect(UnitAttackSource.Fire, 40)) with
        {
            AreaRadius = 1
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 100, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([attack.Id], [attack.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack own = UnitStack(new WorldUnitId("ownLeader"), "Own", player.Id, new WorldPosition(11, 10), leader, hitpoints: 100);
        WorldStack near = UnitStack(new WorldUnitId("nearLeader"), "Near", enemy.Id, new WorldPosition(10, 10), leader, hitpoints: 100);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(attack)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), player.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(own)
            .WithStack(near)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(near.Id, near with { MovementPoints = 6 }),
            new KeyValuePair<WorldStackId, WorldStack>(own.Id, own)
        ])));
    }

    [Test]
    public void ResearchSkipsNonAiSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldSpellDefinition illusion = Spell("illusion", level: 1, new WorldSummonStackSpellEffect(new UnitDefinitionId("illusion"))) with
        {
            AiType = WorldAiSpellType.None
        };
        WorldSpellDefinition summon = Spell("summon", level: 1, new WorldSummonStackSpellEffect(new UnitDefinitionId("summon")));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([MageGuild().Id], [MageGuild().Id]),
            Spellbook = new WorldPlayerSpellbook([illusion.Id, summon.Id], [])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithTurnInfo(new WorldTurnInfo(5, player.Id))
            .WithDefinition(MageGuild())
            .WithDefinition(illusion)
            .WithDefinition(summon)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, illusion.Id, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, summon.Id, new WorldResourceSet(100, 0, 0, 0, 0, 0)))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P3 1000] Research summon"
        ]));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player with
        {
            Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0),
            Spellbook = player.Spellbook with { Acquired = [summon.Id] },
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        }));
    }

    [Test]
    public void CastGlobalHealTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition heal = GlobalSpell("heal", level: 5, new WorldHealSpellEffect(30));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 1001, 1001, 1001, 1001, 0),
            Spellbook = new WorldPlayerSpellbook([heal.Id], [heal.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack scratched = UnitStack(new WorldUnitId("scratchedLeader"), "Scratched", player.Id, new WorldPosition(5, 5), leader, hitpoints: 90);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(10)
            .WithDefinition(leader)
            .WithDefinition(heal)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(scratched)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P4 1000] Cast heal"
        ]));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player with
        {
            Resources = new WorldResourceSet(0, 951, 1001, 1001, 1001, 0),
            TurnActions = new WorldPlayerTurnActions
            {
                SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(heal.Id, 1)
            }
        }));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(scratched.Id, UnitStack(new WorldUnitId("scratchedLeader"), "Scratched", player.Id, new WorldPosition(5, 5), leader, hitpoints: 100))
        ])));
    }

    [Test]
    public void GlobalHealLowSuggestedLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition heal = GlobalSpell("heal", level: 5, new WorldHealSpellEffect(30));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 1001, 1001, 1001, 1001, 0),
            Spellbook = new WorldPlayerSpellbook([heal.Id], [heal.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack scratched = UnitStack(new WorldUnitId("scratchedLeader"), "Scratched", player.Id, new WorldPosition(5, 5), leader, hitpoints: 90);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(9)
            .WithDefinition(leader)
            .WithDefinition(heal)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(scratched)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(scratched.Id, scratched)
        ])));
    }

    [Test]
    public void GlobalHealLowManaTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition heal = GlobalSpell("heal", level: 5, new WorldHealSpellEffect(30));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(5000, 1001, 1000, 1001, 1001, 5000),
            Spellbook = new WorldPlayerSpellbook([heal.Id], [heal.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack scratched = UnitStack(new WorldUnitId("scratchedLeader"), "Scratched", player.Id, new WorldPosition(5, 5), leader, hitpoints: 90);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(10)
            .WithDefinition(leader)
            .WithDefinition(heal)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(scratched)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(scratched.Id, scratched)
        ])));
    }

    [Test]
    public void GlobalHealUnwoundedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition heal = GlobalSpell("heal", level: 5, new WorldHealSpellEffect(30));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 1001, 1001, 1001, 1001, 0),
            Spellbook = new WorldPlayerSpellbook([heal.Id], [heal.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack healthy = UnitStack(new WorldUnitId("healthyLeader"), "Healthy", player.Id, new WorldPosition(9, 9), leader, hitpoints: 100);
        WorldStack garrisoned = UnitStack(new WorldUnitId("garrisonedLeader"), "Garrisoned", player.Id, new WorldPosition(5, 5), leader, hitpoints: 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(10)
            .WithDefinition(leader)
            .WithDefinition(heal)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), player.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(healthy)
            .WithStack(garrisoned)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(garrisoned.Id, garrisoned),
            new KeyValuePair<WorldStackId, WorldStack>(healthy.Id, healthy)
        ])));
    }

    [Test]
    public void CastGlobalInvisibilityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition invisibility = GlobalSpell("invisibility", level: 5, new WorldInvisibilitySpellEffect());

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 1001, 1001, 1001, 1001, 0),
            Spellbook = new WorldPlayerSpellbook([invisibility.Id], [invisibility.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack first = UnitStack(new WorldUnitId("firstLeader"), "First", player.Id, new WorldPosition(0, 5), leader, hitpoints: 100);
        WorldStack second = UnitStack(new WorldUnitId("secondLeader"), "Second", player.Id, new WorldPosition(5, 5), leader, hitpoints: 100);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(10)
            .WithDefinition(leader)
            .WithDefinition(invisibility)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(first)
            .WithStack(second)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P4 1000] Cast invisibility"
        ]));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player with
        {
            Resources = new WorldResourceSet(0, 951, 1001, 1001, 1001, 0),
            TurnActions = new WorldPlayerTurnActions
            {
                SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(invisibility.Id, 1)
            }
        }));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(first.Id, first with { Concealed = true }),
            new KeyValuePair<WorldStackId, WorldStack>(second.Id, second with { Concealed = true })
        ])));
    }

    [Test]
    public void GlobalInvisibilitySingleStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition invisibility = GlobalSpell("invisibility", level: 5, new WorldInvisibilitySpellEffect());

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 1001, 1001, 1001, 1001, 0),
            Spellbook = new WorldPlayerSpellbook([invisibility.Id], [invisibility.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack field = UnitStack(new WorldUnitId("fieldLeader"), "Field", player.Id, new WorldPosition(9, 9), leader, hitpoints: 100);
        WorldStack garrisoned = UnitStack(new WorldUnitId("garrisonedLeader"), "Garrisoned", player.Id, new WorldPosition(5, 5), leader, hitpoints: 100);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(10)
            .WithDefinition(leader)
            .WithDefinition(invisibility)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithCapital(TestHelpers.SimpleCapital(new WorldFortificationId("Capital"), player.Id, new WorldPosition(5, 5), UnitGrid<WorldUnit>.Empty))
            .WithStack(field)
            .WithStack(garrisoned)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(field.Id, field),
            new KeyValuePair<WorldStackId, WorldStack>(garrisoned.Id, garrisoned)
        ])));
    }

    [Test]
    public void GlobalOncePerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition heal = GlobalSpell("heal", level: 4, new WorldHealSpellEffect(30));
        WorldSpellDefinition invisibility = GlobalSpell("invisibility", level: 5, new WorldInvisibilitySpellEffect());

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 1001, 1001, 1001, 1001, 0),
            Spellbook = new WorldPlayerSpellbook([heal.Id, invisibility.Id], [heal.Id, invisibility.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack first = UnitStack(new WorldUnitId("firstLeader"), "First", player.Id, new WorldPosition(0, 5), leader, hitpoints: 90);
        WorldStack second = UnitStack(new WorldUnitId("secondLeader"), "Second", player.Id, new WorldPosition(5, 5), leader, hitpoints: 90);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(10)
            .WithDefinition(leader)
            .WithDefinition(heal)
            .WithDefinition(invisibility)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(first)
            .WithStack(second)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P4 1000] Cast invisibility"
        ]));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player with
        {
            Resources = new WorldResourceSet(0, 951, 1001, 1001, 1001, 0),
            TurnActions = new WorldPlayerTurnActions
            {
                SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(invisibility.Id, 1)
            }
        }));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(first.Id, first with { Concealed = true }),
            new KeyValuePair<WorldStackId, WorldStack>(second.Id, second with { Concealed = true })
        ])));
    }

    [Test]
    public void TargetedSpellWithoutAiTypeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 6);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition weaken = Spell("weaken", level: 1, new WorldWeakenSpellEffect(new UnitModifierDefinitionId("modifier")));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 100, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([weaken.Id], [weaken.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack wounded = UnitStack(new WorldUnitId("woundedLeader"), "Wounded", player.Id, new WorldPosition(5, 5), leader, hitpoints: 40);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(weaken)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(wounded)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(wounded.Id, wounded)
        ])));
    }

    [Test]
    public void GlobalSpellWithoutAiTypeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldSpellDefinition fog = GlobalSpell("fog", level: 5, new WorldFogSpellEffect());

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            IsAi = true,
            Resources = new WorldResourceSet(0, 1001, 1001, 1001, 1001, 0),
            Spellbook = new WorldPlayerSpellbook([fog.Id], [fog.Id])
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack first = UnitStack(new WorldUnitId("firstLeader"), "First", player.Id, new WorldPosition(0, 0), leader, hitpoints: 100);
        WorldStack second = UnitStack(new WorldUnitId("secondLeader"), "Second", player.Id, new WorldPosition(9, 9), leader, hitpoints: 100);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(10)
            .WithDefinition(leader)
            .WithDefinition(fog)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithStack(first)
            .WithStack(second)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo(Array.Empty<string>()));
        Assert.That(session.World.State.Players[player.Id], Is.EqualTo(player));
        Assert.That(session.World.State.Stacks, Is.EqualTo(ImmutableSortedDictionary.CreateRange([
            new KeyValuePair<WorldStackId, WorldStack>(first.Id, first),
            new KeyValuePair<WorldStackId, WorldStack>(second.Id, second)
        ])));
    }
}
