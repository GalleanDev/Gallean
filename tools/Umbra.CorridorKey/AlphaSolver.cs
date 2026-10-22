using TorchSharp;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class AlphaSolver(IReadOnlyDictionary<ChromaKey, CorridorKeyFormer> keyFormers, AlphaOptions options) : IDisposable
{
    public static AlphaSolver Load(string directory, torch.Device device, torch.ScalarType dtype)
    {
        AlphaOptions options = new(device, dtype);
        CorridorKeyOptions modelOptions = CorridorKeyOptions.BasePlus(options.ImageSize);
        Dictionary<ChromaKey, CorridorKeyFormer> keyFormers = new()
        {
            [ChromaKey.Blue] = LoadKeyFormer(Path.Combine(directory, AlphaWeights.BlueFileName), modelOptions, device, dtype),
            [ChromaKey.Green] = LoadKeyFormer(Path.Combine(directory, AlphaWeights.GreenFileName), modelOptions, device, dtype)
        };

        return new AlphaSolver(keyFormers, options);
    }

    public IEnumerable<UmbraUpscaledFrame> Run(IEnumerable<UmbraSequenceGroup> groups, IUmbraDataProvider dataProvider)
    {
        HashSet<UmbraSpriteHash> frameHashes = [];

        foreach (UmbraSequenceGroup group in groups)
        {
            List<UmbraFrame> frames = group.CollectFrames();

            if (frames.Count > 0)
            {
                ChromaKey key = ChromaKey.Select(dataProvider.GetUpscaledSprite(frames[0]));

                foreach (UmbraFrame frame in frames)
                {
                    if (frameHashes.Add(frame.Hash))
                    {
                        UmbraSprite sprite = ProcessSprite(frame.Sprite, key);

                        yield return new UmbraUpscaledFrame(frame.Hash, sprite);
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        foreach (CorridorKeyFormer keyFormer in keyFormers.Values)
        {
            keyFormer.Dispose();
        }
    }

    private static CorridorKeyFormer LoadKeyFormer(
        string path,
        CorridorKeyOptions modelOptions,
        torch.Device device,
        torch.ScalarType dtype)
    {
        CorridorKeyFormer keyFormer = MetaDevice.Construct(() => new CorridorKeyFormer(modelOptions));
        ModuleParameters.Load(keyFormer, AlphaWeights.Map(path, modelOptions, dtype), device);
        keyFormer.eval();

        return keyFormer;
    }

    private UmbraSprite ProcessSprite(UmbraSprite sprite, ChromaKey key)
    {
        UmbraSpriteMetadata metadata = sprite.Metadata;

        if (metadata.Additive || IsOpaque(sprite.Pixels.Span))
        {
            return sprite;
        }
        else
        {
            using IDisposable scope = torch.NewDisposeScope();

            using torch.Tensor planes = PixelTensor.ToPlanes(sprite.Pixels, metadata.PixelWidth, metadata.PixelHeight, torch.CPU);
            using torch.Tensor colors = planes.narrow(0, 0, 3);
            using torch.Tensor hint = planes.narrow(0, 3, 1);
            using torch.Tensor solvedPlanes = Solve(colors, hint, key);

            return new UmbraSprite(metadata, PixelTensor.ToPixels(solvedPlanes));
        }
    }

    private static bool IsOpaque(ReadOnlySpan<byte> pixels)
    {
        for (int offset = 3; offset < pixels.Length; offset += 4)
        {
            if (pixels[offset] != byte.MaxValue)
            {
                return false;
            }
        }

        return true;
    }

    private torch.Tensor Solve(torch.Tensor colors, torch.Tensor hint, ChromaKey key)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long height = colors.shape[1];
        long width = colors.shape[2];

        using torch.Tensor batchedColors = colors.unsqueeze(0);
        using torch.Tensor batchedHint = hint.unsqueeze(0);
        using torch.Tensor coarseHint = Coarsen(batchedHint);
        using torch.Tensor input = AlphaImage.Prepare(batchedColors, coarseHint, options.ImageSize);
        using torch.Tensor modelInput = input.to(options.ModelDtype, options.Device);
        using CorridorKeyPrediction prediction = keyFormers[key].Forward(modelInput);
        using torch.Tensor floatAlpha = prediction.Alpha.to(torch.ScalarType.Float32).cpu();
        using torch.Tensor restoredAlpha = PixelOperations.Resize(floatAlpha, height, width);
        using torch.Tensor boundedAlpha = Bound(restoredAlpha, batchedHint);
        using torch.Tensor alpha = boundedAlpha.squeeze(0).clamp(0.0, 1.0);
        using torch.Tensor floatForeground = prediction.Foreground.to(torch.ScalarType.Float32).cpu();
        using torch.Tensor restoredForeground = PixelOperations.Resize(floatForeground, height, width);
        using torch.Tensor foreground = restoredForeground.squeeze(0).clamp(0.0, 1.0);
        using torch.Tensor recoveredColors = Recover(colors, foreground, alpha, key);

        return torch.cat([recoveredColors, alpha], dim: 0).MoveToOuterDisposeScope();
    }

    private torch.Tensor Recover(
        torch.Tensor colors,
        torch.Tensor foreground,
        torch.Tensor alpha,
        ChromaKey key)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor unmixedColors = Unmix(colors, alpha, key);
        using torch.Tensor opaqueMask = alpha.ge(options.EdgeAlpha).to(colors.dtype);
        using torch.Tensor opaqueColors = unmixedColors.mul(opaqueMask);
        using torch.Tensor edgeMask = opaqueMask.neg().add(1.0);
        using torch.Tensor edgeColors = foreground.mul(edgeMask);

        return opaqueColors.add(edgeColors).MoveToOuterDisposeScope();
    }

    private torch.Tensor Unmix(torch.Tensor colors, torch.Tensor alpha, ChromaKey key)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor keyPlanes = key.ToPlanes(colors.device);
        using torch.Tensor transparency = alpha.neg().add(1.0);
        using torch.Tensor spill = keyPlanes.mul(transparency);
        using torch.Tensor foreground = colors.sub(spill);
        using torch.Tensor divisor = alpha.clamp_min(options.AlphaFloor);

        return foreground.div(divisor).clamp(0.0, 1.0).MoveToOuterDisposeScope();
    }

    private torch.Tensor Coarsen(torch.Tensor hint)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor erodedHint = Erode(hint, options.HintErode);
        torch.Tensor result = Blur(erodedHint, options.HintBlur);

        return result.MoveToOuterDisposeScope();
    }

    private torch.Tensor Bound(torch.Tensor alpha, torch.Tensor hint)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor support = hint.gt(0.0).to(alpha.dtype);
        using torch.Tensor reach = Dilate(support, options.HintBound);

        return alpha.mul(reach).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Erode(torch.Tensor planes, int radius)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (radius > 0)
        {
            using torch.Tensor inverse = planes.neg().add(1.0);
            using torch.Tensor dilatedInverse = Dilate(inverse, radius);

            return dilatedInverse.neg().add(1.0).MoveToOuterDisposeScope();
        }
        else
        {
            return planes.alias().MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor Dilate(torch.Tensor planes, int radius)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (radius > 0)
        {
            return functional.max_pool2d(planes, (2L * radius) + 1, 1, radius).MoveToOuterDisposeScope();
        }
        else
        {
            return planes.alias().MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor Blur(torch.Tensor planes, int radius)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (radius > 0)
        {
            using torch.Tensor paddedPlanes = functional.pad(planes, [radius, radius, radius, radius], PaddingModes.Replicate);
            using torch.Tensor averagedPlanes = functional.avg_pool2d(paddedPlanes, radius + 1, 1);

            return functional.avg_pool2d(averagedPlanes, radius + 1, 1).MoveToOuterDisposeScope();
        }
        else
        {
            return planes.alias().MoveToOuterDisposeScope();
        }
    }
}
