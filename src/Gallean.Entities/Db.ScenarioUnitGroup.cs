using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SUGroup.json", PerScenario = true)]
    public record ScenarioUnitGroup(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("GROUP_ID")] Id GroupId,
        [property: JsonPropertyName("UNIT_0")] Id? Unit0Id,
        [property: JsonPropertyName("UNIT_1")] Id? Unit1Id,
        [property: JsonPropertyName("UNIT_2")] Id? Unit2Id,
        [property: JsonPropertyName("UNIT_3")] Id? Unit3Id,
        [property: JsonPropertyName("UNIT_4")] Id? Unit4Id,
        [property: JsonPropertyName("UNIT_5")] Id? Unit5Id,
        [property: JsonPropertyName("POS_0")] int Pos0,
        [property: JsonPropertyName("POS_1")] int Pos1,
        [property: JsonPropertyName("POS_2")] int Pos2,
        [property: JsonPropertyName("POS_3")] int Pos3,
        [property: JsonPropertyName("POS_4")] int Pos4,
        [property: JsonPropertyName("POS_5")] int Pos5
    );
}
