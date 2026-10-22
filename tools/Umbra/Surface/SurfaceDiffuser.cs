using TorchSharp;

namespace Umbra;

internal sealed class SurfaceDiffuser(
    SurfaceWeights weights,
    WanDit dit,
    WanVae vae,
    SurfaceContext context,
    int denoisingSteps) : IDisposable
{
    private const int FrameQuantum = 4;
    private const int LeadFrames = FrameQuantum + 1;
    private const double GuidanceScale = 5;
    private const int MinDenoisingSteps = 2;
    private const long Seed = 1;
    private const long SpatialFactor = 8;
    private const long TileFrames = 240;
    private const long TileStride = 120;
    private const long TileThreshold = 384;

    private readonly SurfaceFlowMatchScheduler scheduler = new(denoisingSteps);
    private readonly TeaCache teaCache = new(TeaCacheOptions.WanTextToVideoSmall(TeaCacheOptions.DefaultThreshold, denoisingSteps));

    public static int GetTotalFrameCount(int frameCount)
    {
        int sourceFrameCount = frameCount + LeadFrames;

        return ((sourceFrameCount - 1 + FrameQuantum - 1) / FrameQuantum) + 1;
    }

    public static SurfaceDiffuser Load(string directory, torch.ScalarType dtype, int denoisingSteps)
    {
        if (denoisingSteps < MinDenoisingSteps)
        {
            throw new ArgumentOutOfRangeException(nameof(denoisingSteps), denoisingSteps, null);
        }

        SurfaceWeights weights = SurfaceWeights.Load(directory, dtype);

        return new SurfaceDiffuser(
            weights,
            weights.BuildDit(),
            weights.BuildVae(),
            SurfaceContext.Load(directory),
            denoisingSteps);
    }

    public SurfaceConditioning EncodeConditioning(torch.Tensor images, torch.Tensor coverage)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor leadImages = Lead(images);
        using torch.Tensor paddedImages = Hold(leadImages);
        using torch.Tensor video = paddedImages.permute(1, 0, 2, 3).unsqueeze(0).mul(2.0).sub(1.0);

        torch.Tensor rgbLatents = Encode(video).MoveToOuterDisposeScope();
        torch.Tensor mask = BuildMask(coverage, rgbLatents).MoveToOuterDisposeScope();

        return new SurfaceConditioning(rgbLatents, images.shape[0], mask);
    }

    public torch.Tensor Run(SurfaceConditioning conditioning, SurfaceChannelType channel)
    {
        using IDisposable scope = torch.NewDisposeScope();

        teaCache.Reset();

        torch.Tensor rgbLatents = conditioning.Encoded;
        torch.Tensor mask = conditioning.Mask;

        using torch.Tensor positive = context.BuildPositive(channel, rgbLatents.dtype, rgbLatents.device);
        using torch.Tensor negative = context.BuildNegative(rgbLatents.dtype, rgbLatents.device);
        torch.Tensor latents = StableNoise.Generate(Seed, rgbLatents.shape, rgbLatents.dtype, rgbLatents.device);

        for (int step = 0; step < scheduler.StepCount; step++)
        {
            using IDisposable stepScope = torch.NewDisposeScope();
            using torch.Tensor previous = latents;
            using torch.Tensor timestep = torch.full(
                1, scheduler.GetTimestep(step), rgbLatents.dtype, rgbLatents.device);

            torch.Tensor positiveNoise;
            torch.Tensor negativeNoise;

            using (torch.Tensor input = torch.cat([previous, rgbLatents], dim: 1))
            {
                positiveNoise = dit.Forward(input, positive, timestep, teaCache, mask);
                negativeNoise = dit.Forward(input, negative, timestep, teaCache, mask);
            }

            using torch.Tensor unconditional = negativeNoise;
            using torch.Tensor estimate = positiveNoise.sub_(unconditional).mul_(GuidanceScale).add_(unconditional);

            latents = scheduler.Step(estimate, previous, step).MoveToOuterDisposeScope();
        }

        using torch.Tensor finalLatents = latents;
        using torch.Tensor maskedLatents = finalLatents.mul(mask);
        using torch.Tensor decodedVideo = Decode(maskedLatents);

        torch.Tensor result = decodedVideo
            .squeeze(0)
            .permute(1, 0, 2, 3)
            .narrow(0, LeadFrames, conditioning.FrameCount)
            .to(torch.float32)
            .div(2.0)
            .add(0.5)
            .clamp(0.0, 1.0);

        return result.MoveToOuterDisposeScope();
    }

    public void Dispose()
    {
        teaCache.Dispose();
        weights.Dispose();
        dit.Dispose();
        vae.Dispose();
        context.Dispose();
    }

    private static torch.Tensor Lead(torch.Tensor images)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor head = images.narrow(0, 0, 1).expand(LeadFrames, -1, -1, -1);

        torch.Tensor result = torch.cat([head, images], dim: 0);

        return result.MoveToOuterDisposeScope();
    }

    private static torch.Tensor Hold(torch.Tensor images)
    {
        long frames = images.shape[0];
        long padded = (((frames - 1 + FrameQuantum - 1) / FrameQuantum) * FrameQuantum) + 1;

        if (padded == frames)
        {
            return images.alias();
        }
        else
        {
            using IDisposable scope = torch.NewDisposeScope();

            using torch.Tensor tail = images.narrow(0, frames - 1, 1).expand(padded - frames, -1, -1, -1);

            torch.Tensor result = torch.cat([images, tail], dim: 0);

            return result.MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor BuildMask(torch.Tensor coverage, torch.Tensor rgbLatents)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor leadCoverage = Lead(coverage);
        using torch.Tensor paddedCoverage = Hold(leadCoverage);

        long frames = paddedCoverage.shape[0];

        using torch.Tensor firstCoverage = paddedCoverage.narrow(0, 0, 1);
        using torch.Tensor restCoverage = paddedCoverage
            .narrow(0, 1, frames - 1)
            .reshape(-1, FrameQuantum, paddedCoverage.shape[1], paddedCoverage.shape[2], paddedCoverage.shape[3])
            .amax([1]);
        using torch.Tensor latentCoverage = torch.cat([firstCoverage, restCoverage], dim: 0);
        using torch.Tensor cells = torch.nn.functional.max_pool2d(latentCoverage, [SpatialFactor, SpatialFactor], [SpatialFactor, SpatialFactor]);
        using torch.Tensor dilatedCells = torch.nn.functional.max_pool2d(cells, [5, 5], [1, 1], [2, 2]);
        using torch.Tensor coveredCells = dilatedCells.gt(0.0);
        using torch.Tensor anyCovered = coveredCells.any();
        using torch.Tensor allCells = torch.ones_like(coveredCells);
        using torch.Tensor mask = torch.where(anyCovered, coveredCells, allCells);

        return mask.transpose(0, 1).unsqueeze(0).to(rgbLatents.dtype, rgbLatents.device).MoveToOuterDisposeScope();
    }

    private torch.Tensor Encode(torch.Tensor video)
    {
        if (Math.Max(video.shape[3], video.shape[4]) > TileThreshold)
        {
            return vae.EncodeTiled(video, (TileFrames, TileFrames), (TileStride, TileStride));
        }
        else
        {
            return vae.Encode(video);
        }
    }

    private torch.Tensor Decode(torch.Tensor latents)
    {
        if (Math.Max(latents.shape[3], latents.shape[4]) * SpatialFactor > TileThreshold)
        {
            return vae.DecodeTiled(
                latents,
                (TileFrames / SpatialFactor, TileFrames / SpatialFactor),
                (TileStride / SpatialFactor, TileStride / SpatialFactor));
        }
        else
        {
            return vae.Decode(latents);
        }
    }
}
