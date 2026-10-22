using System.Text;
using Gallean.Engine;
using Gallean.Entities.GameData;
using Gallean.Tests.Common;
using GameContent;

namespace Extractor.Tests;

[Explicit]
public sealed class ScenarioFileCorpusTests
{
    private static string GetOriginalAssetsPath()
    {
        string path = GameFiles.Original.Root;

        if (!Directory.Exists(path))
        {
            Assert.Ignore($"Unable to find original game assets at '{path}'.");
        }

        return path;
    }

    private static string FormatScenario(ScenarioFileTables tables)
    {
        Db.ScenarioInfo info = tables.ScenarioInfos.Single();

        return $"{info.Name.Trim()} {info.MapSize}x{info.MapSize} " +
               $"players={tables.ScenarioPlayers.Count} stacks={tables.ScenarioStacks.Count} " +
               $"units={tables.ScenarioUnits.Count} forts={tables.ScenarioFortifications.Count}";
    }

    [Test]
    public void ScenarioTest()
    {
        string originalAssetsPath = GetOriginalAssetsPath();
        StringBuilder report = new();
        int total = 0;
        int parsed = 0;

        foreach (string path in Directory.EnumerateFiles(originalAssetsPath, "*.sg", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
        {
            total++;

            try
            {
                ScenarioFile file = ScenarioFileLoader.Load(path, TestScenarios.Encoding);
                parsed++;
                report.AppendLine($"OK   {Path.GetFileName(path)} | {file.ScenarioId} | {FormatScenario(file.Tables)}");
            }
            catch (Exception ex)
            {
                report.AppendLine($"FAIL {Path.GetFileName(path)} | {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(report.ToString());
        TestContext.Out.WriteLine($"Parsed {parsed} of {total} scenario files.");

        Assert.That(parsed, Is.EqualTo(total));
    }

    [Test]
    public void CampaignTest()
    {
        string originalAssetsPath = GetOriginalAssetsPath();
        StringBuilder report = new();
        int total = 0;
        int parsed = 0;

        foreach (string path in Directory.EnumerateFiles(originalAssetsPath, "*.csg", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
        {
            total++;

            try
            {
                CampaignFile campaign = CampaignFileLoader.Load(path, TestScenarios.Encoding);
                parsed++;
                report.AppendLine($"OK   {Path.GetFileName(path)} | {campaign.Header.Name} | {campaign.Header.Author} | {campaign.Header.PlayRaceId}");

                foreach (CampaignFileScenario scenario in campaign.Scenarios)
                {
                    report.AppendLine($"       {scenario.FileName} | {FormatScenario(scenario.Scenario.Tables)}");
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"FAIL {Path.GetFileName(path)} | {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(report.ToString());
        TestContext.Out.WriteLine($"Parsed {parsed} of {total} campaign files.");

        Assert.That(parsed, Is.EqualTo(total));
    }

    [Test]
    public void ExportsTest()
    {
        using Sandbox sb = Sandbox.Create();

        string exports = Path.Combine(GetOriginalAssetsPath(), "Exports");

        if (!Directory.Exists(exports))
        {
            Assert.Ignore($"'{exports}' was not found.");
        }

        string output = sb.CreateDirectory("Output");

        IReadOnlyList<ScenarioLibraryEntry> entries = ExtractScenarioCommand.Execute(exports, output, TestScenarios.Encoding);

        GlobalDatabase globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        CampaignDatabase campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDb);
        string scenarioDirectory = Path.Combine(output, "Scens");

        StringBuilder report = new();
        int loaded = 0;

        foreach (ScenarioLibraryEntry entry in entries)
        {
            try
            {
                ScenarioDatabase scenarioDb = ScenarioDatabaseLoader.Load(scenarioDirectory, entry.ScenarioId);
                Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId(entry.ScenarioId.Value), definitions, new GlobalVariables());
                loaded++;
                report.AppendLine($"OK   {entry.ScenarioId} {entry.Name} " +
                                  $"{scenario.State.Map.Width}x{scenario.State.Map.Height} stacks={scenario.State.Stacks.Count}");
            }
            catch (Exception ex)
            {
                report.AppendLine($"FAIL {entry.ScenarioId} {entry.Name} | {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(report.ToString());
        TestContext.Out.WriteLine($"Loaded {loaded} of {entries.Count} converted scenarios.");

        Assert.That(loaded, Is.EqualTo(entries.Count));
    }
}
