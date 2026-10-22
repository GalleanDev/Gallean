namespace Gallean.Engine.Tests;

public sealed class WorldEngineChangeTerrainSpellTests
{
    private static readonly WorldSpellDefinitionId ChangeTerrainSpellId = new("change-terrain");

    private static WorldSpellDefinition ChangeTerrainSpell(int? areaRadius, WorldMapFeature feature)
    {
        return TestHelpers.SimpleSpellDefinition(ChangeTerrainSpellId, new WorldChangeTerrainSpellEffect(feature)) with
        {
            AreaRadius = areaRadius
        };
    }

    [Test]
    public void GrowsForestOverFootprintTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        WorldSpellDefinition spell = ChangeTerrainSpell(areaRadius: 1, WorldMapFeature.Forest);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [ChangeTerrainSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(ChangeTerrainSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    ChangeTerrainSpellId,
                    new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldMapFeatureChangedEvent(
                    [
                        new WorldPosition(0, 0),
                        new WorldPosition(1, 0),
                        new WorldPosition(2, 0),
                        new WorldPosition(0, 1),
                        new WorldPosition(1, 1),
                        new WorldPosition(2, 1),
                        new WorldPosition(0, 2),
                        new WorldPosition(1, 2),
                        new WorldPosition(2, 2)
                    ],
                    WorldMapFeature.Forest)
            ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "+++..",
            "+++..",
            "+++.."
        ]));
    }

    [Test]
    public void CellVariantTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldSpellDefinition spell = ChangeTerrainSpell(areaRadius: 0, WorldMapFeature.Forest);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [ChangeTerrainSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map with { Cells = map.Cells.SetItem(1, map.Cells[1] with { FeatureVariant = new WorldObjectVariant(7) }) })
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(ChangeTerrainSpellId, new WorldSpellTarget(new WorldPosition(1, 0), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    ChangeTerrainSpellId,
                    new WorldSpellTarget(new WorldPosition(1, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldMapFeatureChangedEvent([new WorldPosition(1, 0)], WorldMapFeature.Forest)
            ]);

        Assert.That(engine.State.Map, Is.EqualTo(map with
        {
            Cells = map.Cells.SetItem(1, new WorldMapCell(WorldMapTerrain.Neutral, WorldMapFeature.Forest, new WorldObjectVariant(7)))
        }));
    }

    [Test]
    public void WaterMountainsAndRoadsTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "~^=",
            "..+"
        ]);

        WorldSpellDefinition spell = ChangeTerrainSpell(areaRadius: 2, WorldMapFeature.Forest);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [ChangeTerrainSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(ChangeTerrainSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(
                    player.Id,
                    ChangeTerrainSpellId,
                    new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Player),
                new WorldMapFeatureChangedEvent(
                    [
                        new WorldPosition(0, 1),
                        new WorldPosition(1, 1)
                    ],
                    WorldMapFeature.Forest)
            ]);

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "~^=",
            "+++"
        ]));
    }

    [Test]
    public void StructuresTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldSpellDefinition spell = ChangeTerrainSpell(areaRadius: 1, WorldMapFeature.Forest);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [ChangeTerrainSpellId])
        };

        WorldResourceNode node = new(
            new WorldResourceNodeId("node"),
            player.Id,
            new WorldPosition(1, 1),
            WorldResourceType.Gold,
            AiPriority: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResourceNode(node)
            .Build();

        _ = engine.DispatchCommand(new WorldCastSpellCommand(ChangeTerrainSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "++",
            "+."
        ]));
    }

    [Test]
    public void GlobalCastTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..~",
            ".+."
        ]);

        WorldSpellDefinition spell = ChangeTerrainSpell(areaRadius: null, WorldMapFeature.Forest);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [ChangeTerrainSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        _ = engine.DispatchCommand(new WorldCastSpellCommand(ChangeTerrainSpellId, Target: null));

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "++~",
            "+++"
        ]));
    }

    [Test]
    public void ClearsForestTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "++",
            "+."
        ]);

        WorldSpellDefinition spell = ChangeTerrainSpell(areaRadius: 1, WorldMapFeature.None);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [ChangeTerrainSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        _ = engine.DispatchCommand(new WorldCastSpellCommand(ChangeTerrainSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        Assert.That(TestHelpers.SerializeMap(engine.State.Map), Is.EqualTo([
            "..",
            ".."
        ]));
    }

    [Test]
    public void NothingToChangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2, '+');

        WorldSpellDefinition spell = ChangeTerrainSpell(areaRadius: 1, WorldMapFeature.Forest);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [ChangeTerrainSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(ChangeTerrainSpellId, new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }
}
