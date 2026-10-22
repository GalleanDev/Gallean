using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    public record AttackCategory(
        [property: JsonPropertyName("ID")] int Id,
        [property: JsonPropertyName("TEXT")] string Text
    );
}
