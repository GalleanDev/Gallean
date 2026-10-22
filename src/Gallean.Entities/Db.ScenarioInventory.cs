using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SInventr.json", PerScenario = true)]
    public record ScenarioInventory(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("INV_ID")] Id InventoryId,
        [property: JsonPropertyName("ITEM_ID")] Id ItemId
    );
}
