using TorchSharp;

namespace Umbra.Tests;

public sealed class WanDitTests
{
    [Test]
    public void ForwardShapeTest()
    {
        using WanDit dit = new(
            dim: 256,
            inChannels: 32,
            outChannels: 16,
            feedForwardDim: 512,
            textDim: 128,
            frequencyDim: 64,
            heads: 2,
            layers: 2,
            patch: (1, 2, 2));
        using torch.Tensor latents = torch.zeros([1, 32, 3, 8, 8]);
        using torch.Tensor context = torch.zeros([1, 12, 128]);
        using torch.Tensor timestep = torch.zeros(1);
        using torch.Tensor mask = torch.ones([1, 1, 3, 8, 8]);

        using TeaCache cache = new(TeaCacheOptions.None);

        using torch.Tensor result = dit.Forward(latents, context, timestep, cache, mask);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 16, 3, 8, 8 }));
    }

    [Test]
    public void SparseTokensTest()
    {
        torch.manual_seed(5);

        using WanDit dit = new(
            dim: 256,
            inChannels: 32,
            outChannels: 16,
            feedForwardDim: 512,
            textDim: 128,
            frequencyDim: 64,
            heads: 2,
            layers: 2,
            patch: (1, 2, 2));
        using torch.Tensor latents = torch.randn([1, 32, 3, 8, 8]);
        using torch.Tensor context = torch.zeros([1, 12, 128]);
        using torch.Tensor timestep = torch.zeros(1);
        using torch.Tensor mask = torch.zeros([1, 1, 3, 8, 8]);

        mask.narrow(2, 0, 1).fill_(1.0);

        using TeaCache cache = new(TeaCacheOptions.None);

        using torch.Tensor result = dit.Forward(latents, context, timestep, cache, mask);
        using torch.Tensor dropped = result.narrow(2, 1, 2);

        Assert.That(result.shape, Is.EqualTo(new long[] { 1, 16, 3, 8, 8 }));
        Assert.That(dropped.count_nonzero().item<long>(), Is.Zero);
    }

    [Test]
    public void StateDictTest()
    {
        using WanDit dit = new(
            dim: 256,
            inChannels: 32,
            outChannels: 16,
            feedForwardDim: 512,
            textDim: 128,
            frequencyDim: 64,
            heads: 2,
            layers: 2,
            patch: (1, 2, 2));
        Dictionary<string, torch.Tensor> state = dit.state_dict();

        Assert.That(state["patch_embedding.weight"].shape, Is.EqualTo(new long[] { 256, 32, 1, 2, 2 }));
        Assert.That(state["text_embedding.0.weight"].shape, Is.EqualTo(new long[] { 256, 128 }));
        Assert.That(state["time_embedding.0.weight"].shape, Is.EqualTo(new long[] { 256, 64 }));
        Assert.That(state["time_projection.1.weight"].shape, Is.EqualTo(new long[] { 1536, 256 }));
        Assert.That(state["blocks.0.self_attn.q.weight"].shape, Is.EqualTo(new long[] { 256, 256 }));
        Assert.That(state["blocks.1.modulation"].shape, Is.EqualTo(new long[] { 1, 6, 256 }));
        Assert.That(state["head.head.weight"].shape, Is.EqualTo(new long[] { 64, 256 }));
    }

    [Test]
    public void TimestepEmbeddingTest()
    {
        using torch.Tensor position = torch.zeros(1);

        using torch.Tensor embedding = WanTimestepEmbedding.Build(8, position);

        Assert.That(embedding.shape, Is.EqualTo(new long[] { 1, 8 }));
        Assert.That(embedding.reshape(-1).data<float>().ToArray(), Is.EqualTo([1.0f, 1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f]));
    }

    [Test]
    public void ParityTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("wan-dit");

        using WanDit dit = MetaDevice.Construct(() => new WanDit(
            dim: 64,
            inChannels: 32,
            outChannels: 16,
            feedForwardDim: 128,
            textDim: 48,
            frequencyDim: 32,
            heads: 2,
            layers: 2,
            patch: (1, 2, 2)));

        ModuleParameters.Load(dit, TestParity.LoadWeights("wan-dit", "dit."));
        dit.eval();

        using IDisposable scope = torch.no_grad();
        using torch.Tensor input = torch.cat([parity["latents"], parity["conditioning"]], dim: 1);
        using TeaCache cache = new(TeaCacheOptions.None);
        using torch.Tensor mask = torch.ones([1, 1, 2, 4, 4]);
        using torch.Tensor noise = dit.Forward(input, parity["context"], parity["timestep"], cache, mask);

        Assert.That(noise.shape, Is.EqualTo(new long[] { 1, 16, 2, 4, 4 }));
        Assert.That(TestHelpers.MaxDifference(noise, parity["noise"]), Is.LessThan(1e-4f));
    }
}
