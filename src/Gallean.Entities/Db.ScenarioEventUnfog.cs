using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SEvUnfog.json", PerScenario = true)]
    public record ScenarioEventUnfog(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("EVENT_ID")] Id EventId,
        [property: JsonPropertyName("PLAYER")] Id Player
    );
}
