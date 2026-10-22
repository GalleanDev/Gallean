using TorchSharp;

namespace Umbra.Tests;

public sealed class LtxLayersTests
{
    [Test]
    public void AdaLayerNormTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-layers");

        using LtxAdaLayerNorm norm = new(16, LtxDitOptions.ModulationCount);

        ModuleParameters.Load(norm, TestParity.LoadWeights("ltx-layers", "adaln.weights."));

        (torch.Tensor modulation, torch.Tensor embedded) = norm.Forward(parity["adaln.input"], torch.ScalarType.Float32);

        using torch.Tensor ownedModulation = modulation;
        using torch.Tensor ownedEmbedded = embedded;

        Assert.That(TestHelpers.MaxDifference(ownedModulation, parity["adaln.expected"]), Is.LessThan(1e-4f));
        Assert.That(TestHelpers.MaxDifference(ownedEmbedded, parity["adaln.expectedEmbedded"]), Is.LessThan(1e-4f));
    }

    [Test]
    public void RmsNormTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-layers");

        using LtxRmsNorm norm = new(16);

        ModuleParameters.Load(norm, TestParity.LoadWeights("ltx-layers", "norm.weights."));

        using torch.Tensor result = norm.forward(parity["norm.input"]);

        Assert.That(TestHelpers.MaxDifference(result, parity["norm.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void RmsNormChunkTest()
    {
        using torch.Tensor input = torch.ones(1, 20000, 4, torch.ScalarType.Float32);
        using torch.Tensor column = input.select(2, 0);
        using torch.Tensor values = torch.arange(1, 20001, torch.ScalarType.Float32).view(1, 20000);

        column.copy_(values);

        using torch.Tensor normalized = LtxRmsNorm.Normalize(input, LtxRmsNorm.Epsilon);

        List<string> rows = [];

        foreach (long token in (long[])[0, 8191, 8192, 8193, 16383, 16384, 19999])
        {
            using torch.Tensor row = normalized.select(0, 0).select(0, token);

            rows.Add($"{token}: {row[0].item<float>():F8} {row[1].item<float>():F8} {row[2].item<float>():F8} {row[3].item<float>():F8}");
        }

        Assert.That(normalized.shape, Is.EqualTo(new long[] { 1, 20000, 4 }));
        Assert.That(rows, Is.EqualTo([
            "0: 0.99999952 0.99999952 0.99999952 0.99999952",
            "8191: 2.00000000 0.00024414 0.00024414 0.00024414",
            "8192: 2.00000000 0.00024411 0.00024411 0.00024411",
            "8193: 2.00000000 0.00024408 0.00024408 0.00024408",
            "16383: 2.00000000 0.00012207 0.00012207 0.00012207",
            "16384: 2.00000000 0.00012206 0.00012206 0.00012206",
            "19999: 2.00000000 0.00010000 0.00010000 0.00010000"
        ]));
    }

    [Test]
    public void RopeFrequenciesTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-layers");

        (torch.Tensor cos, torch.Tensor sin) = LtxRope.GetFrequencies(
            parity["rope.positions"], 16, 2, 10000.0, [20, 2048, 2048], torch.ScalarType.Float32);

        using torch.Tensor ownedCos = cos;
        using torch.Tensor ownedSin = sin;

        Assert.That(ownedCos.shape, Is.EqualTo(parity["rope.cos"].shape));
        Assert.That(TestHelpers.MaxDifference(ownedCos, parity["rope.cos"]), Is.LessThan(1e-5f));
        Assert.That(TestHelpers.MaxDifference(ownedSin, parity["rope.sin"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void RopeApplyTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-layers");

        using torch.Tensor result = LtxRope.Apply(parity["rope.input"], parity["rope.cos"], parity["rope.sin"], 2);

        Assert.That(TestHelpers.MaxDifference(result, parity["rope.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void SelfAttentionTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-layers");

        using LtxAttention attention = new(16, 16, 2, 8, 3);

        ModuleParameters.Load(attention, TestParity.LoadWeights("ltx-layers", "selfAttention.weights."));

        torch.Tensor input = parity["selfAttention.input"];

        using torch.Tensor result = attention.Forward(input, input, parity["rope.cos"], parity["rope.sin"]);

        Assert.That(TestHelpers.MaxDifference(result, parity["selfAttention.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void CrossAttentionTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-layers");

        using LtxAttention attention = new(16, 16, 2, 8, 3);

        ModuleParameters.Load(attention, TestParity.LoadWeights("ltx-layers", "crossAttention.weights."));

        using torch.Tensor result = attention.Forward(
            parity["crossAttention.input"], parity["crossAttention.context"], null, null);

        Assert.That(TestHelpers.MaxDifference(result, parity["crossAttention.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void FeedForwardTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-layers");

        using LtxFeedForward feedForward = new(16, 4, hasBias: false);

        ModuleParameters.Load(feedForward, TestParity.LoadWeights("ltx-layers", "feedForward.weights."));

        using torch.Tensor result = feedForward.forward(parity["feedForward.input"]);

        Assert.That(TestHelpers.MaxDifference(result, parity["feedForward.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void BlockTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-layers");

        using LtxTransformerBlock block = new(16, 2, 8, 16, 5, 3);

        ModuleParameters.Load(block, TestParity.LoadWeights("ltx-layers", "block.weights."));

        torch.Tensor timesteps = parity["block.timesteps"];
        long batch = timesteps.shape[0];
        long tokens = timesteps.shape[1];

        using torch.Tensor values = timesteps.reshape(batch * tokens, -1);
        using torch.Tensor index = torch.arange(batch * tokens, torch.ScalarType.Int64).view(batch, tokens);
        using torch.Tensor prompt = parity["block.prompt"].view(batch, 1, LtxDitOptions.PromptModulationCount, 16);
        using torch.Tensor result = block.Forward(
            parity["block.input"],
            parity["block.context"],
            values,
            new LtxTimestepSelection(values, index),
            prompt,
            parity["rope.cos"],
            parity["rope.sin"]);

        Assert.That(TestHelpers.MaxDifference(result, parity["block.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void TimestepSelectionTest()
    {
        using torch.Tensor timesteps = torch.tensor([0.5f, 0.0f, 0.5f, 0.5f, 0.0f, 0.0f]).view(2, 3);

        LtxTimestepSelection selection = LtxTimestepSelection.FromTokens(timesteps);

        using torch.Tensor values = selection.Values;
        using torch.Tensor index = selection.Index;
        using torch.Tensor table = torch.tensor([10.0f, 11.0f, 20.0f, 21.0f]).view(2, 2);
        using torch.Tensor gathered = selection.Gather(table);

        Assert.That(values.data<float>().ToArray(), Is.EqualTo([0.0f, 0.5f]));
        Assert.That(index.data<long>().ToArray(), Is.EqualTo(new long[] { 1, 0, 1, 1, 0, 0 }));
        Assert.That(gathered.shape, Is.EqualTo(new long[] { 2, 3, 2 }));
        Assert.That(
            gathered.data<float>().ToArray(),
            Is.EqualTo([20.0f, 21.0f, 10.0f, 11.0f, 20.0f, 21.0f, 20.0f, 21.0f, 10.0f, 11.0f, 10.0f, 11.0f]));
    }

    [Test]
    public void RopeChunkedTableTest()
    {
        torch.manual_seed(5);

        using torch.Tensor positions = torch.rand([1, 3, 8200, 2], torch.ScalarType.Float32).mul(40.0);
        using torch.Tensor head = positions.narrow(2, 0, 8192);
        using torch.Tensor tail = positions.narrow(2, 8192, 8);

        (torch.Tensor cos, torch.Tensor sin) = LtxRope.GetFrequencies(positions, 128, 2, 10000.0, [20, 2048, 2048], torch.ScalarType.Float32);
        (torch.Tensor headCos, torch.Tensor headSin) = LtxRope.GetFrequencies(head, 128, 2, 10000.0, [20, 2048, 2048], torch.ScalarType.Float32);
        (torch.Tensor tailCos, torch.Tensor tailSin) = LtxRope.GetFrequencies(tail, 128, 2, 10000.0, [20, 2048, 2048], torch.ScalarType.Float32);

        using torch.Tensor ownedCos = cos;
        using torch.Tensor ownedSin = sin;
        using torch.Tensor expectedCos = torch.cat([headCos, tailCos], dim: 2);
        using torch.Tensor expectedSin = torch.cat([headSin, tailSin], dim: 2);

        Assert.That(cos.shape, Is.EqualTo(new long[] { 1, 2, 8200, 32 }));
        Assert.That(cos.eq(expectedCos).all().item<bool>(), Is.True);
        Assert.That(sin.eq(expectedSin).all().item<bool>(), Is.True);
    }
}
