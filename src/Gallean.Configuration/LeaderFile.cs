using System.Text.Json.Serialization;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

public sealed record LeaderFile(
    [property: JsonPropertyOrder(-1)] int FormatVersion,
    ScenarioLeader Leader)
{
    public const int CurrentFormatVersion = 1;

    public static LeaderFile Create(ScenarioLeader leader)
    {
        return new LeaderFile(CurrentFormatVersion, leader);
    }
}
