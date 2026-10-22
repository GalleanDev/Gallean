using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("TAiMsg.json")]
    public record AiMessage(
        [property: JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("MSG_CAT")] int MessageCategory,
        [property: JsonPropertyName("TEXT")] string Text
    );
}
