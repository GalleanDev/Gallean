namespace Packer;

internal sealed class ModelStore(string baseDirectory, string modelDirectory)
{
    private const string StagedDirectoryName = "Models";

    public string ModelDirectory => modelDirectory;

    public static IReadOnlyList<string> GetRequiredFiles(IReadOnlyList<EnhancementType> enhancements)
    {
        List<string> files = [];

        if (enhancements.Contains(EnhancementType.Surface))
        {
            files.AddRange(UmbraSurfaceModel.RequiredFiles);
        }

        if (enhancements.Contains(EnhancementType.Upscale))
        {
            files.AddRange(UmbraVsrUpscaleModel.RequiredFiles);
            files.AddRange(UmbraMosrUpscaleModel.RequiredFiles);
        }

        if (enhancements.Contains(EnhancementType.Refine))
        {
            files.AddRange(UmbraRefineModel.RequiredFiles);
        }

        if (enhancements.Contains(EnhancementType.Alpha))
        {
            files.AddRange(UmbraAlphaModel.RequiredFiles);
        }

        return files;
    }

    public string? FindModel(IReadOnlyList<EnhancementType> enhancements)
    {
        string staged = Path.Combine(baseDirectory, StagedDirectoryName);

        if (Directory.Exists(staged))
        {
            return staged;
        }

        foreach (string file in GetRequiredFiles(enhancements))
        {
            if (!File.Exists(Path.Combine(modelDirectory, file)))
            {
                return null;
            }
        }

        return modelDirectory;
    }
}
