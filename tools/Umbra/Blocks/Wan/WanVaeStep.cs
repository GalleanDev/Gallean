using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal static class WanVaeStep
{
    public static torch.Tensor Apply(
        Module<torch.Tensor, torch.Tensor> layer,
        torch.Tensor input,
        WanVaeCache? cache)
    {
        torch.Tensor result = layer switch
        {
            WanVaeResidualBlock residual => residual.Forward(input, cache),
            WanResample resample => resample.Forward(input, cache),
            WanCausalConv3d conv => WanVaeCacheTail.Convolve(conv, input, cache),
            _ => layer.call(input)
        };

        input.Dispose();

        return result;
    }
}
