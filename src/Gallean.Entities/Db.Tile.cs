using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GTileDBI.json")]
    public record Tile(
        [property: JsonPropertyName("TERRAIN")] string Terrain,
        [property: JsonPropertyName("GROUND")] string Ground,
        [property: JsonPropertyName("NDX")] int Index,
        [property: JsonPropertyName("QTY")] int Quantity
    );
}
