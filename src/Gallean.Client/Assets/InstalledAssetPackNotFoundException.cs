namespace Gallean.Client;

internal sealed class InstalledAssetPackNotFoundException()
    : InstalledAssetPackException("No asset pack was found.", innerException: null);
