using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("GbuiList.json")]
    public record BuildingList(
        [property: JsonPropertyName("LORD_ID")] Id LordId,
        [property: JsonPropertyName("BUILD_ID")] Id BuildingId
    );
}
