using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Tleader.json")]
    public record TextLeader(
        [property: JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("SEX_M")] bool? IsMale,
        [property: JsonPropertyName("TEXT")] string Text,
        [property: JsonPropertyName("VERIFIED")] bool? Verified,
        [property: JsonPropertyName("CONTEXT")] string? Context
    );
}
