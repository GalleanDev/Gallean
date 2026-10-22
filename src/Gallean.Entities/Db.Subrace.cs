using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GSubRace.json")]
    public record Subrace(
        [property: JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("RACE_TYPE")] SubraceType SubraceType
    );
}
