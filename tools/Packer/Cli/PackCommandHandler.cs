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

        switch (result)
        {
            case PackBuilderResult.Packed data:
                output.WriteLine($"Packed {data.OutputPath}.");
                return 0;

            case PackBuilderResult.MissingRequired data:
                output.ReportMissing(data);
                return 1;

            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, null);
        }
    }
}
