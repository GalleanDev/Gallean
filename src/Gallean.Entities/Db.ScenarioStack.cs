using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("Sstack.json", PerScenario = true)]
    public record ScenarioStack(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("STACK_ID")] Id StackId,
        [property: JsonPropertyName("SRCTMPL_ID")] Id? SourceTemplateId,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("LEADER_ID")] Id LeaderId,
        [property: JsonPropertyName("LEADR_ALIV")] bool LeaderAlive,
        [property: JsonPropertyName("MORALE")] int Morale,
        [property: JsonPropertyName("MOVE")] int Move,
        [property: JsonPropertyName("FACING")] int Facing,
        [property: JsonPropertyName("BANNER")] Id? BannerId,
        [property: JsonPropertyName("TOME")] Id? TomeId,
        [property: JsonPropertyName("BATTLE1")] Id? Battle1Id,
        [property: JsonPropertyName("BATTLE2")] Id? Battle2Id,
        [property: JsonPropertyName("ARTIFACT1")] Id? Artifact1Id,
        [property: JsonPropertyName("ARTIFACT2")] Id? Artifact2Id,
        [property: JsonPropertyName("BOOTS")] Id? BootsId,
        [property: JsonPropertyName("OWNER")] Id OwnerId,
        [property: JsonPropertyName("SUBRACE")] Id SubraceId,
        [property: JsonPropertyName("INSIDE")] Id? InsideId,
        [property: JsonPropertyName("INVISIBLE")] bool Invisible,
        [property: JsonPropertyName("UPGCOUNT")] int UpgradeCount,
        [property: JsonPropertyName("AI_IGNORE")] bool AiIgnore,
        [property: JsonPropertyName("AIPRIORITY")] int AiPriority,
        [property: JsonPropertyName("ORDER")] OrderType Order,
        [property: JsonPropertyName("ORDER_TARG")] Id? OrderTargetId,
        [property: JsonPropertyName("CREAT_LVL")] int CreationLevel,
        [property: JsonPropertyName("NBBATTLE")] int NumberOfBattles,
        [property: JsonPropertyName("AIORDER")] OrderType AiOrder,
        [property: JsonPropertyName("AIORDERTAR")] Id? AiOrderTargetId
    );
}
