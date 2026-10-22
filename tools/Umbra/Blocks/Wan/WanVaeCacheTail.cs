using TorchSharp;

namespace Umbra;

internal static class WanVaeCacheTail
{
    public static torch.Tensor Take(torch.Tensor input, WanVaeCache cache, int index)
    {
        long available = Math.Min(WanVaeCache.Depth, input.shape[2]);

        torch.Tensor tail = input.narrow(2, input.shape[2] - available, available).clone();

        if (tail.shape[2] < WanVaeCache.Depth && cache.Get(index) is { } previous)
        {
            using torch.Tensor filler = previous.narrow(2, previous.shape[2] - 1, 1).to(tail.dtype, tail.device);
            using torch.Tensor extendedTail = torch.cat([filler, tail], dim: 2);

            tail.Dispose();

            return extendedTail.clone();
        }

        return tail;
    }

    public static torch.Tensor Convolve(WanCausalConv3d conv, torch.Tensor input, WanVaeCache? cache)
    {
        if (cache is null)
        {
            return conv.Forward(input, null);
        }

        int index = cache.Take();
        torch.Tensor tail = Take(input, cache, index);
        torch.Tensor result = conv.Forward(input, cache.Get(index));

        cache.Set(index, tail);

        return result;
    }
}
