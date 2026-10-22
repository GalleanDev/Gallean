namespace Packer;

internal sealed class PreviewCommandHandler(PackerOutput output, NativeLibraryLoader nativeLibraries)
{
    public int Run(PreviewCommandOptions commandOptions)
    {
        if (!File.Exists(commandOptions.PackPath))
        {
            output.WriteError($"Asset pack '{commandOptions.PackPath}' was not found.");

            return 1;
        }

        if (!Directory.Exists(commandOptions.CacheDirectory))
        {
            output.WriteError($"Enhancement cache '{commandOptions.CacheDirectory}' was not found.");

            return 1;
        }

        if (!CategorySelection.TryCreate(commandOptions.Categories, output, out CategorySelection? categories))
        {
            return 1;
        }

        try
        {
            nativeLibraries.UseImageMagick();
        }
        catch (Exception ex)
        {
            output.WriteError(ex.Message);

            return 1;
        }

        PreviewBuilderOptions builderOptions = new(
            categories.Selector,
            commandOptions.CacheDirectory,
            commandOptions.Device,
            commandOptions.Transform,
            commandOptions.OutputPath,
            TimeSpan.FromSeconds(1.0 / commandOptions.FramesPerSecond));

        try
        {
            using AssetPack pack = AssetPack.Read(commandOptions.PackPath);

            PreviewBuilder builder = new(pack);
            PreviewBuilderResult result = builder.Build(builderOptions);

            return Report(result);
        }
        catch (Exception ex)
        {
            output.WriteError(ex.Message);

            return 1;
        }
    }

    private int Report(PreviewBuilderResult result)
    {
        switch (result)
        {
            case PreviewBuilderResult.Written data:
                foreach (string outputPath in data.OutputPaths)
                {
                    output.WriteLine($"Wrote {outputPath}.");
                }
                return 0;

            case PreviewBuilderResult.NoSelection:
                output.WriteError("Nothing in this pack can be previewed.");
                return 1;

            case PreviewBuilderResult.UnmatchedCategories data:
                output.WriteError($"No assets match: {string.Join(", ", data.Patterns)}.");
                return 1;

            case PreviewBuilderResult.MissingFrames data:
                output.ReportMissing(data);
                return 1;

            case PreviewBuilderResult.AmbiguousOutput data:
                output.WriteError(
                    $"The output '{data.OutputPath}' names a single file, " +
                    $"but {data.Ids.Count} entries were selected. Pass a directory instead.");
                return 1;

            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, null);
        }
    }
}
