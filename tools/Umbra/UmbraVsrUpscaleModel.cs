using TorchSharp;

namespace Umbra;

public sealed class UmbraVsrUpscaleModel : IDisposable
{
    public const string FileName = VsrUpscaleWeights.FileName;

    private readonly VsrUpscaler upscaler;

    public UmbraVsrUpscaleModel(string modelDirectory, string scratchDirectory, UmbraDevice device)
    {
        UmbraRuntime.UseDevice(device);

        upscaler = VsrUpscaler.Load(modelDirectory, scratchDirectory, UmbraRuntime.GetDevice(device));
    }

    public static IReadOnlyList<string> RequiredFiles { get; } = [FileName];

    public IEnumerable<UmbraUpscaledFrame> Run(IReadOnlyList<UmbraSequenceGroup> groups)
    {
        using (torch.no_grad())
        {
            foreach (UmbraUpscaledFrame frame in upscaler.Run(groups))
            {
                yield return frame;
            }
        }
    }

    public void Dispose()
    {
        upscaler.Dispose();
    }
}
