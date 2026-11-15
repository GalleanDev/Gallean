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
        return result switch
        {
            PreviewBuilderResult.Written written => ReportWritten(written),
            PreviewBuilderResult.NoSelection => ReportNoSelection(),
            PreviewBuilderResult.UnmatchedCategories unmatchedCategories => ReportUnmatchedCategories(unmatchedCategories),
            PreviewBuilderResult.MissingFrames missingFrames => ReportMissingFrames(missingFrames),
            PreviewBuilderResult.AmbiguousOutput ambiguousOutput => ReportAmbiguousOutput(ambiguousOutput)
        };
    }

    private int ReportWritten(PreviewBuilderResult.Written result)
    {
        foreach (string outputPath in result.OutputPaths)
        {
            output.WriteLine($"Wrote {outputPath}.");
        }

        return 0;
    }

    private int ReportNoSelection()
    {
        output.WriteError("Nothing in this pack can be previewed.");

        return 1;
    }

    private int ReportUnmatchedCategories(PreviewBuilderResult.UnmatchedCategories result)
    {
        output.WriteError($"No assets match: {string.Join(", ", result.Patterns)}.");

        return 1;
    }

    private int ReportMissingFrames(PreviewBuilderResult.MissingFrames result)
    {
        output.ReportMissing(result);

        return 1;
    }

    private int ReportAmbiguousOutput(PreviewBuilderResult.AmbiguousOutput result)
    {
        output.WriteError(
            $"The output '{result.OutputPath}' names a single file, " +
            $"but {result.Ids.Count} entries were selected. Pass a directory instead.");

        return 1;
    }
}
