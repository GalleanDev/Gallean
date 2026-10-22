using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("STalisCh.json", PerScenario = true)]
    public record ScenarioTalismanCharges(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("ID_TALIS")] Id IdTalisman,
        [property: JsonPropertyName("CHARGES")] int Charges
    );
}
