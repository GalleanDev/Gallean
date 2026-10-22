namespace Packer;

internal sealed class EnvironmentBuilder(NativeLibraryStore libraries, ModelDownloader models)
{
    public async Task<IReadOnlyList<EnvironmentComponent>> BuildAsync(
        UmbraDevice device,
        CancellationToken cancellationToken)
    {
        List<EnvironmentComponent> components = [];

        foreach (NativeLibrarySet set in NativeLibrarySet.All(device))
        {
            string libraryDirectory = await libraries.EnsureLibraryAsync(set, cancellationToken);

            components.Add(new EnvironmentComponent(set.Name, libraryDirectory));
        }

        string modelDirectory = await models.EnsureModelAsync(
            [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha, EnhancementType.Surface],
            cancellationToken);

        components.Add(new EnvironmentComponent("models", modelDirectory));

        return components;
    }
}
