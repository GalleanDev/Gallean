using System.Text.RegularExpressions;
using TorchSharp;

namespace Umbra;

internal sealed partial record VsrUpscaleWeights(
    IReadOnlyDictionary<string, torch.Tensor> State,
    int MidChannels,
    int BlockCount) : IDisposable
{
    public const string FileName = "basicvsrpp-reds4.safetensors";

    private const string GeneratorPrefix = "generator.";
    private const string UpsampleConv1 = "upsample1.upsample_conv";
    private const string UpsampleConv2 = "upsample2.upsample_conv";
    private const long UpsampleChunkRows = 128;

    public static VsrUpscaleWeights Load(string directory)
    {
        string path = Path.Combine(directory, FileName);
        Dictionary<string, torch.Tensor> state = [];

        foreach (KeyValuePair<string, torch.Tensor> parameter in SafeTensors.LoadStateDict(path))
        {
            if (parameter.Key.StartsWith(GeneratorPrefix, StringComparison.Ordinal))
            {
                state[Rename(parameter.Key[GeneratorPrefix.Length..])] = parameter.Value;
            }
        }

        return new VsrUpscaleWeights(state, ReadMidChannels(state), ReadBlockCount(state));
    }

    public BasicVsrNetwork BuildNetwork(string scratchDirectory)
    {
        BasicVsrNetwork network = new(MidChannels, BlockCount, scratchDirectory, UpsampleChunkRows);

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

    [GeneratedRegex(@"^(spynet\.basic_module\.\d+\.basic_module\.)(\d+)\.conv\.(weight|bias)$")]
    private static partial Regex SpyNetConvPattern();

    private static string Rename(string name)
    {
        if (name.StartsWith(UpsampleConv1, StringComparison.Ordinal))
        {
            return string.Concat("upconv1", name.AsSpan(UpsampleConv1.Length));
        }
        else if (name.StartsWith(UpsampleConv2, StringComparison.Ordinal))
        {
            return string.Concat("upconv2", name.AsSpan(UpsampleConv2.Length));
        }
        else
        {
            return SpyNetConvPattern().Replace(name, RenameSpyNetConv);
        }
    }

    private static string RenameSpyNetConv(Match match)
    {
        GroupCollection groups = match.Groups;
        int index = int.Parse(groups[2].Value);

        return $"{groups[1].Value}{index * 2}.{groups[3].Value}";
    }

    private static int ReadMidChannels(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        torch.Tensor weight = state["feat_extract.main.0.weight"];

        return (int)weight.shape[0];
    }

    private static int ReadBlockCount(IReadOnlyDictionary<string, torch.Tensor> state)
    {
        int count = 0;

        while (state.ContainsKey($"backbone.backward_1.main.2.{count}.conv1.weight"))
        {
            count++;
        }

        return count;
    }
}
