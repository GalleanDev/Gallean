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

    private static IReadOnlyList<string> FormatStackLeaders(WorldState state, IGameDefinitionProvider definitions)
    {
        List<string> result = [];

        foreach (WorldStack stack in state.Stacks.Values)
        {
            (WorldUnit leader, GridPosition _, UnitDefinition definition) = stack.GetLeader(definitions);
            result.Add($"{stack.Id.Value} {definition.Id.Value} {leader.Name}");
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
        File.Copy(TestScenarios.DoragonRealmPath, Path.Combine(input, "Doragon Realm.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        string stacks = sb.ReadTextFile(Path.Combine("Output", "Scens", "S200SC0000", "Sstack.json"));

        Assert.That(stacks, Does.Not.Contain("S143"));
    }

    [Test]
    public void ScenarioIdMultipleTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.DoragonRealmPath, Path.Combine(input, "Doragon Realm.sg"));
        File.Copy(TestScenarios.TutorialPath, Path.Combine(input, "Tutorial.sg"));

        IReadOnlyList<ScenarioLibraryEntry> entries = ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        Assert.That(FormatEntries(entries), Is.EqualTo([
            "Doragon Realm.sg -> S200SC0000 (Doragon's Realm)",
            "Tutorial.sg -> S201SC0000 (Tutorial)"
        ]));
    }

    [Test]
    public void ScenarioIdSkipTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.DoragonRealmPath, Path.Combine(input, "Doragon Realm.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        File.Copy(Path.Combine(input, "Doragon Realm.sg"), Path.Combine(input, "Second.sg"));

        IReadOnlyList<ScenarioLibraryEntry> entries = ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        Assert.That(FormatEntries(entries), Is.EqualTo([
            "Doragon Realm.sg -> S201SC0000 (Doragon's Realm)",
            "Second.sg -> S202SC0000 (Doragon's Realm)"
        ]));
    }

    [Test]
    public void ScenarioTablesTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.DoragonRealmPath, Path.Combine(input, "Doragon Realm.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        Assert.That(sb.GetTree(), Is.EquivalentTo([
            "Input/",
            "Input/Doragon Realm.sg",
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
            "Output/Scens/S200SC0000/SEvent.json",
            "Output/Scens/S200SC0000/SFort.json",
            "Output/Scens/S200SC0000/SInventr.json",
            "Output/Scens/S200SC0000/SItem.json",
            "Output/Scens/S200SC0000/SLmark.json",
            "Output/Scens/S200SC0000/SLoc.json",
            "Output/Scens/S200SC0000/SMapBlck.json",
            "Output/Scens/S200SC0000/SMapEl.json",
            "Output/Scens/S200SC0000/SMerch.json",
            "Output/Scens/S200SC0000/SMounts.json",
            "Output/Scens/S200SC0000/SObjMap.json",
            "Output/Scens/S200SC0000/SRel.json",
            "Output/Scens/S200SC0000/SRuin.json",
            "Output/Scens/S200SC0000/SSite.json",
            "Output/Scens/S200SC0000/SStkTemp.json",
            "Output/Scens/S200SC0000/SSubRace.json",
            "Output/Scens/S200SC0000/SUGroup.json",
            "Output/Scens/S200SC0000/SUnit.json",
            "Output/Scens/S200SC0000/SUnitM.json",
            "Output/Scens/S200SC0000/Sfog.json",
            "Output/Scens/S200SC0000/Splayer.json",
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
        File.Copy(TestScenarios.DoragonRealmPath, Path.Combine(input, "Doragon Realm.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S200SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(FormatFortifications(state), Is.EqualTo([
            "S200FT0000 WorldCapital (5,5) units=1",
            "S200FT0001 WorldCapital (40,39) units=1",
            "S200FT0002 WorldCity (12,29) units=4",
            "S200FT0003 WorldCity (39,22) units=4",
            "S200FT0004 WorldCity (25,42) units=0",
            "S200FT0005 WorldCity (16,7) units=0",
            "S200RU0000 WorldRuin (31,29) units=2",
            "S200RU0001 WorldRuin (15,16) units=3",
            "S200RU0002 WorldRuin (25,4) units=1",
            "S200RU0003 WorldRuin (40,14) units=2",
            "S200RU0004 WorldRuin (5,31) units=2",
            "S200RU0005 WorldRuin (15,46) units=1"
        ]));
    }

    [Test]
    public void ScenarioLeaderNameTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.DoragonRealmPath, Path.Combine(input, "Doragon Realm.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S200SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(FormatStackLeaders(state, definitions), Is.EqualTo([
            "S200KC0000 g000uu0044 Sigulf",
            "S200KC0001 g000uu8009 Thorne",
            "S200KC0002 g000uu5120 Helthmor Dragon Of Realm ",
            "S200KC0003 g000uu5121 Wyndor Dragon Of The South Sea",
            "S200KC0004 g000uu8144 Dark Elf Gast",
            "S200KC0005 g000uu8009 Erwylle",
            "S200KC0006 g000uu8116 Jona'ath",
            "S200KC0007 g000uu8011 Kera'anil",
            "S200KC0008 g000uu8133 Thistle",
            "S200KC0009 g000uu8116 Thorne",
            "S200KC000a g000uu5105 Forest Elf",
            "S200KC000b g000uu5105 Forest Elf",
            "S200KC000c g000uu5105 Forest Elf",
            "S200KC000d g000uu5105 Forest Elf",
            "S200KC000e g000uu8116 Aellos",
            "S200KC000f g000uu8142 Dark Elf Reaper",
            "S200KC0010 g000uu5139 Wolf",
            "S200KC0011 g000uu5139 Wolf",
            "S200KC0012 g000uu5139 Wolf",
            "S200KC0013 g000uu5139 Wolf",
            "S200KC0014 g000uu8133 Cillen",
            "S200KC0015 g000uu8133 Cillen",
            "S200KC0016 g000uu8115 Thorne",
            "S200KC0017 g000uu8115 Limindell",
            "S200KC0018 g000uu8116 Eboren",
            "S200KC0019 g000uu8116 Thorne",
            "S200KC001a g000uu8133 Cillen",
            "S200KC001b g000uu8133 Rowynn",
            "S200KC001c g000uu5105 Forest Elf"
        ]));
    }

    [Test]
    public void ScenarioMapTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.DoragonRealmPath, Path.Combine(input, "Doragon Realm.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S200SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(state.Map.Width, Is.EqualTo(48));
        Assert.That(FormatMap(state), Is.EqualTo([
            "Dwarf Forest 7",
            "Dwarf None 69",
            "Elf Forest 8",
            "Elf None 78",
            "Human Forest 330",
            "Human Mountain 90",
            "Human None 688",
            "Neutral Forest 2",
            "Neutral Mountain 285",
            "Neutral None 11",
            "Water Mountain 12",
            "Water None 724"
        ]));
    }

    [Test]
    public void ScenarioPlayersTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");
        File.Copy(TestScenarios.DoragonRealmPath, Path.Combine(input, "Doragon Realm.sg"));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S200SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S200SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(state.Players.Values.Select(player => $"{player.Id.Value} {player.Race} gold={player.Resources.Gold}"), Is.EqualTo([
            "1 Neutral gold=0",
            "2 Dwarf gold=1000",
            "3 Elf gold=1000"
        ]));
    }

    [Test]
    public void CampaignTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");

        File.WriteAllBytes(
            Path.Combine(input, "The Elven Crusade.csg"),
            TestCampaignArchiveBuilder.Build("The Elven Crusade", "Emanuel Wall", new Db.Id("G000RR0005"), [
                ("TUTORIAL.SG", TestScenarios.TutorialPath),
                ("DORAGONREALM.SG", TestScenarios.DoragonRealmPath)
            ]));

        IReadOnlyList<ScenarioLibraryEntry> entries = ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        Assert.That(FormatEntries(entries), Is.EqualTo([
            "The Elven Crusade.csg!TUTORIAL.SG -> S200SC0000 (Tutorial)",
            "The Elven Crusade.csg!DORAGONREALM.SG -> S201SC0000 (Doragon's Realm)"
        ]));
    }

    [Test]
    public void CampaignPlayRaceTest()
    {
        using Sandbox sb = Sandbox.Create();

        string input = sb.CreateDirectory("Input");
        string output = sb.CreateDirectory("Output");

        File.WriteAllBytes(
            Path.Combine(input, "The Elven Crusade.csg"),
            TestCampaignArchiveBuilder.Build("The Elven Crusade", "Emanuel Wall", new Db.Id("G000RR0005"), [
                ("TUTORIAL.SG", TestScenarios.TutorialPath),
                ("DORAGONREALM.SG", TestScenarios.DoragonRealmPath)
            ]));

        ExtractScenarioCommand.Execute(input, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(Path.Combine(output, "Scens"), new Db.ScenarioId("S201SC0000"));
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        WorldState state = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId("S201SC0000"), definitions, new GlobalVariables()).State;

        Assert.That(FormatPlayers(state), Is.EqualTo([
            "1 Neutral ai",
            "2 Dwarf ai",
            "3 Elf human"
        ]));
    }
}
