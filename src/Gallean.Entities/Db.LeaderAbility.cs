using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GLabi.json")]
    public record LeaderAbility(
        [property: DataLookupKey, JsonPropertyName("UNIT_ID")] Id UnitId,
        [property: JsonPropertyName("L_ABILITY")] LeaderAbilityType Ability
    );
}
