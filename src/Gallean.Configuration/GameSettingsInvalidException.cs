namespace Gallean.Configuration;

public sealed class GameSettingsInvalidException(string message, Exception innerException)
    : Exception(message, innerException);
