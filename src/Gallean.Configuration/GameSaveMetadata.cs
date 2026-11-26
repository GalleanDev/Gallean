using System.Text.Json.Serialization;

namespace Gallean.Configuration;

public sealed record GameSaveMetadata(
    string Name,
    GameSaveType Type,
    string ScenarioId,
    string ScenarioName,
    int TurnNumber,
    DateTimeOffset SavedAt,
    string? SavedBy = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CustomScenarioId = null)
{
    public const int MaxNameLength = 256;
}
