using TorchSharp;

namespace Umbra;

internal sealed class SurfaceInverseRenderer(
    SurfaceDiffuser diffuser,
    IReadOnlyList<SurfacePass> passes,
    SurfaceOptions options) : IDisposable
{
    public const string DirectoryName = "vrgbx-inverse";

    private const long MaxRestoreSize = 64L * 1024 * 1024;

    private static readonly SurfacePass[] Passes =
    [
        new(UmbraSurfaceLayerType.Normal, SurfaceChannelType.Normal)
    ];

    public static SurfaceInverseRenderer Load(
        string modelDirectory,
        IReadOnlyList<UmbraSurfaceLayerType> layerTypes,
        int denoisingSteps,
        torch.ScalarType dtype)
    {
        SurfaceDiffuser diffuser = SurfaceDiffuser.Load(Path.Combine(modelDirectory, DirectoryName), dtype, denoisingSteps);

        List<SurfacePass> selected = [.. Passes.Where(pass => layerTypes.Contains(pass.Layer))];

        return new SurfaceInverseRenderer(diffuser, selected, new SurfaceOptions());
    }

    public IEnumerable<UmbraSurfaceLayer> Run(IEnumerable<UmbraSequenceGroup> groups)
    {
        foreach (UmbraSequenceGroup group in groups)
        {
            List<UmbraFrame> frames = group.CollectFrames();
            List<SurfaceFrame> selection = SelectFrames(frames);

            foreach (UmbraSurfaceLayer layer in Render(frames, selection))
            {
                yield return layer;
            }
        }
    }

    public void Dispose()
    {
        diffuser.Dispose();
    }

    private static List<SurfaceFrame> SelectFrames(IReadOnlyList<UmbraFrame> frames)
    {
        HashSet<UmbraSpriteHash> frameHashes = [];
        List<SurfaceFrame> selection = [];

        for (int index = 0; index < frames.Count; index++)
        {
            UmbraFrame frame = frames[index];

            if (frameHashes.Add(frame.Hash))
            {
                selection.Add(new SurfaceFrame(index, frame.Hash, frame.Sprite));
            }
        }

        return selection;
    }

    private IReadOnlyList<UmbraSurfaceLayer> Render(IReadOnlyList<UmbraFrame> frames, IReadOnlyList<SurfaceFrame> selection)
    {
        if (selection.Count == 0)
        {
            return [];
        }

        using IDisposable scope = torch.NewDisposeScope();

        SurfaceCanvas canvas = SurfaceCanvas.Build(frames);
        SurfaceResolution resolution = GetResolution(canvas, frames.Count);

        using torch.Tensor input = Compose(frames, canvas, resolution, options.Background);
        using torch.Tensor coverage = GetCoverage(frames, canvas);
        using torch.Tensor sampledCoverage = Sample(coverage, resolution);

        SurfaceConditioning conditioning = diffuser.EncodeConditioning(input, sampledCoverage);

        List<UmbraSurfaceLayer> layers = [];

        foreach (SurfacePass pass in passes)
        {
            using torch.Tensor paddedEstimate = diffuser.Run(conditioning, pass.Channel);
            using torch.Tensor estimate = Crop(paddedEstimate, resolution);

            layers.AddRange(Restore(canvas, selection, pass.Layer, estimate, coverage));
        }

        return layers;
    }

    private SurfaceResolution GetResolution(SurfaceCanvas canvas, int frameCount)
    {
        int totalFrames = SurfaceDiffuser.GetTotalFrameCount(frameCount);

        return SurfaceResolution.Calculate(canvas.PixelHeight, canvas.PixelWidth, options.MaxPixels / totalFrames);
    }

    private static torch.Tensor Pad(torch.Tensor images, SurfaceResolution resolution, double background)
    {
        if (resolution.PixelHeight == resolution.ContentHeight && resolution.PixelWidth == resolution.ContentWidth)
        {
            return images.alias();
        }
        else
        {
            using IDisposable scope = torch.NewDisposeScope();

            torch.Tensor paddedImages = torch.full(
                [images.shape[0], images.shape[1], resolution.PixelHeight, resolution.PixelWidth],
                background,
                images.dtype,
                images.device);

            paddedImages.narrow(2, 0, resolution.ContentHeight).narrow(3, 0, resolution.ContentWidth).copy_(images);

            return paddedImages.MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor Sample(torch.Tensor coverage, SurfaceResolution resolution)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor pooledCoverage = torch.nn.functional.adaptive_max_pool2d(
            coverage,
            [resolution.ContentHeight, resolution.ContentWidth]);

        return Pad(pooledCoverage, resolution, 0).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Crop(torch.Tensor images, SurfaceResolution resolution)
    {
        if (images.shape[2] == resolution.ContentHeight && images.shape[3] == resolution.ContentWidth)
        {
            return images.alias();
        }
        else
        {
            return images.narrow(2, 0, resolution.ContentHeight).narrow(3, 0, resolution.ContentWidth).contiguous();
        }
    }

    private static torch.Tensor Resize(torch.Tensor images, int pixelHeight, int pixelWidth)
    {
        if (images.shape[2] == pixelHeight && images.shape[3] == pixelWidth)
        {
            return images.alias();
        }
        else
        {
            return PixelOperations.Resize(images, pixelHeight, pixelWidth).clamp(0.0, 1.0);
        }
    }

    private static torch.Tensor Compose(
        IReadOnlyList<UmbraFrame> frames,
        SurfaceCanvas canvas,
        SurfaceResolution resolution,
        double background)
    {
        using IDisposable scope = torch.NewDisposeScope();

        List<torch.Tensor> composites = [];

        for (int index = 0; index < frames.Count; index++)
        {
            using IDisposable frameScope = torch.NewDisposeScope();
            using torch.Tensor frame = canvas.Compose(index, frames[index].Sprite, background);
            using torch.Tensor resizedFrame = Resize(frame.unsqueeze(0), resolution.ContentHeight, resolution.ContentWidth);

            composites.Add(Pad(resizedFrame, resolution, background).MoveToOuterDisposeScope());
        }

        return torch.cat(composites, dim: 0).MoveToOuterDisposeScope();
    }

    private static torch.Tensor GetCoverage(IReadOnlyList<UmbraFrame> frames, SurfaceCanvas canvas)
    {
        using IDisposable scope = torch.NewDisposeScope();

        List<torch.Tensor> coverages = [];

        for (int index = 0; index < frames.Count; index++)
        {
            coverages.Add(canvas.GetCoverage(index, frames[index].Sprite));
        }

        return torch.stack(coverages).MoveToOuterDisposeScope();
    }

    private static List<UmbraSurfaceLayer> Restore(
        SurfaceCanvas canvas,
        IReadOnlyList<SurfaceFrame> frames,
        UmbraSurfaceLayerType layer,
        torch.Tensor estimate,
        torch.Tensor coverage)
    {
        List<UmbraSurfaceLayer> result = [];

        long total = estimate.shape[0];
        long chunk = ChunkFrames(estimate.shape[1], canvas);

        for (long start = 0; start < total; start += chunk)
        {
            using IDisposable scope = torch.NewDisposeScope();

            long count = Math.Min(chunk, total - start);

            using torch.Tensor estimateChunk = estimate.narrow(0, start, count);
            using torch.Tensor field = Resize(estimateChunk, canvas.PixelHeight, canvas.PixelWidth);
            using torch.Tensor coverageChunk = coverage.narrow(0, start, count);
            using torch.Tensor planes = SurfaceBackground.Remove(field, coverageChunk);

            foreach (SurfaceFrame frame in frames)
            {
                if (frame.Index >= start && frame.Index < start + count)
                {
                    using IDisposable frameScope = torch.NewDisposeScope();
                    using torch.Tensor canvasPlanes = planes.select(0, frame.Index - start);
                    using torch.Tensor spritePlanes = canvas.Extract(frame.Index, canvasPlanes);

                    result.Add(ToLayer(frame, layer, spritePlanes));
                }
            }
        }

        return result;
    }

    private static long ChunkFrames(long channels, SurfaceCanvas canvas)
    {
        long perFrame = channels * canvas.PixelHeight * canvas.PixelWidth;

        return Math.Max(1, MaxRestoreSize / perFrame);
    }

    private static UmbraSurfaceLayer ToLayer(SurfaceFrame frame, UmbraSurfaceLayerType type, torch.Tensor planes)
    {
        using IDisposable scope = torch.NewDisposeScope();

        UmbraSpriteMetadata metadata = frame.Sprite.Metadata;

        torch.Tensor source = PixelTensor.ToPlanes(frame.Sprite.Pixels, metadata.PixelWidth, metadata.PixelHeight, planes.device);
        torch.Tensor layerPlanes = torch.cat([planes, source.narrow(0, 3, 1)], dim: 0);

        return new UmbraSurfaceLayer(frame.Hash, type, metadata.PixelWidth, metadata.PixelHeight, PixelTensor.ToPixels(layerPlanes));
    }
}
