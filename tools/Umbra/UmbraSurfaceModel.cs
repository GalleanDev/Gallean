using TorchSharp;

namespace Umbra;

public sealed class UmbraSurfaceModel : IDisposable
{
    public const string DirectoryName = SurfaceInverseRenderer.DirectoryName;
    public const string DitFileName = SurfaceWeights.DitFileName;
    public const string VaeFileName = SurfaceWeights.VaeFileName;
    public const string ContextFileName = SurfaceContext.EmbeddingsFileName;

    private static readonly UmbraSurfaceLayerType[] Layers =
    [
        UmbraSurfaceLayerType.Normal
    ];

    private readonly SurfaceInverseRenderer renderer;

    public UmbraSurfaceModel(
        IReadOnlyList<UmbraSurfaceLayerType> types,
        string modelDirectory,
        int denoisingSteps,
        UmbraDevice device)
    {
        UmbraRuntime.UseDevice(device);

        renderer = SurfaceInverseRenderer.Load(modelDirectory, types, denoisingSteps, GetModelDtype(device));
    }

    public static IReadOnlyList<IReadOnlyList<UmbraSurfaceLayerType>> LayerGroups { get; } = [Layers];

    public static IReadOnlyList<string> RequiredFiles { get; } =
    [
        Path.Combine(DirectoryName, DitFileName),
        Path.Combine(DirectoryName, VaeFileName),
        Path.Combine(DirectoryName, ContextFileName)
    ];

    public IEnumerable<UmbraSurfaceLayer> Run(IEnumerable<UmbraSequenceGroup> groups)
    {
        using (torch.no_grad())
        {
            foreach (UmbraSurfaceLayer layer in renderer.Run(groups))
            {
                yield return layer;
            }
        }
    }

    public void Dispose()
    {
        renderer.Dispose();
    }

    private static torch.ScalarType GetModelDtype(UmbraDevice device)
    {
        return device switch
        {
            UmbraDevice.Cpu => torch.ScalarType.Float32,
            UmbraDevice.Gpu => torch.ScalarType.Float16
        };
    }
}
