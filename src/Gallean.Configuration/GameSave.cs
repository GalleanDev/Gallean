using System.Text.Json.Serialization;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

public sealed record GameSave(
    [property: JsonPropertyOrder(-1)] int FormatVersion,
    GameSaveMetadata Metadata,
    WorldState WorldState)
{
    public const int CurrentFormatVersion = 45;

    public static GameSave Create(GameSaveMetadata metadata, WorldState worldState)
    {
        return new GameSave(CurrentFormatVersion, metadata, worldState);
    }
}
