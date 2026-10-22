using TorchSharp;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyFormer : Module
{
    [ComponentName(Name = "encoder")]
    private readonly CorridorKeyHiera encoder;

    [ComponentName(Name = "alpha_decoder")]
    private readonly CorridorKeyDecoderHead alphaDecoder;

    [ComponentName(Name = "fg_decoder")]
    private readonly CorridorKeyDecoderHead foregroundDecoder;

    [ComponentName(Name = "refiner")]
    private readonly CorridorKeyCnnRefiner refiner;

    public CorridorKeyFormer(CorridorKeyOptions options)
        : base(nameof(CorridorKeyFormer))
    {
        IReadOnlyList<int> featureChannels = options.FeatureChannels;
        int refinerInputs = CorridorKeyOptions.ForegroundChannels +
                            CorridorKeyOptions.AlphaChannels +
                            CorridorKeyOptions.ForegroundChannels;

        encoder = new CorridorKeyHiera(options);
        alphaDecoder = new CorridorKeyDecoderHead(
            featureChannels, options.DecoderDimension, CorridorKeyOptions.AlphaChannels);
        foregroundDecoder = new CorridorKeyDecoderHead(
            featureChannels, options.DecoderDimension, CorridorKeyOptions.ForegroundChannels);
        refiner = new CorridorKeyCnnRefiner(
            refinerInputs,
            options.RefinerChannels,
            CorridorKeyOptions.AlphaChannels + CorridorKeyOptions.ForegroundChannels);

        RegisterComponents();
    }

    public CorridorKeyPrediction Forward(torch.Tensor input)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long[] size = [input.shape[2], input.shape[3]];

        IReadOnlyList<torch.Tensor> features = encoder.Forward(input);

        using torch.Tensor alphaLogits = alphaDecoder.Forward(features);
        using torch.Tensor foregroundLogits = foregroundDecoder.Forward(features);

        foreach (torch.Tensor feature in features)
        {
            feature.Dispose();
        }

        using torch.Tensor alphaFull = Resize(alphaLogits, size);
        using torch.Tensor foregroundFull = Resize(foregroundLogits, size);
        using torch.Tensor alphaCoarse = torch.sigmoid(alphaFull);
        using torch.Tensor foregroundCoarse = torch.sigmoid(foregroundFull);
        using torch.Tensor coarse = torch.cat([alphaCoarse, foregroundCoarse], dim: 1);
        using torch.Tensor colors = input.narrow(1, 0, CorridorKeyOptions.ForegroundChannels);
        using torch.Tensor delta = refiner.call(colors, coarse);
        using torch.Tensor alphaDelta = delta.narrow(1, 0, CorridorKeyOptions.AlphaChannels);
        using torch.Tensor foregroundDelta = delta.narrow(
            1, CorridorKeyOptions.AlphaChannels, CorridorKeyOptions.ForegroundChannels);
        using torch.Tensor alphaFinalLogits = alphaFull.add(alphaDelta);
        using torch.Tensor foregroundFinalLogits = foregroundFull.add(foregroundDelta);

        torch.Tensor alpha = torch.sigmoid(alphaFinalLogits);
        torch.Tensor foreground = torch.sigmoid(foregroundFinalLogits);

        return new CorridorKeyPrediction(alpha.MoveToOuterDisposeScope(), foreground.MoveToOuterDisposeScope());
    }

    private static torch.Tensor Resize(torch.Tensor planes, long[] size)
    {
        return functional.interpolate(planes, size: size, mode: torch.InterpolationMode.Bilinear, align_corners: false);
    }
}
