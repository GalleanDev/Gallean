using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SRod.json", PerScenario = true)]
    public record ScenarioRod(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("ROD_ID")] Id RodId,
        [property: JsonPropertyName("OWNER")] Id OwnerId,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY
    );
}
