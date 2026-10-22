using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SVars.json", PerScenario = true)]
    public record ScenarioVariable(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("ID")] int Id,
        [property: JsonPropertyName("NAME")] string Name,
        [property: JsonPropertyName("VALUE")] int Value
    );
}
