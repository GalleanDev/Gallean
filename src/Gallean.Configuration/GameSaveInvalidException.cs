namespace Gallean.Configuration;

public sealed class GameSaveInvalidException(string message, Exception innerException)
    : Exception(message, innerException);
