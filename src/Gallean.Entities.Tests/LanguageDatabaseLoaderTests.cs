using Gallean.Entities.GameData;
using Gallean.Tests.Common;

namespace Gallean.Entities.Tests;

public sealed class LanguageDatabaseLoaderTests
{
    [Test]
    public void LoadTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.CreateDirectory("Globals");
        sb.CreateDirectory("Scens/S253SC0000");
        sb.WriteTextFile("Globals/Tglobal.json", "{\"formatVersion\": 1, \"rows\": [{\"TXT_ID\": \"x000tg0000\", \"TEXT\": \"Империя\", \"VERIFIED\": false, \"CONTEXT\": \"Race name\"}]}");
        sb.WriteTextFile("Globals/Tleader.json", "{\"formatVersion\": 1, \"rows\": [{\"RACE_ID\": \"g000rr0000\", \"SEX_M\": true, \"TEXT\": \"Конрад\", \"VERIFIED\": false, \"CONTEXT\": null}]}");
        sb.WriteTextFile("Scens/S253SC0000/Tscen.json", "{\"formatVersion\": 1, \"rows\": [{\"NO_SCEN\": \"S253SC0000\", \"ID_OBJECT\": \"S253FT0003\", \"ID_STRING\": \"NAME_TXT\", \"TEXT\": \"Неферис\", \"SEQ_NUM\": 0}]}");

        LanguageDatabase index = LanguageDatabaseLoader.LoadIndex(sb.GetPath("Globals"), sb.GetPath("Scens"));
        LanguageDatabase tutorial = LanguageDatabaseLoader.Load(sb.GetPath("Globals"), sb.GetPath("Scens"), new Db.ScenarioId("S253SC0000"));
        LanguageDatabase other = LanguageDatabaseLoader.Load(sb.GetPath("Globals"), sb.GetPath("Scens"), new Db.ScenarioId("S095SC0000"));

        Assert.That(index.TextGlobals, Is.EqualTo([new Db.TextGlobal(new Db.Id("x000tg0000"), "Империя", false, "Race name")]));
        Assert.That(index.TextLeaders, Is.EqualTo([new Db.TextLeader(new Db.Id("g000rr0000"), true, "Конрад", false, null)]));
        Assert.That(index.ScenarioInfos, Is.Empty);
        Assert.That(index.TextScenarios, Is.Empty);
        Assert.That(tutorial.TextGlobals, Is.EqualTo([new Db.TextGlobal(new Db.Id("x000tg0000"), "Империя", false, "Race name")]));
        Assert.That(tutorial.TextLeaders, Is.EqualTo([new Db.TextLeader(new Db.Id("g000rr0000"), true, "Конрад", false, null)]));
        Assert.That(tutorial.ScenarioInfos, Is.Empty);
        Assert.That(tutorial.TextScenarios, Is.EqualTo([new Db.TextScenario("S253SC0000", new Db.Id("S253FT0003"), "NAME_TXT", "Неферис", 0)]));
        Assert.That(other.TextGlobals, Is.EqualTo([new Db.TextGlobal(new Db.Id("x000tg0000"), "Империя", false, "Race name")]));
        Assert.That(other.TextLeaders, Is.EqualTo([new Db.TextLeader(new Db.Id("g000rr0000"), true, "Конрад", false, null)]));
        Assert.That(other.ScenarioInfos, Is.Empty);
        Assert.That(other.TextScenarios, Is.Empty);
    }

    [TestCase("cs", 2292, 251, 70)]
    [TestCase("de", 2292, 251, 63)]
    [TestCase("es", 2292, 251, 58)]
    [TestCase("fr", 2292, 251, 51)]
    [TestCase("it", 2292, 251, 58)]
    [TestCase("pl", 2292, 251, 86)]
    [TestCase("ru", 2293, 251, 111)]
    public void LanguagesTest(string code, int textGlobals, int textLeaders, int scenarioInfos)
    {
        GlobalDatabaseIndex globalDb = GlobalDatabaseLoader.LoadIndex(RiseOfTheElves.GlobalDatabasePath);
        ScenarioDatabaseIndex scenarioDb = ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath);
        HashSet<string> globalTextIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> scenarioInfoIds = new(StringComparer.OrdinalIgnoreCase);

        foreach (Db.Id id in globalDb.TextGlobals.Keys)
        {
            globalTextIds.Add(id.Value);
        }

        foreach (Db.ScenarioInfo info in scenarioDb.ScenarioInfos)
        {
            scenarioInfoIds.Add(info.InfoId.Value);
        }

        LanguageDatabase db = LanguageDatabaseLoader.LoadIndex(
            RiseOfTheElves.GetLanguageGlobalDatabasePath(code),
            RiseOfTheElves.GetLanguageScenarioDatabasePath(code));

        foreach (Db.TextGlobal text in db.TextGlobals)
        {
            Assert.That(globalTextIds, Does.Contain(text.TextId.Value));
        }

        foreach (Db.ScenarioInfo info in db.ScenarioInfos)
        {
            Assert.That(scenarioInfoIds, Does.Contain(info.InfoId.Value));
        }

        Assert.That(db.TextGlobals.Count, Is.EqualTo(textGlobals));
        Assert.That(db.TextLeaders.Count, Is.EqualTo(textLeaders));
        Assert.That(db.ScenarioInfos.Count, Is.EqualTo(scenarioInfos));
    }
}
