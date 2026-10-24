using Gallean.Common;
using Gallean.Entities.GameData;

namespace Gallean.Engine.Tests;

public sealed class ScenarioLoaderTests
{
    private static readonly string[] SkirmishScenarios =
    [
        "S200SC0000",
        "S201SC0000",
        "S202SC0000",
        "S203SC0000",
        "S204SC0000",
        "S205SC0000",
        "S206SC0000",
        "S207SC0000",
        "S208SC0000",
        "S209SC0000",
        "S210SC0000",
        "S211SC0000",
        "S212SC0000",
        "S213SC0000",
        "S214SC0000",
        "S215SC0000",
        "S216SC0000",
        "S217SC0000",
        "S218SC0000",
        "S219SC0000",
        "S220SC0000",
        "S221SC0000",
        "S222SC0000",
        "S223SC0000",
        "S224SC0000",
        "S225SC0000",
        "S226SC0000",
        "S227SC0000",
        "S228SC0000",
        "S229SC0000",
        "S230SC0000",
        "S231SC0000",
        "S232SC0000",
        "S233SC0000",
        "S234SC0000",
        "S235SC0000",
        "S236SC0000",
        "S237SC0000",
        "S238SC0000",
        "S239SC0000",
        "S240SC0000",
        "S241SC0000",
        "S242SC0000",
        "S243SC0000",
        "S244SC0000",
        "S245SC0000",
        "S246SC0000",
        "S247SC0000",
        "S248SC0000",
        "S249SC0000",
        "S250SC0000",
        "S251SC0000",
        "S252SC0000",
        "S253SC0000",
        "S254SC0000",
        "S255SC0000",
        "S256SC0000",
        "S257SC0000",
        "S258SC0000",
        "S259SC0000"
    ];

    private readonly GlobalVariables globals = new();
    private CampaignDatabase campaignDb = null!;
    private IGameDefinitionProvider definitions = null!;
    private GlobalDatabase globalDb = null!;
    private ScenarioDatabase scenarioDb = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        scenarioDb = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        definitions = new GameDefinitionProvider(globalDb);
    }

    private static IReadOnlyList<string> FormatPlayers(ScenarioId scenarioId, WorldState state)
    {
        List<string> rows = [];

        foreach (WorldPlayer player in state.Players.Values)
        {
            string control = "ai";

            if (!player.IsAi)
            {
                control = "human";
            }

            rows.Add($"{scenarioId} {player.Id} {player.Race} {player.LordId} {control}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatRelations(WorldState state)
    {
        List<string> rows = [];

        foreach (WorldRelation relation in state.Relations.Values)
        {
            rows.Add(
                $"{relation.Id} {relation.Current}/{relation.Previous} alliance={relation.Alliance} " +
                $"turn={relation.AllianceTurn} permanentWar={relation.PermanentWar} permanentAlliance={relation.PermanentAlliance}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatEventStates(WorldState state)
    {
        List<string> rows = [];

        foreach ((ScenarioEventId eventId, bool enabled) in state.ScenarioState.EventStates)
        {
            rows.Add($"{eventId} {(enabled ? "enabled" : "disabled")}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatTemplateInstances(WorldState state)
    {
        List<string> rows = [];

        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (stack.SourceTemplateId is { } templateId)
            {
                rows.Add($"{stack.Id} {templateId} owner={stack.OwnerId} at ({stack.Position.X},{stack.Position.Y})");
            }
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatNames(IReadOnlyList<ScenarioName> names)
    {
        return [.. names.Select(name => $"{name.Type} {name.Text}")];
    }

    private static IEnumerable<string> GetScenarios()
    {
        ScenarioDatabaseIndex index = ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath);

        return index.ScenarioInfos.Select(info => info.ScenarioId.Value);
    }

    [TestCaseSource(nameof(GetScenarios))]
    public void LoadScenarioTest(string id)
    {
        ScenarioId scenarioId = new(id);
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);
        WorldState state = scenario.State;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Map.Width, Is.GreaterThan(0));
            Assert.That(state.Players, Is.Not.Empty);
        }
    }

    [TestCaseSource(nameof(GetScenarios))]
    public void LoadScenarioPopupTextTest(string id)
    {
        ScenarioId scenarioId = new(id);
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        GameTextProvider texts = new(globalDb, scenarioDb, scenarioDb.TextScenarios);

        IEnumerable<string> popupTexts = scenario.Provider.Events
            .SelectMany(scenarioEvent => scenarioEvent.Effects)
            .OfType<ScenarioEventPopupEffect>()
            .Select(effect => texts.GetScenarioText(effect.TextId));

        foreach (string text in popupTexts)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(text, Does.Not.Contain("  "));
                Assert.That(text, Does.Not.Contain(" \n"));
                Assert.That(text, Does.Not.Contain("\n "));
                Assert.That(text.Any(ch => char.IsControl(ch) && ch != '\n'), Is.False);
                Assert.That(text, Is.EqualTo(text.Trim()));
            }
        }
    }

    [TestCaseSource(nameof(GetScenarios))]
    public void LoadScenarioTextIdTest(string id)
    {
        ScenarioId scenarioId = new(id);
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);
        GameTextProvider texts = new(globalDb, scenarioDb, scenarioDb.TextScenarios);

        List<ScenarioTextId> textIds = [];

        foreach (ScenarioLocationDefinition location in scenario.Provider.Locations)
        {
            textIds.Add(location.NameTextId);
        }

        foreach (WorldSite site in scenario.State.Sites.Values)
        {
            textIds.Add(site.TitleTextId);
            textIds.Add(site.GreetingTextId);
        }

        foreach (ScenarioEventDefinition scenarioEvent in scenario.Provider.Events)
        {
            foreach (ScenarioEventChangeObjectiveEffect effect in scenarioEvent.Effects.OfType<ScenarioEventChangeObjectiveEffect>())
            {
                textIds.Add(effect.TextId);
            }
        }

        foreach (ScenarioTextId textId in textIds)
        {
            Assert.That(texts.GetScenarioText(textId), Is.Not.Null);
        }
    }

    [Test]
    public void Load_S137SC0000_DefinitionTest()
    {
        ScenarioId scenarioId = new("S137SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(
            scenario.Provider.Definition,
            Is.EqualTo(new ScenarioDefinition(
                scenarioId,
                new ScenarioInfoTexts(
                    new ScenarioTextId("S137IF0000:NAME:0"),
                    Description: null,
                    new ScenarioTextId("S137IF0000:BRIEFING:0"),
                    new ScenarioTextId("S137IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S137IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S137IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S137IF0000:CREATOR:0")),
                SuggestedLevel: 1,
                MaxLeaderLevel: 8,
                MaxCityLevel: 5,
                NextScenarioId: new ScenarioId("S138SC0000"))));
    }

    [Test]
    public void Load_S099SC0000_DefinitionTest()
    {
        ScenarioId scenarioId = new("S099SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(
            scenario.Provider.Definition,
            Is.EqualTo(new ScenarioDefinition(
                scenarioId,
                new ScenarioInfoTexts(
                    new ScenarioTextId("S099IF0000:NAME:0"),
                    new ScenarioTextId("S099IF0000:DESC:0"),
                    new ScenarioTextId("S099IF0000:BRIEFING:0"),
                    new ScenarioTextId("S099IF0000:BRIEFLONG:0"),
                    new ScenarioTextId("S099IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S099IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S099IF0000:CREATOR:0")),
                SuggestedLevel: 1,
                MaxLeaderLevel: 15,
                MaxCityLevel: 5,
                NextScenarioId: null)));
    }

    [Test]
    public void Load_S137SC0000_PlayersTest()
    {
        ScenarioId scenarioId = new("S137SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        WorldPlayer expectedNeutral = new(
            new WorldPlayerId(1),
            new ScenarioTextId("S137PL0000:NAME_TXT:0"),
            new LordDefinitionId("G000LR0013"),
            WorldRace.Neutral,
            new WorldObjectVariant(1),
            WorldAiAttitudeType.Medium,
            IsAi: true,
            WorldResourceSet.Empty,
            new WorldPlayerBuildings([], []),
            new WorldPlayerSpellbook([], []),
            ImmutableBitmap.Create(48, 48),
            [],
            [],
            [],
            new WorldPlayerTurnActions());

        WorldPlayer expectedElf = new(
            new WorldPlayerId(2),
            new ScenarioTextId("S137PL0001:NAME_TXT:0"),
            new LordDefinitionId("G000LR0016"),
            WorldRace.Elf,
            new WorldObjectVariant(0),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            new WorldResourceSet(550, 0, 0, 0, 0, 25),
            new WorldPlayerBuildings(
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0100"),
                    new WorldPlayerBuildingDefinitionId("G000BB0101"),
                    new WorldPlayerBuildingDefinitionId("G000BB0102"),
                    new WorldPlayerBuildingDefinitionId("G000BB0103"),
                    new WorldPlayerBuildingDefinitionId("G000BB0104"),
                    new WorldPlayerBuildingDefinitionId("G000BB0105"),
                    new WorldPlayerBuildingDefinitionId("G000BB0106"),
                    new WorldPlayerBuildingDefinitionId("G000BB0107"),
                    new WorldPlayerBuildingDefinitionId("G000BB0108"),
                    new WorldPlayerBuildingDefinitionId("G000BB0109"),
                    new WorldPlayerBuildingDefinitionId("G000BB0110"),
                    new WorldPlayerBuildingDefinitionId("G000BB0111"),
                    new WorldPlayerBuildingDefinitionId("G000BB0112"),
                    new WorldPlayerBuildingDefinitionId("G000BB0113"),
                    new WorldPlayerBuildingDefinitionId("G000BB0116"),
                    new WorldPlayerBuildingDefinitionId("G000BB0118"),
                    new WorldPlayerBuildingDefinitionId("G000BB0119"),
                    new WorldPlayerBuildingDefinitionId("G000BB0120")
                ],
                []),
            new WorldPlayerSpellbook(
                [
                    new WorldSpellDefinitionId("G000SS0097"),
                    new WorldSpellDefinitionId("G000SS0098"),
                    new WorldSpellDefinitionId("G000SS0099"),
                    new WorldSpellDefinitionId("G000SS0100"),
                    new WorldSpellDefinitionId("G000SS0101"),
                    new WorldSpellDefinitionId("G000SS0102"),
                    new WorldSpellDefinitionId("G000SS0103"),
                    new WorldSpellDefinitionId("G000SS0104"),
                    new WorldSpellDefinitionId("G000SS0105"),
                    new WorldSpellDefinitionId("G000SS0106"),
                    new WorldSpellDefinitionId("G000SS0107"),
                    new WorldSpellDefinitionId("G000SS0108"),
                    new WorldSpellDefinitionId("G000SS0109"),
                    new WorldSpellDefinitionId("G000SS0110"),
                    new WorldSpellDefinitionId("G000SS0111")
                ],
                []),
            ImmutableBitmap.Create(48, 48),
            [],
            [],
            [],
            new WorldPlayerTurnActions());

        Assert.That(scenario.State.Players.Values, Is.EqualTo([expectedNeutral, expectedElf]));
    }

    [Test]
    public void Load_S137SC0000_ConfigurationTest()
    {
        ScenarioId scenarioId = new("S137SC0000");
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Elf, LordCategory.Mage, new WorldObjectVariant(2))], WorldDifficulty.Hard, Leader: null);
        Scenario scenario = ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals, configuration, new FixedRandomProvider());

        WorldPlayer expectedNeutral = new(
            new WorldPlayerId(1),
            new ScenarioTextId("S137PL0000:NAME_TXT:0"),
            new LordDefinitionId("G000LR0013"),
            WorldRace.Neutral,
            new WorldObjectVariant(1),
            WorldAiAttitudeType.Medium,
            IsAi: true,
            WorldResourceSet.Empty,
            new WorldPlayerBuildings([], []),
            new WorldPlayerSpellbook([], []),
            ImmutableBitmap.Create(48, 48),
            [],
            [],
            [],
            new WorldPlayerTurnActions());

        WorldPlayer expectedElf = new(
            new WorldPlayerId(2),
            new ScenarioTextId("S137PL0001:NAME_TXT:0"),
            new LordDefinitionId("G000LR0018"),
            WorldRace.Elf,
            new WorldObjectVariant(2),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            new WorldResourceSet(275, 0, 0, 0, 0, 12),
            new WorldPlayerBuildings(
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0100"),
                    new WorldPlayerBuildingDefinitionId("G000BB0101"),
                    new WorldPlayerBuildingDefinitionId("G000BB0102"),
                    new WorldPlayerBuildingDefinitionId("G000BB0103"),
                    new WorldPlayerBuildingDefinitionId("G000BB0104"),
                    new WorldPlayerBuildingDefinitionId("G000BB0105"),
                    new WorldPlayerBuildingDefinitionId("G000BB0106"),
                    new WorldPlayerBuildingDefinitionId("G000BB0107"),
                    new WorldPlayerBuildingDefinitionId("G000BB0108"),
                    new WorldPlayerBuildingDefinitionId("G000BB0109"),
                    new WorldPlayerBuildingDefinitionId("G000BB0110"),
                    new WorldPlayerBuildingDefinitionId("G000BB0111"),
                    new WorldPlayerBuildingDefinitionId("G000BB0112"),
                    new WorldPlayerBuildingDefinitionId("G000BB0113"),
                    new WorldPlayerBuildingDefinitionId("G000BB0116"),
                    new WorldPlayerBuildingDefinitionId("G000BB0118"),
                    new WorldPlayerBuildingDefinitionId("G000BB0119"),
                    new WorldPlayerBuildingDefinitionId("G000BB0120")
                ],
                []),
            new WorldPlayerSpellbook(
                [
                    new WorldSpellDefinitionId("G000SS0097"),
                    new WorldSpellDefinitionId("G000SS0098"),
                    new WorldSpellDefinitionId("G000SS0099"),
                    new WorldSpellDefinitionId("G000SS0100"),
                    new WorldSpellDefinitionId("G000SS0101"),
                    new WorldSpellDefinitionId("G000SS0102"),
                    new WorldSpellDefinitionId("G000SS0103"),
                    new WorldSpellDefinitionId("G000SS0104"),
                    new WorldSpellDefinitionId("G000SS0105"),
                    new WorldSpellDefinitionId("G000SS0106"),
                    new WorldSpellDefinitionId("G000SS0107"),
                    new WorldSpellDefinitionId("G000SS0108"),
                    new WorldSpellDefinitionId("G000SS0109"),
                    new WorldSpellDefinitionId("G000SS0110"),
                    new WorldSpellDefinitionId("G000SS0111")
                ],
                []),
            ImmutableBitmap.Create(48, 48),
            [],
            [],
            [],
            new WorldPlayerTurnActions());

        Assert.That(scenario.State.Difficulty, Is.EqualTo(WorldDifficulty.Hard));
        Assert.That(scenario.State.Players.Values, Is.EqualTo([expectedNeutral, expectedElf]));
    }

    [Test]
    public void Load_S253SC0000_PlayersTest()
    {
        ScenarioId scenarioId = new("S253SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        WorldPlayer expectedNeutral = new(
            new WorldPlayerId(1),
            new ScenarioTextId("S253PL0000:NAME_TXT:0"),
            new LordDefinitionId("G000LR0013"),
            WorldRace.Neutral,
            new WorldObjectVariant(1),
            WorldAiAttitudeType.Medium,
            IsAi: true,
            WorldResourceSet.Empty,
            new WorldPlayerBuildings([], []),
            new WorldPlayerSpellbook([], []),
            ImmutableBitmap.Create(48, 48),
            [],
            [],
            [],
            new WorldPlayerTurnActions());

        WorldPlayer expectedHuman = new(
            new WorldPlayerId(2),
            new ScenarioTextId("S253PL0009:NAME_TXT:0"),
            new LordDefinitionId("G000LR0001"),
            WorldRace.Human,
            new WorldObjectVariant(0),
            WorldAiAttitudeType.SmallNoExpansion,
            IsAi: false,
            new WorldResourceSet(600, 0, 0, 600, 0, 0),
            new WorldPlayerBuildings(
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0007"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0016"),
                    new WorldPlayerBuildingDefinitionId("G000BB0019"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023"),
                    new WorldPlayerBuildingDefinitionId("G000BB0024"),
                    new WorldPlayerBuildingDefinitionId("G000BB0025")
                ],
                []),
            new WorldPlayerSpellbook(
                [
                    new WorldSpellDefinitionId("G000SS0001"),
                    new WorldSpellDefinitionId("G000SS0002"),
                    new WorldSpellDefinitionId("G000SS0003"),
                    new WorldSpellDefinitionId("G000SS0004"),
                    new WorldSpellDefinitionId("G000SS0005")
                ],
                []),
            ImmutableBitmap.Create(48, 48),
            [],
            [],
            [],
            new WorldPlayerTurnActions());

        Assert.That(scenario.State.Players.Values, Is.EqualTo([expectedNeutral, expectedHuman]));
    }

    [Test]
    public void Load_S253SC0000_DefinitionTest()
    {
        ScenarioId scenarioId = new("S253SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(
            scenario.Provider.Definition,
            Is.EqualTo(new ScenarioDefinition(
                scenarioId,
                new ScenarioInfoTexts(
                    new ScenarioTextId("S253IF0000:NAME:0"),
                    new ScenarioTextId("S253IF0000:DESC:0"),
                    new ScenarioTextId("S253IF0000:BRIEFING:0"),
                    Briefing: null,
                    new ScenarioTextId("S253IF0000:DEBUNKW:0"),
                    new ScenarioTextId("S253IF0000:DEBUNKL:0"),
                    new ScenarioTextId("S253IF0000:CREATOR:0")),
                SuggestedLevel: 1,
                MaxLeaderLevel: 3,
                MaxCityLevel: 5,
                NextScenarioId: null)));
    }

    [Test]
    public void Load_S095SC0000_Stack_S095KC0003_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u5035 = new("G000UU5035");
        UnitDefinitionId u5136 = new("G000UU5136");

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S095UN0014"), u5035, 370, 0, 1, [], null, false), GridPosition.FrontBottom, true)
            .Place(new WorldUnit(new WorldUnitId("S095UN0012"), u5136, 420, 0, 1, [], Name: "Giant Spider", IsTypeLocked: false), GridPosition.FrontTop, true)
            .Build();

        Assert.That(scenario.State.Stacks[new WorldStackId("S095KC0003")], Is.EqualTo(new WorldStack(
            new WorldStackId("S095KC0003"),
            new WorldPlayerId(1),
            new WorldPosition(40, 40),
            WorldStackDirection.NorthEast,
            expectedUnits,
            MovementPoints: 20,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            new WorldSubraceId("S095SR0003"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Guard.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null)));
    }

    [Test]
    public void Load_S096SC0000_Stack_S096KC0041_Test()
    {
        ScenarioId scenarioId = new("S096SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u0072 = new("G000UU0072");
        UnitDefinitionId u0058 = new("G000UU0058");
        UnitDefinitionId u0171 = new("G000UU0171");

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S096UN0115"), u0171, 120, 0, 3, [], null, false), GridPosition.BackBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S096UN0113"), u0072, 135, 0, 8, [UnitModifier.FromScenario(new UnitModifierDefinitionId("G000UM9024"))], Name: "Ysnegrak", IsTypeLocked: false), GridPosition.BackCenter, false)
            .Place(new WorldUnit(new WorldUnitId("S096UN0114"), u0058, 270, 0, 2, [], null, false), GridPosition.FrontTop, true)
            .Build();

        Assert.That(scenario.State.Stacks[new WorldStackId("S096KC0041")], Is.EqualTo(new WorldStack(
            new WorldStackId("S096KC0041"),
            new WorldPlayerId(3),
            new WorldPosition(66, 64),
            WorldStackDirection.SouthEast,
            expectedUnits,
            MovementPoints: 20,
            new WorldInventory([
                new WorldItem(new WorldItemId("S096IM0036"), new ItemDefinitionId("G000IG9119"), Charges: 5),
                new WorldItem(new WorldItemId("S096IM007F"), new ItemDefinitionId("G000IG9005"), Charges: null)
            ]),
            LeaderEquipment.Empty,
            new WorldSubraceId("S096SR000a"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Guard.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 7,
            BattlesWon: 0,
            SourceTemplateId: null)));
    }

    [Test]
    public void Load_S095SC0000_Stack_S095KC0011_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u5017 = new("G000UU5017");
        UnitDefinitionId u5018 = new("G000UU5018");
        UnitDefinitionId u5117 = new("G000UU5117");

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S095UN0045"), u5017, 50, 0, 1, [], null, false), GridPosition.FrontBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0043"), u5018, 40, 0, 1, [], null, false), GridPosition.BackBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0042"), u5117, 50, 0, 1, [], Name: "Goblin", IsTypeLocked: false), GridPosition.FrontTop, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0044"), u5018, 40, 0, 1, [], null, false), GridPosition.BackTop, false)
            .Build();

        Assert.That(scenario.State.Stacks[new WorldStackId("S095KC0011")], Is.EqualTo(new WorldStack(
            new WorldStackId("S095KC0011"),
            new WorldPlayerId(1),
            new WorldPosition(15, 43),
            WorldStackDirection.SouthWest,
            expectedUnits,
            MovementPoints: 15,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            new WorldSubraceId("S095SR0003"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Stand.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null)));
    }

    [Test]
    public void Load_S095SC0000_Stack_S095KC001e_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u0053 = new("G000UU0053");
        UnitDefinitionId u0063 = new("G000UU0063");
        UnitDefinitionId u0072 = new("G000UU0072");

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S095UN0082"), u0053, 170, 0, 2, [], null, false), GridPosition.FrontBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0083"), u0053, 170, 0, 2, [], null, false), GridPosition.FrontTop, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0081"), u0072, 85, 0, 3, [], Name: "Lofemyr", IsTypeLocked: false), GridPosition.BackBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0084"), u0063, 75, 0, 2, [], null, false), GridPosition.BackTop, false)
            .Build();

        Assert.That(scenario.State.Stacks[new WorldStackId("S095KC001e")], Is.EqualTo(new WorldStack(
            new WorldStackId("S095KC001e"),
            new WorldPlayerId(3),
            new WorldPosition(34, 2),
            WorldStackDirection.South,
            expectedUnits,
            MovementPoints: 20,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            new WorldSubraceId("S095SR000a"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Stand.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 2,
            BattlesWon: 0,
            SourceTemplateId: null)));
    }

    [Test]
    public void Load_S095SC0000_Stack_S095KC0027_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u0005 = new("G000UU0005");
        UnitDefinitionId u0150 = new("G000UU0150");
        UnitDefinitionId u0153 = new("G000UU0153");
        UnitDefinitionId u0154 = new("G000UU0154");
        UnitDefinitionId u6008 = new("G000UU6008");

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S095UN00bd"), u0005, 180, 0, 3, [], null, false), GridPosition.FrontBottom, false)
            .Place(
                new WorldUnit(new WorldUnitId("S095UN00aa"), u6008, 200, 0, 1, [UnitModifier.FromScenario(new UnitModifierDefinitionId("G000UM9034"))], Name: "Hubert de Lalye", IsTypeLocked: false),
                GridPosition.FrontTop,
                false)
            .Place(new WorldUnit(new WorldUnitId("S095UN00ac"), u0153, 95, 0, 3, [], null, false), GridPosition.BackBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN00ad"), u0150, 125, 0, 4, [], null, false), GridPosition.BackCenter, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN00ae"), u0154, 135, 0, 3, [], null, false), GridPosition.BackTop, false)
            .Build();

        Assert.That(scenario.State.Stacks[new WorldStackId("S095KC0027")], Is.EqualTo(new WorldStack(
            new WorldStackId("S095KC0027"),
            new WorldPlayerId(1),
            new WorldPosition(44, 46),
            WorldStackDirection.NorthEast,
            expectedUnits,
            MovementPoints: 20,
            new WorldInventory([
                new WorldItem(new WorldItemId("S095IM0029"), new ItemDefinitionId("G000IG0001"), Charges: null),
                new WorldItem(new WorldItemId("S095IM002A"), new ItemDefinitionId("G000IG0001"), Charges: null),
                new WorldItem(new WorldItemId("S095IM002B"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S095IM002C"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S095IM002D"), new ItemDefinitionId("G000IG1001"), Charges: null)
            ]),
            LeaderEquipment.Empty,
            new WorldSubraceId("S095SR0001"),
            AiPriority: 1,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Stand.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null)));
    }

    [Test]
    public void Load_S095SC0000_Capital_S095FT0000_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u0004 = new("G000UU0004");
        UnitDefinitionId u0006 = new("G000UU0006");
        UnitDefinitionId u0011 = new("G000UU0011");
        UnitDefinitionId u3001 = new("G000UU3001");

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S095UN0004"), u0004, 125, 0, 2, [], null, false), GridPosition.FrontBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0000"), u3001, 900, 0, 1, [], null, false), GridPosition.FrontCenter, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0006"), u0011, 50, 0, 1, [], null, false), GridPosition.FrontTop, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0005"), u0006, 45, 0, 1, [], null, false), GridPosition.BackCenter, false)
            .Build();

        Assert.That(scenario.State.Fortifications[new WorldFortificationId("S095FT0000")], Is.EqualTo(new WorldCapital(
            new WorldFortificationId("S095FT0000"),
            new ScenarioTextId("S095FT0000:NAME_TXT:0"),
            new WorldPlayerId(2),
            new WorldFootprint(new WorldPosition(9, 33), 5, 5),
            expectedGarrison,
            new WorldInventory([
                new WorldItem(new WorldItemId("S095IM0000"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S095IM0001"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S095IM0028"), new ItemDefinitionId("G000IG0001"), Charges: null)
            ]),
            AiPriority: 3,
            CapturedById: null)));
    }

    [Test]
    public void Load_S095SC0000_City_S095FT0002_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.State.Fortifications[new WorldFortificationId("S095FT0002")], Is.EqualTo(new WorldCity(
            new WorldFortificationId("S095FT0002"),
            new ScenarioTextId("S095FT0002:NAME_TXT:0"),
            new WorldCityDefinitionId(WorldRace.Neutral, 2),
            new WorldPlayerId(1),
            new WorldSubraceId("S095SR0000"),
            new WorldFootprint(new WorldPosition(6, 2), 4, 4),
            UnitGrid<WorldUnit>.Empty,
            WorldInventory.Empty,
            AiPriority: 1,
            RiotTurns: 0,
            GrowthTurn: 0)));
    }

    [Test]
    public void Load_S112SC0000_Stack_S112KC003c_Test()
    {
        ScenarioId scenarioId = new("S112SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u0013 = new("G000UU0013");
        UnitDefinitionId u0015 = new("G000UU0015");
        UnitDefinitionId u0017 = new("G000UU0017");
        UnitDefinitionId u0154 = new("G000UU0154");
        UnitDefinitionId u5356 = new("G000UU5356");

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S112UN0105"), u0015, 175, 0, 4, [], null, false), GridPosition.FrontBottom, false)
            .Place(
                new WorldUnit(new WorldUnitId("S112UN0101"), u5356, 225, 0, 1, [UnitModifier.FromScenario(new UnitModifierDefinitionId("G000UM9037"))], Name: "Knight Dasannar", IsTypeLocked: false),
                GridPosition.FrontCenter,
                false)
            .Place(new WorldUnit(new WorldUnitId("S112UN0104"), u0015, 175, 0, 4, [], null, false), GridPosition.FrontTop, false)
            .Place(new WorldUnit(new WorldUnitId("S112UN0103"), u0017, 100, 0, 3, [], null, false), GridPosition.BackBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S112UN0106"), u0154, 135, 0, 3, [], null, false), GridPosition.BackCenter, false)
            .Place(new WorldUnit(new WorldUnitId("S112UN0102"), u0013, 100, 0, 3, [], null, false), GridPosition.BackTop, false)
            .Build();

        Assert.That(scenario.State.Stacks[new WorldStackId("S112KC003c")], Is.EqualTo(new WorldStack(
            new WorldStackId("S112KC003c"),
            new WorldPlayerId(2),
            new WorldPosition(60, 56),
            WorldStackDirection.SouthEast,
            expectedUnits,
            MovementPoints: 20,
            new WorldInventory([
                new WorldItem(new WorldItemId("S112IM006E"), new ItemDefinitionId("G000IG0018"), Charges: null),
                new WorldItem(new WorldItemId("S112IM006F"), new ItemDefinitionId("G000IG0001"), Charges: null),
                new WorldItem(new WorldItemId("S112IM0070"), new ItemDefinitionId("G000IG0006"), Charges: null)
            ]),
            LeaderEquipment.Empty,
            new WorldSubraceId("S112SR0001"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Guard.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null)));
    }

    [Test]
    public void Load_S095SC0000_ScenarioEvent_S095EV0000_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S095EV0000"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S095EV0000"),
            [WorldRace.Human],
            [WorldRace.Human, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(new WorldStackId("S095KC0022")), new ScenarioLocationId("S095LO0001")),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S095EV0000:POPUP_TXT:1"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0154"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S095KC0022")),
                    new WorldSubraceId("S095SR0009"),
                    FirstOnly: false,
                    PlayAnimation: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S095SC0000_ScenarioEvent_S095EV0002_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S095EV0002"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S095EV0002"),
            [WorldRace.Human, WorldRace.Heretic, WorldRace.Neutral],
            [WorldRace.Human, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("S095KC0022")))],
            [
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S095EV0001"), false),
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S095EV000d"), false),
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S095EV0017"), false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S095SC0000_ScenarioEvent_S095EV0019_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S095EV0019"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S095EV0019"),
            [WorldRace.Human],
            [WorldRace.Human, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S095LO0004"))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S095EV0019:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU6120"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventCreateStackEffect(new ScenarioStackTemplateId("S095TM0004"), new ScenarioLocationId("S095LO0007")),
                new ScenarioEventCreateStackEffect(new ScenarioStackTemplateId("S095TM0004"), new ScenarioLocationId("S095LO0007")),
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S095KC0029")),
                    new WorldSubraceId("S095SR0009"),
                    FirstOnly: false,
                    PlayAnimation: true),
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S095EV0009"), false),
                new ScenarioEventChangeObjectiveEffect(new ScenarioTextId("S095EV0019:OBJECT_TXT:0"))
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S095SC0000_ScenarioEvent_S095EV0010_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S095EV0010"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S095EV0010"),
            [WorldRace.Human, WorldRace.Heretic, WorldRace.Neutral],
            [WorldRace.Human, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("S095KC0027")))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S095EV0010:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.AllPlayers,
                    new ScenarioEventPortraitId("G000UU6108"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S095EV0010:POPUP_TXT:1"),
                    ScenarioEventPopupAudience.AllPlayers,
                    new ScenarioEventPortraitId("G000UU6120"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventWinEffect(new WorldPlayerId(2))
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S095SC0000_ScenarioEvent_S095EV0009_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S095EV0009"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S095EV0009"),
            [WorldRace.Human],
            [WorldRace.Human, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventFrequencyCondition(33)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S095EV0009:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU6120"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventCreateStackEffect(new ScenarioStackTemplateId("S095TM0004"), new ScenarioLocationId("S095LO0007")),
                new ScenarioEventCreateStackEffect(new ScenarioStackTemplateId("S095TM0004"), new ScenarioLocationId("S095LO0007")),
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S095KC0029")),
                    new WorldSubraceId("S095SR0009"),
                    FirstOnly: false,
                    PlayAnimation: true),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S095EV0009:POPUP_TXT:4"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU6108"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S100SC0000_ScenarioEvent_S100EV000d_Test()
    {
        ScenarioId scenarioId = new("S100SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S100EV000d"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S100EV000d"),
            [WorldRace.Heretic],
            [WorldRace.Dwarf, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(new WorldStackId("S100KC002e")), new ScenarioLocationId("S100LO0006")),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S100EV000d:POPUP_TXT:1"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0036"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S100EV000d:POPUP_TXT:2"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0062"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventMoveStackEffect(new ScenarioStackReference.Stack(new WorldStackId("S100KC002e")), new ScenarioLocationId("S100LO0007")),
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S100KC002e")),
                    new WorldStackOrder.MoveToLocation(new ScenarioLocationId("S100LO0008"), new WorldPosition(7, 17), 1),
                    FirstOnly: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S098SC0000_ScenarioEvent_S098EV0015_Test()
    {
        ScenarioId scenarioId = new("S098SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S098EV0015"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S098EV0015"),
            [WorldRace.Human],
            [WorldRace.Human, WorldRace.Dwarf, WorldRace.Heretic, WorldRace.Neutral],
            [
                new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(new WorldStackId("S098KC001f")), MustExist: true),
                new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("S098KC0040"))),
                new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("S098KC0041"))),
                new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("S098KC0042")))
            ],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S098EV0015:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU5007"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventGiveItemEffect(new ItemDefinitionId("G000IG2004"), ScenarioEventGiveItemTarget.TriggererCapital),
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S098EV0014"), false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S118SC0000_ScenarioEvent_S118EV0024_Test()
    {
        ScenarioId scenarioId = new("S118SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S118EV0024"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S118EV0024"),
            [WorldRace.Dwarf],
            [WorldRace.Dwarf],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Template(new ScenarioStackTemplateId("S118TM0003")))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S118EV0024:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU6106"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventWinEffect(new WorldPlayerId(3))
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 26)));
        Assert.That(scenario.State.Stacks.ContainsKey(new WorldStackId("S118TM0003")), Is.False);
    }

    [Test]
    public void Load_S109SC0000_ScenarioEvent_S109EV0012_Test()
    {
        ScenarioId scenarioId = new("S109SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S109EV0012"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S109EV0012"),
            [WorldRace.Undead],
            [WorldRace.Human, WorldRace.Undead, WorldRace.Neutral],
            [new ScenarioEventVisitingSiteCondition(new WorldSiteId("S109SI0001"))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S109EV0012:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU5001"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S109EV0012:POPUP_TXT:1"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0096"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S109EV0012:POPUP_TXT:2"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU5001"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S109EV0012:POPUP_TXT:3"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0096"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S109EV0012:POPUP_TXT:4"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU5001"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 17)));
    }

    [Test]
    public void Load_S139SC0000_ScenarioEvent_S139EV000f_Test()
    {
        ScenarioId scenarioId = new("S139SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S139EV000f"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S139EV000f"),
            [WorldRace.Elf],
            [WorldRace.Dwarf, WorldRace.Undead, WorldRace.Heretic, WorldRace.Neutral, WorldRace.Elf],
            [
                new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S139LO001a")),
                new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(new WorldStackId("S139KC0008")), MustExist: false)
            ],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S139EV000f:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.AllPlayers,
                    new ScenarioEventPortraitId("G000UU8016"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventMoveStackEffect(
                    new ScenarioStackReference.Template(new ScenarioStackTemplateId("S139TM0000")),
                    new ScenarioLocationId("S139LO0014")),
                new ScenarioEventMoveStackEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S139KC004c")),
                    new ScenarioLocationId("S139LO0013")),
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S139EV0010"), true),
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Template(new ScenarioStackTemplateId("S139TM0000")),
                    new WorldSubraceId("S139SR0000"),
                    FirstOnly: true,
                    PlayAnimation: true),
                new ScenarioEventChangeOwnerEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S139KC004c")),
                    new WorldSubraceId("S139SR0000"),
                    FirstOnly: true,
                    PlayAnimation: true)
            ],
            Enabled: false,
            OccurOnce: true,
            Chance: 100,
            Order: 12)));
    }

    [Test]
    public void Load_S114SC0000_ScenarioEvent_S114EV0016_Test()
    {
        ScenarioId scenarioId = new("S114SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S114EV0016"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S114EV0016"),
            [WorldRace.Undead],
            [WorldRace.Dwarf, WorldRace.Undead, WorldRace.Heretic, WorldRace.Neutral],
            [
                new ScenarioEventStackToLocationCondition(
                    new ScenarioStackReference.Stack(new WorldStackId("S114KC0014")),
                    new ScenarioLocationId("S114LO000d"))
            ],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S114EV0016:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU6117"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S114EV0016:POPUP_TXT:4"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0079"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 9)));
    }

    [Test]
    public void Load_S112SC0000_ScenarioEvent_S112EV0015_Test()
    {
        ScenarioId scenarioId = new("S112SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S112EV0015"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S112EV0015"),
            [WorldRace.Undead],
            [WorldRace.Human, WorldRace.Dwarf, WorldRace.Undead, WorldRace.Neutral],
            [
                new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S112LO0016")),
                new ScenarioEventStackExistenceCondition(new ScenarioStackReference.Stack(new WorldStackId("S112KC0043")), MustExist: true)
            ],
            [
                new ScenarioEventRemoveStackEffect(new ScenarioStackReference.Stack(new WorldStackId("S112KC0043")), FirstOnly: true),
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S112KC0051")),
                    WorldStackOrder.Berserk.Instance,
                    FirstOnly: true),
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S112KC0052")),
                    WorldStackOrder.Berserk.Instance,
                    FirstOnly: true),
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S112KC0054")),
                    WorldStackOrder.Berserk.Instance,
                    FirstOnly: true)
            ],
            Enabled: false,
            OccurOnce: true,
            Chance: 100,
            Order: 7)));
    }

    [Test]
    public void Load_S121SC0000_ScenarioEvent_S121EV0003_Test()
    {
        ScenarioId scenarioId = new("S121SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S121EV0003"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S121EV0003"),
            [WorldRace.Dwarf],
            [WorldRace.Dwarf],
            [new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S121LO0009"))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S121EV0003:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0036"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventBattleEffect(new ScenarioStackReference.Stack(new WorldStackId("S121KC0003")))
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 3)));
    }

    [Test]
    public void Load_S138SC0000_ScenarioEvent_S138EV0012_Test()
    {
        ScenarioId scenarioId = new("S138SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S138EV0012"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S138EV0012"),
            [WorldRace.Elf],
            [WorldRace.Dwarf, WorldRace.Heretic, WorldRace.Neutral, WorldRace.Elf],
            [new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S138LO000f"))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S138EV0012:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.AllPlayers,
                    new ScenarioEventPortraitId("G000UU0036"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventCreateStackEffect(
                    new ScenarioStackTemplateId("S138TM0003"),
                    new ScenarioLocationId("S138LO0010")),
                new ScenarioEventCreateStackEffect(
                    new ScenarioStackTemplateId("S138TM0004"),
                    new ScenarioLocationId("S138LO0011")),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S138EV0012:POPUP_TXT:3"),
                    ScenarioEventPopupAudience.AllPlayers,
                    new ScenarioEventPortraitId("G000UU0036"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventBattleEffect(
                    new ScenarioStackReference.Template(new ScenarioStackTemplateId("S138TM0003")))
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 15)));
    }

    [Test]
    public void Load_S141SC0000_ScenarioEvent_S141EV001d_Test()
    {
        ScenarioId scenarioId = new("S141SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S141EV001d"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S141EV001d"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [new ScenarioEventTransformingLandCondition(35)],
            [
                new ScenarioEventCreateStackEffect(new ScenarioStackTemplateId("S141TM0003"), new ScenarioLocationId("S141LO0011")),
                new ScenarioEventCreateStackEffect(new ScenarioStackTemplateId("S141TM0003"), new ScenarioLocationId("S141LO0012")),
                new ScenarioEventCreateStackEffect(new ScenarioStackTemplateId("S141TM0003"), new ScenarioLocationId("S141LO0013")),
                new ScenarioEventChangeOrderEffect(
                    new ScenarioStackReference.Template(new ScenarioStackTemplateId("S141TM0003")),
                    new WorldStackOrder.AttackStack(new WorldStackId("S141KC001d")),
                    FirstOnly: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S141EV001d:POPUP_TXT:4"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU8032"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
            Enabled: false,
            OccurOnce: true,
            Chance: 100,
            Order: 27)));
    }

    [Test]
    public void Load_S142SC0000_ScenarioEvent_S142EV0005_Test()
    {
        ScenarioId scenarioId = new("S142SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S142EV0005"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S142EV0005"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [new ScenarioEventItemToLocationCondition(new ItemDefinitionId("G000IG9149"), new ScenarioLocationId("S142LO0006"))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S142EV0005:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    null,
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 5)));
    }

    [Test]
    public void Load_S106SC0000_ScenarioEvent_S106EV0000_Test()
    {
        ScenarioId scenarioId = new("S106SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S106EV0000"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S106EV0000"),
            [WorldRace.Human, WorldRace.Undead],
            [WorldRace.Human, WorldRace.Dwarf, WorldRace.Undead, WorldRace.Heretic, WorldRace.Neutral],
            [
                new ScenarioEventDiplomacyRelationsCondition(
                    new WorldPlayerId(1),
                    new WorldPlayerId(2),
                    WorldRelationLevel.War),
                new ScenarioEventFrequencyCondition(1)
            ],
            [
                new ScenarioEventAllyEffect(new WorldPlayerId(1), new WorldPlayerId(2), Permanent: false),
                new ScenarioEventDiplomacyEffect(new WorldPlayerId(1), new WorldPlayerId(2), Relation: 100, PermanentWar: null)
            ],
            Enabled: true,
            OccurOnce: false,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S107SC0000_ScenarioEvent_S107EV0018_Test()
    {
        ScenarioId scenarioId = new("S107SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S107EV0018"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S107EV0018"),
            [WorldRace.Human],
            [WorldRace.Human, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S107LO0014"))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S107EV0018:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0020"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventUnfogEffect(
                    new ScenarioLocationId("S107LO0016"),
                    Reveal: true,
                    Radius: null,
                    [new WorldPlayerId(1)])
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S139SC0000_ScenarioEvent_S139EV000c_Test()
    {
        ScenarioId scenarioId = new("S139SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S139EV000c"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S139EV000c"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S139LO0010"))],
            [
                new ScenarioEventUnfogEffect(
                    new ScenarioLocationId("S139LO0010"),
                    Reveal: true,
                    Radius: 0,
                    [new WorldPlayerId(5)]),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S139EV000c:POPUP_TXT:1"),
                    ScenarioEventPopupAudience.AllPlayers,
                    null,
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 9)));
    }

    [Test]
    public void Load_S139SC0000_ScenarioVariablesTest()
    {
        ScenarioId scenarioId = new("S139SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.Provider.Variables, Is.EqualTo([
            new ScenarioVariableDefinition(new ScenarioVariableId(1), "MOVE-WALLS", 1),
            new ScenarioVariableDefinition(new ScenarioVariableId(2), "WALL2", 0),
            new ScenarioVariableDefinition(new ScenarioVariableId(3), "WALL3", 0)
        ]));
    }

    [Test]
    public void Load_S139SC0000_GetScenarioVariableTest()
    {
        ScenarioId scenarioId = new("S139SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.Provider.GetVariable(new ScenarioVariableId(2)), Is.EqualTo(
            new ScenarioVariableDefinition(new ScenarioVariableId(2), "WALL2", 0)));
    }

    [Test]
    public void Load_S139SC0000_ScenarioEvent_S139EV004d_Test()
    {
        ScenarioId scenarioId = new("S139SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S139EV004d"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S139EV004d"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [
                new ScenarioEventVariableInRangeCondition(
                    new ScenarioVariableRange(new ScenarioVariableId(2), 1, 1),
                    new ScenarioVariableClause(
                        ScenarioVariableOperator.And,
                        new ScenarioVariableRange(new ScenarioVariableId(3), 1, 1)))
            ],
            [
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S139EV004c"), false),
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S139EV004e"), true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 72)));
    }

    [Test]
    public void Load_S140SC0000_ScenarioEvent_S140EV0016_Test()
    {
        ScenarioId scenarioId = new("S140SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S140EV0016"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S140EV0016"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [
                new ScenarioEventStackExistenceCondition(
                    new ScenarioStackReference.Stack(new WorldStackId("S140KC0024")),
                    MustExist: false),
                new ScenarioEventVariableInRangeCondition(
                    new ScenarioVariableRange(new ScenarioVariableId(4), 0, 0),
                    null)
            ],
            [
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S140EV0013"), true),
                new ScenarioEventModifyVariableEffect(
                    new ScenarioVariableId(4),
                    ScenarioVariableOperation.Replace,
                    1)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 22)));
    }

    [Test]
    public void Load_S142SC0000_ScenarioEvent_S142EV003c_Test()
    {
        ScenarioId scenarioId = new("S142SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S142EV003c"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S142EV003c"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [new ScenarioEventFrequencyCondition(1)],
            [
                new ScenarioEventModifyVariableEffect(
                    new ScenarioVariableId(1),
                    ScenarioVariableOperation.Add,
                    1)
            ],
            Enabled: false,
            OccurOnce: false,
            Chance: 100,
            Order: 57)));
    }

    [Test]
    public void Load_S095SC0000_LocationsTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.Provider.Locations, Is.EquivalentTo([
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0000"), new ScenarioTextId("S095LO0000:NAME_TXT:0"), new WorldPosition(17, 7), 2),
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0001"), new ScenarioTextId("S095LO0001:NAME_TXT:0"), new WorldPosition(13, 39), 0),
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0002"), new ScenarioTextId("S095LO0002:NAME_TXT:0"), new WorldPosition(28, 29), 1),
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0003"), new ScenarioTextId("S095LO0003:NAME_TXT:0"), new WorldPosition(25, 24), 1),
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0004"), new ScenarioTextId("S095LO0004:NAME_TXT:0"), new WorldPosition(27, 7), 2),
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0005"), new ScenarioTextId("S095LO0005:NAME_TXT:0"), new WorldPosition(43, 45), 2),
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0007"), new ScenarioTextId("S095LO0007:NAME_TXT:0"), new WorldPosition(19, 2), 1),
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0009"), new ScenarioTextId("S095LO0009:NAME_TXT:0"), new WorldPosition(34, 30), 2)
        ]));
    }

    [Test]
    public void Load_S095SC0000_GetLocationTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.Provider.GetLocation(new ScenarioLocationId("S095LO0004")), Is.EqualTo(
            new ScenarioLocationDefinition(new ScenarioLocationId("S095LO0004"), new ScenarioTextId("S095LO0004:NAME_TXT:0"), new WorldPosition(27, 7), 2)));
    }

    [Test]
    public void Load_S095SC0000_ScenarioMountainsTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);
        List<WorldMapCell> actualCells = [];

        for (int y = 34; y < 39; y++)
        {
            for (int x = 33; x < 38; x++)
            {
                actualCells.Add(scenario.State.Map.GetCellAt(new WorldPosition(x, y)));
            }
        }

        WorldMapCell expectedCell = new(WorldMapTerrain.Neutral, WorldMapFeature.Mountain, new WorldObjectVariant(0));

        Assert.That(actualCells, Is.EqualTo([
            expectedCell, expectedCell, expectedCell, expectedCell, expectedCell,
            expectedCell, expectedCell, expectedCell, expectedCell, expectedCell,
            expectedCell, expectedCell, expectedCell, expectedCell, expectedCell,
            expectedCell, expectedCell, expectedCell, expectedCell, expectedCell,
            expectedCell, expectedCell, expectedCell, expectedCell, expectedCell
        ]));
    }

    [Test]
    public void Load_S095SC0000_MountainsTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.State.Mountains.Values, Is.EqualTo([
            new WorldMountain(new WorldMountainId(3034), new WorldFootprint(new WorldPosition(33, 34), 5, 5), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3035), new WorldFootprint(new WorldPosition(0, 0), 3, 3), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3036), new WorldFootprint(new WorldPosition(43, 0), 3, 3), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3037), new WorldFootprint(new WorldPosition(0, 3), 3, 3), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3038), new WorldFootprint(new WorldPosition(31, 21), 3, 3), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3039), new WorldFootprint(new WorldPosition(24, 26), 3, 3), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3040), new WorldFootprint(new WorldPosition(36, 27), 3, 3), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3041), new WorldFootprint(new WorldPosition(41, 27), 3, 3), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3042), new WorldFootprint(new WorldPosition(44, 29), 3, 3), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3043), new WorldFootprint(new WorldPosition(29, 31), 3, 3), WorldRace.Neutral, new WorldObjectVariant(8)),
            new WorldMountain(new WorldMountainId(3044), new WorldFootprint(new WorldPosition(34, 39), 3, 3), WorldRace.Neutral, new WorldObjectVariant(8)),
            new WorldMountain(new WorldMountainId(3045), new WorldFootprint(new WorldPosition(16, 40), 3, 3), WorldRace.Neutral, new WorldObjectVariant(8)),
            new WorldMountain(new WorldMountainId(3046), new WorldFootprint(new WorldPosition(33, 42), 3, 3), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3047), new WorldFootprint(new WorldPosition(36, 42), 3, 3), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3048), new WorldFootprint(new WorldPosition(12, 43), 3, 3), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3049), new WorldFootprint(new WorldPosition(32, 45), 3, 3), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3050), new WorldFootprint(new WorldPosition(3, 0), 2, 2), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3051), new WorldFootprint(new WorldPosition(41, 0), 2, 2), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3052), new WorldFootprint(new WorldPosition(46, 2), 2, 2), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3053), new WorldFootprint(new WorldPosition(45, 4), 2, 2), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3054), new WorldFootprint(new WorldPosition(46, 6), 2, 2), WorldRace.Neutral, new WorldObjectVariant(6)),
            new WorldMountain(new WorldMountainId(3055), new WorldFootprint(new WorldPosition(0, 21), 2, 2), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3056), new WorldFootprint(new WorldPosition(34, 24), 2, 2), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3057), new WorldFootprint(new WorldPosition(34, 26), 2, 2), WorldRace.Neutral, new WorldObjectVariant(9)),
            new WorldMountain(new WorldMountainId(3058), new WorldFootprint(new WorldPosition(12, 27), 2, 2), WorldRace.Neutral, new WorldObjectVariant(6)),
            new WorldMountain(new WorldMountainId(3059), new WorldFootprint(new WorldPosition(44, 27), 2, 2), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3060), new WorldFootprint(new WorldPosition(6, 29), 2, 2), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3061), new WorldFootprint(new WorldPosition(39, 29), 2, 2), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3062), new WorldFootprint(new WorldPosition(32, 32), 2, 2), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3063), new WorldFootprint(new WorldPosition(45, 32), 2, 2), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3064), new WorldFootprint(new WorldPosition(31, 35), 2, 2), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3065), new WorldFootprint(new WorldPosition(19, 40), 2, 2), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3066), new WorldFootprint(new WorldPosition(35, 45), 2, 2), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3067), new WorldFootprint(new WorldPosition(22, 46), 2, 2), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3068), new WorldFootprint(new WorldPosition(5, 0), 1, 1), WorldRace.Neutral, new WorldObjectVariant(10)),
            new WorldMountain(new WorldMountainId(3069), new WorldFootprint(new WorldPosition(40, 0), 1, 1), WorldRace.Neutral, new WorldObjectVariant(6)),
            new WorldMountain(new WorldMountainId(3070), new WorldFootprint(new WorldPosition(46, 0), 1, 1), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3071), new WorldFootprint(new WorldPosition(5, 1), 1, 1), WorldRace.Neutral, new WorldObjectVariant(6)),
            new WorldMountain(new WorldMountainId(3072), new WorldFootprint(new WorldPosition(3, 2), 1, 1), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3073), new WorldFootprint(new WorldPosition(3, 3), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3074), new WorldFootprint(new WorldPosition(43, 3), 1, 1), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3075), new WorldFootprint(new WorldPosition(44, 3), 1, 1), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3076), new WorldFootprint(new WorldPosition(35, 5), 1, 1), WorldRace.Neutral, new WorldObjectVariant(6)),
            new WorldMountain(new WorldMountainId(3077), new WorldFootprint(new WorldPosition(36, 5), 1, 1), WorldRace.Neutral, new WorldObjectVariant(8)),
            new WorldMountain(new WorldMountainId(3078), new WorldFootprint(new WorldPosition(37, 5), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3079), new WorldFootprint(new WorldPosition(38, 5), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3080), new WorldFootprint(new WorldPosition(47, 5), 1, 1), WorldRace.Neutral, new WorldObjectVariant(6)),
            new WorldMountain(new WorldMountainId(3081), new WorldFootprint(new WorldPosition(0, 6), 1, 1), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3082), new WorldFootprint(new WorldPosition(1, 6), 1, 1), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3083), new WorldFootprint(new WorldPosition(35, 6), 1, 1), WorldRace.Neutral, new WorldObjectVariant(9)),
            new WorldMountain(new WorldMountainId(3084), new WorldFootprint(new WorldPosition(47, 8), 1, 1), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3085), new WorldFootprint(new WorldPosition(0, 18), 1, 1), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3086), new WorldFootprint(new WorldPosition(0, 19), 1, 1), WorldRace.Neutral, new WorldObjectVariant(9)),
            new WorldMountain(new WorldMountainId(3087), new WorldFootprint(new WorldPosition(0, 20), 1, 1), WorldRace.Neutral, new WorldObjectVariant(8)),
            new WorldMountain(new WorldMountainId(3088), new WorldFootprint(new WorldPosition(15, 21), 1, 1), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3089), new WorldFootprint(new WorldPosition(18, 21), 1, 1), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3090), new WorldFootprint(new WorldPosition(30, 21), 1, 1), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3091), new WorldFootprint(new WorldPosition(34, 22), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3092), new WorldFootprint(new WorldPosition(34, 23), 1, 1), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3093), new WorldFootprint(new WorldPosition(32, 24), 1, 1), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3094), new WorldFootprint(new WorldPosition(33, 24), 1, 1), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3095), new WorldFootprint(new WorldPosition(12, 26), 1, 1), WorldRace.Neutral, new WorldObjectVariant(10)),
            new WorldMountain(new WorldMountainId(3096), new WorldFootprint(new WorldPosition(36, 26), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3097), new WorldFootprint(new WorldPosition(6, 28), 1, 1), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3098), new WorldFootprint(new WorldPosition(40, 28), 1, 1), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3099), new WorldFootprint(new WorldPosition(47, 29), 1, 1), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3100), new WorldFootprint(new WorldPosition(8, 30), 1, 1), WorldRace.Neutral, new WorldObjectVariant(9)),
            new WorldMountain(new WorldMountainId(3101), new WorldFootprint(new WorldPosition(9, 30), 1, 1), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3102), new WorldFootprint(new WorldPosition(10, 30), 1, 1), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3103), new WorldFootprint(new WorldPosition(41, 30), 1, 1), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3104), new WorldFootprint(new WorldPosition(42, 30), 1, 1), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3105), new WorldFootprint(new WorldPosition(43, 30), 1, 1), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3106), new WorldFootprint(new WorldPosition(47, 30), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3107), new WorldFootprint(new WorldPosition(6, 31), 1, 1), WorldRace.Neutral, new WorldObjectVariant(9)),
            new WorldMountain(new WorldMountainId(3108), new WorldFootprint(new WorldPosition(10, 31), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3109), new WorldFootprint(new WorldPosition(32, 31), 1, 1), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3110), new WorldFootprint(new WorldPosition(39, 31), 1, 1), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3111), new WorldFootprint(new WorldPosition(40, 31), 1, 1), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3112), new WorldFootprint(new WorldPosition(41, 31), 1, 1), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3113), new WorldFootprint(new WorldPosition(34, 32), 1, 1), WorldRace.Neutral, new WorldObjectVariant(8)),
            new WorldMountain(new WorldMountainId(3114), new WorldFootprint(new WorldPosition(44, 32), 1, 1), WorldRace.Neutral, new WorldObjectVariant(8)),
            new WorldMountain(new WorldMountainId(3115), new WorldFootprint(new WorldPosition(47, 32), 1, 1), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3116), new WorldFootprint(new WorldPosition(34, 33), 1, 1), WorldRace.Neutral, new WorldObjectVariant(10)),
            new WorldMountain(new WorldMountainId(3117), new WorldFootprint(new WorldPosition(35, 33), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3118), new WorldFootprint(new WorldPosition(47, 33), 1, 1), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3119), new WorldFootprint(new WorldPosition(32, 34), 1, 1), WorldRace.Neutral, new WorldObjectVariant(10)),
            new WorldMountain(new WorldMountainId(3120), new WorldFootprint(new WorldPosition(31, 37), 1, 1), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3121), new WorldFootprint(new WorldPosition(32, 37), 1, 1), WorldRace.Neutral, new WorldObjectVariant(4)),
            new WorldMountain(new WorldMountainId(3122), new WorldFootprint(new WorldPosition(0, 38), 1, 1), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3123), new WorldFootprint(new WorldPosition(1, 38), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3124), new WorldFootprint(new WorldPosition(0, 39), 1, 1), WorldRace.Neutral, new WorldObjectVariant(10)),
            new WorldMountain(new WorldMountainId(3125), new WorldFootprint(new WorldPosition(37, 39), 1, 1), WorldRace.Neutral, new WorldObjectVariant(10)),
            new WorldMountain(new WorldMountainId(3126), new WorldFootprint(new WorldPosition(15, 40), 1, 1), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3127), new WorldFootprint(new WorldPosition(15, 41), 1, 1), WorldRace.Neutral, new WorldObjectVariant(6)),
            new WorldMountain(new WorldMountainId(3128), new WorldFootprint(new WorldPosition(13, 42), 1, 1), WorldRace.Neutral, new WorldObjectVariant(7)),
            new WorldMountain(new WorldMountainId(3129), new WorldFootprint(new WorldPosition(19, 42), 1, 1), WorldRace.Neutral, new WorldObjectVariant(10)),
            new WorldMountain(new WorldMountainId(3130), new WorldFootprint(new WorldPosition(20, 42), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3131), new WorldFootprint(new WorldPosition(16, 43), 1, 1), WorldRace.Neutral, new WorldObjectVariant(9)),
            new WorldMountain(new WorldMountainId(3132), new WorldFootprint(new WorldPosition(17, 43), 1, 1), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3134), new WorldFootprint(new WorldPosition(19, 43), 1, 1), WorldRace.Neutral, new WorldObjectVariant(1)),
            new WorldMountain(new WorldMountainId(3135), new WorldFootprint(new WorldPosition(37, 45), 1, 1), WorldRace.Neutral, new WorldObjectVariant(3)),
            new WorldMountain(new WorldMountainId(3136), new WorldFootprint(new WorldPosition(12, 46), 1, 1), WorldRace.Neutral, new WorldObjectVariant(2)),
            new WorldMountain(new WorldMountainId(3137), new WorldFootprint(new WorldPosition(13, 46), 1, 1), WorldRace.Neutral, new WorldObjectVariant(6)),
            new WorldMountain(new WorldMountainId(3138), new WorldFootprint(new WorldPosition(12, 47), 1, 1), WorldRace.Neutral, new WorldObjectVariant(9)),
            new WorldMountain(new WorldMountainId(3139), new WorldFootprint(new WorldPosition(35, 47), 1, 1), WorldRace.Neutral, new WorldObjectVariant(5)),
            new WorldMountain(new WorldMountainId(3140), new WorldFootprint(new WorldPosition(36, 47), 1, 1), WorldRace.Neutral, new WorldObjectVariant(0)),
            new WorldMountain(new WorldMountainId(3141), new WorldFootprint(new WorldPosition(27, 27), 1, 1), WorldRace.Neutral, new WorldObjectVariant(3))
        ]));
    }

    [Test]
    public void Load_S095SC0000_LandmarksTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.State.Landmarks.Values, Is.EqualTo([
            new WorldLandmark(new WorldLandmarkId("S095MM0000"), new WorldLandmarkDefinitionId("G000MG0081"), new WorldFootprint(new WorldPosition(10, 11), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0001"), new WorldLandmarkDefinitionId("G000MG0082"), new WorldFootprint(new WorldPosition(10, 18), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0002"), new WorldLandmarkDefinitionId("G000MG0007"), new WorldFootprint(new WorldPosition(21, 21), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0003"), new WorldLandmarkDefinitionId("G000MG0079"), new WorldFootprint(new WorldPosition(24, 15), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0004"), new WorldLandmarkDefinitionId("G000MG0008"), new WorldFootprint(new WorldPosition(24, 21), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0005"), new WorldLandmarkDefinitionId("G000MG0009"), new WorldFootprint(new WorldPosition(7, 11), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0006"), new WorldLandmarkDefinitionId("G000MG0010"), new WorldFootprint(new WorldPosition(13, 11), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM000b"), new WorldLandmarkDefinitionId("G000MG0058"), new WorldFootprint(new WorldPosition(27, 0), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM000c"), new WorldLandmarkDefinitionId("G000MG0058"), new WorldFootprint(new WorldPosition(16, 0), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM000d"), new WorldLandmarkDefinitionId("G000MG0013"), new WorldFootprint(new WorldPosition(24, 18), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0012"), new WorldLandmarkDefinitionId("G000MG0011"), new WorldFootprint(new WorldPosition(3, 11), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM001a"), new WorldLandmarkDefinitionId("G000MG0010"), new WorldFootprint(new WorldPosition(13, 18), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM001b"), new WorldLandmarkDefinitionId("G000MG0020"), new WorldFootprint(new WorldPosition(19, 21), 2, 2)),
            new WorldLandmark(new WorldLandmarkId("S095MM001e"), new WorldLandmarkDefinitionId("G000MG0011"), new WorldFootprint(new WorldPosition(16, 18), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM001f"), new WorldLandmarkDefinitionId("G000MG0008"), new WorldFootprint(new WorldPosition(16, 11), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0020"), new WorldLandmarkDefinitionId("G000MG0061"), new WorldFootprint(new WorldPosition(0, 11), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0021"), new WorldLandmarkDefinitionId("G000MG0014"), new WorldFootprint(new WorldPosition(16, 8), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0022"), new WorldLandmarkDefinitionId("G000MG0012"), new WorldFootprint(new WorldPosition(16, 3), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0023"), new WorldLandmarkDefinitionId("G000MG0060"), new WorldFootprint(new WorldPosition(24, 12), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0024"), new WorldLandmarkDefinitionId("G000MG0008"), new WorldFootprint(new WorldPosition(27, 12), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0027"), new WorldLandmarkDefinitionId("G000MG0010"), new WorldFootprint(new WorldPosition(7, 18), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0028"), new WorldLandmarkDefinitionId("G000MG0010"), new WorldFootprint(new WorldPosition(4, 18), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0029"), new WorldLandmarkDefinitionId("G000MG0010"), new WorldFootprint(new WorldPosition(1, 18), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM002b"), new WorldLandmarkDefinitionId("G000MG0012"), new WorldFootprint(new WorldPosition(27, 3), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM002c"), new WorldLandmarkDefinitionId("G000MG0014"), new WorldFootprint(new WorldPosition(27, 9), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM002d"), new WorldLandmarkDefinitionId("G000MG0052"), new WorldFootprint(new WorldPosition(38, 4), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM002e"), new WorldLandmarkDefinitionId("G000MG0024"), new WorldFootprint(new WorldPosition(34, 7), 2, 2)),
            new WorldLandmark(new WorldLandmarkId("S095MM002f"), new WorldLandmarkDefinitionId("G000MG0023"), new WorldFootprint(new WorldPosition(33, 8), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0030"), new WorldLandmarkDefinitionId("G000MG0053"), new WorldFootprint(new WorldPosition(39, 5), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0031"), new WorldLandmarkDefinitionId("G000MG0054"), new WorldFootprint(new WorldPosition(44, 8), 2, 2)),
            new WorldLandmark(new WorldLandmarkId("S095MM0032"), new WorldLandmarkDefinitionId("G000MG0022"), new WorldFootprint(new WorldPosition(39, 14), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0033"), new WorldLandmarkDefinitionId("G000MG0018"), new WorldFootprint(new WorldPosition(19, 20), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0034"), new WorldLandmarkDefinitionId("G000MG0109"), new WorldFootprint(new WorldPosition(34, 11), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0035"), new WorldLandmarkDefinitionId("G000MG0107"), new WorldFootprint(new WorldPosition(35, 11), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0036"), new WorldLandmarkDefinitionId("G000MG0108"), new WorldFootprint(new WorldPosition(39, 20), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0037"), new WorldLandmarkDefinitionId("G000MG0109"), new WorldFootprint(new WorldPosition(40, 20), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0038"), new WorldLandmarkDefinitionId("G000MG0107"), new WorldFootprint(new WorldPosition(40, 21), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0039"), new WorldLandmarkDefinitionId("G000MG0110"), new WorldFootprint(new WorldPosition(35, 0), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM003a"), new WorldLandmarkDefinitionId("G000MG0108"), new WorldFootprint(new WorldPosition(46, 9), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM003b"), new WorldLandmarkDefinitionId("G000MG0107"), new WorldFootprint(new WorldPosition(46, 10), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM003c"), new WorldLandmarkDefinitionId("G000MG0108"), new WorldFootprint(new WorldPosition(38, 11), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM003d"), new WorldLandmarkDefinitionId("G000MG0107"), new WorldFootprint(new WorldPosition(41, 9), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM003e"), new WorldLandmarkDefinitionId("G000MG0099"), new WorldFootprint(new WorldPosition(10, 38), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM003f"), new WorldLandmarkDefinitionId("G000MG0100"), new WorldFootprint(new WorldPosition(14, 35), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0040"), new WorldLandmarkDefinitionId("G000MG0101"), new WorldFootprint(new WorldPosition(14, 36), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0041"), new WorldLandmarkDefinitionId("G000MG0102"), new WorldFootprint(new WorldPosition(14, 34), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0042"), new WorldLandmarkDefinitionId("G000MG0120"), new WorldFootprint(new WorldPosition(44, 34), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0043"), new WorldLandmarkDefinitionId("G000MG0121"), new WorldFootprint(new WorldPosition(38, 34), 3, 3)),
            new WorldLandmark(new WorldLandmarkId("S095MM0044"), new WorldLandmarkDefinitionId("G000MG0092"), new WorldFootprint(new WorldPosition(8, 29), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0045"), new WorldLandmarkDefinitionId("G000MG0093"), new WorldFootprint(new WorldPosition(6, 26), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0046"), new WorldLandmarkDefinitionId("G000MG0118"), new WorldFootprint(new WorldPosition(16, 21), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0047"), new WorldLandmarkDefinitionId("G000MG0095"), new WorldFootprint(new WorldPosition(12, 5), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0048"), new WorldLandmarkDefinitionId("G000MG0093"), new WorldFootprint(new WorldPosition(7, 8), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0049"), new WorldLandmarkDefinitionId("G000MG0094"), new WorldFootprint(new WorldPosition(7, 9), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM004a"), new WorldLandmarkDefinitionId("G000MG0096"), new WorldFootprint(new WorldPosition(8, 8), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM004b"), new WorldLandmarkDefinitionId("G000MG0092"), new WorldFootprint(new WorldPosition(7, 15), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM004c"), new WorldLandmarkDefinitionId("G000MG0095"), new WorldFootprint(new WorldPosition(8, 15), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM004d"), new WorldLandmarkDefinitionId("G000MG0118"), new WorldFootprint(new WorldPosition(7, 14), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM004e"), new WorldLandmarkDefinitionId("G000MG0117"), new WorldFootprint(new WorldPosition(5, 6), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM004f"), new WorldLandmarkDefinitionId("G000MG0095"), new WorldFootprint(new WorldPosition(26, 38), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0050"), new WorldLandmarkDefinitionId("G000MG0096"), new WorldFootprint(new WorldPosition(28, 35), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0052"), new WorldLandmarkDefinitionId("G000MG0096"), new WorldFootprint(new WorldPosition(28, 40), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0053"), new WorldLandmarkDefinitionId("G000MG0092"), new WorldFootprint(new WorldPosition(35, 22), 1, 1)),
            new WorldLandmark(new WorldLandmarkId("S095MM0054"), new WorldLandmarkDefinitionId("G000MG0091"), new WorldFootprint(new WorldPosition(42, 26), 1, 1))
        ]));
    }

    [Test]
    public void Load_S095SC0000_CrystalsTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.State.ResourceNodes.Values, Is.EqualTo([
            new WorldResourceNode(new WorldResourceNodeId("S095CR0000"), new WorldPosition(7, 36), WorldResourceType.Gold, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0001"), new WorldPosition(32, 13), WorldResourceType.Gold, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0002"), new WorldPosition(42, 5), WorldResourceType.Gold, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0003"), new WorldPosition(28, 25), WorldResourceType.Gold, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0004"), new WorldPosition(19, 45), WorldResourceType.LifeMana, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0005"), new WorldPosition(42, 24), WorldResourceType.LifeMana, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0006"), new WorldPosition(37, 2), WorldResourceType.InfernalMana, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0007"), new WorldPosition(19, 24), WorldResourceType.InfernalMana, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0008"), new WorldPosition(41, 44), WorldResourceType.RuneMana, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR0009"), new WorldPosition(25, 10), WorldResourceType.DeathMana, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR000a"), new WorldPosition(30, 43), WorldResourceType.Gold, AiPriority: 3),
            new WorldResourceNode(new WorldResourceNodeId("S095CR000b"), new WorldPosition(26, 34), WorldResourceType.LifeMana, AiPriority: 3)
        ]));
    }

    [Test]
    public void Load_S095SC0000_RuinTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u5013 = new("G000UU5013");
        UnitDefinitionId u5017 = new("G000UU5017");
        UnitDefinitionId u5018 = new("G000UU5018");

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S095UN0069"), u5018, 40, 0, 1, [], null, false), GridPosition.BackBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN0068"), u5013, 200, 0, 1, [], null, false), GridPosition.FrontCenter, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN006b"), u5017, 50, 0, 1, [], null, false), GridPosition.FrontTop, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN006a"), u5018, 40, 0, 1, [], null, false), GridPosition.BackTop, false)
            .Build();

        Assert.That(scenario.State.Fortifications[new WorldFortificationId("S095RU0000")], Is.EqualTo(new WorldRuin(
            new WorldFortificationId("S095RU0000"),
            new ScenarioTextId("S095RU0000:TITLE:0"),
            new WorldFootprint(new WorldPosition(3, 41), 3, 3),
            new WorldObjectVariant(1),
            expectedGarrison,
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            new ItemDefinitionId("G000IG7006"),
            null,
            AiPriority: 3)));
    }

    [Test]
    public void Load_S095SC0000_RuinWithoutItemTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u5015 = new("G000UU5015");
        UnitDefinitionId u5017 = new("G000UU5017");

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S095UN006e"), u5017, 50, 0, 1, [], null, false), GridPosition.FrontBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S095UN006c"), u5015, 300, 0, 1, [], null, false), GridPosition.FrontCenter, true)
            .Place(new WorldUnit(new WorldUnitId("S095UN006d"), u5017, 50, 0, 1, [], null, false), GridPosition.FrontTop, false)
            .Build();

        Assert.That(scenario.State.Fortifications[new WorldFortificationId("S095RU0001")], Is.EqualTo(new WorldRuin(
            new WorldFortificationId("S095RU0001"),
            new ScenarioTextId("S095RU0001:TITLE:0"),
            new WorldFootprint(new WorldPosition(24, 30), 3, 3),
            new WorldObjectVariant(2),
            expectedGarrison,
            new WorldResourceSet(100, 0, 0, 0, 0, 0),
            null,
            null,
            AiPriority: 3)));
    }

    [Test]
    public void Load_S095SC0000_BagTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.State.Bags.Values, Is.EqualTo([
            new WorldBag(new WorldBagId("S095BG0000"), new WorldPosition(38, 41), new WorldObjectVariant(1), new WorldInventory([new WorldItem(new WorldItemId("S095IM0009"), new ItemDefinitionId("G000IG1004"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0001"), new WorldPosition(5, 14), new WorldObjectVariant(0), new WorldInventory([new WorldItem(new WorldItemId("S095IM000B"), new ItemDefinitionId("G000IG5036"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0002"), new WorldPosition(1, 31), new WorldObjectVariant(3), new WorldInventory([new WorldItem(new WorldItemId("S095IM000D"), new ItemDefinitionId("G000IG0001"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0003"), new WorldPosition(18, 33), new WorldObjectVariant(0), new WorldInventory([new WorldItem(new WorldItemId("S095IM0011"), new ItemDefinitionId("G000IG0008"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0004"), new WorldPosition(17, 45), new WorldObjectVariant(3), new WorldInventory([new WorldItem(new WorldItemId("S095IM0010"), new ItemDefinitionId("G000IG0006"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0005"), new WorldPosition(2, 21), new WorldObjectVariant(0), new WorldInventory([new WorldItem(new WorldItemId("S095IM0013"), new ItemDefinitionId("G000IG9018"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0006"), new WorldPosition(42, 2), new WorldObjectVariant(0), new WorldInventory([new WorldItem(new WorldItemId("S095IM0014"), new ItemDefinitionId("G000IG0005"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0007"), new WorldPosition(39, 27), new WorldObjectVariant(3), new WorldInventory([new WorldItem(new WorldItemId("S095IM0016"), new ItemDefinitionId("G000IG0015"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0008"), new WorldPosition(30, 4), new WorldObjectVariant(1), new WorldInventory([new WorldItem(new WorldItemId("S095IM0018"), new ItemDefinitionId("G000IG0001"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG0009"), new WorldPosition(31, 25), new WorldObjectVariant(2), new WorldInventory([new WorldItem(new WorldItemId("S095IM001A"), new ItemDefinitionId("G000IG5072"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG000a"), new WorldPosition(32, 41), new WorldObjectVariant(3), new WorldInventory([new WorldItem(new WorldItemId("S095IM001C"), new ItemDefinitionId("G000IG9024"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG000b"), new WorldPosition(15, 3), new WorldObjectVariant(3), new WorldInventory([new WorldItem(new WorldItemId("S095IM001E"), new ItemDefinitionId("G000IG0010"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG000c"), new WorldPosition(21, 15), new WorldObjectVariant(2), new WorldInventory([new WorldItem(new WorldItemId("S095IM0020"), new ItemDefinitionId("G000IG5052"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG000d"), new WorldPosition(1, 42), new WorldObjectVariant(1), new WorldInventory([new WorldItem(new WorldItemId("S095IM0022"), new ItemDefinitionId("G000IG0022"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG000e"), new WorldPosition(17, 21), new WorldObjectVariant(0), new WorldInventory([new WorldItem(new WorldItemId("S095IM0024"), new ItemDefinitionId("G000IG7004"), Charges: null)]), AiPriority: 3),
            new WorldBag(new WorldBagId("S095BG000f"), new WorldPosition(30, 11), new WorldObjectVariant(0), new WorldInventory([new WorldItem(new WorldItemId("S095IM0026"), new ItemDefinitionId("G000IG7003"), Charges: null)]), AiPriority: 3)
        ]));
    }

    [Test]
    public void Load_S095SC0000_SitesTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        WorldSite[] expectedSites =
        [
            new WorldMerchantSite(
                new WorldSiteId("S095SI0000"),
                new WorldFootprint(new WorldPosition(15, 22), 3, 3),
                Variant: new WorldObjectVariant(7),
                TitleTextId: new ScenarioTextId("S095SI0000:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S095SI0000:TXT_DESC:0"),
                [
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0001"), 5),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0002"), 2),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0005"), 10),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0006"), 10),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0014"), 2),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0018"), 3)
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldMerchantSite(
                new WorldSiteId("S095SI0001"),
                new WorldFootprint(new WorldPosition(30, 1), 3, 3),
                Variant: new WorldObjectVariant(0),
                TitleTextId: new ScenarioTextId("S095SI0001:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S095SI0001:TXT_DESC:0"),
                [
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0001"), 5),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0005"), 10),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0006"), 10),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0018"), 3),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG3003"), 1)
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldMageSite(
                new WorldSiteId("S095SI0002"),
                new WorldFootprint(new WorldPosition(41, 15), 3, 3),
                Variant: new WorldObjectVariant(0),
                TitleTextId: new ScenarioTextId("S095SI0002:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S095SI0002:TXT_DESC:0"),
                [
                    new WorldSpellDefinitionId("G000SS0050"),
                    new WorldSpellDefinitionId("G000SS0049")
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldMageSite(
                new WorldSiteId("S095SI0003"),
                new WorldFootprint(new WorldPosition(2, 14), 3, 3),
                Variant: new WorldObjectVariant(2),
                TitleTextId: new ScenarioTextId("S095SI0003:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S095SI0003:TXT_DESC:0"),
                [
                    new WorldSpellDefinitionId("G000SS0034"),
                    new WorldSpellDefinitionId("G000SS0023"),
                    new WorldSpellDefinitionId("G000SS0033")
                ],
                VisitorIds: [],
                AiPriority: 3)
        ];

        Assert.That(scenario.State.Sites.Values, Is.EqualTo(expectedSites));
    }

    [Test]
    public void Load_S112SC0000_SitesTest()
    {
        ScenarioId scenarioId = new("S112SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        WorldSite[] expectedSites =
        [
            new WorldMageSite(
                new WorldSiteId("S112SI0000"),
                new WorldFootprint(new WorldPosition(37, 1), 3, 3),
                Variant: new WorldObjectVariant(2),
                TitleTextId: new ScenarioTextId("S112SI0000:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S112SI0000:TXT_DESC:0"),
                [
                    new WorldSpellDefinitionId("G000SS0023"),
                    new WorldSpellDefinitionId("G000SS0026"),
                    new WorldSpellDefinitionId("G000SS0034"),
                    new WorldSpellDefinitionId("G000SS0085")
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldMerchantSite(
                new WorldSiteId("S112SI0002"),
                new WorldFootprint(new WorldPosition(28, 49), 3, 3),
                Variant: new WorldObjectVariant(4),
                TitleTextId: new ScenarioTextId("S112SI0002:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S112SI0002:TXT_DESC:0"),
                [
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0001"), 5),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0005"), 10),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0006"), 10),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0016"), 1)
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldMerchantSite(
                new WorldSiteId("S112SI0003"),
                new WorldFootprint(new WorldPosition(66, 48), 3, 3),
                Variant: new WorldObjectVariant(4),
                TitleTextId: new ScenarioTextId("S112SI0003:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S112SI0003:TXT_DESC:0"),
                [
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0001"), 5),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0005"), 10),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0006"), 10),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG0017"), 2)
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldMerchantSite(
                new WorldSiteId("S112SI0004"),
                new WorldFootprint(new WorldPosition(53, 53), 3, 3),
                Variant: new WorldObjectVariant(5),
                TitleTextId: new ScenarioTextId("S112SI0004:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S112SI0004:TXT_DESC:0"),
                [
                    new WorldMerchantItem(new ItemDefinitionId("G000IG9016"), 2),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG9017"), 3),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG9018"), 2),
                    new WorldMerchantItem(new ItemDefinitionId("G000IG9019"), 4)
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldMageSite(
                new WorldSiteId("S112SI0006"),
                new WorldFootprint(new WorldPosition(57, 46), 3, 3),
                Variant: new WorldObjectVariant(0),
                TitleTextId: new ScenarioTextId("S112SI0006:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S112SI0006:TXT_DESC:0"),
                [
                    new WorldSpellDefinitionId("G000SS0006"),
                    new WorldSpellDefinitionId("G000SS0017"),
                    new WorldSpellDefinitionId("G000SS0002")
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldTrainerSite(
                new WorldSiteId("S112SI0007"),
                new WorldFootprint(new WorldPosition(45, 61), 3, 3),
                Variant: new WorldObjectVariant(2),
                TitleTextId: new ScenarioTextId("S112SI0007:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S112SI0007:TXT_DESC:0"),
                VisitorIds: [],
                AiPriority: 3),
            new WorldMercenarySite(
                new WorldSiteId("S112SI0008"),
                new WorldFootprint(new WorldPosition(67, 11), 3, 3),
                Variant: new WorldObjectVariant(1),
                TitleTextId: new ScenarioTextId("S112SI0008:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S112SI0008:TXT_DESC:0"),
                [
                    new WorldMercenaryUnit(new UnitDefinitionId("G000UU5024"), 1, false),
                    new WorldMercenaryUnit(new UnitDefinitionId("G000UU5025"), 1, false)
                ],
                VisitorIds: [],
                AiPriority: 3),
            new WorldMercenarySite(
                new WorldSiteId("S112SI0009"),
                new WorldFootprint(new WorldPosition(24, 60), 3, 3),
                Variant: new WorldObjectVariant(0),
                TitleTextId: new ScenarioTextId("S112SI0009:TXT_TITLE:0"),
                GreetingTextId: new ScenarioTextId("S112SI0009:TXT_DESC:0"),
                [
                    new WorldMercenaryUnit(new UnitDefinitionId("G000UU5031"), 1, false),
                    new WorldMercenaryUnit(new UnitDefinitionId("G000UU5030"), 1, false)
                ],
                VisitorIds: [],
                AiPriority: 3)
        ];

        Assert.That(scenario.State.Sites.Values, Is.EqualTo(expectedSites));
    }

    [Test]
    public void Load_S112SC0000_SiteTextsTest()
    {
        ScenarioId scenarioId = new("S112SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);
        GameTextProvider texts = new(globalDb, scenarioDb, scenarioDb.TextScenarios);

        List<string> rows = [];

        foreach (WorldSite site in scenario.State.Sites.Values)
        {
            rows.Add($"{site.Id} {texts.GetScenarioText(site.TitleTextId)}: {texts.GetScenarioText(site.GreetingTextId)}");
        }

        Assert.That(rows, Is.EqualTo([
            "S112SI0000 Malavien's Tower: I think my magical library can be of some help to you.",
            "S112SI0002 Sallat's Shop: Take a look at my wares, I may have something of interest to you.",
            "S112SI0003 Kilgen's Shop: Everything I possess is for sale.",
            "S112SI0004 Thurin's Shop: My wares can help you in your quest.",
            "S112SI0006 Nibor's Tower: My spells are the most potent in the region.",
            "S112SI0007 Forcecus's Camp: You'll be cutting a swathe through the land in no time.",
            "S112SI0008 Turion's Camp: My lizards are the finest in the region.",
            "S112SI0009 Turion's Camp: My Thugs will join you, for a price..."
        ]));
    }

    [Test]
    public void Load_S095SC0000_CityStackTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        WorldStack stack = scenario.State.Stacks[new WorldStackId("S095KC0000")];

        WorldUnit expectedUnit = new(new WorldUnitId("S095UN0001"), new UnitDefinitionId("G000UU0019"), 150, 0, 1, [], Name: "Ardobert", IsTypeLocked: false);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(expectedUnit, new GridPosition(0, 1), false)
            .Build();

        Assert.That(stack, Is.EqualTo(new WorldStack(
            new WorldStackId("S095KC0000"),
            new WorldPlayerId(2),
            new WorldPosition(13, 37),
            WorldStackDirection.SouthWest,
            expectedUnits,
            20,
            WorldInventory.Empty,
            LeaderEquipment.Empty,
            new WorldSubraceId("S095SR0009"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            Order: WorldStackOrder.Normal.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null)));

        Assert.That(scenario.State.Subraces[new WorldSubraceId("S095SR0009")], Is.EqualTo(new WorldSubrace(
            new WorldSubraceId("S095SR0009"),
            new WorldPlayerId(2),
            WorldSubraceType.Human,
            BannerVariant: new WorldObjectVariant(0))));
    }

    [Test]
    public void Load_S107SC0000_CityDefenderStackTest()
    {
        ScenarioId scenarioId = new("S107SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S107UN005b"), new UnitDefinitionId("G000UU5017"), 50, 0, 1, [], null, false), GridPosition.FrontBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S107UN005e"), new UnitDefinitionId("G000UU5018"), 40, 0, 1, [], null, false), GridPosition.FrontCenter, false)
            .Place(new WorldUnit(new WorldUnitId("S107UN005c"), new UnitDefinitionId("G000UU5017"), 50, 0, 1, [], null, false), GridPosition.FrontTop, false)
            .Build();

        Assert.That(scenario.State.Fortifications[new WorldFortificationId("S107FT0003")], Is.EqualTo(new WorldCity(
            new WorldFortificationId("S107FT0003"),
            new ScenarioTextId("S107FT0003:NAME_TXT:0"),
            new WorldCityDefinitionId(WorldRace.Neutral, 2),
            new WorldPlayerId(3),
            new WorldSubraceId("S107SR0002"),
            new WorldFootprint(new WorldPosition(5, 24), 4, 4),
            expectedUnits,
            WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0)));
    }

    [Test]
    public void Load_S107SC0000_CapitalDefenderStackTest()
    {
        ScenarioId scenarioId = new("S107SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S107UN0053"), new UnitDefinitionId("G000UU0001"), 100, 0, 1, [], null, false), GridPosition.FrontBottom, false)
            .Place(new WorldUnit(new WorldUnitId("S107UN0000"), new UnitDefinitionId("G000UU3001"), 900, 0, 1, [], null, false), GridPosition.FrontCenter, false)
            .Place(new WorldUnit(new WorldUnitId("S107UN0054"), new UnitDefinitionId("G000UU0001"), 100, 0, 1, [], null, false), GridPosition.FrontTop, false)
            .Place(new WorldUnit(new WorldUnitId("S107UN0055"), new UnitDefinitionId("G000UU0008"), 35, 0, 1, [], null, false), GridPosition.BackCenter, false)
            .Place(new WorldUnit(new WorldUnitId("S107UN0056"), new UnitDefinitionId("G000UU0011"), 50, 0, 1, [], null, false), GridPosition.BackTop, false)
            .Build();

        Assert.That(scenario.State.Fortifications[new WorldFortificationId("S107FT0000")], Is.EqualTo(new WorldCapital(
            new WorldFortificationId("S107FT0000"),
            new ScenarioTextId("S107FT0000:NAME_TXT:0"),
            new WorldPlayerId(1),
            new WorldFootprint(new WorldPosition(6, 10), 5, 5),
            expectedUnits,
            new WorldInventory([
                new WorldItem(new WorldItemId("S107IM0000"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S107IM0001"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S107IM0002"), new ItemDefinitionId("G000IG0006"), Charges: null),
                new WorldItem(new WorldItemId("S107IM0030"), new ItemDefinitionId("G000IG0001"), Charges: null)
            ]),
            AiPriority: 3,
            CapturedById: null)));
    }

    [Test]
    public void Load_S107SC0000_TurnInfoTest()
    {
        ScenarioId scenarioId = new("S107SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);
        Assert.That(scenario.State.TurnInfo, Is.EqualTo(new WorldTurnInfo(1, new WorldPlayerId(1))));
    }

    [Test]
    public void Load_S107SC0000_PlayerProgressionTest()
    {
        ScenarioId scenarioId = new("S107SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.State.Players[new WorldPlayerId(1)], Is.EqualTo(new WorldPlayer(
            new WorldPlayerId(1),
            new ScenarioTextId("S107PL0000:NAME_TXT:0"),
            new LordDefinitionId("G000LR0001"),
            WorldRace.Human,
            new WorldObjectVariant(3),
            WorldAiAttitudeType.Medium,
            IsAi: false,
            new WorldResourceSet(300, 0, 0, 0, 0, 0),
            new WorldPlayerBuildings(
                [
                    new WorldPlayerBuildingDefinitionId("G000BB0001"),
                    new WorldPlayerBuildingDefinitionId("G000BB0007"),
                    new WorldPlayerBuildingDefinitionId("G000BB0010"),
                    new WorldPlayerBuildingDefinitionId("G000BB0012"),
                    new WorldPlayerBuildingDefinitionId("G000BB0016"),
                    new WorldPlayerBuildingDefinitionId("G000BB0019"),
                    new WorldPlayerBuildingDefinitionId("G000BB0023"),
                    new WorldPlayerBuildingDefinitionId("G000BB0024"),
                    new WorldPlayerBuildingDefinitionId("G000BB0025")
                ],
                []),
            new WorldPlayerSpellbook(
                [
                    new WorldSpellDefinitionId("G000SS0001"),
                    new WorldSpellDefinitionId("G000SS0002"),
                    new WorldSpellDefinitionId("G000SS0003"),
                    new WorldSpellDefinitionId("G000SS0004"),
                    new WorldSpellDefinitionId("G000SS0005"),
                    new WorldSpellDefinitionId("G000SS0006"),
                    new WorldSpellDefinitionId("G000SS0007"),
                    new WorldSpellDefinitionId("G000SS0008"),
                    new WorldSpellDefinitionId("G000SS0009"),
                    new WorldSpellDefinitionId("G000SS0010")
                ],
                []),
            ImmutableBitmap.Create(48, 48),
            [],
            [],
            [],
            new WorldPlayerTurnActions())));
    }

    [Test]
    public void Load_S095SC0000_MapTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(TestHelpers.SerializeMap(scenario.State.Map), Is.EqualTo([
            "^^^^^^+++~~~++++^^^D++~~~.+^^^+...+.++..^^^^^^^.",
            "^^^^^^++D+~~++D+^^^D++~~~++^^^....++RRRR.^^^^^.+",
            "^^^^++DDDD~~~~+D^^^DD~~~~+.^^^.....RRRR....^^^^^",
            "^^^^+DDDDD++~~~D^^^+~~~....^^^...++RRRRR+++^^.^^",
            "^^^++DDDDD+==~~~^^^D~~~...+^^^....R+RRRRRRRR.^^+",
            "^^^++DDDDD+=D~~~^^^+D~~....^^^+...R^^^^RRRRR+^^^",
            "^^DDDD======~~~+DDDDD~~~++....+..RR^RRRRRRRR+.^^",
            "++DDD+=++++~~~~DDDDDDD~~+..+..+..R.RRRRRR+RR+.^^",
            "++DDD+=DD~~~~~+D^^^DDD~~~+.......RRRRRRRR++RRRR^",
            "DDD+D+DD+~~~~+D+^^^D+D~~...^^^..RR+RRRRRRRRRRRR.",
            "++D+DDD+++~~D++D^^^+D~~~...^^^+...++RRRRRR+RRRRR",
            "^^^^^^D^^^^~^^^^^^^+~~~....^^^++...R++R+RRRRRRRR",
            "^^^^^^D^^^^~^^^^^^^D~~~.^^^^^^+...R+RR+RRRRR+RR.",
            "^^^^^^=^^^^~~^^^^^^D~~+.^^^^^^......RR+RRRRRRR+.",
            "++DDDD=D+++~~+~~~++~~~~+^^^^^^+.....R..RRRRRR+..",
            "+DDDD+=DD+~~~~~~~~~~~D~~~^^+.++.+~~+++...RRRR+R.",
            "D+DDD====+~~~~~+~~~~++~~~^~~~...~~~~~+~~~.RRR.R.",
            "DDD+DD+++D~~DD+DDD+~~~~+^^~~~~..~~~~~~~~~.R.+...",
            "^^^^^^^^^^^~^^^^^^^+~~~D^^^.~~~~~~~.~~~+~~~~+...",
            "^^^^^^^^^^^~^^^^^^^+~~~~^^^+.~~+~~+++.+++~~~~..+",
            "^^^^^^^^^^^~~^^^^^^^D~~D^^^...~~~++.+.+RRRR~~...",
            "^^DD+D~~~~+~~~+^DD^^^~~^^^^+..^^^^+....R.+.~~~..",
            "^^++~~~~~~~~~~DDDD+^^^^^^^^++.+^^^^.........~~~.",
            "++++~~~~~~~~~~~DDDDDD^~~^^^++..^^^^.....+...~~~~",
            "D++~~~~~~~~~~~~DDDDDD+~~+.......^^^^....=.....~~",
            "~D~~~~~....~~~~~++DDD~~~..........^^....=...+.+.",
            "~~~~~~.....+^~~~~~~+~~~+^^^....U+U^^^++..+...+.+",
            "~~~~~+.....+^^~~~~~~~~~.^^^^+++UUU^^^^^+.^^^^^++",
            "~~~~~.^.....^^~~~~~~~~~.^^^..+U+U+++^^^.^^^^^^..",
            ".~~~~~^^......+~~~~~~~.+.+...U++UU+U^^^^^^^^^^^^",
            ".+~~~~^^^^^...+~~~~~~~.+.....UUU++U+++U^^^^^^^^^",
            "..~~~~^++.^+.+~~~~+~~~~....+.^^^^+++U+U^^^U.^^^+",
            "+++~~~+H+HHHHH~~+++~~~~....+.^^^^^^U+UUUU+++^^^^",
            "+~~~~~+.HHHHHH.~~+..~~~+.+...^^^^^^^+UUUU+U++^^^",
            "~~~~~~.+HHHHHH.~~.++~~~~+....++.^^^^^^UUUUUUUUUU",
            "~~~~~+..HHHHHHH.~~.~~~~~.......^^^^^^^UUU++U.UUU",
            ".~~~~~..HHHHHHHH.~~~+~~~~+.....^^^^^^^UUUUU+.UU+",
            "+.~~~~+.HHHHHHH...~~+~~~~......^^^^^^^+++U+.++++",
            "^^.~~~~~++H++HH..+~~~~~~~+.....++^^^^^U+++.UUUU+",
            "^..~~~~~~+++HHH..+..~~~~~~........^^^^UUUU+UUU..",
            "~+++~~~~~~+.+H.^^^^^^~~~~~++.+...+^^^UUUUU+UUUUU",
            "~~+...~~~~+..HH^^^^^^~~~~~+....+.+^^^UU++UUUUUU+",
            "~.....+~~~..+^..^^^^^.~~~~~+.....^^^^^^+++UU+UU+",
            "+.+...+~~~~.^^^.^^+^+..~~~~~+....^^^^^^UUUUUUU++",
            ".+~~~~+~~~~.^^^..+.....+~~~~.+..+^^^^^^+UUUUUU++",
            "+~~~~~~~~~~.^^^+.......+~~~~~..+^^^^^^UUUUUUU+U+",
            "~~~~~~~~~~~+^^.+++....^^.~~~~~+.^^^^^+UUUU++UUUU",
            "~~~~~~~~~~.+^++.+....+^^..~~~~++^^^^^UUUUUU++UU."
        ]));
    }

    [Test]
    public void Load_S107SC0000_MapTest()
    {
        ScenarioId scenarioId = new("S107SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(TestHelpers.SerializeMap(scenario.State.Map), Is.EqualTo([
            "^^^^^.~~^^^^^^~~~+++..^^^~~~~~+..+^^~~~~~~~~~~~~",
            "^^^^^^~~~^^^^^~~+...+..^^^^.~+...^^~~~++~~~~~~~~",
            "^^^^^^^~~~~^^^^^......^^^^^^~~.+.^^~~....~~~~~~~",
            "^^^^^^^~~~~~+.^^^...+.+^^^^^^~~~~~^~~+...+.~~~^^",
            "^^^^^^^^~~~~~~^^^++.....^^^^^.~~~~~~~~....++~~^^",
            "^^++^^^^++.~~~~~~~~.....^^^^^^^~~~~~~+++....+~~^",
            "^^^..+^+.+^^^^~~~~~+.....^^+^^^^~~^^~+......~~~~",
            "^+++.H+HHH^^^^^^^~~~......~~~~^~~~^^~~.......~~~",
            "+HHHHHH++H^^^^^^^~~~.....~~~~~~~~~~~~~......+.~~",
            "+HHHHHH+HHH++.^^^+~~~+...~~~+~~~~~~~~^........~~",
            "++HHHHHHHHHHH+^+^^~~~.++~~~++^^~~~^^^^^^+++...^^",
            "^^+HH+HHHHHH=+HH.^^^~~+~~~~.^^^^^^^^^^^^^^...^^^",
            "^^+.H+HHHHHH=HHH^^^^~~.~~+++^^^^^^^+..+^^^+.+^^^",
            "^^^H=HHHHHH+=.H+^^^~~~~~~+..^^^^^.++...^^^..^^^^",
            "^^^^==HHHHHH=HH.H^^~~+~~.+.+++^^.......^^^++^^^^",
            "^^^.+=+H+HH====H^^^~~++++....++++......^^^+.++^^",
            "^^++.=======HH+H^^~~+...+.+.+..++.....^^^^++.+^^",
            "^^^++^.H=+++HH+^^^~~~.......++.+...+.^.^^^++++++",
            "^^^^^^^+=+^^HHH^^^^~~....^++.^^^^^^^^^^^^+++~~~U",
            "^^^^^^^^==^^^^^^^^^~~~..^^^.^^^^^^^^^^^^^++~~~~~",
            "++^^.+^++=.^^^^^^^+.~~~.+^^+.^^^^^^^^^~~^++~~~~~",
            "..+...+++=^^^^^.+....~~.+.^..^+.+^.^^^~~~~~~~~~~",
            "^^.....=.=+^^^^+.....~~+.++......++.^^~~~~~~+~+~",
            "^^^...=====++^^^....~~~+.........++..^^~~~....~~",
            "^^^+.....+=+^^^^...+~~.........^^^++.^^~~+....+~",
            "^^^+......=.+^^^^^+~~~.....=..^^^^^^..^~~+....+~",
            "++.......==++^^^^^+~~~.....=..^^^^^^^.^~~~....U~",
            "^^..+....=...+.^^^^+~~....==+^^^^...^^^^~~~U+UU~",
            "^^^....=====..^^^^^^~~~+===.++^^^...+^^+~~~~~+~~",
            "^^^+.^^^^.++.^^^^+^..~~~+..+...^^...+.+U~~~~~~~~",
            "^^^..^^^^^^^^^^^^++++~~~~~~~~++^^+.+.....~~+~~~~",
            "^^^+.^^^^^^^^^^^^+.+..~~~~~~~~+.+......^^^~+UU~~",
            "^^^+..^^^^^^^+^^^^.+.++~^^^^~~~.......^^^^^+RUU+",
            "^^^^...^^^^^^+++^^~~+..^^^^^^~~^......^^^R^^^^UU",
            "^^^^^...+^^+++.+~~~~~...U.+^^^~~^^^^^RR^^^+^^^^^",
            "^^^^^^........+~~~.~~...UU+^^^~~^^^^^^+R+RRR^^^^",
            "^^^^^^+......++~~...~~~^^^^^^^~~^^^..RR+RRRR+^^^",
            "^^^^^^^++...^++~~~^^+~~~~^^^^~~~^^^RRRRRRRRRRR+^",
            "^^^^^^^....^^^^~~~^^+..+~~~^~~~~.^.RRRRRRRRRRR++",
            "^^^..^^....^^^^^~~^^^^..+~~~~~~...+RRRRRRRRRRRRR",
            "~^^++^+...+.^^^^~~^^^^^...~~+..^^RRRRRRRRRRRRRRR",
            "~~~+~~~~+...~~^^~~~^^^++..+++..^^^RRRRRRRRRR++RR",
            "~~~~~~~~~~~+~~~~^~~^^......+...^^^+RRR+RRRRRR++^",
            "^.~~~+~~~~~~~~~~~~~^^..........^^^^+RRRRRR++R+++",
            "^..~~...~~~~~~~~~~..+.....+..+.^^^^^RRR+RRRRR^^^",
            "^^~~+...+~~~~~~~~~.........+.+..^^^^+R.R++RR^^^^",
            "^^~~~....~~~~^^~~~^^^^+^+..+++++.^^^^++.^^R+^^^^",
            "^~~~+++^^+~~~^^^~^^^^^^^^+.^+.+.+^^^^++^^^^R^^^^"
        ]));
    }

    [Test]
    public void Load_S095SC0000_StackTemplate_S095TM0001_Test()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u0006 = new("G000UU0006");
        UnitDefinitionId u5003 = new("G000UU5003");
        UnitDefinitionId u5354 = new("G000UU5354");

        UnitGrid<ScenarioStackTemplateUnit> expectedUnits = new UnitGrid<ScenarioStackTemplateUnit>.Builder()
            .Place(new ScenarioStackTemplateUnit(u5003, 140, 1, []), GridPosition.FrontBottom, false)
            .Place(new ScenarioStackTemplateUnit(u0006, 45, 1, []), GridPosition.BackBottom, false)
            .Place(new ScenarioStackTemplateUnit(u5354, 135, 1, []), GridPosition.BackCenter, false)
            .Build();

        Assert.That(scenario.Provider.GetStackTemplate(new ScenarioStackTemplateId("S095TM0001")), Is.EqualTo(
            new ScenarioStackTemplateDefinition(
                new ScenarioStackTemplateId("S095TM0001"),
                new WorldPlayerId(1),
                new WorldSubraceId("S095SR0001"),
                expectedUnits,
                new WorldStackOrder.DefendStack(new WorldStackId("S095KC0027")),
                WorldStackDirection.East,
                AiPriority: 3)));
    }

    [Test]
    public void Load_S142SC0000_StackTemplate_S142TM000d_Test()
    {
        ScenarioId scenarioId = new("S142SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u0040 = new("G000UU0040");
        UnitDefinitionId u0046 = new("G000UU0046");
        UnitDefinitionId u0158 = new("G000UU0158");
        UnitDefinitionId u0159 = new("G000UU0159");
        UnitModifierDefinitionId leadershipModifier = new("G000UM9031");

        UnitGrid<ScenarioStackTemplateUnit> expectedUnits = new UnitGrid<ScenarioStackTemplateUnit>.Builder()
            .Place(new ScenarioStackTemplateUnit(u0159, 500, 4, []), GridPosition.FrontBottom, true)
            .Place(new ScenarioStackTemplateUnit(u0040, 275, 4, []), GridPosition.FrontCenter, false)
            .Place(
                new ScenarioStackTemplateUnit(u0046, 215, 16, [UnitModifier.FromScenario(leadershipModifier), UnitModifier.FromScenario(leadershipModifier)]),
                GridPosition.BackCenter,
                false)
            .Place(new ScenarioStackTemplateUnit(u0158, 400, 4, []), GridPosition.FrontTop, true)
            .Build();

        Assert.That(scenario.Provider.GetStackTemplate(new ScenarioStackTemplateId("S142TM000d")), Is.EqualTo(
            new ScenarioStackTemplateDefinition(
                new ScenarioStackTemplateId("S142TM000d"),
                new WorldPlayerId(3),
                new WorldSubraceId("S142SR0002"),
                expectedUnits,
                WorldStackOrder.Normal.Instance,
                Direction: null,
                AiPriority: 3)));
    }

    [Test]
    public void Load_S139SC0000_ScenarioEvent_S139EV0013_Test()
    {
        ScenarioId scenarioId = new("S139SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S139EV0013"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S139EV0013"),
            [WorldRace.Elf],
            [WorldRace.Dwarf, WorldRace.Undead, WorldRace.Heretic, WorldRace.Neutral, WorldRace.Elf],
            [
                new ScenarioEventStackExistenceCondition(
                    new ScenarioStackReference.Template(new ScenarioStackTemplateId("S139TM0000")),
                    MustExist: false)
            ],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S139EV0013:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.AllPlayers,
                    new ScenarioEventPortraitId("G000UU8027"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventWinEffect(WinnerId: null)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 16)));
        Assert.That(scenario.State.Stacks.ContainsKey(new WorldStackId("S139TM0000")), Is.False);
    }

    [Test]
    public void Load_S142SC0000_ScenarioEvent_S142EV0023_Test()
    {
        ScenarioId scenarioId = new("S142SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S142EV0023"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S142EV0023"),
            [WorldRace.Undead],
            [WorldRace.Undead],
            [new ScenarioEventFrequencyCondition(4)],
            [
                new ScenarioEventCreateStackEffect(
                    new ScenarioStackTemplateId("S142TM0010"),
                    new ScenarioLocationId("S142LO0020"))
            ],
            Enabled: true,
            OccurOnce: false,
            Chance: 60,
            Order: 33)));
    }

    [Test]
    public void LoadSkirmishScenarioPlayersTest()
    {
        List<string> rows = [];

        foreach (string id in SkirmishScenarios)
        {
            ScenarioId scenarioId = new(id);
            Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

            rows.AddRange(FormatPlayers(scenarioId, scenario.State));
        }

        Assert.That(rows, Is.EqualTo([
            "S200SC0000 1 Neutral G000LR0013 ai",
            "S200SC0000 2 Undead G000LR0010 human",
            "S200SC0000 3 Elf G000LR0016 ai",
            "S201SC0000 1 Neutral G000LR0013 ai",
            "S201SC0000 2 Human G000LR0001 human",
            "S201SC0000 3 Dwarf G000LR0004 ai",
            "S201SC0000 4 Heretic G000LR0009 ai",
            "S201SC0000 5 Elf G000LR0016 ai",
            "S202SC0000 1 Neutral G000LR0013 ai",
            "S202SC0000 2 Human G000LR0001 human",
            "S202SC0000 3 Heretic G000LR0007 ai",
            "S203SC0000 1 Neutral G000LR0013 ai",
            "S203SC0000 2 Dwarf G000LR0004 human",
            "S203SC0000 3 Elf G000LR0016 ai",
            "S204SC0000 1 Neutral G000LR0013 ai",
            "S204SC0000 2 Human G000LR0001 human",
            "S204SC0000 3 Heretic G000LR0007 ai",
            "S204SC0000 4 Elf G000LR0016 ai",
            "S205SC0000 1 Neutral G000LR0013 ai",
            "S205SC0000 2 Dwarf G000LR0004 human",
            "S205SC0000 3 Heretic G000LR0007 ai",
            "S205SC0000 4 Undead G000LR0010 ai",
            "S205SC0000 5 Elf G000LR0016 ai",
            "S206SC0000 1 Neutral G000LR0013 ai",
            "S206SC0000 2 Human G000LR0001 human",
            "S206SC0000 3 Undead G000LR0010 ai",
            "S207SC0000 1 Neutral G000LR0013 ai",
            "S207SC0000 2 Dwarf G000LR0004 human",
            "S207SC0000 3 Heretic G000LR0007 ai",
            "S207SC0000 4 Undead G000LR0010 ai",
            "S208SC0000 1 Neutral G000LR0013 ai",
            "S208SC0000 2 Human G000LR0001 human",
            "S208SC0000 3 Undead G000LR0010 ai",
            "S209SC0000 1 Neutral G000LR0013 ai",
            "S209SC0000 2 Heretic G000LR0009 human",
            "S209SC0000 3 Dwarf G000LR0004 ai",
            "S210SC0000 1 Neutral G000LR0013 ai",
            "S210SC0000 2 Human G000LR0001 human",
            "S210SC0000 3 Dwarf G000LR0004 ai",
            "S210SC0000 4 Undead G000LR0010 ai",
            "S211SC0000 1 Neutral G000LR0013 ai",
            "S211SC0000 2 Human G000LR0001 human",
            "S211SC0000 3 Heretic G000LR0007 ai",
            "S212SC0000 1 Neutral G000LR0013 ai",
            "S212SC0000 2 Human G000LR0001 human",
            "S212SC0000 3 Heretic G000LR0007 ai",
            "S213SC0000 1 Neutral G000LR0013 ai",
            "S213SC0000 2 Heretic G000LR0007 human",
            "S213SC0000 3 Dwarf G000LR0004 ai",
            "S214SC0000 1 Neutral G000LR0013 ai",
            "S214SC0000 2 Dwarf G000LR0004 human",
            "S214SC0000 3 Elf G000LR0016 ai",
            "S215SC0000 1 Neutral G000LR0013 ai",
            "S215SC0000 2 Human G000LR0001 human",
            "S215SC0000 3 Undead G000LR0010 ai",
            "S216SC0000 1 Neutral G000LR0013 ai",
            "S216SC0000 2 Human G000LR0001 human",
            "S216SC0000 3 Elf G000LR0016 ai",
            "S217SC0000 1 Neutral G000LR0013 ai",
            "S217SC0000 2 Elf G000LR0016 human",
            "S217SC0000 3 Human G000LR0001 ai",
            "S218SC0000 1 Neutral G000LR0013 ai",
            "S218SC0000 2 Dwarf G000LR0006 human",
            "S218SC0000 3 Heretic G000LR0008 ai",
            "S218SC0000 4 Elf G000LR0018 ai",
            "S219SC0000 1 Neutral G000LR0013 ai",
            "S219SC0000 2 Human G000LR0003 human",
            "S219SC0000 3 Dwarf G000LR0005 ai",
            "S219SC0000 4 Heretic G000LR0007 ai",
            "S219SC0000 5 Undead G000LR0010 ai",
            "S220SC0000 1 Neutral G000LR0013 ai",
            "S220SC0000 2 Heretic G000LR0007 human",
            "S220SC0000 3 Undead G000LR0010 ai",
            "S220SC0000 4 Human G000LR0001 ai",
            "S221SC0000 1 Neutral G000LR0013 ai",
            "S221SC0000 2 Undead G000LR0010 human",
            "S221SC0000 3 Elf G000LR0016 ai",
            "S222SC0000 1 Neutral G000LR0013 ai",
            "S222SC0000 2 Human G000LR0001 human",
            "S222SC0000 3 Undead G000LR0010 ai",
            "S223SC0000 1 Neutral G000LR0013 ai",
            "S223SC0000 2 Undead G000LR0010 human",
            "S223SC0000 3 Elf G000LR0016 ai",
            "S224SC0000 1 Neutral G000LR0013 ai",
            "S224SC0000 2 Human G000LR0002 human",
            "S224SC0000 3 Dwarf G000LR0005 ai",
            "S225SC0000 1 Neutral G000LR0013 ai",
            "S225SC0000 2 Dwarf G000LR0004 human",
            "S225SC0000 3 Human G000LR0001 ai",
            "S226SC0000 1 Neutral G000LR0013 ai",
            "S226SC0000 2 Human G000LR0001 human",
            "S226SC0000 3 Heretic G000LR0007 ai",
            "S226SC0000 4 Undead G000LR0010 ai",
            "S227SC0000 1 Neutral G000LR0013 ai",
            "S227SC0000 2 Dwarf G000LR0004 human",
            "S227SC0000 3 Heretic G000LR0007 ai",
            "S228SC0000 1 Neutral G000LR0013 ai",
            "S228SC0000 2 Human G000LR0001 human",
            "S228SC0000 3 Dwarf G000LR0004 ai",
            "S229SC0000 1 Neutral G000LR0013 ai",
            "S229SC0000 2 Dwarf G000LR0004 human",
            "S229SC0000 3 Undead G000LR0011 ai",
            "S230SC0000 1 Neutral G000LR0013 ai",
            "S230SC0000 2 Human G000LR0001 human",
            "S230SC0000 3 Heretic G000LR0007 ai",
            "S230SC0000 4 Undead G000LR0010 ai",
            "S230SC0000 5 Elf G000LR0016 ai",
            "S231SC0000 1 Neutral G000LR0013 ai",
            "S231SC0000 2 Human G000LR0001 human",
            "S231SC0000 3 Heretic G000LR0007 ai",
            "S231SC0000 4 Undead G000LR0010 ai",
            "S232SC0000 1 Neutral G000LR0013 ai",
            "S232SC0000 2 Human G000LR0001 human",
            "S232SC0000 3 Elf G000LR0016 ai",
            "S233SC0000 1 Neutral G000LR0013 ai",
            "S233SC0000 2 Human G000LR0001 human",
            "S233SC0000 3 Heretic G000LR0007 ai",
            "S233SC0000 4 Undead G000LR0010 ai",
            "S233SC0000 5 Elf G000LR0016 ai",
            "S234SC0000 1 Neutral G000LR0013 ai",
            "S234SC0000 2 Dwarf G000LR0004 human",
            "S234SC0000 3 Heretic G000LR0007 ai",
            "S234SC0000 4 Undead G000LR0010 ai",
            "S235SC0000 1 Neutral G000LR0013 ai",
            "S235SC0000 2 Dwarf G000LR0004 human",
            "S235SC0000 3 Heretic G000LR0007 ai",
            "S235SC0000 4 Undead G000LR0010 ai",
            "S236SC0000 1 Neutral G000LR0013 ai",
            "S236SC0000 2 Human G000LR0001 human",
            "S236SC0000 3 Dwarf G000LR0004 ai",
            "S236SC0000 4 Heretic G000LR0007 ai",
            "S236SC0000 5 Undead G000LR0010 ai",
            "S237SC0000 1 Neutral G000LR0013 ai",
            "S237SC0000 2 Undead G000LR0010 human",
            "S237SC0000 3 Elf G000LR0016 ai",
            "S238SC0000 1 Neutral G000LR0013 ai",
            "S238SC0000 2 Dwarf G000LR0005 human",
            "S238SC0000 3 Undead G000LR0010 ai",
            "S239SC0000 1 Neutral G000LR0013 ai",
            "S239SC0000 2 Human G000LR0001 human",
            "S239SC0000 3 Heretic G000LR0007 ai",
            "S239SC0000 4 Undead G000LR0010 ai",
            "S239SC0000 5 Elf G000LR0016 ai",
            "S240SC0000 1 Neutral G000LR0013 ai",
            "S240SC0000 2 Human G000LR0001 human",
            "S240SC0000 3 Heretic G000LR0007 ai",
            "S240SC0000 4 Undead G000LR0010 ai",
            "S241SC0000 1 Neutral G000LR0013 ai",
            "S241SC0000 2 Dwarf G000LR0004 human",
            "S241SC0000 3 Heretic G000LR0007 ai",
            "S241SC0000 4 Undead G000LR0010 ai",
            "S241SC0000 5 Elf G000LR0016 ai",
            "S242SC0000 1 Neutral G000LR0013 ai",
            "S242SC0000 2 Human G000LR0001 human",
            "S242SC0000 3 Dwarf G000LR0004 ai",
            "S243SC0000 1 Neutral G000LR0013 ai",
            "S243SC0000 2 Dwarf G000LR0004 human",
            "S243SC0000 3 Heretic G000LR0007 ai",
            "S243SC0000 4 Undead G000LR0010 ai",
            "S244SC0000 1 Neutral G000LR0013 ai",
            "S244SC0000 2 Human G000LR0001 human",
            "S244SC0000 3 Heretic G000LR0007 ai",
            "S245SC0000 1 Neutral G000LR0013 ai",
            "S245SC0000 2 Dwarf G000LR0004 human",
            "S245SC0000 3 Heretic G000LR0007 ai",
            "S245SC0000 4 Undead G000LR0010 ai",
            "S246SC0000 1 Neutral G000LR0013 ai",
            "S246SC0000 2 Human G000LR0001 human",
            "S246SC0000 3 Elf G000LR0016 ai",
            "S247SC0000 1 Neutral G000LR0013 ai",
            "S247SC0000 2 Heretic G000LR0007 human",
            "S247SC0000 3 Elf G000LR0016 ai",
            "S248SC0000 1 Neutral G000LR0013 ai",
            "S248SC0000 2 Human G000LR0003 human",
            "S248SC0000 3 Dwarf G000LR0006 ai",
            "S248SC0000 4 Undead G000LR0012 ai",
            "S249SC0000 1 Neutral G000LR0013 ai",
            "S249SC0000 2 Human G000LR0001 human",
            "S249SC0000 3 Dwarf G000LR0004 ai",
            "S249SC0000 4 Heretic G000LR0009 ai",
            "S249SC0000 5 Undead G000LR0011 ai",
            "S250SC0000 1 Neutral G000LR0013 ai",
            "S250SC0000 2 Human G000LR0001 human",
            "S250SC0000 3 Dwarf G000LR0005 ai",
            "S250SC0000 4 Heretic G000LR0007 ai",
            "S251SC0000 1 Neutral G000LR0013 ai",
            "S251SC0000 2 Human G000LR0002 human",
            "S251SC0000 3 Dwarf G000LR0004 ai",
            "S251SC0000 4 Elf G000LR0017 ai",
            "S251SC0000 5 Undead G000LR0012 ai",
            "S252SC0000 1 Neutral G000LR0013 ai",
            "S252SC0000 2 Human G000LR0001 human",
            "S252SC0000 3 Dwarf G000LR0006 ai",
            "S252SC0000 4 Undead G000LR0011 ai",
            "S253SC0000 1 Neutral G000LR0013 ai",
            "S253SC0000 2 Human G000LR0001 human",
            "S254SC0000 1 Neutral G000LR0013 ai",
            "S254SC0000 2 Human G000LR0001 human",
            "S254SC0000 3 Heretic G000LR0007 ai",
            "S255SC0000 1 Neutral G000LR0013 ai",
            "S255SC0000 2 Human G000LR0001 human",
            "S255SC0000 3 Heretic G000LR0007 ai",
            "S255SC0000 4 Undead G000LR0011 ai",
            "S256SC0000 1 Neutral G000LR0013 ai",
            "S256SC0000 2 Heretic G000LR0007 human",
            "S256SC0000 3 Undead G000LR0010 ai",
            "S257SC0000 1 Neutral G000LR0013 ai",
            "S257SC0000 2 Human G000LR0001 human",
            "S257SC0000 3 Dwarf G000LR0004 ai",
            "S257SC0000 4 Heretic G000LR0009 ai",
            "S257SC0000 5 Undead G000LR0011 ai",
            "S258SC0000 1 Neutral G000LR0013 ai",
            "S258SC0000 2 Human G000LR0001 human",
            "S258SC0000 3 Elf G000LR0016 ai",
            "S259SC0000 1 Neutral G000LR0013 ai",
            "S259SC0000 2 Dwarf G000LR0004 human",
            "S259SC0000 3 Elf G000LR0016 ai"
        ]));
    }

    [Test]
    public void Load_S110SC0000_ScenarioEvent_S110EV002a_Test()
    {
        ScenarioId scenarioId = new("S110SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S110EV002a"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S110EV002a"),
            [WorldRace.Undead, WorldRace.Heretic, WorldRace.Neutral],
            [WorldRace.Undead, WorldRace.Heretic, WorldRace.Neutral],
            [
                new ScenarioEventStackToLocationCondition(
                    new ScenarioStackReference.Stack(new WorldStackId("S110KC0083")),
                    new ScenarioLocationId("S110LO0018"))
            ],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S110EV002a:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.AllPlayers,
                    new ScenarioEventPortraitId("G000UU5030"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventDestroyItemEffect(new ItemDefinitionId("G000IG7007"), TriggererOnly: true),
                new ScenarioEventDestroyItemEffect(new ItemDefinitionId("G000IG7006"), TriggererOnly: true),
                new ScenarioEventDestroyItemEffect(new ItemDefinitionId("G000IG7004"), TriggererOnly: true),
                new ScenarioEventDestroyItemEffect(new ItemDefinitionId("G000IG0024"), TriggererOnly: true),
                new ScenarioEventRemoveStackEffect(
                    new ScenarioStackReference.Stack(new WorldStackId("S110KC0083")),
                    FirstOnly: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 28)));
    }

    [Test]
    public void Load_S142SC0000_ScenarioEvent_S142EV0004_Test()
    {
        ScenarioId scenarioId = new("S142SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S142EV0004"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S142EV0004"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [
                new ScenarioEventLeaderOwningItemCondition(
                    new ScenarioStackReference.Stack(new WorldStackId("S142KC000a")),
                    new ItemDefinitionId("G000IG9148")),
                new ScenarioEventLeaderOwningItemCondition(
                    new ScenarioStackReference.Stack(new WorldStackId("S142KC000a")),
                    new ItemDefinitionId("G000IG9147"))
            ],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S142EV0004:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0045"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventEnableEventEffect(new ScenarioEventId("S142EV0003"), true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 4)));
    }

    [Test]
    public void Load_S120SC0000_ScenarioEvent_S120EV001a_Test()
    {
        ScenarioId scenarioId = new("S120SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S120EV001a"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S120EV001a"),
            [WorldRace.Dwarf],
            [WorldRace.Dwarf],
            [
                new ScenarioEventOwningItemCondition(new ItemDefinitionId("G000IG7104")),
                new ScenarioEventFrequencyCondition(2)
            ],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S120EV001a:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0046"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S120EV001a:POPUP_TXT:1"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU6106"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S120EV001a:POPUP_TXT:2"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0046"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventDestroyItemEffect(new ItemDefinitionId("G000IG7104"), TriggererOnly: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 22)));
    }

    [Test]
    public void Load_S120SC0000_ScenarioEvent_S120EV000c_Test()
    {
        ScenarioId scenarioId = new("S120SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S120EV000c"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S120EV000c"),
            [WorldRace.Dwarf],
            [WorldRace.Dwarf],
            [new ScenarioEventOwningCityCondition(new WorldFortificationId("S120FT000b"))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S120EV000c:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0010"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventCastSpellEffect(new WorldSpellDefinitionId("G000SS0029"), CasterId: null),
                new ScenarioEventCreateStackEffect(
                    new ScenarioStackTemplateId("S120TM0000"),
                    new ScenarioLocationId("S120LO0003")),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S120EV000c:POPUP_TXT:3"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0010"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 34)));
    }

    [Test]
    public void Load_S103SC0000_ScenarioEvent_S103EV0012_Test()
    {
        ScenarioId scenarioId = new("S103SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S103EV0012"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S103EV0012"),
            [WorldRace.Heretic],
            [WorldRace.Dwarf, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S103LO0008"))],
            [
                new ScenarioEventCastSpellMapEffect(
                    new WorldSpellDefinitionId("G000SS0061"),
                    new ScenarioLocationId("S103LO0008"),
                    new WorldPlayerId(2)),
                new ScenarioEventCastSpellMapEffect(
                    new WorldSpellDefinitionId("G000SS0061"),
                    new ScenarioLocationId("S103LO0008"),
                    new WorldPlayerId(2)),
                new ScenarioEventCastSpellMapEffect(
                    new WorldSpellDefinitionId("G000SS0066"),
                    new ScenarioLocationId("S103LO0008"),
                    new WorldPlayerId(2)),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S103EV0012:POPUP_TXT:3"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0082"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S103EV0012:POPUP_TXT:4"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0071"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S122SC0000_ScenarioEvent_S122EV0005_Test()
    {
        ScenarioId scenarioId = new("S122SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S122EV0005"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S122EV0005"),
            [WorldRace.Dwarf],
            [WorldRace.Dwarf],
            [new ScenarioEventEnteringZoneCondition(new ScenarioLocationId("S122LO0002"))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S122EV0005:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0161"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S122EV0005:POPUP_TXT:1"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0046"),
                    IsPortraitLeft: false,
                    IsPortraitTriggerer: false),
                new ScenarioEventGiveSpellEffect(new WorldSpellDefinitionId("G000SS0088")),
                new ScenarioEventGiveSpellEffect(new WorldSpellDefinitionId("G000SS0036")),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S122EV0005:POPUP_TXT:4"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0046"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventCastSpellMapEffect(
                    new WorldSpellDefinitionId("G000SS0030"),
                    new ScenarioLocationId("S122LO000c"),
                    new WorldPlayerId(3)),
                new ScenarioEventCastSpellMapEffect(
                    new WorldSpellDefinitionId("G000SS0030"),
                    new ScenarioLocationId("S122LO000b"),
                    new WorldPlayerId(3)),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S122EV0005:POPUP_TXT:7"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0038"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventRemoveStackEffect(
                    new ScenarioStackReference.Template(new ScenarioStackTemplateId("S122TM0001")),
                    FirstOnly: true)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 26)));
    }

    [Test]
    public void Load_S095SC0000_Stack_S095KC0003_EquipmentTest()
    {
        ScenarioId scenarioId = new("S095SC0000");
        Db.ScenarioId dbScenarioId = new("S095SC0000");

        Db.ScenarioItem banner = new(dbScenarioId, new Db.Id("S095IM9000"), "G000IG1001");
        Db.ScenarioItem tome = new(dbScenarioId, new Db.Id("S095IM9001"), "G000IG4001");
        Db.ScenarioItem talisman = new(dbScenarioId, new Db.Id("S095IM9002"), "G000IG9101");
        Db.ScenarioItem potion = new(dbScenarioId, new Db.Id("S095IM9003"), "G000IG0005");
        Db.ScenarioItem weapon = new(dbScenarioId, new Db.Id("S095IM9004"), "G000IG3001");
        Db.ScenarioItem armor = new(dbScenarioId, new Db.Id("S095IM9005"), "G000IG2001");
        Db.ScenarioItem boots = new(dbScenarioId, new Db.Id("S095IM9006"), "G000IG1010");

        Db.ScenarioStack stack = scenarioDb.ScenarioStacksByScenarioAndStackId[dbScenarioId][new Db.Id("S095KC0003")];
        Db.ScenarioStack equippedStack = stack with
        {
            BannerId = banner.ItemId,
            TomeId = tome.ItemId,
            Battle1Id = talisman.ItemId,
            Battle2Id = potion.ItemId,
            Artifact1Id = weapon.ItemId,
            Artifact2Id = armor.ItemId,
            BootsId = boots.ItemId
        };

        List<Db.ScenarioStack> stacks = [.. scenarioDb.ScenarioStacks];
        stacks[stacks.IndexOf(stack)] = equippedStack;

        Dictionary<Db.Id, Db.ScenarioItem> items = new(scenarioDb.ScenarioItemsByScenarioAndItemId[dbScenarioId])
        {
            [banner.ItemId] = banner,
            [tome.ItemId] = tome,
            [talisman.ItemId] = talisman,
            [potion.ItemId] = potion,
            [weapon.ItemId] = weapon,
            [armor.ItemId] = armor,
            [boots.ItemId] = boots
        };

        ScenarioDatabase equippedDb = scenarioDb with
        {
            ScenarioStacksByScenario = stacks.ToLookup(row => row.ScenarioId),
            ScenarioItemsByScenarioAndItemId = new Dictionary<Db.ScenarioId, IReadOnlyDictionary<Db.Id, Db.ScenarioItem>>(
                scenarioDb.ScenarioItemsByScenarioAndItemId)
            {
                [dbScenarioId] = items
            }
        };

        Scenario scenario = ScenarioLoader.Load(globalDb, equippedDb, campaignDb, scenarioId, definitions, globals);

        UnitDefinitionId u5035 = new("G000UU5035");
        UnitDefinitionId u5136 = new("G000UU5136");

        UnitModifier bannerModifier = UnitModifier.FromEquipment(new UnitModifierDefinitionId("G000UM2101"));
        UnitModifier tomeModifier = UnitModifier.FromEquipment(new UnitModifierDefinitionId("G000UM2401"));
        UnitModifier weaponModifier = UnitModifier.FromEquipment(new UnitModifierDefinitionId("G000UM2301"));
        UnitModifier armorModifier = UnitModifier.FromEquipment(new UnitModifierDefinitionId("G000UM2201"));
        UnitModifier bootsModifier = UnitModifier.FromEquipment(new UnitModifierDefinitionId("G000UM2110"));

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(new WorldUnit(new WorldUnitId("S095UN0014"), u5035, 370, 0, 1, [bannerModifier], null, false), GridPosition.FrontBottom, true)
            .Place(
                new WorldUnit(new WorldUnitId("S095UN0012"), u5136, 420, 0, 1, [bannerModifier, tomeModifier, weaponModifier, armorModifier, bootsModifier], Name: "Giant Spider", IsTypeLocked: false),
                GridPosition.FrontTop,
                true)
            .Build();

        LeaderEquipment expectedEquipment = LeaderEquipment.Empty
            .With(LeaderEquipmentSlot.Banner, new WorldItem(new WorldItemId("S095IM9000"), new ItemDefinitionId("G000IG1001"), Charges: null))
            .With(LeaderEquipmentSlot.Tome, new WorldItem(new WorldItemId("S095IM9001"), new ItemDefinitionId("G000IG4001"), Charges: null))
            .With(LeaderEquipmentSlot.Battle1, new WorldItem(new WorldItemId("S095IM9002"), new ItemDefinitionId("G000IG9101"), Charges: 5))
            .With(LeaderEquipmentSlot.Battle2, new WorldItem(new WorldItemId("S095IM9003"), new ItemDefinitionId("G000IG0005"), Charges: null))
            .With(LeaderEquipmentSlot.Artifact1, new WorldItem(new WorldItemId("S095IM9004"), new ItemDefinitionId("G000IG3001"), Charges: null))
            .With(LeaderEquipmentSlot.Artifact2, new WorldItem(new WorldItemId("S095IM9005"), new ItemDefinitionId("G000IG2001"), Charges: null))
            .With(LeaderEquipmentSlot.Boots, new WorldItem(new WorldItemId("S095IM9006"), new ItemDefinitionId("G000IG1010"), Charges: null));

        Assert.That(scenario.State.Stacks[new WorldStackId("S095KC0003")], Is.EqualTo(new WorldStack(
            new WorldStackId("S095KC0003"),
            new WorldPlayerId(1),
            new WorldPosition(40, 40),
            WorldStackDirection.NorthEast,
            expectedUnits,
            MovementPoints: 20,
            WorldInventory.Empty,
            expectedEquipment,
            new WorldSubraceId("S095SR0003"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Guard.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null)));
    }

    [Test]
    public void Load_S142SC0000_RelationsTest()
    {
        ScenarioId scenarioId = new("S142SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(FormatRelations(scenario.State), Is.EqualTo([
            "Human-Undead 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Human-Dwarf 100/100 alliance=True turn=1 permanentWar=False permanentAlliance=True",
            "Human-Neutral 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Human-Elf 0/0 alliance=False turn=1 permanentWar=True permanentAlliance=False",
            "Undead-Dwarf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Undead-Neutral 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Undead-Elf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Dwarf-Neutral 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Dwarf-Elf 0/0 alliance=False turn=1 permanentWar=True permanentAlliance=False",
            "Neutral-Elf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False"
        ]));
    }

    [Test]
    public void Load_S108SC0000_ScenarioLeaderTest()
    {
        ScenarioId scenarioId = new("S108SC0000");
        WorldItem banner = new(new WorldItemId("S107IM0001"), new ItemDefinitionId("G000IG1001"), Charges: null);
        WorldItem gem = new(new WorldItemId("S107IM0002"), new ItemDefinitionId("G000IG7001"), Charges: null);
        ScenarioLeader scenarioLeader = new(
            new WorldUnit(
                new WorldUnitId("S108UN0001"),
                new UnitDefinitionId("G000UU0019"),
                Hitpoints: 1,
                Experience: 120,
                Level: 3,
                [UnitModifier.Permanent(new UnitModifierDefinitionId("G000UM4008"))],
                Name: "Uther",
                IsTypeLocked: false),
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, banner),
            new WorldInventory([gem]),
            PendingLeaderUpgrades: 1,
            BattlesWon: 7);
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(0))], WorldDifficulty.Average, scenarioLeader);
        Scenario scenario = ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals, configuration, new FixedRandomProvider());

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(
                new WorldUnit(
                    new WorldUnitId("S108UN0001"),
                    new UnitDefinitionId("G000UU0019"),
                    Hitpoints: 180,
                    Experience: 120,
                    Level: 3,
                    [
                        UnitModifier.Permanent(new UnitModifierDefinitionId("G000UM4008")),
                        UnitModifier.FromEquipment(new UnitModifierDefinitionId("G000UM2101"))
                    ],
                    Name: "Uther",
                    IsTypeLocked: false),
                GridPosition.FrontCenter,
                false)
            .Build();

        Assert.That(scenario.State.Stacks[new WorldStackId("S108KC0000")], Is.EqualTo(new WorldStack(
            new WorldStackId("S108KC0000"),
            new WorldPlayerId(2),
            new WorldPosition(11, 63),
            WorldStackDirection.SouthWest,
            expectedUnits,
            MovementPoints: 22,
            new WorldInventory([gem]),
            LeaderEquipment.Empty.With(LeaderEquipmentSlot.Banner, banner),
            new WorldSubraceId("S108SR0001"),
            AiPriority: 3,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Normal.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 1,
            BattlesWon: 7,
            SourceTemplateId: null)));
    }

    [Test]
    public void Load_S108SC0000_RelationsTest()
    {
        ScenarioId scenarioId = new("S108SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(FormatRelations(scenario.State), Is.EqualTo([
            "Human-Heretic 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Human-Dwarf 50/50 alliance=False turn=1 permanentWar=False permanentAlliance=False",
            "Human-Neutral 50/50 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Heretic-Dwarf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Heretic-Neutral 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Dwarf-Neutral 95/95 alliance=False turn=0 permanentWar=False permanentAlliance=False"
        ]));
    }

    [Test]
    public void Load_S201SC0000_RelationsTest()
    {
        ScenarioId scenarioId = new("S201SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(FormatRelations(scenario.State), Is.EqualTo([
            "Human-Heretic 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Human-Dwarf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Human-Neutral 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Human-Elf 0/0 alliance=False turn=1 permanentWar=True permanentAlliance=False",
            "Heretic-Dwarf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Heretic-Neutral 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Heretic-Elf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Dwarf-Neutral 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Dwarf-Elf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False",
            "Neutral-Elf 0/0 alliance=False turn=0 permanentWar=False permanentAlliance=False"
        ]));
    }

    [Test]
    public void Load_S118SC0000_ScenarioEvent_S118EV0022_Test()
    {
        ScenarioId scenarioId = new("S118SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S118EV0022"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S118EV0022"),
            [WorldRace.Dwarf],
            [WorldRace.Dwarf],
            [new ScenarioEventFrequencyCondition(10)],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S118EV0022:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0039"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S118EV0022:POPUP_TXT:1"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0046"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S118EV0022:POPUP_TXT:2"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0039"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S118EV0022:POPUP_TXT:3"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0039"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S118EV0022:POPUP_TXT:4"),
                    ScenarioEventPopupAudience.Triggerer,
                    new ScenarioEventPortraitId("G000UU0039"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventDiplomacyEffect(new WorldPlayerId(2), new WorldPlayerId(3), Relation: 0, PermanentWar: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 18)));
    }

    [Test]
    public void Load_S201SC0000_ScenarioEvent_S201EV0047_Test()
    {
        ScenarioId scenarioId = new("S201SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S201EV0047"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S201EV0047"),
            [WorldRace.Elf],
            [WorldRace.Elf],
            [new ScenarioEventFrequencyCondition(2)],
            [
                new ScenarioEventAllyEffect(new WorldPlayerId(4), new WorldPlayerId(2), Permanent: true),
                new ScenarioEventAllyEffect(new WorldPlayerId(2), new WorldPlayerId(3), Permanent: true)
            ],
            Enabled: false,
            OccurOnce: true,
            Chance: 100,
            Order: 59)));
    }

    [Test]
    public void Load_S101SC0000_ScenarioEvent_S101EV0018_Test()
    {
        ScenarioId scenarioId = new("S101SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        ScenarioEventDefinition scenarioEvent = scenario.Provider.GetEvent(new ScenarioEventId("S101EV0018"));

        Assert.That(scenarioEvent, Is.EqualTo(new ScenarioEventDefinition(
            new ScenarioEventId("S101EV0018"),
            [WorldRace.Undead, WorldRace.Heretic, WorldRace.Neutral],
            [WorldRace.Undead, WorldRace.Heretic, WorldRace.Neutral],
            [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(new WorldStackId("S101KC0013")))],
            [
                new ScenarioEventPopupEffect(
                    new ScenarioTextId("S101EV0018:POPUP_TXT:0"),
                    ScenarioEventPopupAudience.AllPlayers,
                    new ScenarioEventPortraitId("G000UU0063"),
                    IsPortraitLeft: true,
                    IsPortraitTriggerer: false),
                new ScenarioEventWinEffect(WinnerId: null)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0)));
    }

    [Test]
    public void Load_S217SC0000_ConfiguredRaceTest()
    {
        ScenarioId scenarioId = new("S217SC0000");
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Guildmaster, new WorldObjectVariant(3))], WorldDifficulty.Hard, Leader: null);
        Scenario scenario = ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals, configuration, new FixedRandomProvider());

        Assert.That(FormatPlayers(scenarioId, scenario.State), Is.EqualTo([
            "S217SC0000 1 Neutral G000LR0013 ai",
            "S217SC0000 2 Elf G000LR0016 ai",
            "S217SC0000 3 Human g000LR0002 human"
        ]));
    }

    [Test]
    public void Load_S217SC0000_NeutralRaceTest()
    {
        ScenarioId scenarioId = new("S217SC0000");
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Neutral, LordCategory.Warrior, new WorldObjectVariant(0))], WorldDifficulty.Average, Leader: null);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals, configuration, new FixedRandomProvider()));

        Assert.That(ex.Message, Is.EqualTo("Race 'Neutral' is not playable."));
    }

    [Test]
    public void Load_S217SC0000_MissingRaceTest()
    {
        ScenarioId scenarioId = new("S217SC0000");
        ScenarioConfiguration configuration = new([new ScenarioPlayerConfiguration(WorldRace.Undead, LordCategory.Warrior, new WorldObjectVariant(0))], WorldDifficulty.Average, Leader: null);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals, configuration, new FixedRandomProvider()));

        Assert.That(ex.Message, Is.EqualTo("Race 'Undead' is not playable."));
    }

    [Test]
    public void Load_S217SC0000_TwoHumanPlayersTest()
    {
        ScenarioId scenarioId = new("S217SC0000");
        ScenarioConfiguration configuration = new(
            [
                new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Mage, new WorldObjectVariant(2)),
                new ScenarioPlayerConfiguration(WorldRace.Elf, LordCategory.Guildmaster, new WorldObjectVariant(1))
            ],
            WorldDifficulty.Easy,
            Leader: null);
        Scenario scenario = ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals, configuration, new FixedRandomProvider());

        Assert.That(FormatPlayers(scenarioId, scenario.State), Is.EqualTo([
            "S217SC0000 1 Neutral G000LR0013 ai",
            "S217SC0000 2 Elf g000LR0017 human",
            "S217SC0000 3 Human g000LR0003 human"
        ]));
        Assert.That(scenario.State.Players[new WorldPlayerId(2)].FaceVariant, Is.EqualTo(new WorldObjectVariant(1)));
        Assert.That(scenario.State.Players[new WorldPlayerId(3)].FaceVariant, Is.EqualTo(new WorldObjectVariant(2)));
        Assert.That(scenario.State.Players[new WorldPlayerId(2)].Resources, Is.EqualTo(new WorldResourceSet(1200, 0, 0, 0, 0, 0)));
        Assert.That(scenario.State.Players[new WorldPlayerId(3)].Resources, Is.EqualTo(new WorldResourceSet(1200, 0, 0, 0, 0, 0)));
    }

    [Test]
    public void Load_S217SC0000_DuplicateRaceTest()
    {
        ScenarioId scenarioId = new("S217SC0000");
        ScenarioConfiguration configuration = new(
            [
                new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Warrior, new WorldObjectVariant(0)),
                new ScenarioPlayerConfiguration(WorldRace.Human, LordCategory.Mage, new WorldObjectVariant(1))
            ],
            WorldDifficulty.Average,
            Leader: null);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals, configuration, new FixedRandomProvider()));

        Assert.That(ex.Message, Is.EqualTo("Race 'Human' is configured more than once."));
    }

    [Test]
    public void Load_S217SC0000_NoHumanPlayerTest()
    {
        ScenarioId scenarioId = new("S217SC0000");
        ScenarioConfiguration configuration = new([], WorldDifficulty.Average, Leader: null);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ScenarioLoader.Create(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals, configuration, new FixedRandomProvider()));

        Assert.That(ex.Message, Is.EqualTo("Scenario configuration requires a human player."));
    }

    [Test]
    public void Start_S139SC0000_Test()
    {
        ScenarioId scenarioId = new("S139SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);
        EngineContext context = new(
            new FixedRandomProvider(),
            definitions,
            new UnitStatsProvider(definitions, globals, scenario.Provider.Definition),
            scenario.Provider,
            globals);
        WorldEngine engine = new(context, scenario.State);

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldPlayer elf = engine.State.Players[engine.State.TurnInfo.CurrentPlayerId];

        Assert.That(elf.Race, Is.EqualTo(WorldRace.Elf));
        Assert.That(FormatEventStates(engine.State), Is.EqualTo(["S139EV0000 disabled"]));
        Assert.That(FormatTemplateInstances(engine.State), Is.EqualTo(["Stack0001 S139TM0000 owner=1 at (35,47)"]));
        Assert.That(engine.State.ScenarioState.IsEnded, Is.False);
    }

    [Test]
    public void Load_S107SC0000_ObjectiveTest()
    {
        ScenarioId scenarioId = new("S107SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(scenario.State.ScenarioState.ObjectiveTextId, Is.EqualTo(new ScenarioTextId("S107IF0000:BRIEFING:0")));
    }

    [Test]
    public void LoadNamesTest()
    {
        ScenarioId scenarioId = new("S135SC0000");
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, scenarioId, definitions, globals);

        Assert.That(
            FormatNames(scenario.Provider.Names),
            Is.EqualTo(
            [
                "Faction Servants of Mortis",
                "Character Neutrals",
                "Character Zythaar",
                "Character Thorne",
                "Character Pradesh",
                "Character Spearman",
                "Character Peasant",
                "Character Squire",
                "Character Zombie",
                "Character Tora'ach",
                "Character Phaenos",
                "Character Illumielle",
                "Character Fyrwenn",
                "Character Elf Lord",
                "Character Oracle Elf",
                "Character Imperial Knight",
                "Character Ildest",
                "Character Specter",
                "Character Goblin",
                "Character Lizard Man",
                "Character Giant Black Spider",
                "Character Mermaid",
                "Character Forest Elf",
                "Character Doomdrake",
                "Character Tifhas",
                "Character Pheanos",
                "Character Knight",
                "Character Witch-Hunter",
                "Character Archer",
                "Character Marksman",
                "Character Apprentice",
                "Character Acolyte",
                "Character Imperial Priest",
                "Character Paladin",
                "Character Cleric",
                "Character Matriarch",
                "Character Titan",
                "Character Pegasus Knight",
                "Character Ranger",
                "Character Archmage",
                "Character Arch-Angel",
                "Character Axe Thrower",
                "Character Crossbowman",
                "Character Flame Caster",
                "Character Hill Giant",
                "Character Tempest Giant",
                "Character Tenderfoot",
                "Character Novice",
                "Character Druidess",
                "Character Veteran",
                "Character Venerable Warrior",
                "Character Mountaineer",
                "Character Polar Bear",
                "Character Yeti",
                "Character Engineer",
                "Character Loremaster",
                "Character Roc",
                "Character Valkyrie",
                "Character Berzerker",
                "Character Anti-Paladin",
                "Character Gargoyle",
                "Character Marble Gargoyle",
                "Character Moloch",
                "Character Cultist",
                "Character Demonologist",
                "Character Incubus",
                "Character Pandemoneus",
                "Character Hag",
                "Character Arch-Devil",
                "Character Baroness",
                "Character Hellhound",
                "Character Beliarh",
                "Character Avenger",
                "Character Ghost",
                "Character Initiate",
                "Character Warlock",
                "Character Vampire",
                "Character Lich",
                "Character Wraith",
                "Character Skeleton Warrior",
                "Character Skeleton Champion",
                "Character Templar",
                "Character Wyvern",
                "Character Deathdragon",
                "Character Death Knight",
                "Character Nosferat",
                "Character Lich Queen",
                "Character Banshee",
                "Character Skeleton",
                "Character Evil Ent",
                "Character Thanatos",
                "Character Barbarian Chieftain",
                "Character Ogre",
                "Character Thug",
                "Character Troll",
                "Character Centaur",
                "Character Elf Ranger",
                "Character Ghoul",
                "Character Imp",
                "Character White Dragon",
                "Character Blue Dragon",
                "Character Kraken",
                "Character Centaur Lancer",
                "Character Griffin",
                "Character Skylord",
                "Character Myzrael",
                "Character Vithar",
                "Character Ashkael",
                "Character Ashgan",
                "Character Merman",
                "Character Goblin Archer",
                "Character Master Thug",
                "Character Giant Spider",
                "Character Hierophant",
                "Character Prophetess",
                "Character White Wizard",
                "Character Elementalist",
                "Character Imperial Assassin",
                "Character Holy Avenger",
                "Character Defender of Faith",
                "Character Son of Ymir",
                "Character Archdruidess",
                "Character Alchemist",
                "Character Forge Guardian",
                "Character Hermit",
                "Character Infernal Knight",
                "Character Onyx Gargoyle",
                "Character Tiamath",
                "Character Overlord",
                "Character Abyssal Devil",
                "Character Doppelganger",
                "Character Modeus",
                "Character Succubus",
                "Character Shade",
                "Character Elder Vampire",
                "Character Wight",
                "Character Phantom Warrior",
                "Character Dreadwyrm",
                "Character Dracolich",
                "Character Uther",
                "Character Demon Uther",
                "Character Fat Imp",
                "Character Hubert de Lalye",
                "Character Yataa' Halli",
                "Character Nhiddog",
                "Character Mage Hugin",
                "Character Erhog the Dark",
                "Character Dark Elf Lyf",
                "Character Asteroth",
                "Character Primitive Giant",
                "Character Occultist",
                "Character Master Occultist",
                "Character Manticore",
                "Character Elf Queen",
                "Character Spirit of Fenrir",
                "Character Brown Bear",
                "Character Erhog the Necromancer",
                "Character Drega Zul",
                "Character Nebiros",
                "Character Drullia'an",
                "Character Gallean's Beast",
                "Character Lachla'an",
                "Character Dark Lachla'an",
                "Character Count Flamel Crowley",
                "Character Forest Liege",
                "Character Dryad",
                "Character Sage",
                "Character Centaur Charger",
                "Character Centaur Strider",
                "Character Centaur Savage",
                "Character Hunter",
                "Character Stinger",
                "Character Bandit",
                "Character Watchman",
                "Character Sentry",
                "Character Channeler",
                "Character Archon",
                "Character Theurgist",
                "Character Spiritess",
                "Character Grovemaiden",
                "Character Sun Dancer",
                "Character Sylph",
                "Character Ent Minor",
                "Character Ent",
                "Character Ent Major",
                "Character Verdant",
                "Character Dark Elf Butcher",
                "Character Dark Elf Reaper",
                "Character Dark Elf Crone",
                "Character Dark Elf Gast",
                "Character Trapper",
                "Character Shamaness",
                "Character Goblin Elder",
                "Character Gumtik Bledwater",
                "Character Sir Allemon",
                "Character Brigand",
                "Character Marauder",
                "Place Vogren's Tower",
                "Place Goblin Outpost",
                "Place Trent",
                "Place Hemwyk Court",
                "Place Luguss",
                "Place Umlugh",
                "Place Hearth's Grove",
                "Place Fallen Shrine of Gallean",
                "Place Ruins of Le'essnir",
                "Place Lachla'an's Tomb",
                "Place Gurthug",
                "Faction Empire",
                "Faction Mountain Clans",
                "Faction Legions of the Damned",
                "Faction Undead Hordes",
                "Faction Neutral",
                "Faction Elven Alliance",
                "Item Life Potion",
                "Item Potion of Protection",
                "Item Treebark Potion",
                "Item Iron Skin Potion",
                "Item Potion of Healing",
                "Item Potion of Restoration",
                "Item Highfather's Potion",
                "Item Potion of Striking",
                "Item Potion of Accuracy",
                "Item Potion of Fortune",
                "Item Potion of Swiftness",
                "Item Potion of Speed",
                "Item Quicksilver Potion",
                "Item Potion of Vigor",
                "Item Potion of Strength",
                "Item Titan's Might Potion",
                "Item Banner of Protection",
                "Item Banner of Resistance",
                "Item Banner of Striking",
                "Item Banner of Battle",
                "Item Banner of Speed",
                "Item Banner of Celerity",
                "Item Banner of Strength",
                "Item Banner of Might",
                "Item Elven Boots",
                "Item Boots of the Elements",
                "Item Runestone (Artifact)",
                "Item Holy Chalice (Artifact)",
                "Item Skull Bracers (Artifact)",
                "Item Horn of Awareness (Artifact)",
                "Item Etched Circlet (Artifact)",
                "Item Dwarven Bracer (Artifact)",
                "Item Unholy Chalice (Artifact)",
                "Item Ring of Strength (Artifact)",
                "Item Runic Blade (Artifact)",
                "Item Mjolnir's Crown (Artifact)",
                "Item Ring of the Ages (Artifact)",
                "Item Bethrezen's Claw (Artifact)",
                "Item Tome of Air",
                "Item Tome of Water",
                "Item Tome of Earth",
                "Item Air ward scroll",
                "Item Celerity scroll",
                "Item Strength scroll",
                "Item Lightning scroll",
                "Item Water ward scroll",
                "Item Haste scroll",
                "Item Healing scroll",
                "Item Summon I: Living armor scroll",
                "Item True sight scroll",
                "Item Earth ward scroll",
                "Item Mind ward scroll",
                "Item Holy armor scroll",
                "Item Holy strength scroll",
                "Item Wrath of god scroll",
                "Item Summon II: Golem scroll",
                "Item Fire ward scroll",
                "Item Call to arms scroll",
                "Item Death ward scroll",
                "Item Armageddon scroll",
                "Item Major healing scroll",
                "Item Ice shield scroll",
                "Item Forestwalk scroll",
                "Item Vithar's might scroll",
                "Item Blizzard scroll",
                "Item Summon I: Roc scroll",
                "Item Hymn of the Clans scroll",
                "Item Seafaring scroll",
                "Item Ice pillar scroll",
                "Item Sybil's vision scroll",
                "Item Summon II: Valkyrie scroll",
                "Item Chant of hasting scroll",
                "Item Tempest scroll",
                "Item Chant of arms scroll",
                "Item Chant of fortitude scroll",
                "Item Wotan's blessing scroll",
                "Item Vengeance of Ymir scroll",
                "Item Summon III: Stone ancestor scroll",
                "Item Incorruptible scroll",
                "Item Wotan's chant scroll",
                "Item Incantare Hellhound scroll",
                "Item Incantare Hellhound illudere scroll",
                "Item Ignis mare scroll",
                "Item Menta minoris scroll",
                "Item Tormentio scroll",
                "Item Incantare Beliarh scroll",
                "Item Incantare Beliarh illudere scroll",
                "Item Ignis carn scroll",
                "Item Cursa demoneus scroll",
                "Item Chronos scroll",
                "Item Sanctuera scroll",
                "Item Divis nocte scroll",
                "Item Paraseus scroll",
                "Item Menta potens scroll",
                "Item Tortio menta scroll",
                "Item Sinestra ignis scroll",
                "Item Projicere terra scroll",
                "Item Incantare Avenger scroll",
                "Item Incantare Avenger illudere scroll",
                "Item Deus talonis scroll",
                "Item Summon I: Skeleton scroll",
                "Item Pestilence scroll",
                "Item Ice storm scroll",
                "Item Weakness scroll",
                "Item Rust scroll",
                "Item Summon II: Evil Ent scroll",
                "Item Plague scroll",
                "Item Staff of Thunder",
                "Item Staff of Necromancy",
                "Item Staff of Holiness",
                "Item Staff of Paralyzing",
                "Item Staff of Travelling",
                "Item Staff of Twilight",
                "Item Staff of Light",
                "Item Staff of Invisibility",
                "Item Staff of Earth Elemental Control",
                "Item Staff of Demonology",
                "Item Bronze Ring (Valuable)",
                "Item Silver Ring (Valuable)",
                "Item Emerald (Valuable)",
                "Item Gold Ring (Valuable)",
                "Item Ruby (Valuable)",
                "Item Sapphire (Valuable)",
                "Item Diamond (Valuable)",
                "Item Ancient Relic (Valuable)",
                "Item Royal Scepter (Valuable)",
                "Item Imperial Crown (Valuable)",
                "Item Staff of Dragon mastering",
                "Item Staff of Celerity",
                "Item Spirit Staff",
                "Item Shadow scroll",
                "Item Curse of Nygrael scroll",
                "Item Stone rain scroll",
                "Item Summon III: Nightmare scroll",
                "Item Call decay dragon scroll",
                "Item Touch of Mortis scroll",
                "Item Fog of death scroll",
                "Item Terror scroll",
                "Item Rot scroll",
                "Item Call red dragon scroll",
                "Item Summon IV: Death scroll",
                "Item Damage ward scroll",
                "Item Hecatomb scroll",
                "Item Tiara of Purity (Artifact)",
                "Item Staff of Protection",
                "Item Tome of Fire",
                "Item Chain lightning scroll",
                "Item Highfather's blessing scroll",
                "Item Daylight scroll",
                "Item Winds of restoration scroll",
                "Item Ymir's blessing scroll",
                "Item Ice spirits scroll",
                "Item Winds of travel scroll",
                "Item Ancestor's call scroll",
                "Item Maledicere scroll",
                "Item Ignis potens scroll",
                "Item Protectio ignis scroll",
                "Item Illudere terra scroll",
                "Item Death storm scroll",
                "Item Psalm of death scroll",
                "Item Nightfall scroll",
                "Item Mortis' venom scroll",
                "Item Potion of Invulnerability",
                "Item Healing Ointment",
                "Item Potion of Celerity",
                "Item Potion of Might",
                "Item Potion of Air Warding",
                "Item Potion of Water Warding",
                "Item Potion of Earth Warding",
                "Item Potion of Fire Warding",
                "Item Banner of Fortitude",
                "Item Banner of War",
                "Item Soul Crystal (Artifact)",
                "Item Horn of Incubus (Artifact)",
                "Item Unholy Dagger (Artifact)",
                "Item Wight Blade (Artifact)",
                "Item Thanatos Blade (Artifact)",
                "Item Skull of Thanatos (Artifact)",
                "Item Hag's Ring (Artifact)",
                "Item Tome of War",
                "Item Tome of Arcanum",
                "Item Tome of Sorcery",
                "Item Highfather Staff",
                "Item Staff of Tempest",
                "Item Staff of Daylight",
                "Item Staff of Nightfall",
                "Item Boots of Speed",
                "Item Boots of Traveling",
                "Item Boots of Seven Leagues",
                "Item Squire Orb",
                "Item Angel Orb",
                "Item Venerable Warrior Orb",
                "Item Imp Orb",
                "Item Infernal Knight Orb",
                "Item Incubus Orb",
                "Item Zombie Orb",
                "Item Skeleton Champion Orb",
                "Item Vampire Orb",
                "Item Lich Orb",
                "Item Elder Vampire Orb",
                "Item Elf Lord Orb",
                "Item Goblin Orb",
                "Item Orc Orb",
                "Item Lizard Man Orb",
                "Item Orb of Healing",
                "Item Orb of Restoration",
                "Item Orb of Regeneration",
                "Item Orb of Life",
                "Item Orb of Vigor",
                "Item Orb of Strength",
                "Item Orb of Fire",
                "Item Orb of Inferno",
                "Item Orb of Nosferat",
                "Item Orb of Vampire",
                "Item Orb of Elder Vampires",
                "Item Orb of Thunder",
                "Item Orb of Lightning",
                "Item Orb of Earth",
                "Item Orb of Stone Rain",
                "Item Orb of Water",
                "Item Orb of Icefall",
                "Item Orb of Poison",
                "Item Orb of Venom",
                "Item Orb of Bane",
                "Item Orb of Thanatos",
                "Item Orb of Frost",
                "Item Orb of Freezing",
                "Item Orb of Fear",
                "Item Orb of Witches",
                "Item Squire Talisman",
                "Item Angel Talisman",
                "Item Venerable Warrior Talisman",
                "Item Imp Talisman",
                "Item Infernal Knight Talisman",
                "Item Incubus Talisman",
                "Item Zombie Talisman",
                "Item Skeleton Champion Talisman",
                "Item Vampire Talisman",
                "Item Lich Talisman",
                "Item Elder Vampire Talisman",
                "Item Elf Lord Talisman",
                "Item Goblin Talisman",
                "Item Orc Talisman",
                "Item Lizard Man Talisman",
                "Item Talisman of Healing",
                "Item Talisman of Restoration",
                "Item Talisman of Regeneration",
                "Item Talisman of Life",
                "Item Talisman of Vigor",
                "Item Talisman of Strength",
                "Item Talisman of Fire",
                "Item Talisman of Inferno",
                "Item Talisman of Nosferat",
                "Item Talisman of Vampire",
                "Item Talisman of Elder Vampires",
                "Item Talisman of Thunder",
                "Item Talisman of Lightning",
                "Item Talisman of Earth",
                "Item Talisman of Stone Rain",
                "Item Talisman of Water",
                "Item Talisman of Icefall",
                "Item Talisman of Poison",
                "Item Talisman of Venom",
                "Item Talisman of Bane",
                "Item Talisman of Thanatos",
                "Item Talisman of Frost",
                "Item Talisman of Freezing",
                "Item Talisman of Fear",
                "Item Talisman of Witches",
                "Item Agshlisga Totem",
                "Item Broken Key",
                "Item Wotan's Scroll",
                "Item Ymir's Purity Rune",
                "Item Aurva's Potency Rune",
                "Item Skadi's Liberty Rune",
                "Item Fregga's Healing Rune",
                "Item Idun's Prosperity Rune",
                "Item Braggi's Battle Rune",
                "Item Fulla's Gold Rune",
                "Item Ocean Charm",
                "Item Corrupted Rune",
                "Item Runic Key",
                "Item Holy Artifact",
                "Item Uther's Blood",
                "Item Soul Dagger",
                "Item Doragon's Eye",
                "Item Stone Ring",
                "Item Lachla'an's Head",
                "Item Lachla'an's Body",
                "Item Lachla'an's Heart",
                "Item Rusted Shackles (Artifact)",
                "Item Orb of Flames",
                "Item Talisman of Horror",
                "Item Orb of Weakening",
                "Item Banner of Health",
                "Item Tome of Thought",
                "Item Orb of Rage",
                "Item Lute of Charming (Artifact)",
                "Item Bramble Scroll",
                "Item Summon I: Ent Minor Scroll",
                "Item Tangle Scroll",
                "Item Swift Scroll",
                "Item Swarm Scroll",
                "Item Healing Scroll",
                "Item Summon II: Ent Scroll",
                "Item Singe Scroll",
                "Item Drown Scroll",
                "Item Confusion Scroll",
                "Item Create Grove Scroll",
                "Item Summon III: Ent Major Scroll",
                "Item Will-O-Wisp Scroll",
                "Item Recovery Scroll",
                "Item Distraction Scroll",
                "Item Avalanche Scroll",
                "Item Gust Scroll",
                "Item Mold Scroll",
                "Item Gallean's Curse Scroll",
                "Item Gallean's Boon Scroll",
                "Item Summon IV: Verdant Scroll",
                "Item Wrath of the Glades Scroll",
                "Item Sinkhole Scroll",
                "Item Create Forest Scroll",
                "Item Thorn Pendant",
                "Item Shield Pendant",
                "Item Gleaming Coin",
                "Item Bear Pelt",
                "Item Scroll Case",
                "Item Ember Salts",
                "Item Acidic Slime",
                "Item Clavis",
                "Item Staff of Foliage",
                "Item Staff of Fumbling",
                "Item Staff of Treecalling"
            ]));
    }
}
