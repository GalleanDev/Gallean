using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SMercs.json", PerScenario = true)]
    public record ScenarioMercenary(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("SITE_ID")] Id SiteId,
        [property: JsonPropertyName("UNIT_ID")] Id UnitId,
        [property: JsonPropertyName("UNIT_LEVEL")] int UnitLevel,
        [property: JsonPropertyName("UNIT_UNIQ")] bool UnitUniq
    );
}
