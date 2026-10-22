using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Glord.json")]
    public record Lord(
        [property: DataKey, JsonPropertyName("LORD_ID")] Id LordId,
        [property: JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("CATEGORY")] LordType Category,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText,
        [property: JsonPropertyName("PIC")] string? Pic
    );
}
