using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gallean.Common;
using Gallean.Entities.GameData;
using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldStateJsonSerializerTests
{
    [Test]
    public async Task ScenarioRoundTripTest()
    {
        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        Scenario scenario = ScenarioLoader.Load(
            globalDb,
            scenarioDb,
            campaignDb,
            new ScenarioId("S137SC0000"),
            new GameDefinitionProvider(globalDb),
            new GlobalVariables());
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, scenario.State);
        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(scenario.State));
    }

    [Test]
    public async Task LargeUnitRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        WorldFortificationId capitalId = new("capital");
        WorldUnit largeUnit = new(
            new WorldUnitId("largeUnit"),
            new UnitDefinitionId("large-unit"),
            Hitpoints: 77,
            Experience: 88,
            Level: 3,
            [new UnitModifier(new UnitModifierDefinitionId("modifier"), RemainingTurns: 6)],
            Name: "Ardobert",
            IsTypeLocked: false);
        UnitGrid<WorldUnit> garrison = new UnitGrid<WorldUnit>.Builder()
            .Place(largeUnit, GridPosition.FrontBottom, isLarge: true)
            .Build();
        WorldState state = session.World.State with
        {
            Fortifications = ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty.Add(
                capitalId,
                new WorldCapital(
                    capitalId,
                    new ScenarioTextId("capital-name"),
                    player.Id,
                    new WorldFootprint(new WorldPosition(0, 0), 2, 2),
                    garrison,
                    new WorldInventory([new WorldItem(new WorldItemId("stored-item"), new ItemDefinitionId("capital-item"), Charges: null)]),
                    AiPriority: 1,
                    CapturedById: null))
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);
        string json = Encoding.UTF8.GetString(stream.ToArray());
        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));
        Assert.That(actual.Fortifications[capitalId].Garrison.IsLargeAt(GridPosition.FrontBottom), Is.True);
        Assert.That(json, Does.Contain("\"name\":\"Ardobert\""));
    }

    [Test]
    public async Task CapturedCapitalRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer conqueror = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        WorldPlayer conquered = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(conqueror.LordId, conqueror.Race, LordCategory.Warrior))
            .WithPlayer(conqueror)
            .WithDefinition(TestHelpers.SimpleLord(conquered.LordId, conquered.Race, LordCategory.Warrior))
            .WithPlayer(conquered)
            .BuildSession();
        WorldFortificationId capitalId = new("capital");
        WorldState state = session.World.State with
        {
            Fortifications = ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty.Add(
                capitalId,
                new WorldCapital(
                    capitalId,
                    new ScenarioTextId("capital-name"),
                    conquered.Id,
                    new WorldFootprint(new WorldPosition(0, 0), 2, 2),
                    UnitGrid<WorldUnit>.Empty,
                    WorldInventory.Empty,
                    AiPriority: 1,
                    CapturedById: conqueror.Id))
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);
        string json = Encoding.UTF8.GetString(stream.ToArray());
        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));
        Assert.That(json, Does.Contain("\"capturedById\":1"));
    }

    [Test]
    public async Task SiteRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 4);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        WorldState state = session.World.State with
        {
            Sites = ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty
                .Add(new WorldSiteId("mage"), new WorldMageSite(
                    new WorldSiteId("mage"),
                    new WorldFootprint(new WorldPosition(0, 1), 1, 1),
                    new WorldObjectVariant(1),
                    new ScenarioTextId("mage-title"),
                    new ScenarioTextId("mage-greeting"),
                    [new WorldSpellDefinitionId("spell")],
                    VisitorIds: [],
                    AiPriority: 1))
                .Add(new WorldSiteId("mercenary"), new WorldMercenarySite(
                    new WorldSiteId("mercenary"),
                    new WorldFootprint(new WorldPosition(1, 1), 1, 1),
                    new WorldObjectVariant(2),
                    new ScenarioTextId("mercenary-title"),
                    new ScenarioTextId("mercenary-greeting"),
                    [new WorldMercenaryUnit(new UnitDefinitionId("mercenary-unit"), 4, IsUnique: true)],
                    VisitorIds: [],
                    AiPriority: 2))
                .Add(new WorldSiteId("merchant"), new WorldMerchantSite(
                    new WorldSiteId("merchant"),
                    new WorldFootprint(new WorldPosition(2, 1), 1, 1),
                    new WorldObjectVariant(3),
                    new ScenarioTextId("merchant-title"),
                    new ScenarioTextId("merchant-greeting"),
                    [new WorldMerchantItem(new ItemDefinitionId("merchant-item"), 5)],
                    VisitorIds: [player.Id],
                    AiPriority: 3))
                .Add(new WorldSiteId("trainer"), new WorldTrainerSite(
                    new WorldSiteId("trainer"),
                    new WorldFootprint(new WorldPosition(3, 1), 1, 1),
                    new WorldObjectVariant(4),
                    new ScenarioTextId("trainer-title"),
                    new ScenarioTextId("trainer-greeting"),
                    VisitorIds: [],
                    AiPriority: 4))
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);
        string json = Encoding.UTF8.GetString(stream.ToArray());
        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));
        Assert.That(json, Does.Contain("\"visitorIds\":[1]"));
    }

    [Test]
    public async Task AiMemoryRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map) with { IsAi = true };
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory> stackMemories =
            ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty
                .Add(new WorldStackId("stack-0"), new WorldAiStackMemory(
                    WorldStackOrder.Normal.Instance,
                    new WorldAiTarget.Stack(new WorldStackId("target-0"))))
                .Add(new WorldStackId("stack-1"), new WorldAiStackMemory(
                    WorldStackOrder.Stand.Instance,
                    new WorldAiTarget.Fortification(new WorldFortificationId("fortification-1"))))
                .Add(new WorldStackId("stack-2"), new WorldAiStackMemory(
                    WorldStackOrder.Guard.Instance,
                    new WorldAiTarget.Location(new WorldPosition(2, 0))))
                .Add(new WorldStackId("stack-3"), new WorldAiStackMemory(
                    new WorldStackOrder.AttackStack(new WorldStackId("stack-0")),
                    new WorldAiTarget.Stack(new WorldStackId("target-3"))))
                .Add(new WorldStackId("stack-4"), new WorldAiStackMemory(
                    new WorldStackOrder.DefendStack(new WorldStackId("stack-1")),
                    new WorldAiTarget.Fortification(new WorldFortificationId("fortification-4"))))
                .Add(new WorldStackId("stack-5"), new WorldAiStackMemory(
                    new WorldStackOrder.SecureCity(new WorldFortificationId("city")),
                    new WorldAiTarget.Location(new WorldPosition(5, 0))))
                .Add(new WorldStackId("stack-6"), new WorldAiStackMemory(
                    WorldStackOrder.Roam.Instance,
                    new WorldAiTarget.Stack(new WorldStackId("target-6"))))
                .Add(new WorldStackId("stack-7"), new WorldAiStackMemory(
                    WorldStackOrder.Explore.Instance,
                    new WorldAiTarget.Fortification(new WorldFortificationId("fortification-7"))))
                .Add(new WorldStackId("stack-8"), new WorldAiStackMemory(
                    new WorldStackOrder.MoveToLocation(new ScenarioLocationId("location-1"), new WorldPosition(1, 0), 2),
                    new WorldAiTarget.Location(new WorldPosition(8, 0))))
                .Add(new WorldStackId("stack-9"), new WorldAiStackMemory(
                    new WorldStackOrder.DefendLocation(new ScenarioLocationId("location-2"), new WorldPosition(0, 1), 3),
                    new WorldAiTarget.Stack(new WorldStackId("target-9"))))
                .Add(new WorldStackId("stack-10"), new WorldAiStackMemory(
                    WorldStackOrder.Berserk.Instance,
                    new WorldAiTarget.Fortification(new WorldFortificationId("fortification-10"))))
                .Add(new WorldStackId("stack-11"), new WorldAiStackMemory(
                    new WorldStackOrder.Assist(new WorldStackId("stack-0")),
                    new WorldAiTarget.Location(new WorldPosition(11, 0))))
                .Add(new WorldStackId("stack-12"), new WorldAiStackMemory(
                    WorldStackOrder.Steal.Instance,
                    new WorldAiTarget.Stack(new WorldStackId("target-12"))))
                .Add(new WorldStackId("stack-13"), new WorldAiStackMemory(
                    new WorldStackOrder.DefendCity(new WorldFortificationId("city")),
                    new WorldAiTarget.Fortification(new WorldFortificationId("fortification-13"))))
                .Add(new WorldStackId("stack-14"), new WorldAiStackMemory(
                    WorldStackOrder.Heal.Instance,
                    new WorldAiTarget.Location(new WorldPosition(14, 0))))
                .Add(new WorldStackId("stack-15"), new WorldAiStackMemory(
                    new WorldStackOrder.Reinforce(new WorldFortificationId("city-15")),
                    new WorldAiTarget.Fortification(new WorldFortificationId("fortification-15"))));

        WorldState state = session.World.State with
        {
            AiMemories = ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty.Add(
                player.Id,
                new WorldAiMemory(
                    new WorldAiTurnMemory(
                        7,
                        player.Id,
                        ActionsChosen: 8,
                        UnitsPurchased: 9,
                        BuildingsConstructed: 10,
                        [new WorldStackId("stack-0")]),
                    stackMemories))
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);
        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));
    }

    [Test]
    public async Task ScenarioStateRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        WorldState state = session.World.State with
        {
            ScenarioState = new WorldScenarioState(
                ImmutableSortedDictionary<ScenarioEventId, bool>.Empty
                    .Add(new ScenarioEventId("event-0"), true)
                    .Add(new ScenarioEventId("event-1"), false),
                [
                    new ScenarioDestroyedStack(new WorldStackId("stack-0"), player.Id, SourceTemplateId: null),
                    new ScenarioDestroyedStack(new WorldStackId("stack-1"), DestroyerPlayerId: null, new ScenarioStackTemplateId("template-0")),
                    new ScenarioDestroyedStack(new WorldStackId("stack-2"), player.Id, new ScenarioStackTemplateId("template-1"))
                ],
                ImmutableSortedDictionary<ScenarioVariableId, int>.Empty
                    .Add(new ScenarioVariableId(1), 3)
                    .Add(new ScenarioVariableId(2), -1),
                new ScenarioTextId("Hold the pass."),
                IsEnded: true,
                player.Id,
                [
                    new WorldScenarioMessage(
                        TurnNumber: 3,
                        new ScenarioEventId("event-0"),
                        new ScenarioTextId("Hold the pass."),
                        new ScenarioEventPortraitId("unit-0"),
                        "Captain",
                        IsPortraitLeft: true,
                        player.Id,
                        [player.Id]),
                    new WorldScenarioMessage(
                        TurnNumber: 5,
                        new ScenarioEventId("event-1"),
                        new ScenarioTextId("The pass has fallen."),
                        PortraitId: null,
                        PortraitName: null,
                        IsPortraitLeft: false,
                        player.Id,
                        [])
                ])
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);
        string json = Encoding.UTF8.GetString(stream.ToArray());
        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));
        Assert.That(json, Does.Contain("\"objectiveTextId\":\"Hold the pass.\""));
    }

    [Test]
    public async Task ScenarioStateWithoutMessagesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        WorldState state = session.World.State;
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);
        string json = Encoding.UTF8.GetString(stream.ToArray());

        Assert.That(json, Does.Contain(",\"messages\":[]"));

        string legacyJson = json.Replace(",\"messages\":[]", "", StringComparison.Ordinal);
        using MemoryStream legacyStream = new(Encoding.UTF8.GetBytes(legacyJson));
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(legacyStream);

        Assert.That(actual, Is.EqualTo(state));
        Assert.That(actual.ScenarioState.Messages, Is.Empty);
    }

    [Test]
    public async Task MalformedBitmapTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, session.World.State);
        string json = Encoding.UTF8.GetString(stream.ToArray());
        string invalidJson = json.Replace("\"fogOfWar\":{\"width\":2", "\"fogOfWar\":{\"width\":99", StringComparison.Ordinal);

        Assert.That(invalidJson, Is.Not.EqualTo(json));

        using MemoryStream invalidStream = new(Encoding.UTF8.GetBytes(invalidJson));

        await Assert.ThrowsAsync<JsonException>(async () => await WorldStateJsonSerializer.DeserializeAsync(invalidStream));
    }

    [Test]
    public async Task MalformedTest()
    {
        using MemoryStream stream = new("{not-json"u8.ToArray());

        await Assert.ThrowsAsync<JsonException>(async () => await WorldStateJsonSerializer.DeserializeAsync(stream));
    }

    [Test]
    public async Task InventoryJsonShapeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        WorldBagId bagId = new("bag");
        WorldState state = session.World.State with
        {
            Bags = ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(
                bagId,
                new WorldBag(
                    bagId,
                    new WorldPosition(1, 1),
                    new WorldObjectVariant(0),
                    new WorldInventory([new WorldItem(new WorldItemId("bag-talisman"), new ItemDefinitionId("talisman"), Charges: 4)]),
                    AiPriority: 3))
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);

        string json = Encoding.UTF8.GetString(stream.ToArray());
        JsonNode root = Assert.NotNull(JsonNode.Parse(json));
        JsonNode bagsNode = Assert.NotNull(root["bags"]);
        JsonNode bagNode = Assert.NotNull(bagsNode["bag"]);

        Assert.That(bagNode.ToJsonString(), Is.EqualTo(
            "{\"id\":\"bag\",\"position\":{\"x\":1,\"y\":1},\"variant\":0,\"inventory\":[{\"id\":\"bag-talisman\",\"definitionId\":\"talisman\",\"charges\":4}],\"aiPriority\":3}"));
    }

    [Test]
    public async Task TurnActionsRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        WorldState state = session.World.State with
        {
            Players = session.World.State.Players.SetItem(player.Id, player with
            {
                TurnActions = new WorldPlayerTurnActions
                {
                    SpellResearched = true,
                    BuildingConstructed = true,
                    SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty
                        .Add(new WorldSpellDefinitionId("haste"), 1)
                        .Add(new WorldSpellDefinitionId("summon"), 2),
                    ItemUses = ImmutableSortedDictionary<WorldItemId, int>.Empty
                        .Add(new WorldItemId("first-staff"), 1)
                        .Add(new WorldItemId("second-staff"), 2)
                }
            })
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);

        string json = Encoding.UTF8.GetString(stream.ToArray());

        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));
        Assert.That(json, Does.Contain("\"spellCasts\":{\"haste\":1,\"summon\":2}"));
        Assert.That(json, Does.Contain("\"itemUses\":{\"first-staff\":1,\"second-staff\":2}"));
    }

    [Test]
    public async Task DiplomacyProposalRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .BuildSession();
        WorldState state = session.World.State with
        {
            Players = session.World.State.Players
                .SetItem(player1.Id, player1 with
                {
                    IncomingDiplomacyProposals =
                    [
                        new WorldDiplomacyProposal(player2.Id, player1.Id, new WorldDiplomacyTradeItemAction(new WorldFortificationId("city"), new ItemDefinitionId("sword"), 150)),
                        new WorldDiplomacyProposal(player2.Id, player1.Id, new WorldDiplomacyTradeSpellAction(new WorldSpellDefinitionId("haste"), 200)),
                        new WorldDiplomacyProposal(player2.Id, player1.Id, new WorldDiplomacyProposeAllianceAction()),
                        new WorldDiplomacyProposal(player2.Id, player1.Id, new WorldDiplomacyExchangeMapAction())
                    ]
                })
                .SetItem(player2.Id, player2 with
                {
                    TurnActions = new WorldPlayerTurnActions { MapExchangeProposals = [player1.Id] }
                })
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);

        string json = Encoding.UTF8.GetString(stream.ToArray());

        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));
        Assert.That(json, Does.Contain("\"action\":{\"type\":\"tradeItem\",\"sourceFortificationId\":\"city\",\"itemId\":\"sword\",\"price\":150}"));
        Assert.That(json, Does.Contain("\"action\":{\"type\":\"exchangeMap\"}"));
        Assert.That(json, Does.Contain("\"mapExchangeProposals\":[1]"));
    }

    [Test]
    public async Task RelationRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Elf, WorldRace.Human),
            Current: 72,
            Previous: 41,
            Alliance: true,
            AllianceTurn: 9,
            PermanentWar: false,
            PermanentAlliance: true);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRelation(relation)
            .BuildSession();
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, session.World.State);

        string json = Encoding.UTF8.GetString(stream.ToArray());

        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(session.World.State));
        Assert.That(json, Does.Contain(
            "\"relations\":{\"Human-Elf\":{\"id\":\"Human-Elf\",\"current\":72,\"previous\":41,\"alliance\":true,\"allianceTurn\":9,\"permanentWar\":false,\"permanentAlliance\":true}}"));
    }

    [Test]
    public async Task MalformedRelationIdTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Elf, WorldRace.Human),
            Current: 72,
            Previous: 41,
            Alliance: true,
            AllianceTurn: 9,
            PermanentWar: false,
            PermanentAlliance: true);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRelation(relation)
            .BuildSession();
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, session.World.State);

        string json = Encoding.UTF8.GetString(stream.ToArray());
        string invalidJson = json.Replace("\"Human-Elf\"", "\"Human-Goblin\"", StringComparison.Ordinal);

        Assert.That(invalidJson, Is.Not.EqualTo(json));

        using MemoryStream invalidStream = new(Encoding.UTF8.GetBytes(invalidJson));

        await Assert.ThrowsAsync<JsonException>(async () => await WorldStateJsonSerializer.DeserializeAsync(invalidStream));
    }

    [Test]
    public async Task StackOrderRoundTripTest()
    {
        WorldMap map = TestHelpers.BuildMap(["..", ".."]);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithPlayer(player).BuildSession();
        WorldCanonicalStackOrder[] orders =
        [
            WorldStackOrder.Normal.Instance,
            WorldStackOrder.Stand.Instance,
            WorldStackOrder.Guard.Instance,
            new WorldStackOrder.AttackStack(new WorldStackId("target")),
            new WorldStackOrder.DefendStack(new WorldStackId("target")),
            new WorldStackOrder.SecureCity(new WorldFortificationId("city")),
            WorldStackOrder.Roam.Instance,
            WorldStackOrder.Explore.Instance,
            new WorldStackOrder.MoveToLocation(new ScenarioLocationId("location"), new WorldPosition(1, 0), 2),
            new WorldStackOrder.DefendLocation(new ScenarioLocationId("location"), new WorldPosition(0, 1), 3),
            WorldStackOrder.Berserk.Instance,
            new WorldStackOrder.Assist(new WorldStackId("target")),
            WorldStackOrder.Steal.Instance,
            new WorldStackOrder.DefendCity(new WorldFortificationId("city"))
        ];

        ImmutableSortedDictionary<WorldStackId, WorldStack>.Builder stacks =
            ImmutableSortedDictionary.CreateBuilder<WorldStackId, WorldStack>();

        for (int i = 0; i < orders.Length; i++)
        {
            WorldStackId stackId = new($"stack-{i}");

            stacks.Add(stackId, TestHelpers.SimpleStack(
                    stackId,
                    player.Id,
                    new WorldPosition(0, 0),
                    UnitGrid<WorldUnit>.Empty,
                    movementPoints: 0) with
                {
                    Order = orders[i]
                });
        }

        WorldState state = session.World.State with { Stacks = stacks.ToImmutable() };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);

        string json = Encoding.UTF8.GetString(stream.ToArray());

        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));

        JsonNode root = Assert.NotNull(JsonNode.Parse(json));
        JsonNode stacksNode = Assert.NotNull(root["stacks"]);
        List<string> serializedOrders = [];

        for (int i = 0; i < orders.Length; i++)
        {
            JsonNode stackNode = Assert.NotNull(stacksNode[$"stack-{i}"]);
            JsonNode orderNode = Assert.NotNull(stackNode["order"]);
            serializedOrders.Add(orderNode.ToJsonString());
        }

        Assert.That(serializedOrders, Is.EqualTo([
            "{\"type\":\"normal\"}",
            "{\"type\":\"stand\"}",
            "{\"type\":\"guard\"}",
            "{\"type\":\"attackStack\",\"targetId\":\"target\"}",
            "{\"type\":\"defendStack\",\"targetId\":\"target\"}",
            "{\"type\":\"secureCity\",\"targetId\":\"city\"}",
            "{\"type\":\"roam\"}",
            "{\"type\":\"explore\"}",
            "{\"type\":\"moveToLocation\",\"targetId\":\"location\",\"targetPosition\":{\"x\":1,\"y\":0},\"radius\":2}",
            "{\"type\":\"defendLocation\",\"targetId\":\"location\",\"targetPosition\":{\"x\":0,\"y\":1},\"radius\":3}",
            "{\"type\":\"berserk\"}",
            "{\"type\":\"assist\",\"targetId\":\"target\"}",
            "{\"type\":\"steal\"}",
            "{\"type\":\"defendCity\",\"targetId\":\"city\"}"
        ]));
    }

    [Test]
    public async Task NonCanonicalStackOrderTest()
    {
        WorldMap map = TestHelpers.BuildMap(["..", ".."]);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithPlayer(player).BuildSession();
        WorldStackId stackId = new("stack");
        WorldState state = session.World.State with
        {
            Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(
                stackId,
                TestHelpers.SimpleStack(stackId, player.Id, new WorldPosition(0, 0), UnitGrid<WorldUnit>.Empty, movementPoints: 0))
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);

        string json = Encoding.UTF8.GetString(stream.ToArray());
        string invalidJson = json.Replace("\"order\":{\"type\":\"normal\"}", "\"order\":{\"type\":\"heal\"}", StringComparison.Ordinal);

        Assert.That(invalidJson, Is.Not.EqualTo(json));

        using MemoryStream invalidStream = new(Encoding.UTF8.GetBytes(invalidJson));

        await Assert.ThrowsAsync<JsonException>(async () => await WorldStateJsonSerializer.DeserializeAsync(invalidStream));
    }

    [Test]
    public async Task MissingPropertyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, session.World.State);

        string json = Encoding.UTF8.GetString(stream.ToArray());
        string invalidJson = json.Replace("\"incomingDiplomacyProposals\":[],", string.Empty, StringComparison.Ordinal);

        Assert.That(invalidJson, Is.Not.EqualTo(json));

        using MemoryStream invalidStream = new(Encoding.UTF8.GetBytes(invalidJson));

        await Assert.ThrowsAsync<JsonException>(async () => await WorldStateJsonSerializer.DeserializeAsync(invalidStream));
    }

    [Test]
    public async Task MissingTurnActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, session.World.State);

        string json = Encoding.UTF8.GetString(stream.ToArray());
        string invalidJson = json.Replace("\"spellResearched\":false,", string.Empty, StringComparison.Ordinal);

        Assert.That(invalidJson, Is.Not.EqualTo(json));

        using MemoryStream invalidStream = new(Encoding.UTF8.GetBytes(invalidJson));

        await Assert.ThrowsAsync<JsonException>(async () => await WorldStateJsonSerializer.DeserializeAsync(invalidStream));
    }

    [Test]
    public async Task MissingModifierSourceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldStackId stackId = new("stack");
        WorldUnit unit = new(
            new WorldUnitId("unit"),
            new UnitDefinitionId("unit"),
            Hitpoints: 10,
            Experience: 0,
            Level: 1,
            [new UnitModifier(new UnitModifierDefinitionId("modifier"), RemainingTurns: 2)],
            Name: null,
            IsTypeLocked: false);
        UnitGrid<WorldUnit> units = new UnitGrid<WorldUnit>.Builder()
            .Place(unit, GridPosition.FrontBottom, isLarge: false)
            .Build();
        WorldState state = session.World.State with
        {
            Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(
                stackId,
                TestHelpers.SimpleStack(stackId, player.Id, new WorldPosition(0, 0), units, movementPoints: 0))
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);

        string json = Encoding.UTF8.GetString(stream.ToArray());
        string invalidJson = json.Replace(",\"source\":\"Unit\"", string.Empty, StringComparison.Ordinal);

        Assert.That(invalidJson, Is.Not.EqualTo(json));

        using MemoryStream invalidStream = new(Encoding.UTF8.GetBytes(invalidJson));

        await Assert.ThrowsAsync<JsonException>(async () => await WorldStateJsonSerializer.DeserializeAsync(invalidStream));
    }

    [Test]
    public async Task TombRoundTripTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Elf, map);
        (GameSession session, _) = new TestWorldEngineBuilder().WithMap(map).WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior)).WithPlayer(player).BuildSession();
        WorldState state = session.World.State with
        {
            Tombs = ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty
                .Add(
                    new WorldTombId("Tomb0001"),
                    new WorldTomb(new WorldTombId("Tomb0001"), new WorldPosition(1, 1), new WorldPlayerId(2), new UnitDefinitionId("hero"), "Aldric", new WorldPlayerId(1), new UnitDefinitionId("orc"), "Morwen", Turn: 3))
                .Add(
                    new WorldTombId("Tomb0002"),
                    new WorldTomb(new WorldTombId("Tomb0002"), new WorldPosition(1, 1), new WorldPlayerId(1), new UnitDefinitionId("hero"), LeaderName: null, DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 5))
        };
        using MemoryStream stream = new();

        await WorldStateJsonSerializer.SerializeAsync(stream, state);

        string json = Encoding.UTF8.GetString(stream.ToArray());
        JsonNode root = Assert.NotNull(JsonNode.Parse(json));
        JsonNode tombsNode = Assert.NotNull(root["tombs"]);
        JsonNode namedNode = Assert.NotNull(tombsNode["Tomb0001"]);
        JsonNode unnamedNode = Assert.NotNull(tombsNode["Tomb0002"]);

        stream.Position = 0;
        WorldState actual = await WorldStateJsonSerializer.DeserializeAsync(stream);

        Assert.That(actual, Is.EqualTo(state));
        Assert.That(namedNode.ToJsonString(), Is.EqualTo(
            "{\"id\":\"Tomb0001\",\"position\":{\"x\":1,\"y\":1},\"ownerId\":2,\"leaderDefinitionId\":\"hero\",\"leaderName\":\"Aldric\",\"destroyerPlayerId\":1,\"destroyerLeaderDefinitionId\":\"orc\",\"destroyerLeaderName\":\"Morwen\",\"turn\":3}"));
        Assert.That(unnamedNode.ToJsonString(), Is.EqualTo(
            "{\"id\":\"Tomb0002\",\"position\":{\"x\":1,\"y\":1},\"ownerId\":1,\"leaderDefinitionId\":\"hero\",\"leaderName\":null,\"destroyerPlayerId\":null,\"destroyerLeaderDefinitionId\":null,\"destroyerLeaderName\":null,\"turn\":5}"));
    }
}
