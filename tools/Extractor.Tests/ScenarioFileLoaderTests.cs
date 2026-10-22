using System.Text;

namespace Extractor.Tests;

public sealed class ScenarioFileLoaderTests
{
    private const int SaveVersionOffset = 16;

    private static MemoryStream OpenSaveWithVersion(int version)
    {
        byte[] data = File.ReadAllBytes(TestScenarios.TutorialPath);
        BitConverter.GetBytes(version).CopyTo(data, SaveVersionOffset);

        return new MemoryStream(data, writable: false);
    }

    private static IReadOnlyList<string> FormatTexts(ScenarioFileTables tables, string stringId, string objectTypeCode)
    {
        return
        [
            .. tables.TextScenarios
                .Where(text => text.IdString == stringId && text.IdObject.TypeCode == objectTypeCode)
                .Select(text => $"{text.IdObject} {text.Text}")
        ];
    }

    private static IReadOnlyList<string> FormatTableCounts(ScenarioFileTables tables)
    {
        return
        [
            $"bags {tables.ScenarioBags.Count}",
            $"crystals {tables.ScenarioCrystals.Count}",
            $"eventConditions {tables.ScenarioEventConditions.Count}",
            $"eventEffects {tables.ScenarioEventEffects.Count}",
            $"events {tables.ScenarioEvents.Count}",
            $"fogs {tables.ScenarioFogs.Count}",
            $"fortifications {tables.ScenarioFortifications.Count}",
            $"infos {tables.ScenarioInfos.Count}",
            $"inventories {tables.ScenarioInventories.Count}",
            $"items {tables.ScenarioItems.Count}",
            $"landmarks {tables.ScenarioLandmarks.Count}",
            $"locations {tables.ScenarioLocations.Count}",
            $"mages {tables.ScenarioMages.Count}",
            $"mapBlocks {tables.ScenarioMapBlocks.Count}",
            $"mercenaries {tables.ScenarioMercenaries.Count}",
            $"merchants {tables.ScenarioMerchants.Count}",
            $"mountains {tables.ScenarioMountains.Count}",
            $"players {tables.ScenarioPlayers.Count}",
            $"roads {tables.ScenarioRoads.Count}",
            $"ruins {tables.ScenarioRuins.Count}",
            $"sites {tables.ScenarioSites.Count}",
            $"spells {tables.ScenarioSpells.Count}",
            $"stacks {tables.ScenarioStacks.Count}",
            $"subraces {tables.ScenarioSubraces.Count}",
            $"texts {tables.TextScenarios.Count}",
            $"unitGroups {tables.ScenarioUnitGroups.Count}",
            $"unitModifiers {tables.ScenarioUnitModifiers.Count}",
            $"units {tables.ScenarioUnits.Count}"
        ];
    }

    private static IReadOnlyList<string> FormatStacks(ScenarioFile file, int count)
    {
        ScenarioFileTables tables = file.Tables;
        Dictionary<Db.Id, Db.ScenarioUnitGroup> groups = tables.ScenarioUnitGroups.ToDictionary(group => group.GroupId);
        Dictionary<Db.Id, Db.ScenarioUnit> units = tables.ScenarioUnits.ToDictionary(unit => unit.UnitId);
        List<string> result = [];

        foreach (Db.ScenarioStack stack in tables.ScenarioStacks.OrderBy(stack => stack.StackId.Value, StringComparer.Ordinal).Take(count))
        {
            Db.ScenarioUnitGroup group = groups[stack.StackId];
            StringBuilder members = new();

            foreach (Db.Id? unitId in new[] { group.Unit0Id, group.Unit1Id, group.Unit2Id, group.Unit3Id, group.Unit4Id, group.Unit5Id })
            {
                if (unitId is not null)
                {
                    Db.ScenarioUnit unit = units[unitId.Value];
                    members.Append($" {unit.TypeId}/lvl{unit.Level}/hp{unit.Hp}/xp{unit.Xp}");
                }
            }

            result.Add($"{stack.StackId} ({stack.PositionX},{stack.PositionY}) owner={stack.OwnerId} order={stack.Order} move={stack.Move}{members}");
        }

        return result;
    }

    [Test]
    public void LoadExportFormatTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(file.Format, Is.EqualTo(ScenarioFileFormat.Scenario));
    }

    [Test]
    public void LoadSaveFormatTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.TutorialPath, TestScenarios.Encoding);

        Assert.That(file.Format, Is.EqualTo(ScenarioFileFormat.Save(35)));
    }

    [Test]
    public void LoadExportScenarioIdTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(file.ScenarioId, Is.EqualTo(new Db.ScenarioId("S143SC0000")));
    }

    [Test]
    public void LoadExportInfoTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);
        Db.ScenarioInfo info = file.Tables.ScenarioInfos.Single();

        Assert.That(info, Is.EqualTo(new Db.ScenarioInfo(
            new Db.ScenarioId("S143SC0000"),
            new Db.Id("S143IF0000"),
            CurrentTurn: 0,
            MapSeed: 10332288,
            MapSize: 48,
            QuantityCities: 4,
            Creator: "Emanuel Wall ",
            SourceM: false,
            DifficultyGame: Db.DifficultyType.Average,
            DifficultyScenario: Db.DifficultyType.VeryHard,
            CampaignId: new Db.Id("C000CC0001"),
            MaxUnit: 2,
            MaxLeader: 3,
            MaxCity: 5,
            MaxSpell: 2,
            Player1: 4,
            Player2: 3,
            Player3: 5,
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
            Name: "Doragon's Realm",
            Description: info.Description,
            Briefing: info.Briefing,
            BriefingLong1: info.BriefingLong1,
            BriefingLong2: info.BriefingLong2,
            BriefingLong3: info.BriefingLong3,
            BriefingLong4: info.BriefingLong4,
            BriefingLong5: info.BriefingLong5,
            DebunkWin: info.DebunkWin,
            DebunkWin2: info.DebunkWin2,
            DebunkWin3: info.DebunkWin3,
            DebunkWin4: info.DebunkWin4,
            DebunkWin5: info.DebunkWin5,
            DebunkLose: info.DebunkLose,
            SuggestedLevel: 1,
            O: false)));
    }

    [Test]
    public void LoadExportTablesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(FormatTableCounts(file.Tables), Is.EqualTo([
            "bags 8",
            "crystals 13",
            "eventConditions 32",
            "eventEffects 38",
            "events 26",
            "fogs 144",
            "fortifications 6",
            "infos 1",
            "inventories 52",
            "items 52",
            "landmarks 29",
            "locations 9",
            "mages 0",
            "mapBlocks 72",
            "mercenaries 0",
            "merchants 6",
            "mountains 75",
            "players 3",
            "roads 0",
            "ruins 6",
            "sites 3",
            "spells 0",
            "stacks 29",
            "subraces 12",
            "texts 140",
            "unitGroups 41",
            "unitModifiers 2",
            "units 71"
        ]));
    }

    [Test]
    public void LoadExportStacksTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(FormatStacks(file, 4), Is.EqualTo([
            "S143KC0000 (1,1) owner=S143PL0001 order=Normal move=20 G000UU0044/lvl1/hp225/xp0",
            "S143KC0001 (36,35) owner=S143PL0002 order=Normal move=25 G000UU8009/lvl1/hp135/xp0",
            "S143KC0002 (41,3) owner=S143PL0000 order=Stand move=20 G000UU5120/lvl1/hp700/xp0",
            "S143KC0003 (3,43) owner=S143PL0000 order=Stand move=20 G000UU5121/lvl1/hp700/xp0"
        ]));
    }

    [Test]
    public void LoadSaveTablesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.TutorialPath, TestScenarios.Encoding);

        Assert.That(FormatTableCounts(file.Tables), Is.EqualTo([
            "bags 7",
            "crystals 4",
            "eventConditions 44",
            "eventEffects 72",
            "events 42",
            "fogs 96",
            "fortifications 4",
            "infos 1",
            "inventories 15",
            "items 15",
            "landmarks 84",
            "locations 35",
            "mages 3",
            "mapBlocks 72",
            "mercenaries 0",
            "merchants 9",
            "mountains 298",
            "players 2",
            "roads 39",
            "ruins 3",
            "sites 4",
            "spells 0",
            "stacks 31",
            "subraces 11",
            "texts 178",
            "unitGroups 38",
            "unitModifiers 0",
            "units 81"
        ]));
    }

    [Test]
    public void LoadSaveStacksTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.TutorialPath, TestScenarios.Encoding);

        Assert.That(FormatStacks(file, 4), Is.EqualTo([
            "S001KC0000 (30,9) owner=S001PL0009 order=Normal move=20 G000UU0019/lvl1/hp150/xp0",
            "S001KC0002 (34,5) owner=S001PL0000 order=Stand move=20 G000UU5101/lvl1/hp40/xp0 G000UU5001/lvl1/hp40/xp0",
            "S001KC0003 (32,4) owner=S001PL0000 order=Stand move=20 G000UU5117/lvl1/hp50/xp0 G000UU5017/lvl1/hp50/xp0",
            "S001KC0004 (24,2) owner=S001PL0000 order=DefendLocation move=20 G000UU5117/lvl1/hp50/xp0 G000UU5018/lvl1/hp40/xp0 G000UU5018/lvl1/hp40/xp0"
        ]));
    }

    [Test]
    public void LoadExportCityNamesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(FormatTexts(file.Tables, Db.TextScenario.NameTextId, "FT"), Is.EqualTo([
            "S143FT0000 Otzwall",
            "S143FT0001 Kurn Fort",
            "S143FT0002 Archon Castle ",
            "S143FT0003 Sentry Castle",
            "S143FT0004 Dorogan City ",
            "S143FT0005 Doragon Cult Castle "
        ]));
    }

    [Test]
    public void LoadExportRuinTitlesTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(FormatTexts(file.Tables, Db.TextScenario.RuinTitleTextId, "RU"), Is.EqualTo([
            "S143RU0000 Spritess Woods ",
            "S143RU0001 Oracle Tower ",
            "S143RU0002 Griffen Kassel",
            "S143RU0003 Doragon's Ruin",
            "S143RU0004 Forest Smugglers ",
            "S143RU0005 Wolves Den "
        ]));
    }

    [Test]
    public void LoadExportSiteTextsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(FormatTexts(file.Tables, Db.TextScenario.SiteTitleTextId, "SI"), Is.EqualTo([
            "S143SI0000 Zollian's Camp",
            "S143SI0001 Unnens Camp",
            "S143SI0002 Ebhon's Potion Shop"
        ]));

        Assert.That(FormatTexts(file.Tables, Db.TextScenario.SiteDescriptionTextId, "SI"), Is.EqualTo([
            "S143SI0000 Come in, I will make worthy fighters out of your frail soldiers.",
            "S143SI0001 Come in, I will make worthy fighters out of your frail soldiers.",
            "S143SI0002 Everything I possess is for sale."
        ]));
    }

    [Test]
    public void LoadExportLandmarkDescriptionsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(FormatTexts(file.Tables, Db.TextScenario.DescriptionTextId, "MM"), Is.EqualTo([
            "S143MM0000 Skeleton",
            "S143MM0001 Doragon's Altar",
            "S143MM0002 Mountain",
            "S143MM0003 Mountain",
            "S143MM0004 Mountain",
            "S143MM0005 Mountain",
            "S143MM0006 Mountain",
            "S143MM0008 Mountain",
            "S143MM0009 Plateau",
            "S143MM000b Plateau",
            "S143MM000c Mountain",
            "S143MM0014 Fall",
            "S143MM0015 Plateau",
            "S143MM0016 Elf City",
            "S143MM0017 Elf City",
            "S143MM0018 Elf City",
            "S143MM0019 Elf City",
            "S143MM001a Elf City",
            "S143MM001c Elf City",
            "S143MM001d Elf City",
            "S143MM001e Elf City",
            "S143MM001f Elf City",
            "S143MM0020 Elf City",
            "S143MM0021 Elf City",
            "S143MM0022 Elven House",
            "S143MM0023 Elf City",
            "S143MM0024 Elf City",
            "S143MM0026 Impaled Skeleton",
            "S143MM0027 Impaled Skeleton"
        ]));
    }

    [Test]
    public void LoadExportPlayerDescriptionsTest()
    {
        ScenarioFile file = ScenarioFileLoader.Load(TestScenarios.DoragonRealmPath, TestScenarios.Encoding);

        Assert.That(FormatTexts(file.Tables, Db.TextScenario.DescriptionTextId, "PL"), Is.EqualTo([
            "S143PL0000 No race description",
            "S143PL0001 Highest lord of the dwarf lords, he is known for his intensity in battle and his courage in the face of overwhelming odds. He once fought seven days and nights against the mighty red dragon ARFURUZON to finally defeat him.",
            "S143PL0002 A veteran in the struggle to reclaim Elven glory, nobody enters the forest without his knowledge."
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
        using MemoryStream stream = OpenSaveWithVersion(version);
        ScenarioFile file = ScenarioFileLoader.Load(stream, TestScenarios.Encoding);

        Assert.That(file.Format, Is.EqualTo(ScenarioFileFormat.Save(version)));
        Assert.That(file.ScenarioId, Is.EqualTo(new Db.ScenarioId("S001SC0000")));
    }

    [TestCase(1)]
    [TestCase(30)]
    [TestCase(36)]
    public void LoadUnsupportedSaveVersionTest(int version)
    {
        using MemoryStream stream = OpenSaveWithVersion(version);

        Assert.That(
            () => ScenarioFileLoader.Load(stream, TestScenarios.Encoding),
            Throws.TypeOf<InvalidDataException>());
    }
}
