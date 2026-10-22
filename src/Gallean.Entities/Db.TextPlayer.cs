using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Tplayer.json")]
    public record TextPlayer(
        [property: JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("TEXT_M")] string TextMale,
        [property: JsonPropertyName("TEXT_F")] string TextFemale,
        [property: JsonPropertyName("VERIFIED")] bool Verified,
        [property: JsonPropertyName("CONTEXT")] string? Context
    );
}
