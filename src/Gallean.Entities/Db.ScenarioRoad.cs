using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SRoad.json", PerScenario = true)]
    public record ScenarioRoad(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("ROAD_ID")] Id RoadId,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("INDEX")] int Index,
        [property: JsonPropertyName("VAR")] int Var
    );
}
