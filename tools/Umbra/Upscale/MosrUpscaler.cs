using TorchSharp;

namespace Umbra;

internal sealed class MosrUpscaler(MosrNetwork network, int scale, torch.Device compute) : IDisposable
{
    public static MosrUpscaler Load(string modelDirectory, torch.Device compute)
    {
        using MosrUpscaleWeights weights = MosrUpscaleWeights.Load(modelDirectory);

        return new MosrUpscaler(weights.BuildNetwork(), MosrUpscaleWeights.Options.Scale, compute);
    }

    public IEnumerable<UmbraUpscaledFrame> Run(IReadOnlyList<UmbraSequence> sequences)
    {
        HashSet<UmbraSpriteHash> upscaledHashes = [];

        foreach (UmbraSequence sequence in sequences)
        {
            foreach (UmbraFrame frame in sequence.Frames)
            {
                if (upscaledHashes.Add(frame.Hash))
                {
                    yield return new UmbraUpscaledFrame(frame.Hash, RunFrame(frame.Sprite));
                }
            }
        }
    }

    public void Dispose()
    {
        network.Dispose();
    }

    private static bool IsOpaque(torch.Tensor alpha)
    {
        using torch.Tensor minimum = alpha.min();

        return minimum.item<float>() >= 1;
    }

    private torch.Tensor RestoreColors(torch.Tensor colors, bool additive)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (additive)
        {
            using torch.Tensor empty = torch.zeros_like(colors);
            using torch.Tensor restored = network.call(colors);
            using torch.Tensor response = network.call(empty);

            return restored.sub(response).clamp_min(0.0).MoveToOuterDisposeScope();
        }
        else
        {
            return network.call(colors).MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor SpreadColor(torch.Tensor planes, bool additive)
    {
        if (additive)
        {
            return planes.alias();
        }
        else
        {
            return PixelOperations.SpreadColor(planes);
        }
    }

    private torch.Tensor RestoreAlpha(torch.Tensor alpha, bool additive)
    {
        using IDisposable scope = torch.NewDisposeScope();
        long[] shape = [alpha.shape[0], 1, alpha.shape[2] * scale, alpha.shape[3] * scale];

        if (additive)
        {
            torch.Tensor empty = torch.zeros(shape, alpha.dtype, alpha.device);

            return empty.MoveToOuterDisposeScope();
        }
        else if (IsOpaque(alpha))
        {
            torch.Tensor opaque = torch.ones(shape, alpha.dtype, alpha.device);

            return opaque.MoveToOuterDisposeScope();
        }
        else
        {
            using torch.Tensor planes = alpha.expand(alpha.shape[0], 3, alpha.shape[2], alpha.shape[3]).contiguous();
            using torch.Tensor empty = torch.zeros_like(planes);
            using torch.Tensor restored = network.call(planes);
            using torch.Tensor response = network.call(empty);

            return restored.sub(response).narrow(1, 0, 1).clamp_min(0.0).MoveToOuterDisposeScope();
        }
    }

    private UmbraSprite RunFrame(UmbraSprite sprite)
    {
        using IDisposable scope = torch.NewDisposeScope();

        UmbraSpriteMetadata metadata = sprite.Metadata;
        using torch.Tensor frame = PixelTensor.ToPlanes(
            sprite.Pixels, metadata.PixelWidth, metadata.PixelHeight, compute);

        using torch.Tensor planes = frame.unsqueeze(0);
        using torch.Tensor spread = SpreadColor(planes, metadata.Additive);
        using torch.Tensor colors = spread.narrow(1, 0, 3);
        using torch.Tensor alpha = spread.narrow(1, 3, 1);
        using torch.Tensor restoredColors = RestoreColors(colors, metadata.Additive);
        using torch.Tensor restoredAlpha = RestoreAlpha(alpha, metadata.Additive);
        using torch.Tensor combined = torch.cat([restoredColors, restoredAlpha], dim: 1).clamp(0.0, 1.0);

        UmbraSpriteMetadata scaled = metadata with
        {
            PixelWidth = metadata.PixelWidth * scale,
            PixelHeight = metadata.PixelHeight * scale
        };

        return new UmbraSprite(scaled, PixelTensor.ToPixels(combined.squeeze(0)));
    }
}
