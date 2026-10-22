using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SStkTemM.json", PerScenario = true)]
    public record ScenarioStackTemplateModifier(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("TEMPLAT_ID")] Id TemplateId,
        [property: JsonPropertyName("MODIF_ID")] Id ModifierId,
        [property: JsonPropertyName("UNIT_POS")] int UnitPos
    );
}
