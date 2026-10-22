using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SEvCond.json", PerScenario = true)]
    public record ScenarioEventCondition(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataLookupKey, JsonPropertyName("ID")] Id Id,
        [property: JsonPropertyName("NUM")] string? Number,
        [property: JsonPropertyName("CATEGORY")] ScenarioEventConditionType Category,
        [property: JsonPropertyName("ID_LOC")] Id? IdLocation,
        [property: JsonPropertyName("ID_STACK")] Id? IdStack,
        [property: JsonPropertyName("ID_RUIN")] Id? IdRuin,
        [property: JsonPropertyName("ID_CITY")] Id? IdCity,
        [property: JsonPropertyName("ID_SITE")] Id? IdSite,
        [property: JsonPropertyName("ID_PLAYER1")] Id? IdPlayer1,
        [property: JsonPropertyName("ID_PLAYER2")] Id? IdPlayer2,
        [property: JsonPropertyName("TYPE_ITEM")] Id? TypeItem,
        [property: JsonPropertyName("FREQUENCY")] int? Frequency,
        [property: JsonPropertyName("PCT_LAND")] int? PercentLand,
        [property: JsonPropertyName("DIPLOMACY")] int? Diplomacy,
        [property: JsonPropertyName("ALLIANCE")] string? Alliance,
        [property: JsonPropertyName("MISC_INT")] int? MiscInt,
        [property: JsonPropertyName("MISC_INT2")] int? MiscInt2,
        [property: JsonPropertyName("MISC_INT3")] int? MiscInt3,
        [property: JsonPropertyName("MISC_INT4")] int? MiscInt4,
        [property: JsonPropertyName("MISC_INT5")] int? MiscInt5,
        [property: JsonPropertyName("MISC_INT6")] int? MiscInt6,
        [property: JsonPropertyName("MISC_INT7")] int? MiscInt7
    );
}
