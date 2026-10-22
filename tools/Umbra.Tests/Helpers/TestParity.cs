using TorchSharp;

namespace Umbra.Tests;

internal static class TestParity
{
    private const string DirectoryName = "Parity";

    public static Dictionary<string, torch.Tensor> Load(string name)
    {
        return SafeTensors.LoadStateDict(Path.Combine(AppContext.BaseDirectory, DirectoryName, $"{name}.safetensors"));
    }

    public static Dictionary<string, torch.Tensor> LoadWeights(string name, string prefix)
    {
        Dictionary<string, torch.Tensor> weights = [];

        foreach ((string key, torch.Tensor tensor) in Load(name))
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                weights[key[prefix.Length..]] = tensor;
            }
        }

        return weights;
    }
}
