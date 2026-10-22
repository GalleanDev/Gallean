using TorchSharp;

namespace Umbra;

public sealed class UmbraMosrUpscaleModel : IDisposable
{
    public const string FileName = MosrUpscaleWeights.FileName;

    private readonly MosrUpscaler upscaler;

    public UmbraMosrUpscaleModel(string modelDirectory, UmbraDevice device)
    {
        UmbraRuntime.UseDevice(device);

        upscaler = MosrUpscaler.Load(modelDirectory, UmbraRuntime.GetDevice(device));
    }

    public static IReadOnlyList<string> RequiredFiles { get; } = [FileName];

    public IEnumerable<UmbraUpscaledFrame> Run(IReadOnlyList<UmbraSequence> sequences)
    {
        using (torch.no_grad())
        {
            foreach (UmbraUpscaledFrame frame in upscaler.Run(sequences))
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
