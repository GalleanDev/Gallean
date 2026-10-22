using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GspellR.json")]
    public record SpellResearch(
        [property: JsonPropertyName("LORD_ID")] Id LordId,
        [property: JsonPropertyName("SPELL_ID")] Id SpellId,
        [property: JsonPropertyName("RESEARCH")] ResourceSet Research
    );
}
