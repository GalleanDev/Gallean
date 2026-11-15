using GameContent;

namespace Packer;

internal sealed class PackCommandHandler(PackerOutput output, NativeLibraryLoader nativeLibraries)
{
    public int Run(PackCommandOptions commandOptions)
    {
        try
        {
            nativeLibraries.UseImageMagick();
        }
        catch (Exception ex)
        {
            output.WriteError(ex.Message);

            return 1;
        }

        GameAssetSource? source = GameAssetSource.TryLoadDefault();

        if (source is null)
        {
            output.WriteError($"Original assets folder '{GameFiles.Original.Root}' was not found.");

            return 1;
        }

        PackBuilderOptions builderOptions = new(commandOptions.OutputPath, DateTimeOffset.UtcNow, PackerVersion.Get());
        IReadOnlyList<PackBuilderEntry> catalog = PackBuilderCatalog.ListEntries(source.Descriptors);
        PackBuilder builder = new(catalog, source, output.ReportProgress);
        PackBuilderResult result = builder.Build(builderOptions);

        return result switch
        {
            PackBuilderResult.Packed packed => ReportPacked(packed),
            PackBuilderResult.MissingRequired missingRequired => ReportMissingRequired(missingRequired)
        };
    }

    private int ReportPacked(PackBuilderResult.Packed result)
    {
        output.WriteLine($"Packed {result.OutputPath}.");

        return 0;
    }

    private int ReportMissingRequired(PackBuilderResult.MissingRequired result)
    {
        output.ReportMissing(result);

        return 1;
    }
}
