using TorchSharp;
using TorchSharp.Modules;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.CorridorKey;

internal sealed class CorridorKeyDecoderHead : Module
{
    [ComponentName(Name = "linear_c4")]
    private readonly CorridorKeyProjection fourth;

    [ComponentName(Name = "linear_c3")]
    private readonly CorridorKeyProjection third;

    [ComponentName(Name = "linear_c2")]
    private readonly CorridorKeyProjection second;

    [ComponentName(Name = "linear_c1")]
    private readonly CorridorKeyProjection first;

    [ComponentName(Name = "linear_fuse")]
    private readonly Conv2d fuse;

    [ComponentName(Name = "bn")]
    private readonly CorridorKeyBatchNorm normalization;

    [ComponentName(Name = "classifier")]
    private readonly Conv2d classifier;

    public CorridorKeyDecoderHead(IReadOnlyList<int> featureChannels, int embeddingDimension, int outputChannels)
        : base(nameof(CorridorKeyDecoderHead))
    {
        fourth = new CorridorKeyProjection(featureChannels[3], embeddingDimension);
        third = new CorridorKeyProjection(featureChannels[2], embeddingDimension);
        second = new CorridorKeyProjection(featureChannels[1], embeddingDimension);
        first = new CorridorKeyProjection(featureChannels[0], embeddingDimension);
        fuse = Conv2d(embeddingDimension * 4, embeddingDimension, kernel_size: 1, bias: false);
        normalization = new CorridorKeyBatchNorm(embeddingDimension);
        classifier = Conv2d(embeddingDimension, outputChannels, kernel_size: 1, bias: true);

        RegisterComponents();
    }

    public torch.Tensor Forward(IReadOnlyList<torch.Tensor> features)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long[] size = [features[0].shape[2], features[0].shape[3]];

        using torch.Tensor fourthEmbedding = fourth.call(features[3]);
        using torch.Tensor thirdEmbedding = third.call(features[2]);
        using torch.Tensor secondEmbedding = second.call(features[1]);
        using torch.Tensor firstEmbedding = first.call(features[0]);
        using torch.Tensor resizedFourthEmbedding = Resize(fourthEmbedding, size);
        using torch.Tensor resizedThirdEmbedding = Resize(thirdEmbedding, size);
        using torch.Tensor resizedSecondEmbedding = Resize(secondEmbedding, size);
        using torch.Tensor embeddings = torch.cat(
            [resizedFourthEmbedding, resizedThirdEmbedding, resizedSecondEmbedding, firstEmbedding], dim: 1);
        using torch.Tensor fusion = fuse.call(embeddings);
        using torch.Tensor normalizedFusion = normalization.call(fusion);
        using torch.Tensor activations = functional.relu(normalizedFusion);

        return classifier.call(activations).MoveToOuterDisposeScope();
    }

    private static torch.Tensor Resize(torch.Tensor planes, long[] size)
    {
        return functional.interpolate(planes, size: size, mode: torch.InterpolationMode.Bilinear, align_corners: false);
    }
}
