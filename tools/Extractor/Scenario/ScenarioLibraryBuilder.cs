using System.Text.Json;

namespace Extractor;

internal sealed class ScenarioLibraryBuilder(string scenarioDirectory, string campaignDirectory, ScenarioTextEncoding encoding)
{
    private const int FirstSlot = 200;
    private const int LastSlot = 999;

    private const string ScenarioExtension = ".sg";
    private const string CampaignExtension = ".csg";
    private const string DefaultBriefingVideo = "DEFAULT";
    private const string DefaultWinVideo = "DEFWIN";
    private const string DefaultLoseVideo = "DEFLOSE";
    private const string DefaultWinAudio = "GENBRIEF";

    private static readonly Db.Id SkirmishCampaignId = new("C000CC0001");

    private readonly List<Db.CampaignScenario> campaignScenarios = [];
    private readonly List<Db.ScenarioInfo> infos = [];
    private HashSet<string> usedScenarioIds = new(StringComparer.OrdinalIgnoreCase);
    private int nextSlot = FirstSlot;
    private int nextScenarioNumber = 1;

    public IReadOnlyList<ScenarioLibraryEntry> Add(string inputDirectory)
    {
        usedScenarioIds = new HashSet<string>(ReadUsedScenarioIds().Select(id => id.Value), StringComparer.OrdinalIgnoreCase);
        nextSlot = FirstSlot;
        nextScenarioNumber = ReadNextScenarioNumber();

        List<ScenarioLibraryEntry> entries = [];

        foreach (string path in EnumerateInputFiles(inputDirectory, ScenarioExtension))
        {
            using FileStream stream = File.OpenRead(path);

            entries.Add(AddScenario(
                Path.GetFileName(path),
                ScenarioFileLoader.Load(stream, encoding, AllocateScenarioId())));
        }

        foreach (string path in EnumerateInputFiles(inputDirectory, CampaignExtension))
        {
            CampaignFileContent campaign = CampaignFileLoader.Read(path, encoding);

            foreach (CampaignFileEntry entry in campaign.Scenarios)
            {
                using MemoryStream stream = new(entry.Data, writable: false);

                ScenarioFile file = ScenarioFileLoader.Load(stream, encoding, AllocateScenarioId());

                entries.Add(AddScenario(
                    $"{Path.GetFileName(path)}!{entry.FileName}",
                    SetHumanPlayers(file, campaign.Header.PlayRaceId)));
            }
        }

        ScenarioTableWriter.AppendScenarioInfos(scenarioDirectory, infos);
        ScenarioTableWriter.AppendCampaignScenarios(campaignDirectory, campaignScenarios);

        return entries;
    }

    private static IEnumerable<string> EnumerateInputFiles(string inputDirectory, string extension)
    {
        return Directory.EnumerateFiles(inputDirectory, "*" + extension, SearchOption.AllDirectories)
            .Order(StringComparer.OrdinalIgnoreCase);
    }

    private static ScenarioFile SetHumanPlayers(ScenarioFile file, Db.Id playRaceId)
    {
        return file with
        {
            Tables = file.Tables with
            {
                ScenarioPlayers =
                [
                    .. file.Tables.ScenarioPlayers.Select(player => player with { IsHuman = player.RaceId == playRaceId })
                ]
            }
        };
    }

    private IReadOnlyList<Db.ScenarioId> ReadUsedScenarioIds()
    {
        return
        [
            .. Directory.EnumerateDirectories(scenarioDirectory)
                .Select(directory => new Db.ScenarioId(Path.GetFileName(directory)))
        ];
    }

    private int ReadNextScenarioNumber()
    {
        string path = Path.Combine(campaignDirectory, "CScen.json");

        if (!File.Exists(path))
        {
            return 1;
        }

        List<JsonElement> rows = DbFileReader.Read(path);
        int result = 1;

        foreach (JsonElement row in rows)
        {
            if (!row.TryGetProperty("CAMP_ID", out JsonElement campaign) ||
                !string.Equals(campaign.GetString(), SkirmishCampaignId.Value, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (row.TryGetProperty("SCEN_NO", out JsonElement number) && number.ValueKind == JsonValueKind.Number)
            {
                result = Math.Max(result, number.GetInt32() + 1);
            }
        }

        return result;
    }

    private Db.ScenarioId AllocateScenarioId()
    {
        while (nextSlot <= LastSlot)
        {
            Db.ScenarioId candidate = new($"S{nextSlot:D3}SC0000");
            nextSlot++;

            if (usedScenarioIds.Add(candidate.Value))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Cannot find scenario slot.");
    }

    private ScenarioLibraryEntry AddScenario(string source, ScenarioFile file)
    {
        Db.ScenarioInfo info = file.Tables.ScenarioInfos.Single();

        ScenarioTableWriter.WriteScenario(Path.Combine(scenarioDirectory, file.ScenarioId.Value), file.Tables);

        infos.Add(info);
        campaignScenarios.Add(new Db.CampaignScenario(
            SkirmishCampaignId,
            nextScenarioNumber,
            file.ScenarioId,
            DefaultBriefingVideo,
            DefaultWinVideo,
            DefaultLoseVideo,
            DefaultWinAudio));

        nextScenarioNumber++;

        return new ScenarioLibraryEntry(source, file.ScenarioId, info.Name);
    }
}
