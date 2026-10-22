using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("Sfog.json", PerScenario = true)]
    public record ScenarioFog(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("FOG_ID")] Id FogId,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("FOG")] string Fog
    );
}
