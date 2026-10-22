using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GcityInf.json")]
    public record CityInfo(
        [property: JsonPropertyName("RACE_ID")] Id RaceId,
        [property: JsonPropertyName("SIZE")] int Size,
        [property: JsonPropertyName("SCOUT")] int Scout,
        [property: JsonPropertyName("REGEN_F")] int Regeneration,
        [property: JsonPropertyName("PROTECTION")] Id ProtectionId,
        [property: JsonPropertyName("GROWTH")] ResourceSet Growth,
        [property: JsonPropertyName("RES_M")] int? GoldIncome
    );
}
