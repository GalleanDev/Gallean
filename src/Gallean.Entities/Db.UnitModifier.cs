using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Gmodif.json")]
    public record UnitModifier(
        [property: DataKey, JsonPropertyName("MODIF_ID")] Id ModifierId,
        [property: JsonPropertyName("SOURCE")] AttackSourceType Source,
        [property: JsonPropertyName("COMMENTS")] string Comments
    );
}
