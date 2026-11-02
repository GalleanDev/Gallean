using Gallean.Entities.GameData;

namespace Gallean.Engine.Tests;

public sealed class GameTextProviderTests
{
    private const string ScenarioId = "S095SC0000";

    private static GlobalDatabase LoadGlobalDatabase()
    {
        return GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
    }

    private static ScenarioDatabase LoadScenarioDatabase()
    {
        return ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath, ScenarioId);
    }

    [Test]
    public void GetGlobalTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        GlobalText actual = provider.GetGlobalText(new GlobalTextId("X000TG2001"));

        Assert.That(actual, Is.EqualTo(new GlobalText(
            new GlobalTextId("x000tg2001"),
            "Squire",
            false,
            null)));
    }

    [Test]
    public void CaseInsensitiveIdTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        GlobalText lower = provider.GetGlobalText(new GlobalTextId("x000tg2001"));
        GlobalText upper = provider.GetGlobalText(new GlobalTextId("X000TG2001"));

        Assert.That(lower, Is.EqualTo(new GlobalText(
            new GlobalTextId("x000tg2001"),
            "Squire",
            false,
            null)));
        Assert.That(upper, Is.EqualTo(new GlobalText(
            new GlobalTextId("x000tg2001"),
            "Squire",
            false,
            null)));
    }

    [Test]
    public void UnknownTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        Assert.Throws<KeyNotFoundException>(() => provider.GetGlobalText(new GlobalTextId("invalid")));
    }

    [Test]
    public void EmptyTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        Assert.Throws<InvalidDataException>(() => provider.GetGlobalText(new GlobalTextId("x000tg7000")));
    }

    [Test]
    public void GetScenarioTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095EV0003:POPUP_TXT:0"));

        Assert.That(actual, Is.EqualTo("Spearman\n\"Sorry Nobar... Except you, none can pass here. Philip's orders!\""));
    }

    [Test]
    public void ScenarioTextCaseInsensitiveIdTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string lower = provider.GetScenarioText(new ScenarioTextId("s095ev0003:popup_txt:0"));
        string upper = provider.GetScenarioText(new ScenarioTextId("S095EV0003:POPUP_TXT:0"));

        Assert.That(lower, Is.EqualTo("Spearman\n\"Sorry Nobar... Except you, none can pass here. Philip's orders!\""));
        Assert.That(upper, Is.EqualTo("Spearman\n\"Sorry Nobar... Except you, none can pass here. Philip's orders!\""));
    }

    [Test]
    public void UnknownScenarioTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        Assert.Throws<KeyNotFoundException>(() => provider.GetScenarioText(new ScenarioTextId("invalid")));
    }

    [Test]
    public void ScenarioInfoNameTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095IF0000:NAME:0"));

        Assert.That(actual, Is.EqualTo("Antagonists"));
    }

    [Test]
    public void ScenarioInfoQuotedNameTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S256IF0000:NAME:0"));

        Assert.That(actual, Is.EqualTo("Vertigo"));
    }

    [Test]
    public void ScenarioInfoJoinedPartsTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095IF0000:DEBUNKW:0"));

        Assert.That(actual, Is.EqualTo("The defeat of Hubert de Lalye raised morale in the beleaguered realm. No one thought Uther capable of overcoming such a treacherous enemy.\n\nThe Emperor Demosthene's illness miraculously healed, his bitter heart comforted by the return of his long lost son, Uther. He knew he finally had the means to restore peace to his land..."));
    }

    [Test]
    public void ScenarioInfoBriefingTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095IF0000:BRIEFLONG:0"));

        Assert.That(actual, Is.EqualTo("Before long, Hubert de Lalye had assembled a considerable force. Moving north, his army met little resistance, and those cities which refused his ruling were quickly crushed. The heads of all dissenters were displayed on pikes throughout the Empire... warning all of the fate of those who dared oppose Hubert de Lalye's rule.\n\n\nBy that time, news of Uther's return had spread. Uther was reported to be leading an army of Clansmen down the peaks of Griffin Heights to cleanse the Empire of his father's enemies.\n\n\nThe emperor Demosthene decided to move his court north, to the fortified city of Fhindar. Inquisitor Phillip d'Agincourt resided there; he would surely ensure the emperor's safety in these grievous times. Demosthene hastily prepared his troops, knowing that the divided forces would shortly meet."));
    }

    [Test]
    public void ScenarioInfoParagraphMarkersTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S142IF0000:DEBUNKW:0"));

        Assert.That(actual,
            Is.EqualTo(
                "When the grand walls of Temperance were brought down, the people of the forests poured into its courtyard. The combined might of the Dwarves and Humans was unable to withstand the Elven assault and the city was soon lost. King Emry had signaled a retreat - he would not sacrifice more of his people to a hopeless battle. Even after the struggle, new bands of Elves continued to arrive.\n\n\n\n\n\n\nThere was no celebration. The task of burying the fallen was monumental. Wild Elves interred noble Elves with somber respect while the nobles honored those lost in battle, including Tora'ach. Fyrwenn inscribed great tales about the fallen leader, who gave his own life to ensure her safety.\n\n\n\n\n\n\nIllumielle struggled to raise the spirits of her people in such sorrowful times. She left the rebuilding of the great city, soon to be the new Elven capital, to her greatest general, Serphis. But what of the simple oracle whose meager voice sparked an entire race to rebel?\n\n\n\n\n\n\nMillu sat in the corner of her room, drenched in sweat. As she dug her fingernails into her palms, the presence spoke through her in a forceful whisper. This is only the beginning...this is only the beginning..."));
    }

    [Test]
    public void ScenarioInfoMissingBriefingTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S253IF0000:BRIEFLONG:0"));

        Assert.That(actual, Is.EqualTo(""));
    }

    [Test]
    public void DatabaseIndexTest()
    {
        GameTextProvider provider = new(
            GlobalDatabaseLoader.LoadIndex(RiseOfTheElves.GlobalDatabasePath),
            ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath),
            [], LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095IF0000:BRIEFING:0"));

        Assert.That(actual, Is.EqualTo("Destroy Hubert de Lalye"));
    }

    [Test]
    public void ParagraphBreakTextTest()
    {
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath, "S101SC0000");
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S101EV000a:POPUP_TXT:0"));

        Assert.That(actual, Is.EqualTo("A phantom appears before you...\n\nSpecter\n\"All who enter Mortisss' terrritory... ssshall feel her powersss.\""));
    }

    [Test]
    public void EscapedLineBreaksTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(
            LoadGlobalDatabase(),
            scenarioDb,
            [.. scenarioDb.TextScenarios, new Db.TextScenario(ScenarioId, new Db.Id("S095EV00ff"), "POPUP_TXT", @"Prima parte.\n\nSeconda parte.", 0)],
            LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095EV00ff:POPUP_TXT:0"));

        Assert.That(actual, Is.EqualTo("Prima parte.\n\nSeconda parte."));
    }

    [Test]
    public void EscapedLineBreaksInLanguageScenarioTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [],
            [],
            [],
            [new Db.TextScenario(ScenarioId, new Db.Id("S095IF0000"), "BRIEFLONG", "Prima parte.\\n\\nSeconda parte.", 0)]);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095IF0000:BRIEFLONG:0"));

        Assert.That(actual, Is.EqualTo("Prima parte.\n\nSeconda parte."));
    }

    [Test]
    public void EscapedLineBreaksInLanguageGlobalTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [new Db.TextGlobal(new Db.Id("x160tgc229"), "Totschläger\\nKritischer Treffer", false, null)],
            [],
            [],
            []);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        GlobalText actual = provider.GetGlobalText(new GlobalTextId("x160tgc229"));

        Assert.That(actual.Text, Is.EqualTo("Totschläger\nKritischer Treffer"));
    }

    [Test]
    public void UnitAndBuildingTextsTest()
    {
        GlobalDatabase globalDb = LoadGlobalDatabase();
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(globalDb, scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        List<string> textIds = [];

        foreach (Db.Unit unit in globalDb.Units.Values)
        {
            textIds.Add(unit.NameText);
            textIds.Add(unit.DescriptionText);
            textIds.Add(unit.AbilityText);
        }

        foreach (Db.Building building in globalDb.Buildings.Values)
        {
            textIds.Add(building.NameText);
            textIds.Add(building.DescriptionText);
        }

        List<string> unresolved = [];

        foreach (string textId in textIds)
        {
            try
            {
                provider.GetGlobalText(new GlobalTextId(textId));
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidDataException)
            {
                unresolved.Add(textId);
            }
        }

        Assert.That(unresolved, Is.Empty);
    }

    [Test]
    public void CityNameTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095FT0002:NAME_TXT:0"));

        Assert.That(actual, Is.EqualTo("Fhindar"));
    }

    [Test]
    public void RuinNameTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095RU0000:TITLE:0"));

        Assert.That(actual, Is.EqualTo("Ruined Keep"));
    }

    [Test]
    public void PlayerNameTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        string actual = provider.GetScenarioText(new ScenarioTextId("S095PL0009:NAME_TXT:0"));

        Assert.That(actual, Is.EqualTo("Goar"));
    }

    [Test]
    public void LanguageGlobalTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [new Db.TextGlobal(new Db.Id("X000TG2001"), "Сквайр", true, "Unit name")],
            [],
            [],
            []);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        GlobalText actual = provider.GetGlobalText(new GlobalTextId("x000tg2001"));

        Assert.That(actual, Is.EqualTo(new GlobalText(
            new GlobalTextId("X000TG2001"),
            "Сквайр",
            true,
            "Unit name")));
    }

    [Test]
    public void LanguageEmptyGlobalTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [new Db.TextGlobal(new Db.Id("x000tg2001"), "", null, null)],
            [],
            [],
            []);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        GlobalText actual = provider.GetGlobalText(new GlobalTextId("x000tg2001"));

        Assert.That(actual, Is.EqualTo(new GlobalText(
            new GlobalTextId("x000tg2001"),
            "Squire",
            false,
            null)));
    }

    [Test]
    public void LanguageUntranslatedGlobalTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [new Db.TextGlobal(new Db.Id("x000tg2001"), "Сквайр", null, null)],
            [],
            [],
            []);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        GlobalText actual = provider.GetGlobalText(new GlobalTextId("x000tg2002"));

        Assert.That(actual, Is.EqualTo(new GlobalText(
            new GlobalTextId("x000tg2002"),
            "Knight",
            false,
            null)));
    }

    [Test]
    public void LanguageScenarioTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [],
            [],
            [],
            [new Db.TextScenario("S095SC0000", new Db.Id("S095EV0003"), "POPUP_TXT", "Копейщик", 0)]);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        Assert.That(provider.GetScenarioText(new ScenarioTextId("S095EV0003:POPUP_TXT:0")), Is.EqualTo("Копейщик"));
        Assert.That(provider.GetScenarioText(new ScenarioTextId("S095LO0000:NAME_TXT:0")), Is.EqualTo("Dragon Guard"));
    }

    [Test]
    public void LanguageEmptyScenarioTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [],
            [],
            [],
            [new Db.TextScenario("S095SC0000", new Db.Id("S095LO0000"), "NAME_TXT", null, 0)]);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        Assert.That(provider.GetScenarioText(new ScenarioTextId("S095LO0000:NAME_TXT:0")), Is.EqualTo("Dragon Guard"));
    }

    [Test]
    public void LanguageScenarioInfoTextTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [],
            [],
            [scenarioDb.ScenarioInfos[0] with { Name = "'Антагонисты'", Description = null }],
            []);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        Assert.That(provider.GetScenarioText(new ScenarioTextId("S095IF0000:NAME:0")), Is.EqualTo("Антагонисты"));
        Assert.That(
            provider.GetScenarioText(new ScenarioTextId("S095IF0000:DESC:0")),
            Is.EqualTo("The Emperor Demosthene decided to move his court to the north, in the fortified city of Fhindar. Once there, the Inquisitor Philip d'Agincourt would surely protect him."));
    }

    [Test]
    public void LeaderNameTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        Assert.That(provider.GetLeaderName(new LeaderNameId(WorldRace.Human, true, 0)), Is.EqualTo("Konrad"));
        Assert.That(provider.GetLeaderName(new LeaderNameId(WorldRace.Human, false, 0)), Is.EqualTo("Zolian"));
        Assert.That(provider.GetLeaderName(new LeaderNameId(WorldRace.Neutral, true, 0)), Is.EqualTo("Neutrals"));
    }

    [Test]
    public void UnknownLeaderNameTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        Assert.Throws<KeyNotFoundException>(() => provider.GetLeaderName(new LeaderNameId(WorldRace.Human, true, 30)));
    }

    [Test]
    public void LanguageLeaderNameTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        LanguageDatabase language = new(
            [],
            [
                new Db.TextLeader(new Db.Id("G000RR0000"), true, "Конрад", null, null),
                new Db.TextLeader(new Db.Id("G000RR0000"), true, "", null, null),
                new Db.TextLeader(new Db.Id("G000RR0000"), false, "Золиан", null, null)
            ],
            [],
            []);
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, language);

        Assert.That(provider.GetLeaderName(new LeaderNameId(WorldRace.Human, true, 0)), Is.EqualTo("Конрад"));
        Assert.That(provider.GetLeaderName(new LeaderNameId(WorldRace.Human, true, 1)), Is.EqualTo("Ardobert"));
        Assert.That(provider.GetLeaderName(new LeaderNameId(WorldRace.Human, true, 2)), Is.EqualTo("Clovis"));
        Assert.That(provider.GetLeaderName(new LeaderNameId(WorldRace.Human, false, 0)), Is.EqualTo("Золиан"));
    }

    [Test]
    public void UnitNameTest()
    {
        ScenarioDatabase scenarioDb = LoadScenarioDatabase();
        GameTextProvider provider = new(LoadGlobalDatabase(), scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);

        Assert.That(provider.GetUnitName(new LiteralUnitName("Uther")), Is.EqualTo("Uther"));
        Assert.That(provider.GetUnitName(new ScenarioUnitName(new ScenarioTextId("S095LO0000:NAME_TXT:0"))), Is.EqualTo("Dragon Guard"));
        Assert.That(provider.GetUnitName(new LeaderUnitName(new LeaderNameId(WorldRace.Human, true, 0))), Is.EqualTo("Konrad"));
    }
}
