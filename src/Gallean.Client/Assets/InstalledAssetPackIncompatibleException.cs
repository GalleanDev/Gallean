namespace Gallean.Client;

internal sealed class InstalledAssetPackIncompatibleException(string path, AssetPackUnsupportedFormatException innerException)
    : InstalledAssetPackException($"The asset pack '{path}' has an unsupported format.", innerException)
{
    public string Path { get; } = path;
}
