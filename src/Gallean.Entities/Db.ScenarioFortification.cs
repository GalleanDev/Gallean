using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SFort.json", PerScenario = true)]
    public record ScenarioFortification(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("CITY_ID")] Id CityId,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("OWNER")] Id OwnerId,
        [property: JsonPropertyName("SUBRACE")] Id SubraceId,
        [property: JsonPropertyName("STACK")] Id? Stack,
        [property: JsonPropertyName("SIZE")] int? Size,
        [property: JsonPropertyName("SIZE_MAX")] string? SizeMax,
        [property: JsonPropertyName("REGEN_B")] int? RegenerationBonus,
        [property: JsonPropertyName("PROTECT_B")] Id? ProtectionBuildingId,
        [property: JsonPropertyName("MORALE")] int? Morale,
        [property: JsonPropertyName("GROWTH_T")] int? GrowthTurn,
        [property: JsonPropertyName("P_O_UN")] bool? ProtectedFromUndead,
        [property: JsonPropertyName("P_O_HE")] bool? ProtectedFromHeretic,
        [property: JsonPropertyName("P_O_HU")] bool? ProtectedFromHuman,
        [property: JsonPropertyName("P_O_DW")] bool? ProtectedFromDwarf,
        [property: JsonPropertyName("P_O_EL")] bool? ProtectedFromElf,
        [property: JsonPropertyName("AIPRIORITY")] int AiPriority,
        [property: JsonPropertyName("RIOT_T")] int? RiotTurns
    );
}
