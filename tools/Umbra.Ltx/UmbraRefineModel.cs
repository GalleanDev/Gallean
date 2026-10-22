using TorchSharp;

namespace Umbra.Ltx;

public sealed class UmbraRefineModel : IDisposable
{
    public const string DirectoryName = "ltx-2";
    public const string TransformerFileName = RefineWeights.TransformerFileName;
    public const string VaeFileName = RefineWeights.VaeFileName;
    public const string UpsamplerFileName = RefineWeights.UpsamplerFileName;
    public const string LoraFileName = RefineWeights.LoraFileName;
    public const string ContextFileName = RefineContext.FileName;

    private readonly Refiner refiner;

    public UmbraRefineModel(
        string modelDirectory,
        UmbraDevice device,
        RefinePromptConfiguration promptConfiguration,
        RefineRepaintConfiguration repaintConfiguration,
        IReadOnlyDictionary<UmbraSequenceId, RefineConfiguration> configurations)
    {
        UmbraRuntime.UseDevice(device);

        refiner = Refiner.Load(
            Path.Combine(modelDirectory, DirectoryName),
            UmbraRuntime.GetDevice(device),
            GetModelDtype(device),
            promptConfiguration,
            repaintConfiguration,
            configurations);
    }

    public static IReadOnlyList<string> RequiredFiles { get; } =
    [
        Path.Combine(DirectoryName, TransformerFileName),
        Path.Combine(DirectoryName, VaeFileName),
        Path.Combine(DirectoryName, UpsamplerFileName),
        Path.Combine(DirectoryName, LoraFileName),
        Path.Combine(DirectoryName, ContextFileName)
    ];

    public IEnumerable<UmbraUpscaledFrame> Run(IEnumerable<UmbraSequenceGroup> groups)
    {
        using (torch.no_grad())
        {
            foreach (UmbraUpscaledFrame frame in refiner.Run(groups))
            {
                yield return frame;
            }
        }
    }

    public void Dispose()
    {
        refiner.Dispose();
    }

    private static torch.ScalarType GetModelDtype(UmbraDevice device)
    {
        return device switch
        {
            UmbraDevice.Cpu => torch.ScalarType.Float32,
            UmbraDevice.Gpu => torch.ScalarType.BFloat16
        };
    }
}
