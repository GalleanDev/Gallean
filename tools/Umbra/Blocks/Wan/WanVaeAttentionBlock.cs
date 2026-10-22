using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanVaeAttentionBlock : Module<torch.Tensor, torch.Tensor>
{
    private readonly WanRmsNorm norm;

    [ComponentName(Name = "to_qkv")]
    private readonly Conv2d toQkv;

    private readonly Conv2d proj;

    public WanVaeAttentionBlock(int channels)
        : base(nameof(WanVaeAttentionBlock))
    {
        norm = new WanRmsNorm(channels);
        toQkv = Conv2d(channels, channels * 3, kernel_size: 1);
        proj = Conv2d(channels, channels, kernel_size: 1);

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long channels = input.shape[1];
        long frames = input.shape[2];
        long height = input.shape[3];
        long width = input.shape[4];

        using torch.Tensor planes = input.permute(0, 2, 1, 3, 4).reshape(batch * frames, channels, height, width);
        using torch.Tensor normalizedPlanes = norm.call(planes);
        using torch.Tensor queryKeyValue = toQkv.call(normalizedPlanes);
        using torch.Tensor tokens = queryKeyValue
            .reshape(batch * frames, 1, channels * 3, height * width)
            .permute(0, 1, 3, 2)
            .contiguous();

        torch.Tensor[] parts = tokens.chunk(3, dim: -1);

        using torch.Tensor query = parts[0];
        using torch.Tensor key = parts[1];
        using torch.Tensor value = parts[2];
        using torch.Tensor attention = functional.scaled_dot_product_attention(query, key, value);
        using torch.Tensor attentionPlanes = attention
            .squeeze(1)
            .permute(0, 2, 1)
            .reshape(batch * frames, channels, height, width);
        using torch.Tensor output = proj.call(attentionPlanes);
        using torch.Tensor video = output
            .reshape(batch, frames, channels, height, width)
            .permute(0, 2, 1, 3, 4)
            .contiguous();

        torch.Tensor result = video.add(input);

        return result.MoveToOuterDisposeScope();
    }
}
