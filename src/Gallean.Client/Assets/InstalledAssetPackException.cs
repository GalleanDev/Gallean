namespace Gallean.Client;

internal closed class InstalledAssetPackException(string message, Exception? innerException)
    : Exception(message, innerException);
