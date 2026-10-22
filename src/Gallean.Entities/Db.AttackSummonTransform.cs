using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Gtransf.json")]
    public record AttackSummonTransform(
        [property: DataLookupKey, JsonPropertyName("ATTACK_ID")] Id AttackId,
        [property: JsonPropertyName("TRANSF_ID")] Id TransformId
    );
}
