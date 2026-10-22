using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SObjMap.json", PerScenario = true)]
    public record ScenarioObjectMap(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("OBJ_ID")] Id ObjId,
        [property: JsonPropertyName("WHAT")] string What
    );
}
