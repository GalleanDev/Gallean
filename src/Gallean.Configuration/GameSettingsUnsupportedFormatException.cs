namespace Gallean.Configuration;

public sealed class GameSettingsUnsupportedFormatException(string message)
    : Exception(message);
