using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SLmark.json", PerScenario = true)]
    public record ScenarioLandmark(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("LMARK_ID")] Id LandmarkId,
        [property: JsonPropertyName("TYPE")] Id TypeId,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("DESC_TXT")] string? DescriptionText
    );
}
