using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SBag.json", PerScenario = true)]
    public record ScenarioBag(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("BAG_ID")] Id BagId,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("OBJV")] string? ObjValue,
        [property: JsonPropertyName("IMAGE")] int Image,
        [property: JsonPropertyName("AIPRIORITY")] int AiPriority
    );
}
