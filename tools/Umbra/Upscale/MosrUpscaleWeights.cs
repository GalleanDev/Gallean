using TorchSharp;

namespace Umbra;

internal sealed record MosrUpscaleWeights(IReadOnlyDictionary<string, torch.Tensor> State) : IDisposable
{
    public const string FileName = "mosr-aomr-2x.safetensors";

    public static MosrOptions Options { get; } = new(
        InChannels: 3,
        OutChannels: 3,
        Scale: 2,
        BlockCount: 24,
        Dimension: 64,
        KernelSize: 7,
        ExpansionRatio: 1.5,
        ConvRatio: 1.0,
        SampleGroups: 4);

    public static MosrUpscaleWeights Load(string directory)
    {
        string path = Path.Combine(directory, FileName);

        return new MosrUpscaleWeights(SafeTensors.LoadStateDict(path));
    }

    public MosrNetwork BuildNetwork()
    {
        MosrNetwork network = new(Options);

        network.load_state_dict(new Dictionary<string, torch.Tensor>(State), strict: true);
        network.to(torch.get_default_device());
        network.eval();

        return network;
    }

    public void Dispose()
    {
        foreach (KeyValuePair<string, torch.Tensor> parameter in State)
        {
            parameter.Value.Dispose();
        }
    }
}
