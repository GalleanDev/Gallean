using Gallean.Configuration;

namespace Packer.Tests;

internal static class TestModels
{
    private static string TryGetModelDirectory(IReadOnlyList<string> files)
    {
        foreach (string file in files)
        {
            string path = Path.Combine(AppDirectories.EnhanceModels, file);

            if (!File.Exists(path))
            {
                Assert.Ignore($"Unable to find model at '{path}'.");
            }
        }

        return AppDirectories.EnhanceModels;
    }

    public static string TryGetSurfaceModelDirectory()
    {
        return TryGetModelDirectory(UmbraSurfaceModel.RequiredFiles);
    }

    public static string TryGetUpscaleModelDirectory()
    {
        return TryGetModelDirectory(UmbraVsrUpscaleModel.RequiredFiles);
    }

    public static string TryGetMosrUpscaleModelDirectory()
    {
        return TryGetModelDirectory(UmbraMosrUpscaleModel.RequiredFiles);
    }

    public static string TryGetRefineModelDirectory()
    {
        return TryGetModelDirectory(UmbraRefineModel.RequiredFiles);
    }

    public static string TryGetAlphaModelDirectory()
    {
        return TryGetModelDirectory(UmbraAlphaModel.RequiredFiles);
    }
}
