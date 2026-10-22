using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Gaction.json")]
    public record Action(
        [property: DataKey, JsonPropertyName("ACTION_ID")] Id ActionId,
        [property: JsonPropertyName("CATEGORY")] ActionType Category,
        [property: JsonPropertyName("CHANCE")] int Chance,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText
    );
}
