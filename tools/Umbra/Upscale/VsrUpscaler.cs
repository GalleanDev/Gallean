using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra;

internal sealed class VsrUpscaler(BasicVsrNetwork network, torch.Device device) : IDisposable
{
    private const int DownsampleFactor = 2;
    private const int WindowFrames = 3;
    private const int MinimumInputSize = 64;
    private const int ModelScale = 4;
    private const int InputSizeMultiple = 4;

    public static VsrUpscaler Load(string modelDirectory, string scratchDirectory, torch.Device device)
    {
        using VsrUpscaleWeights weights = VsrUpscaleWeights.Load(modelDirectory);

        return new VsrUpscaler(weights.BuildNetwork(scratchDirectory), device);
    }

    public void Dispose()
    {
        network.Dispose();
    }

    public IEnumerable<UmbraUpscaledFrame> Run(IReadOnlyList<UmbraSequenceGroup> groups)
    {
        HashSet<UmbraSpriteHash> frameHashes = [];

        List<UmbraSequence> sequences = groups.CollectSequences();

        foreach (UpscaleClip clip in UpscaleClipBuilder.Build(sequences))
        {
            foreach (UmbraUpscaledFrame frame in RunClip(clip))
            {
                if (frameHashes.Add(frame.Hash))
                {
                    yield return frame;
                }
            }
        }
    }

    private List<UmbraUpscaledFrame> RunClip(UpscaleClip clip)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor canvas = UpscaleCanvas.Build(clip, device);
        int windowFrames = Math.Min(WindowFrames, clip.Frames.Count);

        List<UmbraUpscaledFrame> results = [];

        for (int start = 0; start + windowFrames <= clip.Frames.Count; start++)
        {
            List<UmbraUpscaledFrame> frames = RunWindow(clip, canvas, start, windowFrames);

            results.AddRange(frames);
        }

        return results;
    }

    private List<UmbraUpscaledFrame> RunWindow(UpscaleClip clip, torch.Tensor canvas, int start, int count)
    {
        using IDisposable scope = torch.NewDisposeScope();

        torch.Tensor colors = canvas.narrow(0, start, count).narrow(1, 0, 3);

        List<UmbraUpscaledFrame> results = [];
        int lastStart = clip.Frames.Count - count;

        UpscaleFrames(colors, (offset, frameColors) =>
        {
            int position = start + offset;
            int centeredStart = Math.Clamp(position - (count / 2), 0, lastStart);

            if (start == centeredStart)
            {
                UpscaleClipFrame clipFrame = clip.Frames[position];
                UmbraSprite frame = CutFrame(frameColors, canvas, clipFrame, position);

                results.Add(new UmbraUpscaledFrame(clipFrame.Frame.Hash, frame));
            }
        });

        return results;
    }

    private static UmbraSprite CutFrame(
        torch.Tensor frameColors,
        torch.Tensor canvas,
        UpscaleClipFrame clipFrame,
        int position)
    {
        using IDisposable scope = torch.NewDisposeScope();

        UmbraSpriteMetadata oldMetadata = clipFrame.Frame.Sprite.Metadata;

        torch.Tensor frameAlpha = ResolveAlpha(canvas, clipFrame, position);
        torch.Tensor cutColors = Cut(frameColors, clipFrame);
        torch.Tensor cutAlpha = Cut(frameAlpha, clipFrame);
        torch.Tensor planes = torch.cat([cutColors, cutAlpha], dim: 0);
        torch.Tensor downsampledPlanes = functional.avg_pool2d(planes.unsqueeze(0), DownsampleFactor).squeeze(0);

        UmbraSpriteMetadata newMetadata = oldMetadata with
        {
            PixelWidth = (int)downsampledPlanes.shape[2],
            PixelHeight = (int)downsampledPlanes.shape[1]
        };

        return new UmbraSprite(newMetadata, PixelTensor.ToPixels(downsampledPlanes));
    }

    private static torch.Tensor Cut(torch.Tensor frame, UpscaleClipFrame clipFrame)
    {
        UmbraSpriteMetadata metadata = clipFrame.Frame.Sprite.Metadata;

        torch.Tensor rows = frame.narrow(1, clipFrame.Top * ModelScale, metadata.PixelHeight * ModelScale);

        return rows.narrow(2, clipFrame.Left * ModelScale, metadata.PixelWidth * ModelScale);
    }

    private static torch.Tensor ResolveAlpha(torch.Tensor canvas, UpscaleClipFrame clipFrame, int position)
    {
        UmbraSpriteMetadata metadata = clipFrame.Frame.Sprite.Metadata;

        if (metadata.Additive)
        {
            return torch.zeros(
                [1, canvas.shape[2] * ModelScale, canvas.shape[3] * ModelScale],
                canvas.dtype,
                canvas.device);
        }
        else
        {
            torch.Tensor alpha = canvas.narrow(1, 3, 1);
            torch.Tensor frameAlpha = alpha.narrow(0, position, 1);
            torch.Tensor antialiasedAlpha = PixelOperations.Antialias(frameAlpha, ModelScale);

            return antialiasedAlpha.squeeze(0);
        }
    }

    private void UpscaleFrames(torch.Tensor frames, Action<int, torch.Tensor> frameFunc)
    {
        long height = frames.shape[2];
        long width = frames.shape[3];

        torch.Tensor paddedFrames = Pad(frames, AlignInputSize(height), AlignInputSize(width));

        network.Upsample(paddedFrames.unsqueeze(0), (index, frame) =>
        {
            torch.Tensor planes = frame
                .squeeze(0)
                .narrow(1, 0, height * ModelScale)
                .narrow(2, 0, width * ModelScale);

            frameFunc(index, planes.clamp_(0.0, 1.0));
        });
    }

    private static long AlignInputSize(long size)
    {
        long aligned = (size + InputSizeMultiple - 1) / InputSizeMultiple * InputSizeMultiple;

        return Math.Max(MinimumInputSize, aligned);
    }

    private static torch.Tensor Pad(torch.Tensor frames, long targetHeight, long targetWidth)
    {
        long height = frames.shape[2];
        long width = frames.shape[3];

        if (targetHeight == height && targetWidth == width)
        {
            return frames.alias();
        }
        else
        {
            return functional.pad(frames, [0, targetWidth - width, 0, targetHeight - height], PaddingModes.Replicate);
        }
    }
}
