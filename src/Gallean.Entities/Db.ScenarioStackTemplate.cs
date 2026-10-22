using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SStkTemp.json", PerScenario = true)]
    public record ScenarioStackTemplate(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("ID")] Id Id,
        [property: JsonPropertyName("LEADER")] Id Leader,
        [property: JsonPropertyName("LEADER_LVL")] int LeaderLevel,
        [property: JsonPropertyName("OWNER")] Id OwnerId,
        [property: JsonPropertyName("SUBRACE")] Id SubraceId,
        [property: JsonPropertyName("UNIT_0")] Id Unit0Id,
        [property: JsonPropertyName("UNIT_0_LVL")] int Unit0Level,
        [property: JsonPropertyName("UNIT_1")] Id? Unit1Id,
        [property: JsonPropertyName("UNIT_1_LVL")] int Unit1Level,
        [property: JsonPropertyName("UNIT_2")] Id? Unit2Id,
        [property: JsonPropertyName("UNIT_2_LVL")] int Unit2Level,
        [property: JsonPropertyName("UNIT_3")] Id? Unit3Id,
        [property: JsonPropertyName("UNIT_3_LVL")] int Unit3Level,
        [property: JsonPropertyName("UNIT_4")] Id? Unit4Id,
        [property: JsonPropertyName("UNIT_4_LVL")] int Unit4Level,
        [property: JsonPropertyName("UNIT_5")] Id? Unit5Id,
        [property: JsonPropertyName("UNIT_5_LVL")] int Unit5Level,
        [property: JsonPropertyName("POS_0")] int Position0,
        [property: JsonPropertyName("POS_1")] int Position1,
        [property: JsonPropertyName("POS_2")] int Position2,
        [property: JsonPropertyName("POS_3")] int Position3,
        [property: JsonPropertyName("POS_4")] int Position4,
        [property: JsonPropertyName("POS_5")] int Position5,
        [property: JsonPropertyName("ORDER")] OrderType Order,
        [property: JsonPropertyName("ORDER_TARG")] Id? OrderTargetId,
        [property: JsonPropertyName("USE_FACING")] bool UseFacing,
        [property: JsonPropertyName("FACING")] int Facing,
        [property: JsonPropertyName("AIPRIORITY")] int AiPriority
    );
}
