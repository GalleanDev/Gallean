using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SMounts.json", PerScenario = true)]
    public record ScenarioMountain(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: JsonPropertyName("ID_MOUNT")] int IdMountain,
        [property: JsonPropertyName("SIZE_X")] int SizeX,
        [property: JsonPropertyName("SIZE_Y")] int SizeY,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("IMAGE")] int Image,
        [property: JsonPropertyName("RACE")] int Race
    );
}
