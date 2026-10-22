using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [CampaignDataFile("CCamp.json")]
    public record Campaign(
        [property: DataKey, JsonPropertyName("CAMP_ID")] Id CampaignId,
        [property: JsonPropertyName("PLAY_RACE")] RaceType PlayRace,
        [property: JsonPropertyName("NAME_ID")] string NameId,
        [property: JsonPropertyName("SKIRMISH")] bool Skirmish
    );
}
