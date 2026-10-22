using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [ScenarioDataFile("SRuin.json", PerScenario = true)]
    public record ScenarioRuin(
        [property: ScenarioKey, JsonPropertyName("NO_SCEN")] ScenarioId ScenarioId,
        [property: DataKey, JsonPropertyName("RUIN_ID")] Id RuinId,
        [property: JsonPropertyName("IMAGE")] int Image,
        [property: JsonPropertyName("POS_X")] int PositionX,
        [property: JsonPropertyName("POS_Y")] int PositionY,
        [property: JsonPropertyName("CASH")] string Cash,
        [property: JsonPropertyName("ITEM")] Id? Item,
        [property: JsonPropertyName("LOOTER")] Id? Looter,
        [property: JsonPropertyName("AIPRIORITY")] int AiPriority
    );
}
