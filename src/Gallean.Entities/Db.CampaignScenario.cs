using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [CampaignDataFile("CScen.json")]
    public record CampaignScenario(
        [property: DataLookupKey, JsonPropertyName("CAMP_ID")] Id CampaignId,
        [property: JsonPropertyName("SCEN_NO")] int? ScenarioNumber,
        [property: JsonPropertyName("SCEN_ID")] ScenarioId ScenarioId,
        [property: JsonPropertyName("SMK_BRIEF")] string BriefingVideo,
        [property: JsonPropertyName("SMK_WIN")] string WinVideo,
        [property: JsonPropertyName("SMK_LOSE")] string LoseVideo,
        [property: JsonPropertyName("WAV_WIN")] string WinAudio
    );
}
