using System.Text.Json.Serialization;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

public sealed record LeaderFile(
    [property: JsonPropertyOrder(-1)] int FormatVersion,
    ScenarioLeader Leader,
    DateTimeOffset SavedAt,
    string? SavedBy = null)
{
    public const int CurrentFormatVersion = 2;

    public static LeaderFile Create(ScenarioLeader leader, DateTimeOffset savedAt, string? savedBy)
    {
        return new LeaderFile(CurrentFormatVersion, leader, savedAt, savedBy);
    }
}
