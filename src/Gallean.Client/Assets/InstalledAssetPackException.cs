namespace Gallean.Client;

internal abstract class InstalledAssetPackException(string message, Exception? innerException)
    : Exception(message, innerException);
