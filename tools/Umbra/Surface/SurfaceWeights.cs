using TorchSharp;

namespace Umbra;

internal sealed class SurfaceWeights(
    Dictionary<string, torch.Tensor> dit,
    Dictionary<string, torch.Tensor> vae) : IDisposable
{
    public const string DitFileName = "dit.safetensors";
    public const string VaeFileName = "vae.safetensors";

    public int Dim { get; } = (int)dit["patch_embedding.weight"].shape[0];
    public int InputChannels { get; } = (int)dit["patch_embedding.weight"].shape[1];
    public (long Time, long Height, long Width) Patch { get; } = ReadPatch(dit);
    public int OutputChannels { get; } = ReadOutputChannels(dit);
    public int FeedForwardDim { get; } = (int)dit["blocks.0.ffn.0.weight"].shape[0];
    public int TextDim { get; } = (int)dit["text_embedding.0.weight"].shape[1];
    public int FrequencyDim { get; } = (int)dit["time_embedding.0.weight"].shape[1];
    public int Layers { get; } = ReadLayers(dit);
    public int VaeDim { get; } = (int)vae["encoder.conv1.weight"].shape[0];
    public int LatentChannels { get; } = (int)vae["conv2.weight"].shape[0];
    public IReadOnlyList<int> VaeMultipliers { get; } = ReadVaeMultipliers(vae);
    public int VaeResidualBlocks { get; } = ReadVaeResidualBlocks(vae);
    public int Heads => Dim / 128;

    public static SurfaceWeights Load(string directory, torch.ScalarType dtype)
    {
        return new SurfaceWeights(
            MapState(Path.Combine(directory, DitFileName), dtype),
            MapState(Path.Combine(directory, VaeFileName), dtype));
    }

    public WanDit BuildDit()
    {
        WanDit built = MetaDevice.Construct(() => new WanDit(
            Dim,
            InputChannels,
            OutputChannels,
            FeedForwardDim,
            TextDim,
            FrequencyDim,
            Heads,
            Layers,
            Patch));

        ModuleParameters.Load(built, dit);
        built.eval();

        return built;
    }

    public WanVae BuildVae()
    {
        WanVae built = MetaDevice.Construct(() => new WanVae(
            VaeDim,
            LatentChannels,
            VaeMultipliers,
            VaeResidualBlocks));

        ModuleParameters.Load(built, vae);
        built.eval();

        return built;
    }

    public void Dispose()
    {
        DisposeState(dit);
        DisposeState(vae);
    }

    private static Dictionary<string, torch.Tensor> MapState(string path, torch.ScalarType dtype)
    {
        Dictionary<string, torch.Tensor> mapped = SafeTensors.MapStateDict(path);
        Dictionary<string, torch.Tensor> result = [];

        foreach ((string name, torch.Tensor tensor) in mapped)
        {
            torch.Tensor parameter = tensor.to(dtype);

            if (!ReferenceEquals(parameter, tensor))
            {
                tensor.Dispose();
            }

            result[name] = parameter;
        }

        return result;
    }

    private static (long Time, long Height, long Width) ReadPatch(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        long[] shape = state["patch_embedding.weight"].shape;

        return (shape[2], shape[3], shape[4]);
    }

    private static int ReadOutputChannels(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        (long Time, long Height, long Width) patch = ReadPatch(state);

        return (int)(state["head.head.weight"].shape[0] / (patch.Time * patch.Height * patch.Width));
    }

    private static int ReadLayers(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        int count = 0;

        while (state.ContainsKey($"blocks.{count}.modulation"))
        {
            count++;
        }

        return count;
    }

    private static IReadOnlyList<int> ReadVaeMultipliers(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        int dim = (int)state["encoder.conv1.weight"].shape[0];

        int stride = ReadVaeResidualBlocks(state) + 1;
        List<int> multipliers = [];

        while (state.TryGetValue($"encoder.downsamples.{multipliers.Count * stride}.residual.2.weight", out torch.Tensor? weight))
        {
            multipliers.Add((int)weight.shape[0] / dim);
        }

        return multipliers;
    }

    private static int ReadVaeResidualBlocks(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        int count = 0;

        while (state.ContainsKey($"encoder.downsamples.{count}.residual.2.weight"))
        {
            count++;
        }

        return count;
    }

    private static void DisposeState(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        foreach (KeyValuePair<string, torch.Tensor> parameter in state)
        {
            parameter.Value.Dispose();
        }
    }
}
