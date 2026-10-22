using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal static class WanAttention
{
    public static torch.Tensor Apply(torch.Tensor query, torch.Tensor key, torch.Tensor value, int heads)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor splitQuery = Split(query, heads);
        using torch.Tensor splitKey = Split(key, heads);
        using torch.Tensor splitValue = Split(value, heads);
        using torch.Tensor attention = functional.scaled_dot_product_attention(splitQuery, splitKey, splitValue);

        torch.Tensor result = attention
            .transpose(1, 2)
            .reshape(query.shape[0], attention.shape[2], -1);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Split(torch.Tensor input, int heads)
    {
        return input
            .reshape(input.shape[0], input.shape[1], heads, -1)
            .transpose(1, 2)
            .contiguous();
    }
}
