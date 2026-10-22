using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SCrys.json", PerScenario = true)]
    public record ScenarioCrystal(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("CRYSTAL_ID")] Id CrystalId,
        [property: JsonPropertyName("RESOURCE")] int Resource,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("AIPRIORITY")] int AiPriority
    );
}
