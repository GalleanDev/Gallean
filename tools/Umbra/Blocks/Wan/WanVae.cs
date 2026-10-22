using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class WanVae : Module
{
    private const int ChunkFrames = 4;
    private const int Compression = 8;
    private const double WeightFloor = 1e-4;

    private static readonly double[] LatentMean =
    [
        -0.7571, -0.7089, -0.9113, 0.1075, -0.1745, 0.9653, -0.1517, 1.5508,
        0.4134, -0.0715, 0.5517, -0.3632, -0.1922, -0.9497, 0.2503, -0.2921
    ];

    private static readonly double[] LatentDeviation =
    [
        2.8184, 1.4541, 2.3275, 2.6558, 1.2196, 1.7708, 2.6052, 2.0743,
        3.2687, 2.1526, 2.8652, 1.5579, 1.6382, 1.1253, 2.8251, 1.9160
    ];

    private readonly int latentChannels;
    private readonly WanVaeEncoder encoder;
    private readonly WanVaeDecoder decoder;
    private readonly WanCausalConv3d conv1;
    private readonly WanCausalConv3d conv2;

    public WanVae(int dim, int latentChannels, IReadOnlyList<int> multipliers, int residualBlocks)
        : base(nameof(WanVae))
    {
        this.latentChannels = latentChannels;

        bool[] temporalDownsample = [false, true, true];
        bool[] temporalUpsample = [.. temporalDownsample.Reverse()];

        encoder = new WanVaeEncoder(dim, latentChannels * 2, multipliers, residualBlocks, temporalDownsample);
        conv1 = new WanCausalConv3d(latentChannels * 2, latentChannels * 2, (1, 1, 1), (0, 0, 0));
        conv2 = new WanCausalConv3d(latentChannels, latentChannels, (1, 1, 1), (0, 0, 0));
        decoder = new WanVaeDecoder(dim, latentChannels, multipliers, residualBlocks, temporalUpsample);

        RegisterComponents();
    }

    public int ConvolutionCount(bool forEncoder)
    {
        Module module = forEncoder ? encoder : decoder;
        int count = 0;

        foreach ((string _, Module child) in module.named_modules())
        {
            if (child is WanCausalConv3d)
            {
                count++;
            }
        }

        return count;
    }

    public torch.Tensor Encode(torch.Tensor source)
    {
        using IDisposable scope = torch.NewDisposeScope();
        using WanVaeCache cache = new(ConvolutionCount(forEncoder: true));

        using torch.Tensor images = source.to(conv1.Dtype);

        long frames = images.shape[2];
        long chunks = 1 + ((frames - 1) / ChunkFrames);

        List<torch.Tensor> outputs = [];

        for (long chunk = 0; chunk < chunks; chunk++)
        {
            cache.Rewind();

            long start = chunk == 0 ? 0 : 1 + (ChunkFrames * (chunk - 1));
            long count = chunk == 0 ? 1 : Math.Min(ChunkFrames, frames - start);

            using torch.Tensor slice = images.narrow(2, start, count);

            outputs.Add(encoder.Forward(slice, cache));
        }

        using torch.Tensor output = torch.cat(outputs, dim: 2);
        using torch.Tensor moments = conv1.Forward(output, null);
        using torch.Tensor mean = moments.narrow(1, 0, latentChannels);

        torch.Tensor result = Whiten(mean);

        foreach (torch.Tensor part in outputs)
        {
            part.Dispose();
        }

        return result.MoveToOuterDisposeScope();
    }

    public torch.Tensor EncodeTiled(
        torch.Tensor source,
        (long Height, long Width) tile,
        (long Height, long Width) stride)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long height = source.shape[3];
        long width = source.shape[4];
        long frames = 1 + ((source.shape[2] - 1) / ChunkFrames);

        List<(long Start, long End)> rows = WanVaeTiling.BuildTiles(height, tile.Height, stride.Height);
        List<(long Start, long End)> columns = WanVaeTiling.BuildTiles(width, tile.Width, stride.Width);

        using torch.Tensor values = torch.zeros(
            [1, latentChannels, frames, height / Compression, width / Compression],
            torch.ScalarType.Float32, torch.CPU);
        using torch.Tensor weights = torch.zeros(
            [1, 1, frames, height / Compression, width / Compression], torch.ScalarType.Float32, torch.CPU);

        foreach ((long top, long bottom) in rows)
        {
            foreach ((long left, long right) in columns)
            {
                using IDisposable tileScope = torch.NewDisposeScope();

                using torch.Tensor patch = source
                    .narrow(3, top, bottom - top)
                    .narrow(4, left, right - left);
                using torch.Tensor latents = Encode(patch);
                using torch.Tensor cpuLatents = latents.to(torch.ScalarType.Float32, torch.CPU);
                using torch.Tensor mask = WanVaeTiling.BuildMask(
                    cpuLatents.shape[3],
                    cpuLatents.shape[4],
                    (top == 0, bottom >= height, left == 0, right >= width),
                    ((tile.Height - stride.Height) / Compression, (tile.Width - stride.Width) / Compression),
                    torch.ScalarType.Float32,
                    torch.CPU);
                using torch.Tensor weightedLatents = cpuLatents.mul(mask);

                values
                    .narrow(3, top / Compression, cpuLatents.shape[3])
                    .narrow(4, left / Compression, cpuLatents.shape[4])
                    .add_(weightedLatents);

                weights
                    .narrow(3, top / Compression, cpuLatents.shape[3])
                    .narrow(4, left / Compression, cpuLatents.shape[4])
                    .add_(mask);
            }
        }

        using torch.Tensor average = values.div(weights.clamp_min(WeightFloor));

        return average.to(conv1.Dtype, source.device).MoveToOuterDisposeScope();
    }

    public torch.Tensor DecodeTiled(
        torch.Tensor source,
        (long Height, long Width) tile,
        (long Height, long Width) stride)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long height = source.shape[3];
        long width = source.shape[4];
        long frames = ((source.shape[2] - 1) * ChunkFrames) + 1;

        List<(long Start, long End)> rows = WanVaeTiling.BuildTiles(height, tile.Height, stride.Height);
        List<(long Start, long End)> columns = WanVaeTiling.BuildTiles(width, tile.Width, stride.Width);

        using torch.Tensor values = torch.zeros(
            [1, 3, frames, height * Compression, width * Compression], torch.ScalarType.Float32, torch.CPU);
        using torch.Tensor weights = torch.zeros(
            [1, 1, frames, height * Compression, width * Compression], torch.ScalarType.Float32, torch.CPU);

        foreach ((long top, long bottom) in rows)
        {
            foreach ((long left, long right) in columns)
            {
                using IDisposable tileScope = torch.NewDisposeScope();

                using torch.Tensor patch = source
                    .narrow(3, top, bottom - top)
                    .narrow(4, left, right - left);
                using torch.Tensor video = Decode(patch);
                using torch.Tensor cpuVideo = video.to(torch.ScalarType.Float32, torch.CPU);
                using torch.Tensor mask = WanVaeTiling.BuildMask(
                    cpuVideo.shape[3],
                    cpuVideo.shape[4],
                    (top == 0, bottom >= height, left == 0, right >= width),
                    ((tile.Height - stride.Height) * Compression, (tile.Width - stride.Width) * Compression),
                    torch.ScalarType.Float32,
                    torch.CPU);
                using torch.Tensor weightedVideo = cpuVideo.mul(mask);

                values
                    .narrow(3, top * Compression, cpuVideo.shape[3])
                    .narrow(4, left * Compression, cpuVideo.shape[4])
                    .add_(weightedVideo);

                weights
                    .narrow(3, top * Compression, cpuVideo.shape[3])
                    .narrow(4, left * Compression, cpuVideo.shape[4])
                    .add_(mask);
            }
        }

        using torch.Tensor average = values.div(weights.clamp_min(WeightFloor));

        return average.to(conv2.Dtype, source.device).MoveToOuterDisposeScope();
    }

    public torch.Tensor Decode(torch.Tensor source)
    {
        using IDisposable scope = torch.NewDisposeScope();
        using WanVaeCache cache = new(ConvolutionCount(forEncoder: false));

        using torch.Tensor latents = source.to(conv2.Dtype);
        using torch.Tensor unwhitenedLatents = Unwhiten(latents);
        using torch.Tensor features = conv2.Forward(unwhitenedLatents, null);

        List<torch.Tensor> outputs = [];

        for (long frame = 0; frame < features.shape[2]; frame++)
        {
            cache.Rewind();

            using torch.Tensor slice = features.narrow(2, frame, 1);

            outputs.Add(decoder.Forward(slice, cache));
        }

        torch.Tensor result = torch.cat(outputs, dim: 2);

        foreach (torch.Tensor part in outputs)
        {
            part.Dispose();
        }

        return result.MoveToOuterDisposeScope();
    }

    private torch.Tensor Whiten(torch.Tensor latents)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor mean = Statistic(LatentMean, latents);
        using torch.Tensor deviation = Statistic(LatentDeviation, latents);

        torch.Tensor result = latents.sub(mean).div(deviation);

        return result.MoveToOuterDisposeScope();
    }

    private torch.Tensor Unwhiten(torch.Tensor latents)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor mean = Statistic(LatentMean, latents);
        using torch.Tensor deviation = Statistic(LatentDeviation, latents);

        torch.Tensor result = latents.mul(deviation).add(mean);

        return result.MoveToOuterDisposeScope();
    }

    private torch.Tensor Statistic(double[] values, torch.Tensor reference)
    {
        return torch.tensor(values[..latentChannels], reference.dtype, reference.device)
            .reshape(1, latentChannels, 1, 1, 1);
    }
}
