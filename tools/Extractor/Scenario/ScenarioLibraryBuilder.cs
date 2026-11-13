using System.Text.Json;

namespace Extractor;

internal sealed class ScenarioLibraryBuilder(
    string scenarioDirectory,
    string campaignDirectory,
    string globalDirectory,
    ScenarioTextEncoding encoding)
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

    public IReadOnlyList<ScenarioLibraryEntry> Add(string inputDirectory)
    {
        List<ScenarioLibraryEntry> entries = [];

        foreach (string path in EnumerateInputFiles(inputDirectory, ScenarioExtension))
        {
            using FileStream stream = File.OpenRead(path);

            ScenarioFile file = ScenarioFileLoader.Load(stream, encoding, AllocateScenarioId());

            entries.Add(AddScenario(Path.GetFileName(path), file, SkirmishCampaignId));
        }

        foreach (string path in EnumerateInputFiles(inputDirectory, CampaignExtension))
        {
            entries.AddRange(AddCampaign(path));
        }

        return entries;
    }

    private static IEnumerable<string> EnumerateInputFiles(string inputDirectory, string extension)
    {
        EnumerationOptions enumerationOptions = new()
        {
            RecurseSubdirectories = true,
            MatchCasing = MatchCasing.CaseInsensitive
        };

        IEnumerable<string> files = Directory.EnumerateFiles(inputDirectory, "*" + extension, enumerationOptions);

        return files.Order(StringComparer.OrdinalIgnoreCase);
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

    private static List<JsonElement> ReadRows(string path)
    {
        if (File.Exists(path))
        {
            return DbFileReader.Read(path);
        }
        else
        {
            return [];
        }
    }

    private static bool HasId(JsonElement row, string column, Db.Id id)
    {
        JsonElement value = row.GetProperty(column);

        return string.Equals(value.GetString(), id.Value, StringComparison.OrdinalIgnoreCase);
    }

    private Db.RaceType ReadRaceType(Db.Id raceId)
    {
        string path = Path.Combine(globalDirectory, "Grace.json");

        foreach (JsonElement row in ReadRows(path))
        {
            if (HasId(row, "RACE_ID", raceId))
            {
                JsonElement raceType = row.GetProperty("RACE_TYPE");

                return (Db.RaceType)raceType.GetInt32();
            }
        }

        throw new InvalidDataException($"Cannot find race '{raceId.Value}' in '{path}'.");
    }

    private int ReadNextScenarioNumber(Db.Id campaignId)
    {
        int result = 1;

        foreach (JsonElement row in ReadRows(Path.Combine(campaignDirectory, "CScen.json")))
        {
            JsonElement number = row.GetProperty("SCEN_NO");

            if (HasId(row, "CAMP_ID", campaignId) && number.ValueKind == JsonValueKind.Number)
            {
                result = Math.Max(result, number.GetInt32() + 1);
            }
        }

        return result;
    }

    private Db.ScenarioId AllocateScenarioId()
    {
        for (int slot = FirstSlot; slot <= LastSlot; slot++)
        {
            Db.ScenarioId scenarioId = new($"S{slot:D3}SC0000");

            if (!Directory.Exists(Path.Combine(scenarioDirectory, scenarioId.Value)))
            {
                return scenarioId;
            }
        }

        throw new InvalidOperationException("Cannot find scenario slot.");
    }

    private Db.Id AllocateCampaignId()
    {
        List<JsonElement> rows = ReadRows(Path.Combine(campaignDirectory, "CCamp.json"));

        for (int slot = FirstSlot; slot <= LastSlot; slot++)
        {
            Db.Id campaignId = new($"C000CC{slot:D4}");

            if (!rows.Exists(row => HasId(row, "CAMP_ID", campaignId)))
            {
                return campaignId;
            }
        }

        throw new InvalidOperationException("Cannot find campaign slot.");
    }

    private List<ScenarioLibraryEntry> AddCampaign(string path)
    {
        CampaignFileContent campaign = CampaignFileLoader.Read(path, encoding);
        Db.Id campaignId = AllocateCampaignId();
        Db.Id nameId = new($"X000TAE{campaignId.TypeIndex:X3}");
        Db.RaceType playRace = ReadRaceType(campaign.Header.PlayRaceId);

        ScenarioTableWriter.AppendCampaigns(campaignDirectory, [new Db.Campaign(campaignId, playRace, nameId.Value, Skirmish: false, CanImport: true)]);
        ScenarioTableWriter.AppendTextGlobals(globalDirectory, [new Db.TextGlobal(nameId, campaign.Header.Name, Verified: false, Context: null)]);

        List<ScenarioLibraryEntry> entries = [];

        foreach (CampaignFileEntry entry in campaign.Scenarios)
        {
            using MemoryStream stream = new(entry.Data, writable: false);

            ScenarioFile file = ScenarioFileLoader.Load(stream, encoding, AllocateScenarioId());
            string source = $"{Path.GetFileName(path)}!{entry.FileName}";

            entries.Add(AddScenario(source, SetHumanPlayers(file, campaign.Header.PlayRaceId), campaignId));
        }

        return entries;
    }

    private ScenarioLibraryEntry AddScenario(string source, ScenarioFile file, Db.Id campaignId)
    {
        Db.ScenarioInfo fileInfo = file.Tables.ScenarioInfos.Single();
        Db.ScenarioInfo info = fileInfo with { CampaignId = campaignId };
        Db.CampaignScenario campaignScenario = new(
            campaignId,
            ReadNextScenarioNumber(campaignId),
            file.ScenarioId,
            DefaultBriefingVideo,
            DefaultWinVideo,
            DefaultLoseVideo,
            DefaultWinAudio);

        ScenarioTableWriter.WriteScenario(Path.Combine(scenarioDirectory, file.ScenarioId.Value), file.Tables);
        ScenarioTableWriter.AppendScenarioInfos(scenarioDirectory, [info]);
        ScenarioTableWriter.AppendCampaignScenarios(campaignDirectory, [campaignScenario]);

        return new ScenarioLibraryEntry(source, file.ScenarioId, info.Name);
    }
}
