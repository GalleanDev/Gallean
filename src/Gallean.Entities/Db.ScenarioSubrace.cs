using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SSubRace.json", PerScenario = true)]
    public record ScenarioSubrace(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("SUBRACE_ID")] Id SubraceId,
        [property: JsonPropertyName("PLAYER_ID")] Id PlayerId,
        [property: JsonPropertyName("SUBRACE")] int Subrace,
        [property: JsonPropertyName("BANNER")] int Banner
    );
}
