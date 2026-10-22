using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Gimmu.json")]
    public record UnitAttackSourceImmunity(
        [property: JsonPropertyName("UNIT_ID")] Id UnitId,
        [property: JsonPropertyName("IMMUNITY")] AttackSourceType Immunity,
        [property: JsonPropertyName("IMMUNECAT")] ImmunityType ImmunityType
    );
}
