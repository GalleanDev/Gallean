using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SLoc.json", PerScenario = true)]
    public record ScenarioLocation(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("LOC_ID")] Id LocationId,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("RADIUS")] int Radius
    );
}
