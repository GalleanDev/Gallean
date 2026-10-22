using Gallean.Configuration;

namespace Umbra.Tests;

internal static class TestModels
{
    public static VsrUpscaleWeights TryLoadVsrUpscaleModel()
    {
        string path = Path.Combine(AppDirectories.EnhanceModels, VsrUpscaleWeights.FileName);

        if (!File.Exists(path))
        {
            Assert.Ignore($"Unable to find model at '{path}'.");
        }

        return VsrUpscaleWeights.Load(AppDirectories.EnhanceModels);
    }
}
