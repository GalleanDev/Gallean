using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GLmark.json")]
    public record Landmark(
        [property: DataKey, JsonPropertyName("LMARK_ID")] Id LandmarkId,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText,
        [property: JsonPropertyName("CX")] int Cx,
        [property: JsonPropertyName("CY")] int Cy,
        [property: JsonPropertyName("MOUNTAIN")] bool? Mountain,
        [property: JsonPropertyName("CATEGORY")] LandmarkType LandmarkType
    );
}
