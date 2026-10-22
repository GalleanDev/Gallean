using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SMerch.json", PerScenario = true)]
    public record ScenarioMerchant(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("SITE_ID")] Id SiteId,
        [property: JsonPropertyName("ITEM_ID")] Id ItemId,
        [property: JsonPropertyName("ITEM_COUNT")] int ItemCount
    );
}
