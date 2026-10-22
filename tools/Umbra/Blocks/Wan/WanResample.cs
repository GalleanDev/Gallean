using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanResample : Module<torch.Tensor, torch.Tensor>
{
    private readonly WanResampleType type;
    private readonly ModuleList<Module<torch.Tensor, torch.Tensor>> resample;

    [ComponentName(Name = "time_conv")]
    private readonly WanCausalConv3d? timeConv;

    public WanResample(int channels, WanResampleType type)
        : base(nameof(WanResample))
    {
        this.type = type;

        resample = type switch
        {
            WanResampleType.Upsample2d or WanResampleType.Upsample3d =>
                new ModuleList<Module<torch.Tensor, torch.Tensor>>(
                    Upsample(scale_factor: [2.0, 2.0], mode: torch.UpsampleMode.Nearest),
                    Conv2d(channels, channels / 2, kernel_size: 3, padding: 1)),
            WanResampleType.Downsample2d or WanResampleType.Downsample3d =>
                new ModuleList<Module<torch.Tensor, torch.Tensor>>(
                    ZeroPad2d((0, 1, 0, 1)),
                    Conv2d(channels, channels, kernel_size: 3, stride: 2))
        };

        timeConv = type switch
        {
            WanResampleType.Upsample3d => new WanCausalConv3d(channels, channels * 2, (3, 1, 1), (1, 0, 0)),
            WanResampleType.Downsample3d => new WanCausalConv3d(channels, channels, (3, 1, 1), (0, 0, 0), stride: (2, 1, 1)),
            _ => null
        };

        RegisterComponents();
    }

    public override torch.Tensor forward(torch.Tensor input)
    {
        return Forward(input, null);
    }

    public torch.Tensor Forward(torch.Tensor input, WanVaeCache? cache)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor result = input;

        if (type == WanResampleType.Upsample3d && cache is not null && timeConv is not null)
        {
            result = UpsampleTime(result, timeConv, cache);
        }

        result = ApplySpatial(result);

        if (type == WanResampleType.Downsample3d && cache is not null && timeConv is not null)
        {
            result = DownsampleTime(result, timeConv, cache);
        }

        return result.MoveToOuterDisposeScope();
    }

    private torch.Tensor ApplySpatial(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long batch = input.shape[0];
        long frames = input.shape[2];

        using torch.Tensor planes = input.permute(0, 2, 1, 3, 4).reshape(
            batch * frames, input.shape[1], input.shape[3], input.shape[4]);

        using torch.Tensor resizedPlanes = resample[0].call(planes);
        using torch.Tensor resampledPlanes = resample[1].call(resizedPlanes);

        torch.Tensor result = resampledPlanes
            .reshape(batch, frames, resampledPlanes.shape[1], resampledPlanes.shape[2], resampledPlanes.shape[3])
            .permute(0, 2, 1, 3, 4)
            .contiguous();

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor UpsampleTime(torch.Tensor input, WanCausalConv3d conv, WanVaeCache cache)
    {
        using IDisposable scope = torch.NewDisposeScope();

        int index = cache.Take();

        if (cache.IsEmpty(index))
        {
            cache.MarkRepeated(index);

            return input.alias().MoveToOuterDisposeScope();
        }

        torch.Tensor tail = WanVaeCacheTail.Take(input, cache, index);
        torch.Tensor output = conv.Forward(input, cache.Get(index));

        cache.Set(index, tail);

        long batch = input.shape[0];
        long channels = input.shape[1];
        long frames = input.shape[2];

        using torch.Tensor halves = output.reshape(batch, 2, channels, frames, input.shape[3], input.shape[4]);
        using torch.Tensor first = halves.select(1, 0);
        using torch.Tensor second = halves.select(1, 1);
        using torch.Tensor stack = torch.stack([first, second], dim: 3);

        torch.Tensor result = stack.reshape(batch, channels, frames * 2, input.shape[3], input.shape[4]);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor DownsampleTime(torch.Tensor input, WanCausalConv3d conv, WanVaeCache cache)
    {
        using IDisposable scope = torch.NewDisposeScope();

        int index = cache.Take();

        if (cache.Get(index) is not { } stored)
        {
            cache.Set(index, input.clone());

            return input.alias().MoveToOuterDisposeScope();
        }

        using torch.Tensor previous = stored.narrow(2, stored.shape[2] - 1, 1);
        using torch.Tensor extendedInput = torch.cat([previous.to(input.dtype, input.device), input], dim: 2);

        torch.Tensor tail = input.narrow(2, input.shape[2] - 1, 1).clone();
        torch.Tensor result = conv.Forward(extendedInput, null);

        cache.Set(index, tail);

        return result.MoveToOuterDisposeScope();
    }
}
