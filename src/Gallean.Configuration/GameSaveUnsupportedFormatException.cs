namespace Gallean.Configuration;

public sealed class GameSaveUnsupportedFormatException(string message, GameSaveMetadata? metadata)
    : Exception(message)
{
    public GameSaveMetadata? Metadata { get; } = metadata;
}
