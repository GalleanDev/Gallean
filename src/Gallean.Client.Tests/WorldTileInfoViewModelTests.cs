using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class WorldTileInfoViewModelTests
{
    private static IReadOnlyList<string> FormatLines(IReadOnlyList<TextSpans> lines)
    {
        List<string> rows = [];

        foreach (TextSpans line in lines)
        {
            rows.Add(line.Text);
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatTileInfo(WorldTileInfoViewModel info)
    {
        List<string> rows = [info.Title];

        rows.AddRange(FormatLines(info.Lines));
        rows.AddRange(FormatLines(info.Tombs));

        return rows;
    }

    private static WorldTileInfoViewModel? TryCreateInfo(GameSession session, IEngineContext engine, WorldPosition position)
    {
        WorldVisibility visibility = new(session.World.State.Map.Width, session.World.State.Map.Height, Fog: null);

        return WorldTileInfoViewModel.TryCreate(
            session.World.State,
            session.World.State.TurnInfo.CurrentPlayerId,
            position,
            visibility,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);
    }

    private static WorldTileInfoViewModel CreateInfo(GameSession session, IEngineContext engine, WorldPosition position)
    {
        return Assert.NotNull(TryCreateInfo(session, engine, position));
    }

    private static ItemDefinition Item(string id, string name)
    {
        return new ItemDefinition(
            new ItemDefinitionId(id),
            new GlobalTextId(name),
            new GlobalTextId("desc"),
            ItemCategory.PotionHeal,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: new HealItemEffect(50),
            AnimationUnitId: null);
    }

    [Test]
    public void RuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(new WorldRuin(
                Id: new WorldFortificationId("ruin"),
                NameTextId: new ScenarioTextId("Abandoned Farm"),
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
                Variant: new WorldObjectVariant(0),
                Garrison: UnitGrid<WorldUnit>.Empty,
                Resources: new WorldResourceSet(300, 0, 0, 0, 0, 0),
                Item: null,
                LooterId: null,
                AiPriority: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(0, 1));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Abandoned Farm", "Unexplored"]));
    }

    [Test]
    public void PlunderedRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRuin(new WorldRuin(
                Id: new WorldFortificationId("ruin"),
                NameTextId: new ScenarioTextId("Abandoned Farm"),
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
                Variant: new WorldObjectVariant(0),
                Garrison: UnitGrid<WorldUnit>.Empty,
                Resources: WorldResourceSet.Empty,
                Item: null,
                LooterId: player.Id,
                AiPriority: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(1, 1));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Abandoned Farm", "Plundered"]));
    }

    [Test]
    public void MerchantSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(new WorldMerchantSite(
                Id: new WorldSiteId("site"),
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
                Variant: new WorldObjectVariant(0),
                TitleTextId: new ScenarioTextId("Title"),
                GreetingTextId: new ScenarioTextId("Greeting"),
                Items:
                [
                    new WorldMerchantItem(new ItemDefinitionId("item1"), 5),
                    new WorldMerchantItem(new ItemDefinitionId("item2"), 2)
                ],
                VisitorIds: [],
                AiPriority: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(1, 1));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Title – Merchant", "Greeting"]));
    }

    [Test]
    public void MageSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(new WorldMageSite(
                Id: new WorldSiteId("site"),
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
                Variant: new WorldObjectVariant(0),
                TitleTextId: new ScenarioTextId("Title"),
                GreetingTextId: new ScenarioTextId("Greeting"),
                Spells:
                [
                    new WorldSpellDefinitionId("spell1"),
                    new WorldSpellDefinitionId("spell2"),
                    new WorldSpellDefinitionId("spell3")
                ],
                VisitorIds: [],
                AiPriority: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(1, 1));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Title – Mage tower", "Greeting"]));
    }

    [Test]
    public void MercenarySiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(new WorldMercenarySite(
                Id: new WorldSiteId("site"),
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
                Variant: new WorldObjectVariant(0),
                TitleTextId: new ScenarioTextId("Title"),
                GreetingTextId: new ScenarioTextId("Greeting"),
                Units: [new WorldMercenaryUnit(new UnitDefinitionId("unit"), 1, false)],
                VisitorIds: [],
                AiPriority: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(1, 1));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Title – Mercenary camp", "Greeting"]));
    }

    [Test]
    public void TrainerSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSite(new WorldTrainerSite(
                Id: new WorldSiteId("site"),
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
                Variant: new WorldObjectVariant(0),
                TitleTextId: new ScenarioTextId("Title"),
                GreetingTextId: new ScenarioTextId("Greeting"),
                VisitorIds: [],
                AiPriority: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(1, 1));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Title – Trainer", "Greeting"]));
    }

    [Test]
    public void ResourceNodesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer undeadPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer elfPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Elf, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(undeadPlayer.LordId, undeadPlayer.Race, LordCategory.Warrior))
            .WithPlayer(undeadPlayer)
            .WithDefinition(TestHelpers.SimpleLord(elfPlayer.LordId, elfPlayer.Race, LordCategory.Warrior))
            .WithPlayer(elfPlayer)
            .WithResourceNode(new WorldResourceNode(
                Id: new WorldResourceNodeId("mine"),
                OwnerId: undeadPlayer.Id,
                Position: new WorldPosition(1, 1),
                ResourceType: WorldResourceType.Gold,
                AiPriority: 1))
            .WithResourceNode(new WorldResourceNode(
                Id: new WorldResourceNodeId("crystal"),
                OwnerId: elfPlayer.Id,
                Position: new WorldPosition(2, 0),
                ResourceType: WorldResourceType.LifeMana,
                AiPriority: 1))
            .BuildSession();

        Assert.That(FormatTileInfo(CreateInfo(session, engine, new WorldPosition(1, 1))), Is.EqualTo(["Gold mine", "Owner: Undead Hordes"]));
        Assert.That(FormatTileInfo(CreateInfo(session, engine, new WorldPosition(2, 0))), Is.EqualTo(["Life mana crystal", "Owner: Elven Alliance"]));
    }

    [Test]
    public void RodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Heretic, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRod(new WorldRod(
                Id: new WorldRodId("rod"),
                OwnerId: player.Id,
                Position: new WorldPosition(0, 1)))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(0, 1));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Rod", "Owner: Legions of the Damned"]));
    }

    [Test]
    public void BagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(Item("item1", "Healing Potion"))
            .WithDefinition(Item("item2", "Ancient Relic"))
            .WithBag(new WorldBag(
                Id: new WorldBagId("bag"),
                Position: new WorldPosition(1, 0),
                Variant: new WorldObjectVariant(0),
                Inventory: new WorldInventory([new WorldItem(new WorldItemId("HealingPotion"), new ItemDefinitionId("item1"), Charges: null), new WorldItem(new WorldItemId("AncientRelic"), new ItemDefinitionId("item2"), Charges: null)]),
                AiPriority: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(1, 0));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Bag", "Unknown treasure"]));
    }

    [Test]
    public void LandmarkTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(new WorldLandmarkDefinition(
                new WorldLandmarkDefinitionId("landmark"),
                new GlobalTextId("Ancient Obelisk"),
                new GlobalTextId("desc"),
                WorldLandmarkType.Structure,
                false,
                2,
                2))
            .WithLandmark(new WorldLandmark(
                Id: new WorldLandmarkId("landmark1"),
                DefinitionId: new WorldLandmarkDefinitionId("landmark"),
                Footprint: new WorldFootprint(new WorldPosition(1, 1), 2, 2)))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(2, 2));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Ancient Obelisk"]));
        Assert.That(info.HasDetails, Is.False);
    }

    [Test]
    public void EmptyTerrainTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+^",
            "=H~"
        ]);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        Assert.That(TryCreateInfo(session, engine, new WorldPosition(0, 0)), Is.Null);
        Assert.That(TryCreateInfo(session, engine, new WorldPosition(1, 0)), Is.Null);
        Assert.That(TryCreateInfo(session, engine, new WorldPosition(2, 0)), Is.Null);
        Assert.That(TryCreateInfo(session, engine, new WorldPosition(0, 1)), Is.Null);
        Assert.That(TryCreateInfo(session, engine, new WorldPosition(1, 1)), Is.Null);
        Assert.That(TryCreateInfo(session, engine, new WorldPosition(2, 1)), Is.Null);
    }

    [Test]
    public void UnexploredTileTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithResourceNode(new WorldResourceNode(
                Id: new WorldResourceNodeId("mine"),
                OwnerId: player.Id,
                Position: new WorldPosition(1, 1),
                ResourceType: WorldResourceType.Gold,
                AiPriority: 1))
            .BuildSession();

        WorldVisibility visibility = new(map.Width, map.Height, ImmutableBitmap.Create(map.Width, map.Height));

        WorldTileInfoViewModel? info = WorldTileInfoViewModel.TryCreate(
            session.World.State,
            session.World.State.TurnInfo.CurrentPlayerId,
            new WorldPosition(1, 1),
            visibility,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        Assert.That(info, Is.Null);
    }

    [Test]
    public void SpiedRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            SpiedRuins = [new WorldFortificationId("ruin")]
        };

        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 100, initiative: 40, damage: 5) with
        {
            NameTextId = new GlobalTextId("Wyvern")
        };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(guardDefinition)
            .WithDefinition(Item("item1", "Ancient Relic"))
            .WithRuin(new WorldRuin(
                Id: new WorldFortificationId("ruin"),
                NameTextId: new ScenarioTextId("Abandoned Farm"),
                Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
                Variant: new WorldObjectVariant(0),
                Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guardDefinition),
                Resources: new WorldResourceSet(300, 0, 0, 0, 0, 0),
                Item: new ItemDefinitionId("item1"),
                LooterId: null,
                AiPriority: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(0, 1));

        List<WorldUnitGroupInfoUnitViewModel> expectedGarrison =
        [
            new(
                new AssetSpriteKey.UnitFace(guardDefinition.Id),
                "Wyvern",
                100,
                100,
                IsAlive: true,
                Row: 2,
                Column: 1,
                ColumnSpan: 1,
                Statuses: [])
        ];

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Abandoned Farm"]));
        Assert.That(Assert.NotNull(info.Garrison).Units, Is.EqualTo(expectedGarrison));
        Assert.That(info.Item, Is.EqualTo(new WorldTileInfoItemViewModel("Ancient Relic", new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(new ItemDefinitionId("item1"))))));
        Assert.That(info.Reward, Is.EqualTo(new ResourceSetViewModel(new WorldResourceSet(300, 0, 0, 0, 0, 0), ResourceSetProjection.All)));
    }

    [Test]
    public void TombTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Neutral, map);

        UnitDefinition knight = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            NameTextId = new GlobalTextId("Knight")
        };
        UnitDefinition orcKing = TestHelpers.SimpleUnitDefinition("orc-king", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            NameTextId = new GlobalTextId("Orc King")
        };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(knight)
            .WithDefinition(orcKing)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithTomb(new WorldTomb(new WorldTombId("tomb1"), new WorldPosition(1, 0), human.Id, knight.Id, "Aldric", undead.Id, orcKing.Id, "Morwen", Turn: 3))
            .WithTomb(new WorldTomb(new WorldTombId("tomb2"), new WorldPosition(1, 0), human.Id, knight.Id, LeaderName: null, undead.Id, orcKing.Id, DestroyerLeaderName: null, Turn: 4))
            .WithTomb(new WorldTomb(new WorldTombId("tomb3"), new WorldPosition(1, 0), human.Id, knight.Id, LeaderName: null, undead.Id, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 5))
            .WithTomb(new WorldTomb(new WorldTombId("tomb4"), new WorldPosition(1, 0), human.Id, knight.Id, LeaderName: null, neutral.Id, orcKing.Id, DestroyerLeaderName: null, Turn: 6))
            .WithTomb(new WorldTomb(new WorldTombId("tomb5"), new WorldPosition(1, 0), human.Id, knight.Id, LeaderName: null, neutral.Id, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 7))
            .WithTomb(new WorldTomb(new WorldTombId("tomb6"), new WorldPosition(1, 0), undead.Id, orcKing.Id, LeaderName: null, DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 8))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(1, 0));

        Assert.That(FormatTileInfo(info), Is.EqualTo([
            "Here lies:",
            "Aldric, defeated by Morwen",
            "Knight, defeated by Orc King",
            "Knight, defeated by Undead Hordes",
            "Knight, defeated by Orc King",
            "Knight, defeated by Neutral",
            "Orc King"
        ]));
    }

    [Test]
    public void TombUnderBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 2);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithBag(new WorldBag(
                Id: new WorldBagId("bag"),
                Position: new WorldPosition(1, 0),
                Variant: new WorldObjectVariant(0),
                Inventory: WorldInventory.Empty,
                AiPriority: 1))
            .WithTomb(new WorldTomb(new WorldTombId("tomb"), new WorldPosition(1, 0), player.Id, new UnitDefinitionId("leader"), "Aldric", DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 1))
            .BuildSession();

        WorldTileInfoViewModel info = CreateInfo(session, engine, new WorldPosition(1, 0));

        Assert.That(FormatTileInfo(info), Is.EqualTo(["Bag", "Unknown treasure"]));
    }
}
