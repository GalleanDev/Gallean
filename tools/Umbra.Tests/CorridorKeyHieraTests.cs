using TorchSharp;

namespace Umbra.Tests;

public sealed class CorridorKeyHieraTests
{
    private const int Image = 32;
    private const int Grid = 16;
    private const int Embed = 8;
    private const int Stride = 2;

    [Test]
    public void UnrollTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using torch.Tensor result = CorridorKeyUnroll.Forward(dump["unroll.input"], Grid, Stride, steps: 3);

        Assert.That(result.shape, Is.EqualTo(dump["unroll.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["unroll.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void RerollTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        for (int stage = 0; stage < 4; stage++)
        {
            using torch.Tensor result = CorridorKeyReroll.Forward(
                dump[$"reroll{stage}.input"], Grid >> stage, Stride, steps: 3 - stage);

            Assert.That(result.shape, Is.EqualTo(dump[$"reroll{stage}.expected"].shape));
            Assert.That(TestHelpers.MaxDifference(result, dump[$"reroll{stage}.expected"]), Is.LessThan(1e-5f));
        }
    }

    [Test]
    public void StemTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyPatchEmbed patchEmbed = new(4, Embed, kernel: 3, stride: 2, padding: 1);

        ModuleParameters.Load(patchEmbed, TestParity.LoadWeights("corridorkey-hiera", "hiera.weights.patch_embed."));

        using torch.Tensor embedded = patchEmbed.forward(dump["hiera.input"]);
        using torch.Tensor positioned = embedded.add(dump["hiera.weights.pos_embed"]);
        using torch.Tensor result = CorridorKeyUnroll.Forward(positioned, Grid, Stride, steps: 3);

        Assert.That(result.shape, Is.EqualTo(dump["stem.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["stem.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void WindowedAttentionTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyAttention attention = new(8, 8, heads: 1, queryStride: 1, windowSize: 64, maskUnitAttention: true);

        ModuleParameters.Load(attention, TestParity.LoadWeights("corridorkey-hiera", "windowed.weights."));

        using torch.Tensor result = attention.forward(dump["windowed.input"]);

        Assert.That(result.shape, Is.EqualTo(dump["windowed.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["windowed.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void PooledWindowedAttentionTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyAttention attention = new(8, 16, heads: 2, queryStride: 4, windowSize: 16, maskUnitAttention: true);

        ModuleParameters.Load(attention, TestParity.LoadWeights("corridorkey-hiera", "windowedPooled.weights."));

        using torch.Tensor result = attention.forward(dump["windowedPooled.input"]);

        Assert.That(result.shape, Is.EqualTo(dump["windowedPooled.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["windowedPooled.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void GlobalAttentionTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyAttention attention = new(32, 32, heads: 4, queryStride: 1, windowSize: 4, maskUnitAttention: false);

        ModuleParameters.Load(attention, TestParity.LoadWeights("corridorkey-hiera", "global.weights."));

        using torch.Tensor result = attention.forward(dump["global.input"]);

        Assert.That(result.shape, Is.EqualTo(dump["global.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["global.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void PooledGlobalAttentionTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyAttention attention = new(32, 64, heads: 8, queryStride: 4, windowSize: 1, maskUnitAttention: false);

        ModuleParameters.Load(attention, TestParity.LoadWeights("corridorkey-hiera", "globalPooled.weights."));

        using torch.Tensor result = attention.forward(dump["globalPooled.input"]);

        Assert.That(result.shape, Is.EqualTo(dump["globalPooled.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["globalPooled.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void PlainBlockTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyBlock block = new(8, 8, heads: 1, mlpRatio: 4.0, queryStride: 1, windowSize: 64, maskUnitAttention: true);

        ModuleParameters.Load(block, TestParity.LoadWeights("corridorkey-hiera", "plainBlock.weights."));

        using torch.Tensor result = block.forward(dump["plainBlock.input"]);

        Assert.That(result.shape, Is.EqualTo(dump["plainBlock.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["plainBlock.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void ExpandingBlockTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyBlock block = new(8, 16, heads: 2, mlpRatio: 4.0, queryStride: 4, windowSize: 16, maskUnitAttention: true);

        ModuleParameters.Load(block, TestParity.LoadWeights("corridorkey-hiera", "expandingBlock.weights."));

        using torch.Tensor result = block.forward(dump["expandingBlock.input"]);

        Assert.That(result.shape, Is.EqualTo(dump["expandingBlock.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["expandingBlock.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void GlobalBlockTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyBlock block = new(32, 64, heads: 8, mlpRatio: 4.0, queryStride: 4, windowSize: 1, maskUnitAttention: false);

        ModuleParameters.Load(block, TestParity.LoadWeights("corridorkey-hiera", "globalBlock.weights."));

        using torch.Tensor result = block.forward(dump["globalBlock.input"]);

        Assert.That(result.shape, Is.EqualTo(dump["globalBlock.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["globalBlock.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void BackboneTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-hiera");

        using CorridorKeyHiera hiera = new(new CorridorKeyOptions(
            ImageSize: Image,
            InputChannels: 4,
            EmbedDimension: Embed,
            Heads: 1,
            Stages: [2, 2, 2, 2],
            MaskUnitAttention: [true, true, false, false],
            PoolStages: 3,
            MaskUnitSize: 8,
            PatchKernel: 3,
            PatchStride: 2,
            PatchPadding: 1,
            MlpRatio: 4.0,
            DecoderDimension: 32,
            RefinerChannels: 16));

        ModuleParameters.Load(hiera, TestParity.LoadWeights("corridorkey-hiera", "hiera.weights."));

        IReadOnlyList<torch.Tensor> features = hiera.Forward(dump["hiera.input"]);

        Assert.That(features, Has.Count.EqualTo(4));

        for (int stage = 0; stage < features.Count; stage++)
        {
            using torch.Tensor feature = features[stage];

            Assert.That(feature.shape, Is.EqualTo(dump[$"hiera.expected{stage}"].shape));
            Assert.That(TestHelpers.MaxDifference(feature, dump[$"hiera.expected{stage}"]), Is.LessThan(1e-5f));
        }
    }
}
