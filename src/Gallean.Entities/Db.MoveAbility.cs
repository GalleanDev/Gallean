using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GMabi.json")]
    public record MoveAbility(
        [property: DataLookupKey, JsonPropertyName("UNIT_ID")] Id UnitId,
        [property: JsonPropertyName("M_ABILITY")] GroundType Ability
    );
}
