using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SItem.json", PerScenario = true)]
    public record ScenarioItem(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("ITEM_ID")] Id ItemId,
        [property: JsonPropertyName("ITEM_TYPE")] Id ItemType
    );
}
