using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SRel.json", PerScenario = true)]
    public record ScenarioRelation(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: JsonPropertyName("RACE_1")] RaceType Race1,
        [property: JsonPropertyName("RACE_2")] RaceType Race2,
        [property: JsonPropertyName("RELATION")] int Relation
    );
}
