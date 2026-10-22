using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SCount.json", PerScenario = true)]
    public record ScenarioCount(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("ID")] string Id,
        [property: JsonPropertyName("TYPE")] Id TypeId,
        [property: JsonPropertyName("COUNT")] int Count
    );
}
