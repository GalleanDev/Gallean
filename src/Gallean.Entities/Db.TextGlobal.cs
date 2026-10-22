using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Tglobal.json", Index = true)]
    public record TextGlobal(
        [property: DataKey, JsonPropertyName("TXT_ID")] Id TextId,
        [property: JsonPropertyName("TEXT")] string? Text,
        [property: JsonPropertyName("VERIFIED")] bool? Verified,
        [property: JsonPropertyName("CONTEXT")] string? Context
    );
}
