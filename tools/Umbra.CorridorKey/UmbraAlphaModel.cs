using TorchSharp;

namespace Umbra.CorridorKey;

public sealed class UmbraAlphaModel : IDisposable
{
    public const string DirectoryName = "corridorkey";
    public const string BlueFileName = AlphaWeights.BlueFileName;
    public const string GreenFileName = AlphaWeights.GreenFileName;

    private readonly AlphaSolver solver;

    public UmbraAlphaModel(string modelDirectory, UmbraDevice device)
    {
        UmbraRuntime.UseDevice(device);

        solver = AlphaSolver.Load(
            Path.Combine(modelDirectory, DirectoryName), UmbraRuntime.GetDevice(device), GetModelDtype(device));
    }

    public static IReadOnlyList<string> RequiredFiles { get; } =
    [
        Path.Combine(DirectoryName, BlueFileName),
        Path.Combine(DirectoryName, GreenFileName)
    ];

    public IEnumerable<UmbraUpscaledFrame> Run(IEnumerable<UmbraSequenceGroup> groups, IUmbraDataProvider dataProvider)
    {
        using (torch.no_grad())
        {
            foreach (UmbraUpscaledFrame frame in solver.Run(groups, dataProvider))
            {
                yield return frame;
            }
        }
    }

    public void Dispose()
    {
        solver.Dispose();
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
