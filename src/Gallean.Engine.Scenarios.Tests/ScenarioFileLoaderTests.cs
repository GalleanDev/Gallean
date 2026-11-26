namespace Gallean.Engine.Scenarios.Tests;

public sealed class ScenarioFileLoaderTests
{
    private const int SaveVersionOffset = 16;

    [Test]
    public void LoadExportFormatTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Format, Is.EqualTo(ScenarioFileFormat.Scenario));
    }

    [Test]
    public void LoadSaveFormatTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Format, Is.EqualTo(ScenarioFileFormat.Save(35)));
    }

    [Test]
    public void LoadExportScenarioIdTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.ScenarioId, Is.EqualTo(new Db.ScenarioId("S417SC0000")));
    }

    [Test]
    public void LoadExportInfoTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioInfos, Is.EqualTo([
            new Db.ScenarioInfo(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417IF0000"),
                CurrentTurn: 0,
                MapSeed: 20261125,
                MapSize: 48,
                QuantityCities: 3,
                Creator: "Gallean Contributors",
                SourceM: false,
                DifficultyGame: Db.DifficultyType.Average,
                DifficultyScenario: Db.DifficultyType.Hard,
                CampaignId: new Db.Id("C000CC0001"),
                MaxUnit: 3,
                MaxLeader: 3,
                MaxCity: 4,
                MaxSpell: 3,
                Player1: 4,
                Player2: 3,
                Player3: 1,
                Player4: 99,
                Player5: 99,
                Player6: 99,
                Player7: 99,
                Player8: 99,
                Player9: 99,
                Player10: 99,
                Player11: 99,
                Player12: 99,
                Player13: 99,
                Name: "Greywater Crossing",
                Description: "The Mountain Clans and the Undead Hordes both covet the only ford across the Greywater. Whoever holds Brinewatch holds the crossing.",
                Briefing: "Capture Brinewatch and hold the ford.",
                BriefingLong1: "Thane Halgrim has marched his clan down from the passes to claim the Greywater ford before winter closes the roads.",
                BriefingLong2: " Across the river, Mistress Ysolde gathers the dead of Mourncairn for the same purpose.",
                BriefingLong3: null,
                BriefingLong4: null,
                BriefingLong5: null,
                DebunkWin: "The ford is yours, and the Greywater runs quiet again.",
                DebunkWin2: " The clans will remember this crossing.",
                DebunkWin3: null,
                DebunkWin4: null,
                DebunkWin5: null,
                DebunkLose: "The crossing is lost, and with it the road home.",
                SuggestedLevel: 2,
                O: false)
        ]));
    }

    [Test]
    public void LoadExportPlayersTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioPlayers, Is.EqualTo([
            new Db.ScenarioPlayer(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417PL0000"),
                new Db.Id("G000RR0004"),
                new Db.Id("G000LR0013"),
                new Db.Id("S417FG0000"),
                ResearchTurn: 0,
                ConstructTurn: 0,
                BuildsId: new Db.Id("S417PB0000"),
                KnownId: new Db.Id("S417KS0000"),
                Bank: new Db.ResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                SpellBank: new Db.ResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                Attitude: Db.AiAttitudeType.Medium,
                Face: 1,
                Spy1Id: null,
                Spy2Id: null,
                Spy3Id: null,
                QuantityBreaks: 0,
                IsHuman: false,
                CapturedById: null,
                AlwaysAi: false,
                ExMapId1: null,
                ExMapId2: null,
                ExMapId3: null,
                ExMapTurn1: 0,
                ExMapTurn2: 0,
                ExMapTurn3: 0),
            new Db.ScenarioPlayer(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417PL0001"),
                new Db.Id("G000RR0001"),
                new Db.Id("G000LR0004"),
                new Db.Id("S417FG0001"),
                ResearchTurn: 0,
                ConstructTurn: 0,
                BuildsId: new Db.Id("S417PB0001"),
                KnownId: new Db.Id("S417KS0001"),
                Bank: new Db.ResourceSet(Gold: 800, DeathMana: 0, RuneMana: 300, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                SpellBank: new Db.ResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                Attitude: Db.AiAttitudeType.Medium,
                Face: 0,
                Spy1Id: null,
                Spy2Id: null,
                Spy3Id: null,
                QuantityBreaks: 0,
                IsHuman: false,
                CapturedById: null,
                AlwaysAi: false,
                ExMapId1: null,
                ExMapId2: null,
                ExMapId3: null,
                ExMapTurn1: 0,
                ExMapTurn2: 0,
                ExMapTurn3: 0),
            new Db.ScenarioPlayer(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417PL0002"),
                new Db.Id("G000RR0003"),
                new Db.Id("G000LR0012"),
                new Db.Id("S417FG0002"),
                ResearchTurn: 0,
                ConstructTurn: 0,
                BuildsId: new Db.Id("S417PB0002"),
                KnownId: new Db.Id("S417KS0002"),
                Bank: new Db.ResourceSet(Gold: 600, DeathMana: 250, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                SpellBank: new Db.ResourceSet(Gold: 100, DeathMana: 50, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                Attitude: Db.AiAttitudeType.Large,
                Face: 2,
                Spy1Id: null,
                Spy2Id: null,
                Spy3Id: null,
                QuantityBreaks: 0,
                IsHuman: false,
                CapturedById: null,
                AlwaysAi: false,
                ExMapId1: null,
                ExMapId2: null,
                ExMapId3: null,
                ExMapTurn1: 0,
                ExMapTurn2: 0,
                ExMapTurn3: 0)
        ]));
    }

    [Test]
    public void LoadExportSubracesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioSubraces, Is.EqualTo([
            new Db.ScenarioSubrace(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SR0000"), new Db.Id("S417PL0000"), Subrace: 5, Banner: 4),
            new Db.ScenarioSubrace(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SR0001"), new Db.Id("S417PL0001"), Subrace: 4, Banner: 3),
            new Db.ScenarioSubrace(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SR0002"), new Db.Id("S417PL0002"), Subrace: 2, Banner: 1),
            new Db.ScenarioSubrace(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SR0003"), new Db.Id("S417PL0000"), Subrace: 6, Banner: 5),
            new Db.ScenarioSubrace(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SR0004"), new Db.Id("S417PL0000"), Subrace: 8, Banner: 7),
            new Db.ScenarioSubrace(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SR0005"), new Db.Id("S417PL0000"), Subrace: 13, Banner: 12),
            new Db.ScenarioSubrace(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SR0006"), new Db.Id("S417PL0000"), Subrace: 0, Banner: 6)
        ]));
    }

    [Test]
    public void LoadExportRelationsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioRelations, Is.EqualTo([
            new Db.ScenarioRelation(new Db.ScenarioId("S417SC0000"), Race1: Db.RaceType.Neutral, Race2: Db.RaceType.Dwarf, Relation: 0),
            new Db.ScenarioRelation(new Db.ScenarioId("S417SC0000"), Race1: Db.RaceType.Neutral, Race2: Db.RaceType.Undead, Relation: 0),
            new Db.ScenarioRelation(new Db.ScenarioId("S417SC0000"), Race1: Db.RaceType.Dwarf, Race2: Db.RaceType.Undead, Relation: 0)
        ]));
    }

    [Test]
    public void LoadExportVariablesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioVariables, Is.EqualTo([
            new Db.ScenarioVariable(new Db.ScenarioId("S417SC0000"), Id: 0, Name: "FerryTolls", Value: 0),
            new Db.ScenarioVariable(new Db.ScenarioId("S417SC0000"), Id: 1, Name: "BellsRung", Value: 5)
        ]));
    }

    [Test]
    public void LoadExportSpellsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioSpells, Is.EqualTo([
            new Db.ScenarioSpell(new Db.ScenarioId("S417SC0000"), new Db.Id("S417KS0001"), new Db.Id("G000SS0021")),
            new Db.ScenarioSpell(new Db.ScenarioId("S417SC0000"), new Db.Id("S417KS0002"), new Db.Id("G000SS0061")),
            new Db.ScenarioSpell(new Db.ScenarioId("S417SC0000"), new Db.Id("S417KS0002"), new Db.Id("G000SS0064"))
        ]));
    }

    [Test]
    public void LoadExportUnitsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioUnits, Is.EqualTo([
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0000"), Hp: 900, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU3002"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0001"), Hp: 225, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0044"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0002"), Hp: 150, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0036"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0003"), Hp: 65, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0026"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0004"), Hp: 900, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU3004"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0005"), Hp: 65, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0098"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0006"), Hp: 120, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0086"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0007"), Hp: 45, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0078"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0008"), Hp: 140, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5103"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0009"), Hp: 95, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5002"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN000a"), Hp: 40, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5001"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN000b"), Hp: 80, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5002"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN000c"), Hp: 200, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5113"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN000d"), Hp: 40, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5018"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN000e"), Hp: 50, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5017"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN000f"), Hp: 180, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5139"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0010"), Hp: 180, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5039"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0011"), Hp: 150, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5033"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0012"), Hp: 420, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5036"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0013"), Hp: 150, Xp: 40, Creation: 1, TypeId: new Db.Id("G000UU0047"), Level: 2, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0014"), Hp: 60, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0033"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0015"), Hp: 90, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0097"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0016"), Hp: 100, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0092"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0017"), Hp: 600, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5123"), Level: 1, Transf: false, DynLevel: false)
        ]));
    }

    [Test]
    public void LoadExportUnitModifiersTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioUnitModifiers, Is.EqualTo([
            new Db.ScenarioUnitModifier(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0013"), new Db.Id("G000UM9031"))
        ]));
    }

    [Test]
    public void LoadExportUnitGroupsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioUnitGroups, Is.EqualTo([
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0000"),
                Unit0Id: new Db.Id("S417UN0000"),
                Unit1Id: null,
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: 0,
                Pos3: -1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0001"),
                Unit0Id: new Db.Id("S417UN0004"),
                Unit1Id: null,
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: 0,
                Pos3: -1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0002"),
                Unit0Id: new Db.Id("S417UN000a"),
                Unit1Id: new Db.Id("S417UN000b"),
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: 0,
                Pos1: -1,
                Pos2: -1,
                Pos3: 1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0003"),
                Unit0Id: null,
                Unit1Id: null,
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: -1,
                Pos3: -1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0004"),
                Unit0Id: null,
                Unit1Id: null,
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: -1,
                Pos3: -1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0000"),
                Unit0Id: new Db.Id("S417UN0002"),
                Unit1Id: new Db.Id("S417UN0003"),
                Unit2Id: new Db.Id("S417UN0001"),
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: 0,
                Pos1: 1,
                Pos2: 2,
                Pos3: -1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0001"),
                Unit0Id: new Db.Id("S417UN0006"),
                Unit1Id: new Db.Id("S417UN0005"),
                Unit2Id: new Db.Id("S417UN0007"),
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: 0,
                Pos1: -1,
                Pos2: -1,
                Pos3: 1,
                Pos4: -1,
                Pos5: 2),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0002"),
                Unit0Id: new Db.Id("S417UN0008"),
                Unit1Id: new Db.Id("S417UN0009"),
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: 0,
                Pos3: -1,
                Pos4: 1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0003"),
                Unit0Id: new Db.Id("S417UN000e"),
                Unit1Id: new Db.Id("S417UN000c"),
                Unit2Id: new Db.Id("S417UN000d"),
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: 0,
                Pos1: -1,
                Pos2: 1,
                Pos3: 2,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0004"),
                Unit0Id: new Db.Id("S417UN000f"),
                Unit1Id: new Db.Id("S417UN0010"),
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: 0,
                Pos3: -1,
                Pos4: 1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0005"),
                Unit0Id: new Db.Id("S417UN0013"),
                Unit1Id: new Db.Id("S417UN0014"),
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: 0,
                Pos3: 1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0006"),
                Unit0Id: new Db.Id("S417UN0016"),
                Unit1Id: new Db.Id("S417UN0015"),
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: 0,
                Pos1: -1,
                Pos2: -1,
                Pos3: 1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0007"),
                Unit0Id: new Db.Id("S417UN0017"),
                Unit1Id: null,
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: 0,
                Pos3: -1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417RU0000"),
                Unit0Id: new Db.Id("S417UN0011"),
                Unit1Id: new Db.Id("S417UN0012"),
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: 0,
                Pos1: -1,
                Pos2: 1,
                Pos3: -1,
                Pos4: -1,
                Pos5: -1),
            new Db.ScenarioUnitGroup(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417RU0001"),
                Unit0Id: null,
                Unit1Id: null,
                Unit2Id: null,
                Unit3Id: null,
                Unit4Id: null,
                Unit5Id: null,
                Pos0: -1,
                Pos1: -1,
                Pos2: -1,
                Pos3: -1,
                Pos4: -1,
                Pos5: -1)
        ]));
    }

    [Test]
    public void LoadExportItemsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioItems, Is.EqualTo([
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0000"), new Db.Id("G000IG0001")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0001"), new Db.Id("G000IG0005")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0002"), new Db.Id("G000IG0005")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0003"), new Db.Id("G000IG1001")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0004"), new Db.Id("G000IG1010")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0005"), new Db.Id("G000IG2001")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0006"), new Db.Id("G000IG4001")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0007"), new Db.Id("G000IG9116")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0008"), new Db.Id("G000IG0006")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0009"), new Db.Id("G000IG7001")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM000a"), new Db.Id("G000IG5004")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM000b"), new Db.Id("G000IG9114")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM000c"), new Db.Id("G000IG7002")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM000d"), new Db.Id("G000IG6001")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM000e"), new Db.Id("G000IG9016")),
            new Db.ScenarioItem(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM000f"), new Db.Id("G000IG3001"))
        ]));
    }

    [Test]
    public void LoadExportInventoriesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioInventories, Is.EqualTo([
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0000"), new Db.Id("S417IM0000")),
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0000"), new Db.Id("S417IM0001")),
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0001"), new Db.Id("S417IM0002")),
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0003"), new Db.Id("S417IM000c")),
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417KC0005"), new Db.Id("S417IM0008")),
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417KC0006"), new Db.Id("S417IM000e")),
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417BG0000"), new Db.Id("S417IM0009")),
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417BG0000"), new Db.Id("S417IM000a")),
            new Db.ScenarioInventory(new Db.ScenarioId("S417SC0000"), new Db.Id("S417BG0001"), new Db.Id("S417IM000b"))
        ]));
    }

    [Test]
    public void LoadExportTalismanChargesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioTalismanCharges, Is.EqualTo([
            new Db.ScenarioTalismanCharges(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM0007"), Charges: 3),
            new Db.ScenarioTalismanCharges(new Db.ScenarioId("S417SC0000"), new Db.Id("S417IM000b"), Charges: 5)
        ]));
    }

    [Test]
    public void LoadExportStacksTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioStacks, Is.EqualTo([
            new Db.ScenarioStack(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0000"),
                SourceTemplateId: null,
                PositionX: 2,
                PositionY: 2,
                LeaderId: new Db.Id("S417UN0001"),
                LeaderAlive: true,
                Morale: 0,
                Move: 20,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S417PL0001"),
                SubraceId: new Db.Id("S417SR0001"),
                InsideId: new Db.Id("S417FT0000"),
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.Normal,
                OrderTargetId: null,
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0001"),
                SourceTemplateId: null,
                PositionX: 41,
                PositionY: 41,
                LeaderId: new Db.Id("S417UN0005"),
                LeaderAlive: true,
                Morale: 0,
                Move: 20,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S417PL0002"),
                SubraceId: new Db.Id("S417SR0002"),
                InsideId: new Db.Id("S417FT0001"),
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.Normal,
                OrderTargetId: null,
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0002"),
                SourceTemplateId: null,
                PositionX: 20,
                PositionY: 21,
                LeaderId: new Db.Id("S417UN0008"),
                LeaderAlive: true,
                Morale: 0,
                Move: 25,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S417PL0000"),
                SubraceId: new Db.Id("S417SR0006"),
                InsideId: new Db.Id("S417FT0002"),
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.DefendCity,
                OrderTargetId: new Db.Id("S417FT0002"),
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0003"),
                SourceTemplateId: null,
                PositionX: 24,
                PositionY: 31,
                LeaderId: new Db.Id("S417UN000c"),
                LeaderAlive: true,
                Morale: 0,
                Move: 20,
                Facing: 3,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S417PL0000"),
                SubraceId: new Db.Id("S417SR0004"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.DefendLocation,
                OrderTargetId: new Db.Id("S417LO0000"),
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0004"),
                SourceTemplateId: null,
                PositionX: 8,
                PositionY: 24,
                LeaderId: new Db.Id("S417UN000f"),
                LeaderAlive: true,
                Morale: 0,
                Move: 35,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S417PL0000"),
                SubraceId: new Db.Id("S417SR0005"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 1,
                Order: Db.OrderType.Roam,
                OrderTargetId: null,
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0005"),
                SourceTemplateId: null,
                PositionX: 9,
                PositionY: 12,
                LeaderId: new Db.Id("S417UN0013"),
                LeaderAlive: true,
                Morale: 0,
                Move: 25,
                Facing: 5,
                BannerId: new Db.Id("S417IM0003"),
                TomeId: new Db.Id("S417IM0006"),
                Battle1Id: new Db.Id("S417IM0007"),
                Battle2Id: new Db.Id("S417IM000d"),
                Artifact1Id: new Db.Id("S417IM0005"),
                Artifact2Id: new Db.Id("S417IM000f"),
                BootsId: new Db.Id("S417IM0004"),
                OwnerId: new Db.Id("S417PL0001"),
                SubraceId: new Db.Id("S417SR0001"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 1,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.Normal,
                OrderTargetId: null,
                CreationLevel: 2,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0006"),
                SourceTemplateId: null,
                PositionX: 38,
                PositionY: 36,
                LeaderId: new Db.Id("S417UN0015"),
                LeaderAlive: true,
                Morale: 0,
                Move: 35,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S417PL0002"),
                SubraceId: new Db.Id("S417SR0002"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.SecureCity,
                OrderTargetId: new Db.Id("S417FT0003"),
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.AttackStack,
                AiOrderTargetId: new Db.Id("S417KC0005")),
            new Db.ScenarioStack(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417KC0007"),
                SourceTemplateId: null,
                PositionX: 44,
                PositionY: 20,
                LeaderId: new Db.Id("S417UN0017"),
                LeaderAlive: true,
                Morale: 0,
                Move: 30,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S417PL0000"),
                SubraceId: new Db.Id("S417SR0000"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: true,
                AiPriority: 3,
                Order: Db.OrderType.Stand,
                OrderTargetId: null,
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null)
        ]));
    }

    [Test]
    public void LoadExportStackTemplatesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioStackTemplates, Is.EqualTo([
            new Db.ScenarioStackTemplate(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417TM0000"),
                new Db.Id("G000UU5112"),
                LeaderLevel: 2,
                OwnerId: new Db.Id("S417PL0000"),
                SubraceId: new Db.Id("S417SR0004"),
                Unit0Id: new Db.Id("G000UU5112"),
                Unit0Level: 2,
                Unit1Id: new Db.Id("G000UU5017"),
                Unit1Level: 1,
                Unit2Id: new Db.Id("G000UU5018"),
                Unit2Level: 1,
                Unit3Id: null,
                Unit3Level: 0,
                Unit4Id: null,
                Unit4Level: 0,
                Unit5Id: null,
                Unit5Level: 0,
                Position0: 1,
                Position1: -1,
                Position2: 0,
                Position3: 2,
                Position4: -1,
                Position5: -1,
                Order: Db.OrderType.Berserk,
                OrderTargetId: null,
                UseFacing: false,
                Facing: 0,
                AiPriority: 4),
            new Db.ScenarioStackTemplate(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417TM0001"),
                new Db.Id("G000UU0099"),
                LeaderLevel: 1,
                OwnerId: new Db.Id("S417PL0002"),
                SubraceId: new Db.Id("S417SR0002"),
                Unit0Id: new Db.Id("G000UU0099"),
                Unit0Level: 1,
                Unit1Id: new Db.Id("G000UU0078"),
                Unit1Level: 1,
                Unit2Id: null,
                Unit2Level: 0,
                Unit3Id: null,
                Unit3Level: 0,
                Unit4Id: null,
                Unit4Level: 0,
                Unit5Id: null,
                Unit5Level: 0,
                Position0: -1,
                Position1: 1,
                Position2: 0,
                Position3: -1,
                Position4: -1,
                Position5: -1,
                Order: Db.OrderType.MoveToLocation,
                OrderTargetId: new Db.Id("S417LO0002"),
                UseFacing: true,
                Facing: 5,
                AiPriority: 3)
        ]));
    }

    [Test]
    public void LoadExportStackTemplateModifiersTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioStackTemplateModifiers, Is.EqualTo([
            new Db.ScenarioStackTemplateModifier(new Db.ScenarioId("S417SC0000"), new Db.Id("S417TM0000"), new Db.Id("G000UM9001"), UnitPos: 2)
        ]));
    }

    [Test]
    public void LoadExportFortificationsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioFortifications, Is.EqualTo([
            new Db.ScenarioFortification(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0000"),
                PositionX: 2,
                PositionY: 2,
                OwnerId: new Db.Id("S417PL0001"),
                SubraceId: new Db.Id("S417SR0001"),
                Stack: new Db.Id("S417KC0000"),
                Size: null,
                SizeMax: null,
                RegenerationBonus: null,
                ProtectionBuildingId: null,
                Morale: null,
                GrowthTurn: null,
                ProtectedFromUndead: null,
                ProtectedFromHeretic: null,
                ProtectedFromHuman: null,
                ProtectedFromDwarf: null,
                ProtectedFromElf: null,
                AiPriority: 3,
                RiotTurns: null),
            new Db.ScenarioFortification(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0001"),
                PositionX: 41,
                PositionY: 41,
                OwnerId: new Db.Id("S417PL0002"),
                SubraceId: new Db.Id("S417SR0002"),
                Stack: new Db.Id("S417KC0001"),
                Size: null,
                SizeMax: null,
                RegenerationBonus: null,
                ProtectionBuildingId: null,
                Morale: null,
                GrowthTurn: null,
                ProtectedFromUndead: null,
                ProtectedFromHeretic: null,
                ProtectedFromHuman: null,
                ProtectedFromDwarf: null,
                ProtectedFromElf: null,
                AiPriority: 3,
                RiotTurns: null),
            new Db.ScenarioFortification(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0002"),
                PositionX: 20,
                PositionY: 21,
                OwnerId: new Db.Id("S417PL0000"),
                SubraceId: new Db.Id("S417SR0003"),
                Stack: new Db.Id("S417KC0002"),
                Size: 2,
                SizeMax: null,
                RegenerationBonus: 10,
                ProtectionBuildingId: null,
                Morale: 0,
                GrowthTurn: 0,
                ProtectedFromUndead: true,
                ProtectedFromHeretic: false,
                ProtectedFromHuman: false,
                ProtectedFromDwarf: false,
                ProtectedFromElf: false,
                AiPriority: 3,
                RiotTurns: 0),
            new Db.ScenarioFortification(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0003"),
                PositionX: 34,
                PositionY: 32,
                OwnerId: new Db.Id("S417PL0000"),
                SubraceId: new Db.Id("S417SR0000"),
                Stack: null,
                Size: 1,
                SizeMax: null,
                RegenerationBonus: 0,
                ProtectionBuildingId: null,
                Morale: 0,
                GrowthTurn: 0,
                ProtectedFromUndead: false,
                ProtectedFromHeretic: false,
                ProtectedFromHuman: false,
                ProtectedFromDwarf: false,
                ProtectedFromElf: false,
                AiPriority: 6,
                RiotTurns: 0),
            new Db.ScenarioFortification(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417FT0004"),
                PositionX: 30,
                PositionY: 40,
                OwnerId: new Db.Id("S417PL0002"),
                SubraceId: new Db.Id("S417SR0002"),
                Stack: null,
                Size: 3,
                SizeMax: null,
                RegenerationBonus: 15,
                ProtectionBuildingId: null,
                Morale: 0,
                GrowthTurn: 2,
                ProtectedFromUndead: false,
                ProtectedFromHeretic: false,
                ProtectedFromHuman: true,
                ProtectedFromDwarf: true,
                ProtectedFromElf: false,
                AiPriority: 4,
                RiotTurns: 1)
        ]));
    }

    [Test]
    public void LoadExportRuinsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioRuins, Is.EqualTo([
            new Db.ScenarioRuin(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RU0000"), Image: 2, PositionX: 10, PositionY: 30, Cash: "G0150:R0000:Y0000:E0000:W0000:B0000", Item: new Db.Id("G000IG2002"), Looter: null, AiPriority: 3),
            new Db.ScenarioRuin(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RU0001"), Image: 4, PositionX: 33, PositionY: 10, Cash: "G0200:R0000:Y0000:E0000:W0050:B0000", Item: new Db.Id("G000IG5007"), Looter: null, AiPriority: 5)
        ]));
    }

    [Test]
    public void LoadExportSitesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioSites, Is.EqualTo([
            new Db.ScenarioSite(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417SI0000"),
                ImageIso: 0,
                ImageInterface: null,
                VisiterId: null,
                PositionX: 26,
                PositionY: 20,
                QuantityVisit: null,
                QuantitySpell: null,
                QuantityUnit: null,
                QuantityItem: 3,
                BuyArmor: true,
                BuyJewel: true,
                BuyWeapon: false,
                BuyBanner: true,
                BuyPotion: true,
                BuyScroll: true,
                BuyWand: false,
                BuyValue: true,
                Mission: false,
                AiPriority: 3),
            new Db.ScenarioSite(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417SI0001"),
                ImageIso: 1,
                ImageInterface: "MAGE01",
                VisiterId: null,
                PositionX: 6,
                PositionY: 36,
                QuantityVisit: null,
                QuantitySpell: 2,
                QuantityUnit: null,
                QuantityItem: null,
                BuyArmor: null,
                BuyJewel: null,
                BuyWeapon: null,
                BuyBanner: null,
                BuyPotion: null,
                BuyScroll: null,
                BuyWand: null,
                BuyValue: null,
                Mission: null,
                AiPriority: 2),
            new Db.ScenarioSite(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417SI0002"),
                ImageIso: 2,
                ImageInterface: null,
                VisiterId: null,
                PositionX: 14,
                PositionY: 6,
                QuantityVisit: null,
                QuantitySpell: null,
                QuantityUnit: null,
                QuantityItem: null,
                BuyArmor: null,
                BuyJewel: null,
                BuyWeapon: null,
                BuyBanner: null,
                BuyPotion: null,
                BuyScroll: null,
                BuyWand: null,
                BuyValue: null,
                Mission: null,
                AiPriority: 3),
            new Db.ScenarioSite(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417SI0003"),
                ImageIso: 0,
                ImageInterface: null,
                VisiterId: null,
                PositionX: 38,
                PositionY: 22,
                QuantityVisit: null,
                QuantitySpell: null,
                QuantityUnit: 2,
                QuantityItem: null,
                BuyArmor: null,
                BuyJewel: null,
                BuyWeapon: null,
                BuyBanner: null,
                BuyPotion: null,
                BuyScroll: null,
                BuyWand: null,
                BuyValue: null,
                Mission: null,
                AiPriority: 3),
            new Db.ScenarioSite(
                new Db.ScenarioId("S417SC0000"),
                new Db.Id("S417SI0004"),
                ImageIso: 0,
                ImageInterface: null,
                VisiterId: null,
                PositionX: 16,
                PositionY: 40,
                QuantityVisit: null,
                QuantitySpell: null,
                QuantityUnit: null,
                QuantityItem: null,
                BuyArmor: null,
                BuyJewel: null,
                BuyWeapon: null,
                BuyBanner: null,
                BuyPotion: null,
                BuyScroll: null,
                BuyWand: null,
                BuyValue: null,
                Mission: null,
                AiPriority: 3)
        ]));
    }

    [Test]
    public void LoadExportMerchantsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioMerchants, Is.EqualTo([
            new Db.ScenarioMerchant(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0000"), new Db.Id("G000IG0001"), ItemCount: 2),
            new Db.ScenarioMerchant(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0000"), new Db.Id("G000IG0005"), ItemCount: 5),
            new Db.ScenarioMerchant(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0000"), new Db.Id("G000IG5002"), ItemCount: 1)
        ]));
    }

    [Test]
    public void LoadExportMagesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioMages, Is.EqualTo([
            new Db.ScenarioMage(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0001"), new Db.Id("G000SS0001")),
            new Db.ScenarioMage(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0001"), new Db.Id("G000SS0007"))
        ]));
    }

    [Test]
    public void LoadExportMercenariesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioMercenaries, Is.EqualTo([
            new Db.ScenarioMercenary(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0003"), new Db.Id("G000UU5003"), UnitLevel: 1, UnitUniq: false),
            new Db.ScenarioMercenary(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0003"), new Db.Id("G000UU5015"), UnitLevel: 2, UnitUniq: true)
        ]));
    }

    [Test]
    public void LoadExportBagsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioBags, Is.EqualTo([
            new Db.ScenarioBag(new Db.ScenarioId("S417SC0000"), new Db.Id("S417BG0000"), PositionX: 18, PositionY: 12, ObjValue: null, Image: 0, AiPriority: 3),
            new Db.ScenarioBag(new Db.ScenarioId("S417SC0000"), new Db.Id("S417BG0001"), PositionX: 36, PositionY: 38, ObjValue: null, Image: 1, AiPriority: 2)
        ]));
    }

    [Test]
    public void LoadExportCrystalsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioCrystals, Is.EqualTo([
            new Db.ScenarioCrystal(new Db.ScenarioId("S417SC0000"), new Db.Id("S417CR0000"), Resource: 0, PositionX: 9, PositionY: 5, AiPriority: 3),
            new Db.ScenarioCrystal(new Db.ScenarioId("S417SC0000"), new Db.Id("S417CR0001"), Resource: 4, PositionX: 4, PositionY: 10, AiPriority: 3),
            new Db.ScenarioCrystal(new Db.ScenarioId("S417SC0000"), new Db.Id("S417CR0002"), Resource: 3, PositionX: 38, PositionY: 43, AiPriority: 3),
            new Db.ScenarioCrystal(new Db.ScenarioId("S417SC0000"), new Db.Id("S417CR0003"), Resource: 0, PositionX: 28, PositionY: 28, AiPriority: 5),
            new Db.ScenarioCrystal(new Db.ScenarioId("S417SC0000"), new Db.Id("S417CR0004"), Resource: 2, PositionX: 44, PositionY: 14, AiPriority: 3),
            new Db.ScenarioCrystal(new Db.ScenarioId("S417SC0000"), new Db.Id("S417CR0005"), Resource: 1, PositionX: 15, PositionY: 45, AiPriority: 3),
            new Db.ScenarioCrystal(new Db.ScenarioId("S417SC0000"), new Db.Id("S417CR0006"), Resource: 5, PositionX: 3, PositionY: 20, AiPriority: 3)
        ]));
    }

    [Test]
    public void LoadExportLandmarksTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioLandmarks, Is.EqualTo([
            new Db.ScenarioLandmark(new Db.ScenarioId("S417SC0000"), new Db.Id("S417MM0000"), new Db.Id("G000MG0004"), PositionX: 24, PositionY: 14, DescriptionText: null),
            new Db.ScenarioLandmark(new Db.ScenarioId("S417SC0000"), new Db.Id("S417MM0001"), new Db.Id("G000MG0008"), PositionX: 28, PositionY: 4, DescriptionText: null),
            new Db.ScenarioLandmark(new Db.ScenarioId("S417SC0000"), new Db.Id("S417MM0002"), new Db.Id("G000MG0046"), PositionX: 12, PositionY: 22, DescriptionText: null)
        ]));
    }

    [Test]
    public void LoadExportLocationsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioLocations, Is.EqualTo([
            new Db.ScenarioLocation(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0000"), PositionX: 23, PositionY: 27, Radius: 1),
            new Db.ScenarioLocation(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0001"), PositionX: 10, PositionY: 10, Radius: 3),
            new Db.ScenarioLocation(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0002"), PositionX: 36, PositionY: 36, Radius: 3),
            new Db.ScenarioLocation(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0003"), PositionX: 11, PositionY: 34, Radius: 1),
            new Db.ScenarioLocation(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0004"), PositionX: 40, PositionY: 5, Radius: 2)
        ]));
    }

    [Test]
    public void LoadExportRoadsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioRoads, Is.EqualTo([
            new Db.ScenarioRoad(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RA0000"), PositionX: 7, PositionY: 7, Index: 8, Var: 0),
            new Db.ScenarioRoad(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RA0001"), PositionX: 8, PositionY: 8, Index: 10, Var: 0),
            new Db.ScenarioRoad(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RA0002"), PositionX: 9, PositionY: 9, Index: 8, Var: 1),
            new Db.ScenarioRoad(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RA0003"), PositionX: 10, PositionY: 10, Index: 2, Var: 0)
        ]));
    }

    [Test]
    public void LoadExportMountainsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioMountains, Is.EqualTo([
            new Db.ScenarioMountain(new Db.ScenarioId("S417SC0000"), IdMountain: 1, SizeX: 5, SizeY: 5, PositionX: 40, PositionY: 8, Image: 2, Race: 4),
            new Db.ScenarioMountain(new Db.ScenarioId("S417SC0000"), IdMountain: 2, SizeX: 3, SizeY: 3, PositionX: 2, PositionY: 40, Image: 5, Race: 4),
            new Db.ScenarioMountain(new Db.ScenarioId("S417SC0000"), IdMountain: 3, SizeX: 2, SizeY: 2, PositionX: 30, PositionY: 14, Image: 1, Race: 3),
            new Db.ScenarioMountain(new Db.ScenarioId("S417SC0000"), IdMountain: 4, SizeX: 1, SizeY: 1, PositionX: 46, PositionY: 30, Image: 0, Race: 4)
        ]));
    }

    [Test]
    public void LoadExportRodsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioRods, Is.Empty);
    }

    [Test]
    public void LoadExportEventUnfogsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioEventUnfogs, Is.EqualTo([
            new Db.ScenarioEventUnfog(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0000"), new Db.Id("S417PL0001")),
            new Db.ScenarioEventUnfog(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0000"), new Db.Id("S417PL0002"))
        ]));
    }

    [Test]
    public void LoadExportTextsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterCrossingPath, TestScenarios.Encoding);

        Assert.That(file.Tables.TextScenarios, Is.EqualTo([
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417PL0000"), IdString: "NAME_TXT", Text: "Free Folk", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417PL0000"), IdString: "DESC_TXT", Text: "Ferrymen, marsh raiders and whatever else the river shelters.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417PL0001"), IdString: "NAME_TXT", Text: "Thane Halgrim", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417PL0001"), IdString: "DESC_TXT", Text: "A stubborn clan lord who counts every toll the river owes him.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417PL0002"), IdString: "NAME_TXT", Text: "Mistress Ysolde", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417PL0002"), IdString: "DESC_TXT", Text: "She was drowned at the ford once and has not forgiven it.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SR0006"), IdString: "NAME_TXT", Text: "Ferry Wardens", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0001"), IdString: "NAME_TXT", Text: "Halgrim", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0005"), IdString: "NAME_TXT", Text: "Ysolde", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0008"), IdString: "NAME_TXT", Text: "Warden Orlec", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN000c"), IdString: "NAME_TXT", Text: "Grukk", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN000f"), IdString: "NAME_TXT", Text: "Greymane", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0013"), IdString: "NAME_TXT", Text: "Borrin", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0015"), IdString: "NAME_TXT", Text: "Vessa", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417UN0017"), IdString: "NAME_TXT", Text: "The Reed Wyrm", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0000"), IdString: "NAME_TXT", Text: "Halgrim's Deep", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0000"), IdString: "DESC_TXT", Text: "Seat of the Greywater clans.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0001"), IdString: "NAME_TXT", Text: "Mourncairn", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0002"), IdString: "NAME_TXT", Text: "Brinewatch", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0002"), IdString: "DESC_TXT", Text: "A ferry town on the north bank.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0003"), IdString: "NAME_TXT", Text: "Coldharbour", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0004"), IdString: "NAME_TXT", Text: "Gallowmere", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417FT0004"), IdString: "DESC_TXT", Text: "Nobody living has paid its toll.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417TM0000"), IdString: "NAME_TXT", Text: "Marsh Raiders", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417TM0001"), IdString: "NAME_TXT", Text: "Barrow Wights", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RU0000"), IdString: "TITLE", Text: "Drowned Chapel of Saint Orléc", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RU0000"), IdString: "DESC", Text: "Its bells still toll beneath the water.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417RU0001"), IdString: "TITLE", Text: "Toll Tower", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0000"), IdString: "TXT_TITLE", Text: "Ferryman's Stall", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0000"), IdString: "TXT_DESC", Text: "Coin first, then cargo.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0001"), IdString: "TXT_TITLE", Text: "Reedwitch Hut", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0001"), IdString: "TXT_DESC", Text: "The marsh keeps secrets. I sell a few.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0002"), IdString: "TXT_TITLE", Text: "Old Sergeant's Yard", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0002"), IdString: "TXT_DESC", Text: "Stand straight. We begin at dawn.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0003"), IdString: "TXT_TITLE", Text: "Sellsword Camp", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0003"), IdString: "TXT_DESC", Text: "Blades for hire, no questions.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0004"), IdString: "TXT_TITLE", Text: "Salt Exchange", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417SI0004"), IdString: "TXT_DESC", Text: "Salt for silver, silver for salt.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417MM0000"), IdString: "DESC_TXT", Text: "Ford marker", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417MM0001"), IdString: "DESC_TXT", Text: "Shepherd's Table", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0000"), IdString: "NAME_TXT", Text: "Ford", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0001"), IdString: "NAME_TXT", Text: "Clan Road", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0002"), IdString: "NAME_TXT", Text: "Barrow Field", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0003"), IdString: "NAME_TXT", Text: "Chapel Steps", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417LO0004"), IdString: "NAME_TXT", Text: "North Shore", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0000"), IdString: "NAME_TXT", Text: "Ford sighted", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0000"), IdString: "POPUP_TXT", Text: "Scout \n\"The ford, Thane. Shallow enough for wagons, and nobody holds the far bank yet.\"", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0000"), IdString: "OBJECT_TXT", Text: "Take Brinewatch before the dead do.", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0001"), IdString: "NAME_TXT", Text: "Brinewatch taken", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0002"), IdString: "NAME_TXT", Text: "Brinewatch lost", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0003"), IdString: "NAME_TXT", Text: "Chapel looted", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0004"), IdString: "NAME_TXT", Text: "Raiders slain", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0005"), IdString: "NAME_TXT", Text: "Truce at the stall", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0006"), IdString: "NAME_TXT", Text: "Relic carried", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0007"), IdString: "NAME_TXT", Text: "Wolves turn", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0008"), IdString: "NAME_TXT", Text: "Garrison relieved", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0009"), IdString: "NAME_TXT", Text: "Bells beneath the water", SeqNum: 0),
            new Db.TextScenario(new Db.ScenarioId("S417SC0000"), new Db.Id("S417EV0009"), IdString: "POPUP_TXT", Text: "The chapel bells ring under the river, and for one night nobody draws steel.", SeqNum: 0)
        ]));
    }

    [Test]
    public void LoadSaveInfoTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioInfos, Is.EqualTo([
            new Db.ScenarioInfo(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418IF0000"),
                CurrentTurn: 3,
                MapSeed: 20261125,
                MapSize: 48,
                QuantityCities: 3,
                Creator: "Gallean Contributors",
                SourceM: true,
                DifficultyGame: Db.DifficultyType.Average,
                DifficultyScenario: Db.DifficultyType.Hard,
                CampaignId: new Db.Id("C000CC0001"),
                MaxUnit: 3,
                MaxLeader: 3,
                MaxCity: 4,
                MaxSpell: 3,
                Player1: 4,
                Player2: 3,
                Player3: 1,
                Player4: 99,
                Player5: 99,
                Player6: 99,
                Player7: 99,
                Player8: 99,
                Player9: 99,
                Player10: 99,
                Player11: 99,
                Player12: 99,
                Player13: 99,
                Name: "Greywater Siege",
                Description: "Three days after the clans reached the river, the dead of Mourncairn have closed around Brinewatch. The ford must hold until the thaw.",
                Briefing: "Hold Brinewatch against the Undead Hordes.",
                BriefingLong1: "The ferry town is ringed with barrow fires, and the Greywater is rising.",
                BriefingLong2: " Thane Halgrim will not give up the crossing he bled for.",
                BriefingLong3: null,
                BriefingLong4: null,
                BriefingLong5: null,
                DebunkWin: "The barrow fires gutter out, and the ford stays open.",
                DebunkWin2: " Brinewatch will remember its wardens.",
                DebunkWin3: null,
                DebunkWin4: null,
                DebunkWin5: null,
                DebunkLose: "Brinewatch has fallen silent, and the river carries the dead north.",
                SuggestedLevel: 2,
                O: false)
        ]));
    }

    [Test]
    public void LoadSavePlayersTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioPlayers, Is.EqualTo([
            new Db.ScenarioPlayer(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418PL0000"),
                new Db.Id("G000RR0004"),
                new Db.Id("G000LR0013"),
                new Db.Id("S418FG0000"),
                ResearchTurn: 0,
                ConstructTurn: 0,
                BuildsId: new Db.Id("S418PB0000"),
                KnownId: new Db.Id("S418KS0000"),
                Bank: new Db.ResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                SpellBank: new Db.ResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                Attitude: Db.AiAttitudeType.Medium,
                Face: 1,
                Spy1Id: null,
                Spy2Id: null,
                Spy3Id: null,
                QuantityBreaks: 0,
                IsHuman: false,
                CapturedById: null,
                AlwaysAi: null,
                ExMapId1: null,
                ExMapId2: null,
                ExMapId3: null,
                ExMapTurn1: 0,
                ExMapTurn2: 0,
                ExMapTurn3: 0),
            new Db.ScenarioPlayer(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418PL0001"),
                new Db.Id("G000RR0001"),
                new Db.Id("G000LR0004"),
                new Db.Id("S418FG0001"),
                ResearchTurn: 0,
                ConstructTurn: 3,
                BuildsId: new Db.Id("S418PB0001"),
                KnownId: new Db.Id("S418KS0001"),
                Bank: new Db.ResourceSet(Gold: 800, DeathMana: 0, RuneMana: 300, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                SpellBank: new Db.ResourceSet(Gold: 0, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                Attitude: Db.AiAttitudeType.Medium,
                Face: 0,
                Spy1Id: null,
                Spy2Id: null,
                Spy3Id: null,
                QuantityBreaks: 0,
                IsHuman: false,
                CapturedById: null,
                AlwaysAi: null,
                ExMapId1: null,
                ExMapId2: null,
                ExMapId3: null,
                ExMapTurn1: 0,
                ExMapTurn2: 0,
                ExMapTurn3: 0),
            new Db.ScenarioPlayer(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418PL0002"),
                new Db.Id("G000RR0003"),
                new Db.Id("G000LR0012"),
                new Db.Id("S418FG0002"),
                ResearchTurn: 2,
                ConstructTurn: 0,
                BuildsId: new Db.Id("S418PB0002"),
                KnownId: new Db.Id("S418KS0002"),
                Bank: new Db.ResourceSet(Gold: 600, DeathMana: 250, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                SpellBank: new Db.ResourceSet(Gold: 100, DeathMana: 50, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
                Attitude: Db.AiAttitudeType.Large,
                Face: 2,
                Spy1Id: new Db.Id("S418PL0001"),
                Spy2Id: null,
                Spy3Id: null,
                QuantityBreaks: 0,
                IsHuman: false,
                CapturedById: null,
                AlwaysAi: null,
                ExMapId1: null,
                ExMapId2: null,
                ExMapId3: null,
                ExMapTurn1: 0,
                ExMapTurn2: 0,
                ExMapTurn3: 0)
        ]));
    }

    [Test]
    public void LoadSaveRelationsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioRelations, Is.EqualTo([
            new Db.ScenarioRelation(new Db.ScenarioId("S418SC0000"), Race1: Db.RaceType.Neutral, Race2: Db.RaceType.Dwarf, Relation: 0),
            new Db.ScenarioRelation(new Db.ScenarioId("S418SC0000"), Race1: Db.RaceType.Neutral, Race2: Db.RaceType.Undead, Relation: 0),
            new Db.ScenarioRelation(new Db.ScenarioId("S418SC0000"), Race1: Db.RaceType.Dwarf, Race2: Db.RaceType.Undead, Relation: 5160)
        ]));
    }

    [Test]
    public void LoadSaveVariablesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioVariables, Is.EqualTo([
            new Db.ScenarioVariable(new Db.ScenarioId("S418SC0000"), Id: 0, Name: "FerryTolls", Value: 2),
            new Db.ScenarioVariable(new Db.ScenarioId("S418SC0000"), Id: 1, Name: "BellsRung", Value: 5)
        ]));
    }

    [Test]
    public void LoadSaveUnitsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioUnits, Is.EqualTo([
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0000"), Hp: 900, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU3002"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0001"), Hp: 225, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0044"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0002"), Hp: 150, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0036"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0003"), Hp: 65, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0026"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0004"), Hp: 900, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU3004"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0005"), Hp: 65, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0098"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0006"), Hp: 120, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0086"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0007"), Hp: 45, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0078"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0008"), Hp: 140, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5103"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0009"), Hp: 95, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5002"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN000a"), Hp: 40, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5001"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN000b"), Hp: 80, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5002"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN000c"), Hp: 200, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5113"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN000d"), Hp: 40, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5018"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN000e"), Hp: 50, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5017"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN000f"), Hp: 180, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5139"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0010"), Hp: 180, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5039"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0011"), Hp: 150, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5033"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0012"), Hp: 420, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5036"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0013"), Hp: 150, Xp: 40, Creation: 1, TypeId: new Db.Id("G000UU0047"), Level: 2, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0014"), Hp: 50, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5034"), Level: 1, Transf: true, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0015"), Hp: 90, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0097"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0016"), Hp: 100, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU0092"), Level: 1, Transf: false, DynLevel: false),
            new Db.ScenarioUnit(new Db.ScenarioId("S418SC0000"), new Db.Id("S418UN0017"), Hp: 600, Xp: 0, Creation: 0, TypeId: new Db.Id("G000UU5123"), Level: 1, Transf: false, DynLevel: null)
        ]));
    }

    [Test]
    public void LoadSaveStacksTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioStacks, Is.EqualTo([
            new Db.ScenarioStack(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418KC0000"),
                SourceTemplateId: null,
                PositionX: 2,
                PositionY: 2,
                LeaderId: new Db.Id("S418UN0001"),
                LeaderAlive: true,
                Morale: 0,
                Move: 20,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S418PL0001"),
                SubraceId: new Db.Id("S418SR0001"),
                InsideId: new Db.Id("S418FT0000"),
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.Normal,
                OrderTargetId: null,
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418KC0001"),
                SourceTemplateId: null,
                PositionX: 41,
                PositionY: 41,
                LeaderId: new Db.Id("S418UN0005"),
                LeaderAlive: true,
                Morale: 0,
                Move: 20,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S418PL0002"),
                SubraceId: new Db.Id("S418SR0002"),
                InsideId: new Db.Id("S418FT0001"),
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.Normal,
                OrderTargetId: null,
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418KC0002"),
                SourceTemplateId: null,
                PositionX: 20,
                PositionY: 21,
                LeaderId: new Db.Id("S418UN0008"),
                LeaderAlive: true,
                Morale: 0,
                Move: 25,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S418PL0000"),
                SubraceId: new Db.Id("S418SR0006"),
                InsideId: new Db.Id("S418FT0002"),
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.DefendCity,
                OrderTargetId: new Db.Id("S418FT0002"),
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418KC0003"),
                SourceTemplateId: new Db.Id("S418TM0000"),
                PositionX: 24,
                PositionY: 31,
                LeaderId: new Db.Id("S418UN000c"),
                LeaderAlive: true,
                Morale: 0,
                Move: 20,
                Facing: 3,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S418PL0000"),
                SubraceId: new Db.Id("S418SR0004"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.DefendLocation,
                OrderTargetId: new Db.Id("S418LO0000"),
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418KC0004"),
                SourceTemplateId: null,
                PositionX: 8,
                PositionY: 24,
                LeaderId: new Db.Id("S418UN000f"),
                LeaderAlive: true,
                Morale: 0,
                Move: 35,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S418PL0000"),
                SubraceId: new Db.Id("S418SR0005"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 1,
                Order: Db.OrderType.Roam,
                OrderTargetId: null,
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418KC0005"),
                SourceTemplateId: null,
                PositionX: 9,
                PositionY: 12,
                LeaderId: new Db.Id("S418UN0013"),
                LeaderAlive: true,
                Morale: 0,
                Move: 25,
                Facing: 5,
                BannerId: new Db.Id("S418IM0003"),
                TomeId: new Db.Id("S418IM0006"),
                Battle1Id: new Db.Id("S418IM0007"),
                Battle2Id: new Db.Id("S418IM000d"),
                Artifact1Id: new Db.Id("S418IM0005"),
                Artifact2Id: new Db.Id("S418IM000f"),
                BootsId: new Db.Id("S418IM0004"),
                OwnerId: new Db.Id("S418PL0001"),
                SubraceId: new Db.Id("S418SR0001"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 1,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.Normal,
                OrderTargetId: null,
                CreationLevel: 2,
                NumberOfBattles: 1,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null),
            new Db.ScenarioStack(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418KC0006"),
                SourceTemplateId: null,
                PositionX: 38,
                PositionY: 36,
                LeaderId: new Db.Id("S418UN0015"),
                LeaderAlive: true,
                Morale: 0,
                Move: 35,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S418PL0002"),
                SubraceId: new Db.Id("S418SR0002"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: false,
                AiPriority: 3,
                Order: Db.OrderType.SecureCity,
                OrderTargetId: new Db.Id("S418FT0003"),
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.AttackStack,
                AiOrderTargetId: new Db.Id("S418KC0005")),
            new Db.ScenarioStack(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418KC0007"),
                SourceTemplateId: null,
                PositionX: 44,
                PositionY: 20,
                LeaderId: new Db.Id("S418UN0017"),
                LeaderAlive: true,
                Morale: 0,
                Move: 30,
                Facing: 0,
                BannerId: null,
                TomeId: null,
                Battle1Id: null,
                Battle2Id: null,
                Artifact1Id: null,
                Artifact2Id: null,
                BootsId: null,
                OwnerId: new Db.Id("S418PL0000"),
                SubraceId: new Db.Id("S418SR0000"),
                InsideId: null,
                Invisible: false,
                UpgradeCount: 0,
                AiIgnore: true,
                AiPriority: 3,
                Order: Db.OrderType.Stand,
                OrderTargetId: null,
                CreationLevel: 1,
                NumberOfBattles: 0,
                AiOrder: Db.OrderType.Normal,
                AiOrderTargetId: null)
        ]));
    }

    [Test]
    public void LoadSaveFortificationsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioFortifications, Is.EqualTo([
            new Db.ScenarioFortification(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418FT0000"),
                PositionX: 2,
                PositionY: 2,
                OwnerId: new Db.Id("S418PL0001"),
                SubraceId: new Db.Id("S418SR0001"),
                Stack: new Db.Id("S418KC0000"),
                Size: null,
                SizeMax: null,
                RegenerationBonus: null,
                ProtectionBuildingId: null,
                Morale: null,
                GrowthTurn: null,
                ProtectedFromUndead: null,
                ProtectedFromHeretic: null,
                ProtectedFromHuman: null,
                ProtectedFromDwarf: null,
                ProtectedFromElf: null,
                AiPriority: 3,
                RiotTurns: null),
            new Db.ScenarioFortification(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418FT0001"),
                PositionX: 41,
                PositionY: 41,
                OwnerId: new Db.Id("S418PL0002"),
                SubraceId: new Db.Id("S418SR0002"),
                Stack: new Db.Id("S418KC0001"),
                Size: null,
                SizeMax: null,
                RegenerationBonus: null,
                ProtectionBuildingId: null,
                Morale: null,
                GrowthTurn: null,
                ProtectedFromUndead: null,
                ProtectedFromHeretic: null,
                ProtectedFromHuman: null,
                ProtectedFromDwarf: null,
                ProtectedFromElf: null,
                AiPriority: 3,
                RiotTurns: null),
            new Db.ScenarioFortification(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418FT0002"),
                PositionX: 20,
                PositionY: 21,
                OwnerId: new Db.Id("S418PL0000"),
                SubraceId: new Db.Id("S418SR0003"),
                Stack: new Db.Id("S418KC0002"),
                Size: 2,
                SizeMax: null,
                RegenerationBonus: 10,
                ProtectionBuildingId: null,
                Morale: 0,
                GrowthTurn: 0,
                ProtectedFromUndead: true,
                ProtectedFromHeretic: false,
                ProtectedFromHuman: false,
                ProtectedFromDwarf: false,
                ProtectedFromElf: null,
                AiPriority: 3,
                RiotTurns: 0),
            new Db.ScenarioFortification(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418FT0003"),
                PositionX: 34,
                PositionY: 32,
                OwnerId: new Db.Id("S418PL0000"),
                SubraceId: new Db.Id("S418SR0000"),
                Stack: null,
                Size: 1,
                SizeMax: null,
                RegenerationBonus: 0,
                ProtectionBuildingId: null,
                Morale: 0,
                GrowthTurn: 0,
                ProtectedFromUndead: false,
                ProtectedFromHeretic: false,
                ProtectedFromHuman: false,
                ProtectedFromDwarf: false,
                ProtectedFromElf: null,
                AiPriority: 6,
                RiotTurns: 0),
            new Db.ScenarioFortification(
                new Db.ScenarioId("S418SC0000"),
                new Db.Id("S418FT0004"),
                PositionX: 30,
                PositionY: 40,
                OwnerId: new Db.Id("S418PL0002"),
                SubraceId: new Db.Id("S418SR0002"),
                Stack: null,
                Size: 3,
                SizeMax: null,
                RegenerationBonus: 15,
                ProtectionBuildingId: null,
                Morale: 0,
                GrowthTurn: 2,
                ProtectedFromUndead: false,
                ProtectedFromHeretic: false,
                ProtectedFromHuman: true,
                ProtectedFromDwarf: true,
                ProtectedFromElf: null,
                AiPriority: 4,
                RiotTurns: 1)
        ]));
    }

    [Test]
    public void LoadSaveRuinsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioRuins, Is.EqualTo([
            new Db.ScenarioRuin(new Db.ScenarioId("S418SC0000"), new Db.Id("S418RU0000"), Image: 2, PositionX: 10, PositionY: 30, Cash: "G0150:R0000:Y0000:E0000:W0000", Item: new Db.Id("G000IG2002"), Looter: null, AiPriority: 3),
            new Db.ScenarioRuin(new Db.ScenarioId("S418SC0000"), new Db.Id("S418RU0001"), Image: 4, PositionX: 33, PositionY: 10, Cash: "G0000:R0000:Y0000:E0000:W0000", Item: null, Looter: new Db.Id("S418PL0001"), AiPriority: 5)
        ]));
    }

    [Test]
    public void LoadSaveRodsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.GreywaterSiegePath, TestScenarios.Encoding);

        Assert.That(file.Tables.ScenarioRods, Is.EqualTo([
            new Db.ScenarioRod(new Db.ScenarioId("S418SC0000"), new Db.Id("S418RD0000"), new Db.Id("S418PL0001"), PositionX: 10, PositionY: 5)
        ]));
    }

    [Test]
    public void LoadUnknownSignatureTest()
    {
        using MemoryStream stream = new(new byte[64]);

        Assert.That(
            () => ScenarioFileLoader.Load(stream, TestScenarios.Encoding),
            Throws.TypeOf<InvalidDataException>());
    }

    [TestCase(31)]
    [TestCase(32)]
    [TestCase(33)]
    [TestCase(34)]
    [TestCase(35)]
    public void LoadSaveVersionTest(int version)
    {
        byte[] data = File.ReadAllBytes(TestScenarios.GreywaterSiegePath);
        BitConverter.GetBytes(version).CopyTo(data, SaveVersionOffset);

        using MemoryStream stream = new(data, writable: false);

        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding);

        Assert.That(file.Format, Is.EqualTo(ScenarioFileFormat.Save(version)));
        Assert.That(file.ScenarioId, Is.EqualTo(new Db.ScenarioId("S418SC0000")));
    }

    [TestCase(1)]
    [TestCase(30)]
    [TestCase(36)]
    public void LoadUnsupportedSaveVersionTest(int version)
    {
        byte[] data = File.ReadAllBytes(TestScenarios.GreywaterSiegePath);
        BitConverter.GetBytes(version).CopyTo(data, SaveVersionOffset);

        using MemoryStream stream = new(data, writable: false);

        Assert.That(
            () => ScenarioFileLoader.Load(stream, TestScenarios.Encoding),
            Throws.TypeOf<InvalidDataException>());
    }
}
