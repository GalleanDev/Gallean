using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SUnit.json", PerScenario = true)]
    public record ScenarioUnit(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("UNIT_ID")] Id UnitId,
        [property: JsonPropertyName("HP")] int Hp,
        [property: JsonPropertyName("XP")] int Xp,
        [property: JsonPropertyName("CREATION")] int Creation,
        [property: JsonPropertyName("TYPE")] Id TypeId,
        [property: JsonPropertyName("LEVEL")] int Level,
        [property: JsonPropertyName("TRANSF")] bool Transf,
        [property: JsonPropertyName("DYNLEVEL")] bool? DynLevel
    );
}
