using System.Text.Json;
using Gallean.Engine;
using Gallean.Entities.GameData;
using Gallean.Tests.Common;

namespace Extractor.Tests;

public sealed class ExtractScenarioTests
{
    private static IReadOnlyList<string> FormatEntries(IReadOnlyList<ScenarioLibraryEntry> entries)
    {
        return [.. entries.Select(entry => $"{entry.Source} -> {entry.ScenarioId} ({entry.Name})")];
    }

    private static IReadOnlyList<string> FormatTextGlobals(string path)
    {
        List<string> rows = [];

        foreach (JsonElement row in DbFileReader.Read(path))
        {
            JsonElement id = row.GetProperty("TXT_ID");
            JsonElement text = row.GetProperty("TEXT");

            rows.Add($"{id.GetString()} {text.GetString()}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatScenarioCampaigns(string path)
    {
        List<string> rows = [];

        foreach (JsonElement row in DbFileReader.Read(path))
        {
            JsonElement scenarioId = row.GetProperty("NO_SCEN");
            JsonElement campaignId = row.GetProperty("CAMPAIGN");

            rows.Add($"{scenarioId.GetString()} {campaignId.GetString()}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatPlayers(WorldState state)
    {
        List<string> rows = [];

        foreach (WorldPlayer player in state.Players.Values)
        {
            string control = "ai";

            if (!player.IsAi)
            {
                control = "human";
            }

            rows.Add($"{player.Id} {player.Race} {control}");
        }

        return rows;
    }

    private static IReadOnlyList<string> FormatMap(WorldState state)
    {
        Dictionary<string, int> counts = [];

        foreach (WorldMapCell cell in state.Map.Cells)
        {
            string key = $"{cell.Terrain} {cell.Feature}";
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        return [.. counts.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => $"{entry.Key} {entry.Value}")];
    }

    private static IReadOnlyList<string> FormatStackLeaders(WorldState state, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        List<string> result = [];

        foreach (WorldStack stack in state.Stacks.Values)
        {
            (WorldUnit leader, GridPosition _, UnitDefinition definition) = stack.GetLeader(definitions);
            UnitName name = Assert.NotNull(leader.Name);
            result.Add($"{stack.Id.Value} {definition.Id.Value} {texts.GetUnitName(name)}");
        }

        return result;
    }

    private static IReadOnlyList<string> FormatFortifications(WorldState state)
    {
        List<string> result = [];

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            result.Add($"{fortification.Id.Value} {fortification.GetType().Name} " +
                       $"({fortification.Entrance.X},{fortification.Entrance.Y}) units={fortification.Garrison.GetUnits().Count()}");
        }

        return result;
    }

    [Test]
    public void ScenarioIdTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.GreywaterCrossingPath, Path.Combine(input, "Greywater Crossing.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        string stacks = sb.ReadTextFile(Path.Combine("Output", "Scens", "S200SC0000", "Sstack.json"));

        Assert.That(stacks, Does.Not.Contain("S417"));
    }

    [Test]
    public void ScenarioIdMultipleTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.GreywaterCrossingPath, Path.Combine(input, "Greywater Crossing.sg"));
        File.Copy(TestScenarios.GreywaterSiegePath, Path.Combine(input, "Greywater Siege.sg"));

        IReadOnlyList<ScenarioLibraryEntry> entries = ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        Assert.That(FormatEntries(entries), Is.EqualTo([
            "Greywater Crossing.sg -> S200SC0000 (Greywater Crossing)",
            "Greywater Siege.sg -> S201SC0000 (Greywater Siege)"
        ]));
    }

    [Test]
    public void ScenarioIdSkipTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.GreywaterCrossingPath, Path.Combine(input, "Greywater Crossing.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        File.Copy(Path.Combine(input, "Greywater Crossing.sg"), Path.Combine(input, "Second.sg"));

        IReadOnlyList<ScenarioLibraryEntry> entries = ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        Assert.That(FormatEntries(entries), Is.EqualTo([
            "Greywater Crossing.sg -> S201SC0000 (Greywater Crossing)",
            "Second.sg -> S202SC0000 (Greywater Crossing)"
        ]));
    }

    [Test]
    public void ScenarioTablesTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.GreywaterCrossingPath, Path.Combine(input, "Greywater Crossing.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        Assert.That(sb.GetTree(), Is.EquivalentTo([
            "Input/",
            "Input/Greywater Crossing.sg",
            "Output/",
            "Output/Campaign/",
            "Output/Campaign/CScen.json",
            "Output/Scens/",
            "Output/Scens/Sinfo.json",
            "Output/Scens/S200SC0000/",
            "Output/Scens/S200SC0000/SBag.json",
            "Output/Scens/S200SC0000/SCrys.json",
            "Output/Scens/S200SC0000/SEvCond.json",
            "Output/Scens/S200SC0000/SEvEffec.json",
            "Output/Scens/S200SC0000/SEvUnfog.json",
            "Output/Scens/S200SC0000/SEvent.json",
            "Output/Scens/S200SC0000/SFort.json",
            "Output/Scens/S200SC0000/SInventr.json",
            "Output/Scens/S200SC0000/SItem.json",
            "Output/Scens/S200SC0000/SLmark.json",
            "Output/Scens/S200SC0000/SLoc.json",
            "Output/Scens/S200SC0000/SMage.json",
            "Output/Scens/S200SC0000/SMapBlck.json",
            "Output/Scens/S200SC0000/SMapEl.json",
            "Output/Scens/S200SC0000/SMerch.json",
            "Output/Scens/S200SC0000/SMercs.json",
            "Output/Scens/S200SC0000/SMounts.json",
            "Output/Scens/S200SC0000/SObjMap.json",
            "Output/Scens/S200SC0000/SRel.json",
            "Output/Scens/S200SC0000/SRoad.json",
            "Output/Scens/S200SC0000/SRuin.json",
            "Output/Scens/S200SC0000/SSite.json",
            "Output/Scens/S200SC0000/SStkTemM.json",
            "Output/Scens/S200SC0000/SStkTemp.json",
            "Output/Scens/S200SC0000/SSubRace.json",
            "Output/Scens/S200SC0000/STalisCh.json",
            "Output/Scens/S200SC0000/SUGroup.json",
            "Output/Scens/S200SC0000/SUnit.json",
            "Output/Scens/S200SC0000/SUnitM.json",
            "Output/Scens/S200SC0000/SVars.json",
            "Output/Scens/S200SC0000/Sfog.json",
            "Output/Scens/S200SC0000/Splayer.json",
            "Output/Scens/S200SC0000/Sspell.json",
            "Output/Scens/S200SC0000/Sstack.json",
            "Output/Scens/S200SC0000/Tscen.json"
        ]));
    }

    [Test]
    public void ScenarioLoadTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.GreywaterCrossingPath, Path.Combine(input, "Greywater Crossing.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S200SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(FormatFortifications(state), Is.EqualTo([
            "S200FT0000 WorldCapital (6,6) units=1",
            "S200FT0001 WorldCapital (45,45) units=1",
            "S200FT0002 WorldCity (23,24) units=2",
            "S200FT0003 WorldCity (37,35) units=0",
            "S200FT0004 WorldCity (33,43) units=0",
            "S200RU0000 WorldRuin (12,32) units=2",
            "S200RU0001 WorldRuin (35,12) units=0"
        ]));
    }

    [Test]
    public void ScenarioLeaderNameTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.GreywaterCrossingPath, Path.Combine(input, "Greywater Crossing.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S200SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        IGameTextProvider texts = new GameTextProvider(globalDb, scenarioDb, scenarioDb.TextScenarios, LanguageDatabase.Empty);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(FormatStackLeaders(state, definitions, texts), Is.EqualTo([
            "S200KC0000 g000uu0044 Halgrim",
            "S200KC0001 g000uu0098 Ysolde",
            "S200KC0002 g000uu5103 Warden Orlec",
            "S200KC0003 g000uu5113 Grukk",
            "S200KC0004 g000uu5139 Greymane",
            "S200KC0005 g000uu0047 Borrin",
            "S200KC0006 g000uu0097 Vessa",
            "S200KC0007 g000uu5123 The Reed Wyrm"
        ]));
    }

    [Test]
    public void ScenarioMapTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.GreywaterCrossingPath, Path.Combine(input, "Greywater Crossing.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S200SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(state.Map.Width, Is.EqualTo(48));
        Assert.That(FormatMap(state), Is.EqualTo([
            "Dwarf None 97",
            "Dwarf Road 3",
            "Neutral Forest 66",
            "Neutral Mountain 48",
            "Neutral None 1861",
            "Neutral Road 1",
            "Undead None 100",
            "Water None 128"
        ]));
    }

    [Test]
    public void ScenarioPlayersTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.GreywaterCrossingPath, Path.Combine(input, "Greywater Crossing.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S200SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(state.Players.Values.Select(player => $"{player.Id.Value} {player.Race} gold={player.Resources.Gold}"), Is.EqualTo([
            "1 Neutral gold=0",
            "2 Dwarf gold=800",
            "3 Undead gold=700"
        ]));
    }

    [Test]
    public void CampaignTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        string globals = sb.CreateDirectory("Output/Globals");

        File.Copy(Path.Combine(RiseOfTheElves.GlobalDatabasePath, "Grace.json"), Path.Combine(globals, "Grace.json"));

        File.WriteAllBytes(
            Path.Combine(input, "The Greywater Chronicle.csg"),
            TestCampaignArchiveBuilder.Build("The Greywater Chronicle", "Gallean Contributors", new Db.Id("G000RR0003"), [
                ("CROSSING.SG", TestScenarios.GreywaterCrossingPath),
                ("SIEGE.SG", TestScenarios.GreywaterSiegePath)
            ]));

        IReadOnlyList<ScenarioLibraryEntry> entries = ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        Assert.That(FormatEntries(entries), Is.EqualTo([
            "The Greywater Chronicle.csg!CROSSING.SG -> S200SC0000 (Greywater Crossing)",
            "The Greywater Chronicle.csg!SIEGE.SG -> S201SC0000 (Greywater Siege)"
        ]));

        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(Path.Combine(output, "Campaign"));

        Assert.That(campaignDb.Campaigns.Values, Is.EqualTo([
            new Db.Campaign(new Db.Id("C000CC0200"), Db.RaceType.Undead, "X000TAE200", Skirmish: false, CanImport: true)
        ]));
        Assert.That(campaignDb.CampaignScenarios, Is.EqualTo([
            new Db.CampaignScenario(new Db.Id("C000CC0200"), 1, new Db.ScenarioId("S200SC0000"), "DEFAULT", "DEFWIN", "DEFLOSE", "GENBRIEF"),
            new Db.CampaignScenario(new Db.Id("C000CC0200"), 2, new Db.ScenarioId("S201SC0000"), "DEFAULT", "DEFWIN", "DEFLOSE", "GENBRIEF")
        ]));
        Assert.That(FormatTextGlobals(Path.Combine(globals, "Tglobal.json")), Is.EqualTo([
            "X000TAE200 The Greywater Chronicle"
        ]));
        Assert.That(FormatScenarioCampaigns(Path.Combine(output, "Scens", "Sinfo.json")), Is.EqualTo([
            "S200SC0000 C000CC0200",
            "S201SC0000 C000CC0200"
        ]));
    }

    [Test]
    public void CampaignPlayRaceTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        string globals = sb.CreateDirectory("Output/Globals");

        File.Copy(Path.Combine(RiseOfTheElves.GlobalDatabasePath, "Grace.json"), Path.Combine(globals, "Grace.json"));

        File.WriteAllBytes(
            Path.Combine(input, "The Greywater Chronicle.csg"),
            TestCampaignArchiveBuilder.Build("The Greywater Chronicle", "Gallean Contributors", new Db.Id("G000RR0003"), [
                ("CROSSING.SG", TestScenarios.GreywaterCrossingPath),
                ("SIEGE.SG", TestScenarios.GreywaterSiegePath)
            ]));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(Path.Combine(output, "Campaign"));
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S201SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S201SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(FormatPlayers(state), Is.EqualTo([
            "1 Neutral ai",
            "2 Dwarf ai",
            "3 Undead human"
        ]));
    }
}
