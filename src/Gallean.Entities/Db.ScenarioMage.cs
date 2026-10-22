using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SMage.json", PerScenario = true)]
    public record ScenarioMage(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("SITE_ID")] Id SiteId,
        [property: JsonPropertyName("SPELL_ID")] Id SpellId
    );
}
