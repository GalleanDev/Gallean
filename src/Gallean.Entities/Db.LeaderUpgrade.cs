using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GleaUpg.json")]
    public record LeaderUpgrade(
        [property: DataLookupKey, JsonPropertyName("BELONGS_TO")] Id BelongsToId,
        [property: JsonPropertyName("MODIF_ID")] Id ModifierId,
        [property: JsonPropertyName("MIN_LEVEL")] int MinLevel,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText,
        [property: JsonPropertyName("PRIORITY")] int Priority
    );
}
