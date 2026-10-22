using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GmodifL.json")]
    public record UnitModifierItem(
        [property: DataLookupKey, JsonPropertyName("BELONGS_TO")] Id BelongsToId,
        [property: JsonPropertyName("DESC")] string Description,
        [property: JsonPropertyName("TYPE")] UnitModifierItemType Type,
        [property: JsonPropertyName("PERCENT")] int? Percent,
        [property: JsonPropertyName("NUMBER")] int? Number,
        [property: JsonPropertyName("ABILITY")] LeaderAbilityType? Ability,
        [property: JsonPropertyName("IMMUNITY")] AttackSourceType? Immunity,
        [property: JsonPropertyName("IMMUNECAT")] ImmunityType? ImmunityType,
        [property: JsonPropertyName("MOVE")] GroundType? Move
    );
}
