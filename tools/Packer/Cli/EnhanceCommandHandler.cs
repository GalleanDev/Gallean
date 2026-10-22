namespace Packer;

internal sealed class EnhanceCommandHandler(
    PackerOutput output,
    NativeLibraryLoader nativeLibraries,
    ModelStore models)
{
    private const int SurfaceDenoisingSteps = 35;

    public int Run(EnhanceCommandOptions commandOptions)
    {
        if (!File.Exists(commandOptions.PackPath))
        {
            output.WriteError($"Asset pack '{commandOptions.PackPath}' was not found.");

            return 1;
        }

        if (commandOptions is { CacheOnly: true, OutputPath: not null })
        {
            output.WriteError("The --cache-only cannot be combined with --out.");

            return 1;
        }
        else if (commandOptions is { CacheOnly: false, OutputPath: null })
        {
            output.WriteError("Either --out or --cache-only must be passed.");

            return 1;
        }

        if (commandOptions is { Sparse: true, OutputPath: null })
        {
            output.WriteError("The --sparse requires --out.");

            return 1;
        }

        if (!CategorySelection.TryCreate(commandOptions.Categories, output, out CategorySelection? categories))
        {
            return 1;
        }

        try
        {
            nativeLibraries.UseImageMagick();
            nativeLibraries.UseTorch(commandOptions.Device);
        }
        catch (Exception ex)
        {
            output.WriteError(ex.Message);

            return 1;
        }

        string? modelDirectory = models.FindModel(commandOptions.Enhancements);

        if (modelDirectory is null)
        {
            output.WriteError("The models are missing.");

            return 1;
        }

        PackEnhancerOptions enhanceOptions = new(
            categories.Selector,
            modelDirectory,
            SurfaceDenoisingSteps,
            commandOptions.Device,
            commandOptions.OutputPath,
            commandOptions.Sparse,
            DateTimeOffset.UtcNow,
            PackerVersion.Get(),
            commandOptions.CacheDirectory,
            commandOptions.CacheDirectory,
            commandOptions.Enhancements);

        try
        {
            output.WriteLine($"Running the models on the {commandOptions.Device}.");

            using AssetPack pack = AssetPack.Read(commandOptions.PackPath);

            PackEnhancer enhancer = new(pack, output.ReportProgress);
            PackEnhancerResult result = enhancer.Enhance(enhanceOptions);

            return Report(commandOptions, result);
        }
        catch (Exception ex)
        {
            output.WriteError(ex.Message);

            return 1;
        }
    }

    private int Report(EnhanceCommandOptions commandOptions, PackEnhancerResult result)
    {
        switch (result)
        {
            case PackEnhancerResult.Enhanced data:
                if (commandOptions.CacheOnly)
                {
                    output.WriteLine($"Enhanced {data.EntryCount} entries, wrote {data.CacheEntryCount} cache entries.");
                }
                else
                {
                    output.WriteLine($"Enhanced {data.EntryCount} entries: {data.OutputPath}.");
                }
                return 0;

            case PackEnhancerResult.NoSelection:
                output.WriteError("Nothing in this pack can be enhanced.");
                return 1;

            case PackEnhancerResult.UnmatchedCategories data:
                output.WriteError($"No assets match: {string.Join(", ", data.Patterns)}.");
                return 1;

            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, null);
        }
    }
}
