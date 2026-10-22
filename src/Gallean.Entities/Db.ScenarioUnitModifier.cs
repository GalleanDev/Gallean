using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SUnitM.json", PerScenario = true)]
    public record ScenarioUnitModifier(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("UNIT_ID")] Id UnitId,
        [property: JsonPropertyName("MODIF_ID")] Id ModifierId
    );
}
