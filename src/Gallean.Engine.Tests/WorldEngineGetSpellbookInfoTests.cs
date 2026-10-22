using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetSpellbookInfoTests
{
    private static readonly WorldSpellDefinitionId RestoreSpellId = new("restore");
    private static readonly WorldSpellDefinitionId HasteSpellId = new("haste");
    private static readonly WorldSpellDefinitionId DaylightSpellId = new("daylight");

    private static WorldSpellDefinition SpellDefinition(WorldSpellDefinitionId id, WorldResourceSet castingCost)
    {
        return TestHelpers.SimpleSpellDefinition(id, new WorldRestoreMovementSpellEffect(50)) with
        {
            Race = WorldRace.Undead,
            CastingCost = castingCost
        };
    }

    private static WorldPlayerBuildingDefinition MageGuildDefinition(WorldPlayerBuildingDefinitionId id)
    {
        return TestHelpers.SimpleBuildingDefinition(id, WorldPlayerBuildingCategory.Spell) with
        {
            Cost = new WorldResourceSet(Gold: 200, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0)
        };
    }

    [Test]
    public void WarriorLordCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinition restore = SpellDefinition(RestoreSpellId, WorldResourceSet.Empty);
        WorldSpellDefinition haste = SpellDefinition(HasteSpellId, WorldResourceSet.Empty);
        WorldSpellDefinition daylight = SpellDefinition(
            DaylightSpellId,
            new WorldResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 10, InfernalMana: 0, GroveMana: 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId, HasteSpellId, DaylightSpellId]),
            TurnActions = new WorldPlayerTurnActions
            {
                SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty
                    .Add(RestoreSpellId, 1)
                    .Add(HasteSpellId, 2)
            }
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(restore)
            .WithDefinition(haste)
            .WithDefinition(daylight)
            .WithPlayer(player)
            .Build();

        WorldGetSpellbookInfoCommand.Result result = engine.CheckCommand(
            new WorldGetSpellbookInfoCommand(player.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetSpellbookInfoCommand.Result(
            [
                new WorldGetSpellbookInfoCommand.Cast(
                    DaylightSpellId,
                    new WorldResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 10, InfernalMana: 0, GroveMana: 0),
                    WorldCommandError.InsufficientResources),
                new WorldGetSpellbookInfoCommand.Cast(HasteSpellId, WorldResourceSet.Empty, WorldCommandError.SpellAlreadyCastThisTurn),
                new WorldGetSpellbookInfoCommand.Cast(RestoreSpellId, WorldResourceSet.Empty, WorldCommandError.SpellAlreadyCastThisTurn)
            ],
            [],
            [],
            RequiredBuildingId: null,
            RequiredBuildingError: null)));
    }

    [Test]
    public void MageLordCastTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinition restore = SpellDefinition(RestoreSpellId, WorldResourceSet.Empty);
        WorldSpellDefinition haste = SpellDefinition(HasteSpellId, WorldResourceSet.Empty);
        WorldSpellDefinition daylight = SpellDefinition(
            DaylightSpellId,
            new WorldResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 10, InfernalMana: 0, GroveMana: 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [RestoreSpellId, HasteSpellId, DaylightSpellId]),
            TurnActions = new WorldPlayerTurnActions
            {
                SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty
                    .Add(RestoreSpellId, 1)
                    .Add(HasteSpellId, 2)
            }
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithDefinition(restore)
            .WithDefinition(haste)
            .WithDefinition(daylight)
            .WithPlayer(player)
            .Build();

        WorldGetSpellbookInfoCommand.Result result = engine.CheckCommand(
            new WorldGetSpellbookInfoCommand(player.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetSpellbookInfoCommand.Result(
            [
                new WorldGetSpellbookInfoCommand.Cast(
                    DaylightSpellId,
                    new WorldResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 10, InfernalMana: 0, GroveMana: 0),
                    WorldCommandError.InsufficientResources),
                new WorldGetSpellbookInfoCommand.Cast(HasteSpellId, WorldResourceSet.Empty, WorldCommandError.SpellAlreadyCastThisTurn),
                new WorldGetSpellbookInfoCommand.Cast(RestoreSpellId, WorldResourceSet.Empty, Error: null)
            ],
            [],
            [],
            RequiredBuildingId: null,
            RequiredBuildingError: null)));
    }

    [Test]
    public void MissingSpellBuildingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(Gold: 500, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(MageGuildDefinition(mageGuildId))
            .WithPlayer(player)
            .Build();

        WorldGetSpellbookInfoCommand.Result result = engine.CheckCommand(
            new WorldGetSpellbookInfoCommand(player.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetSpellbookInfoCommand.Result(
            [],
            [],
            [],
            mageGuildId,
            RequiredBuildingError: null)));
    }

    [Test]
    public void SpellBuildingConstructedThisTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(Gold: 500, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], []),
            TurnActions = new WorldPlayerTurnActions { BuildingConstructed = true }
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(MageGuildDefinition(mageGuildId))
            .WithPlayer(player)
            .Build();

        WorldGetSpellbookInfoCommand.Result result = engine.CheckCommand(
            new WorldGetSpellbookInfoCommand(player.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetSpellbookInfoCommand.Result(
            [],
            [],
            [],
            mageGuildId,
            WorldCommandError.BuildingAlreadyConstructedThisTurn)));
    }

    [Test]
    public void BuiltSpellBuildingTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(MageGuildDefinition(mageGuildId))
            .WithPlayer(player)
            .Build();

        WorldGetSpellbookInfoCommand.Result result = engine.CheckCommand(
            new WorldGetSpellbookInfoCommand(player.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetSpellbookInfoCommand.Result([], [], [], RequiredBuildingId: null, RequiredBuildingError: null)));
    }

    [Test]
    public void LockedSpellsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");
        WorldSpellDefinition restore = SpellDefinition(RestoreSpellId, WorldResourceSet.Empty);
        WorldSpellDefinition haste = SpellDefinition(HasteSpellId, WorldResourceSet.Empty);
        WorldSpellDefinition daylight = SpellDefinition(DaylightSpellId, WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(Gold: 500, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([RestoreSpellId, HasteSpellId], [HasteSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(MageGuildDefinition(mageGuildId))
            .WithDefinition(restore)
            .WithDefinition(haste)
            .WithDefinition(daylight)
            .WithResearchableSpell(new WorldSpellResearchCost(
                WorldRace.Undead,
                RestoreSpellId,
                new WorldResourceSet(Gold: 100, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0)))
            .WithResearchableSpell(new WorldSpellResearchCost(
                WorldRace.Undead,
                HasteSpellId,
                new WorldResourceSet(Gold: 100, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0)))
            .WithResearchableSpell(new WorldSpellResearchCost(
                WorldRace.Undead,
                DaylightSpellId,
                new WorldResourceSet(Gold: 100, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0)))
            .WithPlayer(player)
            .Build();

        WorldGetSpellbookInfoCommand.Result result = engine.CheckCommand(
            new WorldGetSpellbookInfoCommand(player.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetSpellbookInfoCommand.Result(
            [new WorldGetSpellbookInfoCommand.Cast(HasteSpellId, WorldResourceSet.Empty, Error: null)],
            [
                new WorldGetSpellbookInfoCommand.Research(
                    RestoreSpellId,
                    new WorldResourceSet(Gold: 100, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                    Error: null)
            ],
            [DaylightSpellId],
            RequiredBuildingId: null,
            RequiredBuildingError: null)));
    }
}
