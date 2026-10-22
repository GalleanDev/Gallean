using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SMapBlck.json", PerScenario = true)]
    public record ScenarioMapBlock(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("BLOCKID")] Id BlockId,
        [property: JsonPropertyName("BLOCKDATA")] string BlockData
    );
}
