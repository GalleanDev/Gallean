namespace Gallean.Client.Tests;

internal static class TestAssets
{
    public static AssetProvider LoadOrIgnore()
    {
        AssetProvider? provider = null;

        try
        {
            provider = AssetProvider.TryLoadDefault();
        }
        catch (InstalledAssetPackIncompatibleException ex)
        {
            Assert.Ignore(ex.Message);
        }

        if (provider is null)
        {
            Assert.Ignore();
        }

        return provider;
    }
}
