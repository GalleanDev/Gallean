namespace Gallean.Assets;

public sealed class AssetPackUnsupportedFormatException(string message)
    : Exception(message);
