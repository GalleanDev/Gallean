using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GimmuC.json")]
    public record UnitAttackImmunity(
        [property: JsonPropertyName("UNIT_ID")] Id UnitId,
        [property: JsonPropertyName("IMMUNITY")] AttackType Immunity,
        [property: JsonPropertyName("IMMUNECAT")] ImmunityType ImmunityType
    );
}
