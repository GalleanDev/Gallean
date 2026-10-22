using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [GlobalDataFile("Gbuild.json")]
    public record Building(
        [property: DataKey, JsonPropertyName("BUILD_ID")] Id BuildingId,
        [property: JsonPropertyName("NAME_TXT")] string NameText,
        [property: JsonPropertyName("DESC_TXT")] string DescriptionText,
        [property: JsonPropertyName("REQUIRED")] Id? RequiredId,
        [property: JsonPropertyName("COST")] ResourceSet Cost,
        [property: JsonPropertyName("LEVEL")] int Level,
        [property: JsonPropertyName("CATEGORY")] BuildingType Category,
        [property: JsonPropertyName("BRANCH")] UnitBranchType? Branch,
        [property: JsonPropertyName("PIC_1")] string? Picture1,
        [property: JsonPropertyName("FRAME_1")] int? Frame1,
        [property: JsonPropertyName("FRAME_2")] int? Frame2,
        [property: JsonPropertyName("POS_X")] int? PositionX,
        [property: JsonPropertyName("POS_Y")] int? PositionY,
        [property: JsonPropertyName("POS_CX")] int? PositionCx,
        [property: JsonPropertyName("POS_CY")] int? PositionCy,
        [property: JsonPropertyName("POS_Z")] int? PositionZ,
        [property: JsonPropertyName("COMMENTS")] string Comments
    );
}
