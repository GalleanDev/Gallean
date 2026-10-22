namespace Gallean.Engine.Tests;

public sealed class WorldEngineResearchSpellTests
{
    private static WorldSpellDefinition Spell(WorldSpellDefinitionId id, int level)
    {
        return TestHelpers.SimpleSpellDefinition(id, new WorldRestoreMovementSpellEffect(100)) with
        {
            Race = WorldRace.Undead,
            Level = level,
            AreaRadius = null
        };
    }

    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 1))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, new WorldResourceSet(200, 0, 0, 0, 0, 0)))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, spellId),
            [new WorldSpellResearchedEvent(player.Id, spellId, new WorldResourceSet(200, 0, 0, 0, 0, 0))]);

        WorldPlayer expected = player with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Spellbook = player.Spellbook with { Acquired = [spellId] },
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(player.Id, expected)
        }));
    }

    [Test]
    public void NotPlayerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer current = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };
        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(TestHelpers.SimpleLord(current.LordId, current.Race, LordCategory.Warrior))
            .WithPlayer(current)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommandError(
            new WorldResearchSpellCommand(other.Id, spellId),
            WorldCommandError.NotPlayerTurn);
    }

    [Test]
    public void AlreadyKnownTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [spellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommandError(
            new WorldResearchSpellCommand(player.Id, spellId),
            WorldCommandError.SpellAlreadyKnown);
    }

    [Test]
    public void BuildingRequiredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([], []),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommandError(
            new WorldResearchSpellCommand(player.Id, spellId),
            WorldCommandError.SpellBuildingRequired);
    }

    [Test]
    public void NotResearchableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommandError(
            new WorldResearchSpellCommand(player.Id, spellId),
            WorldCommandError.SpellNotResearchable);
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 1))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, new WorldResourceSet(200, 0, 0, 0, 0, 0)))
            .Build();

        engine.CheckCommandError(
            new WorldResearchSpellCommand(player.Id, spellId),
            WorldCommandError.InsufficientResources);
    }

    [Test]
    public void AlreadyResearchedThisTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], []),
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommandError(
            new WorldResearchSpellCommand(player.Id, spellId),
            WorldCommandError.SpellAlreadyResearchedThisTurn);
    }

    [Test]
    public void TurnStartResetsResearchedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId firstSpellId = new("first");
        WorldSpellDefinitionId secondSpellId = new("second");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([firstSpellId, secondSpellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(firstSpellId, level: 1))
            .WithDefinition(Spell(secondSpellId, level: 1))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, firstSpellId, WorldResourceSet.Empty))
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, secondSpellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, firstSpellId),
            [new WorldSpellResearchedEvent(player.Id, firstSpellId, WorldResourceSet.Empty)]);

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(player.Id, player with
            {
                Spellbook = player.Spellbook with { Acquired = [firstSpellId] },
                TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
            })
        }));

        engine.CheckCommandError(
            new WorldResearchSpellCommand(player.Id, secondSpellId),
            WorldCommandError.SpellAlreadyResearchedThisTurn);

        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(player.Id, player with
            {
                Resources = new WorldResourceSet(600, 25, 0, 0, 0, 0),
                Spellbook = player.Spellbook with { Acquired = [firstSpellId] }
            })
        }));

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, secondSpellId),
            [new WorldSpellResearchedEvent(player.Id, secondSpellId, WorldResourceSet.Empty)]);
    }

    [Test]
    public void WarriorLordLevelLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 5))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommandError(
            new WorldResearchSpellCommand(player.Id, spellId),
            WorldCommandError.SpellResearchLevelTooHigh);
    }

    [Test]
    public void GuildmasterLordLevelLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 5))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommandError(
            new WorldResearchSpellCommand(player.Id, spellId),
            WorldCommandError.SpellResearchLevelTooHigh);
    }

    [Test]
    public void WarriorLordAtLevelLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 4))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, spellId),
            [new WorldSpellResearchedEvent(player.Id, spellId, WorldResourceSet.Empty)]);
    }

    [Test]
    public void GuildmasterLordAtLevelLimitTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 4))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, WorldResourceSet.Empty))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, spellId),
            [new WorldSpellResearchedEvent(player.Id, spellId, WorldResourceSet.Empty)]);
    }

    [Test]
    public void MageLordTopLevelTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 5))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, new WorldResourceSet(200, 0, 0, 0, 0, 0)))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, spellId),
            [new WorldSpellResearchedEvent(player.Id, spellId, new WorldResourceSet(100, 0, 0, 0, 0, 0))]);

        WorldPlayer expected = player with
        {
            Resources = new WorldResourceSet(400, 0, 0, 0, 0, 0),
            Spellbook = player.Spellbook with { Acquired = [spellId] },
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(player.Id, expected)
        }));
    }

    [Test]
    public void MageLordCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 300, 0, 100, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 1))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, new WorldResourceSet(200, 100, 0, 25, 0, 0)))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, spellId),
            [new WorldSpellResearchedEvent(player.Id, spellId, new WorldResourceSet(100, 50, 0, 13, 0, 0))]);

        WorldPlayer expected = player with
        {
            Resources = new WorldResourceSet(400, 250, 0, 87, 0, 0),
            Spellbook = player.Spellbook with { Acquired = [spellId] },
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(player.Id, expected)
        }));
    }

    [Test]
    public void WarriorLordCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 300, 0, 100, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 1))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, new WorldResourceSet(200, 100, 0, 25, 0, 0)))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, spellId),
            [new WorldSpellResearchedEvent(player.Id, spellId, new WorldResourceSet(200, 100, 0, 25, 0, 0))]);

        WorldPlayer expected = player with
        {
            Resources = new WorldResourceSet(300, 200, 0, 75, 0, 0),
            Spellbook = player.Spellbook with { Acquired = [spellId] },
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(player.Id, expected)
        }));
    }

    [Test]
    public void GuildmasterLordCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(500, 300, 0, 100, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 1))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Guildmaster))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, new WorldResourceSet(200, 100, 0, 25, 0, 0)))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, spellId),
            [new WorldSpellResearchedEvent(player.Id, spellId, new WorldResourceSet(200, 100, 0, 25, 0, 0))]);

        WorldPlayer expected = player with
        {
            Resources = new WorldResourceSet(300, 200, 0, 75, 0, 0),
            Spellbook = player.Spellbook with { Acquired = [spellId] },
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(player.Id, expected)
        }));
    }

    [Test]
    public void MageLordAffordsOnlyDiscountedCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldSpellDefinitionId spellId = new("spell");
        WorldPlayerBuildingDefinitionId mageGuildId = new("MageGuild");

        WorldPlayerBuildingDefinition mageGuild = TestHelpers.SimpleBuildingDefinition(mageGuildId, WorldPlayerBuildingCategory.Spell) with
        {
            Level = 2
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([mageGuildId], [mageGuildId]),
            Spellbook = new WorldPlayerSpellbook([spellId], [])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageGuild)
            .WithDefinition(Spell(spellId, level: 1))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spellId, new WorldResourceSet(200, 0, 0, 0, 0, 0)))
            .Build();

        engine.CheckCommand(
            new WorldResearchSpellCommand(player.Id, spellId),
            [new WorldSpellResearchedEvent(player.Id, spellId, new WorldResourceSet(100, 0, 0, 0, 0, 0))]);

        WorldPlayer expected = player with
        {
            Resources = WorldResourceSet.Empty,
            Spellbook = player.Spellbook with { Acquired = [spellId] },
            TurnActions = new WorldPlayerTurnActions { SpellResearched = true }
        };

        Assert.That(engine.State, Is.EqualTo(engine.State with
        {
            Players = engine.State.Players.SetItem(player.Id, expected)
        }));
    }
}
