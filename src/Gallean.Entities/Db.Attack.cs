using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Gattacks.json")]
    public record Attack(
        [property: DataKey, JsonPropertyName("ATT_ID")] Id AttackId,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText,
        [property: JsonPropertyName("INITIATIVE")] int Initiative,
        [property: JsonPropertyName("SOURCE")] AttackSourceType Source,
        [property: JsonPropertyName("CLASS")] AttackType Type,
        [property: JsonPropertyName("POWER")] int Power,
        [property: JsonPropertyName("REACH")] AttackReachType Reach,
        [property: JsonPropertyName("QTY_HEAL")] int? QuantityHeal,
        [property: JsonPropertyName("QTY_DAM")] int? QuantityDamage,
        [property: JsonPropertyName("LEVEL")] int? Level,
        [property: JsonPropertyName("ALT_ATTACK")] Id? AltAttackId,
        [property: JsonPropertyName("INFINITE")] bool? Infinite,
        [property: JsonPropertyName("QTY_WARDS")] int? QuantityWards,
        [property: JsonPropertyName("WARD1")] Id? Ward1,
        [property: JsonPropertyName("WARD2")] Id? Ward2,
        [property: JsonPropertyName("WARD3")] Id? Ward3,
        [property: JsonPropertyName("WARD4")] Id? Ward4,
        [property: JsonPropertyName("CRIT_HIT")] bool? CritHit
    );
}
