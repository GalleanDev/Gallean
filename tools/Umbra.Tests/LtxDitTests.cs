using TorchSharp;

namespace Umbra.Tests;

public sealed class LtxDitTests
{
    private static List<string> FormatShapes(torch.nn.Module module, Func<string, bool> selector)
    {
        List<string> rows = [];

        foreach ((string name, torch.Tensor tensor) in module.state_dict())
        {
            if (selector(name))
            {
                rows.Add($"{name} {string.Join("x", tensor.shape)}");
            }
        }

        rows.Sort(StringComparer.Ordinal);

        return rows;
    }

    [Test]
    public void ForwardTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-dit");

        using LtxDit transformer = MetaDevice.Construct(() => new LtxDit(new LtxDitOptions(
            HiddenSize: 16,
            Heads: 2,
            Depth: 2,
            InputChannels: 6,
            OutputChannels: 6,
            ContextChannels: 16,
            RopeTheta: 10000.0,
            RopeMaxPositions: [20, 2048, 2048],
            TimestepScale: 1000.0)
        {
            MlpChunkTokens = 7
        }));

        ModuleParameters.Load(transformer, TestParity.LoadWeights("ltx-dit", "dit.weights."));

        LtxTimestepSelection selection = LtxTimestepSelection.FromTokens(parity["dit.timesteps"]);

        using torch.Tensor values = selection.Values;
        using torch.Tensor index = selection.Index;
        using torch.Tensor result = transformer.Forward(
            parity["dit.latent"],
            selection,
            parity["dit.sigma"],
            parity["dit.positions"],
            parity["dit.context"],
            parity["dit.keyframes"]);

        Assert.That(result.shape, Is.EqualTo(parity["dit.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, parity["dit.expected"]), Is.LessThan(1e-4f));
    }

    [Test]
    public void StreamedLoraTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-dit");
        LtxDitOptions options = new(
            HiddenSize: 16,
            Heads: 2,
            Depth: 2,
            InputChannels: 6,
            OutputChannels: 6,
            ContextChannels: 16,
            RopeTheta: 10000.0,
            RopeMaxPositions: [20, 2048, 2048],
            TimestepScale: 1000.0)
        {
            MlpChunkTokens = 7
        };

        torch.manual_seed(11);

        using torch.Tensor down = torch.randn(4, 16);
        using torch.Tensor up = torch.randn(16, 4);

        using LtxDit fused = MetaDevice.Construct(() => new LtxDit(options));
        Dictionary<string, torch.Tensor> fusedState = TestParity.LoadWeights("ltx-dit", "dit.weights.");
        string target = "transformer_blocks.1.attn1.to_q.weight";

        fusedState[target] = LtxLora.Fuse(fusedState[target], down, up, 0.5);

        ModuleParameters.Load(fused, fusedState);

        using LtxDit streamed = MetaDevice.Construct(() => new LtxDit(options));

        ModuleParameters.Load(streamed, TestParity.LoadWeights("ltx-dit", "dit.weights."));

        streamed.StreamBlocks(
            new LtxLora(
                new Dictionary<string, LtxLoraEntry>
                {
                    [target] = new(down.alias(), up.alias())
                },
                0.5),
            1,
            torch.ScalarType.Float32,
            torch.CPU);

        LtxTimestepSelection selection = LtxTimestepSelection.FromTokens(parity["dit.timesteps"]);

        using torch.Tensor values = selection.Values;
        using torch.Tensor index = selection.Index;
        using torch.Tensor expected = fused.Forward(
            parity["dit.latent"],
            selection,
            parity["dit.sigma"],
            parity["dit.positions"],
            parity["dit.context"],
            parity["dit.keyframes"]);
        using torch.Tensor result = streamed.Forward(
            parity["dit.latent"],
            selection,
            parity["dit.sigma"],
            parity["dit.positions"],
            parity["dit.context"],
            parity["dit.keyframes"]);

        Assert.That(TestHelpers.MaxDifference(result, expected), Is.LessThan(1e-5f));
    }

    [Test]
    public void StreamedBlocksTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-dit");

        using LtxDit transformer = MetaDevice.Construct(() => new LtxDit(new LtxDitOptions(
            HiddenSize: 16,
            Heads: 2,
            Depth: 2,
            InputChannels: 6,
            OutputChannels: 6,
            ContextChannels: 16,
            RopeTheta: 10000.0,
            RopeMaxPositions: [20, 2048, 2048],
            TimestepScale: 1000.0)
        {
            MlpChunkTokens = 7
        }));

        ModuleParameters.Load(transformer, TestParity.LoadWeights("ltx-dit", "dit.weights."));

        transformer.MoveResidentTo(torch.ScalarType.Float32, torch.CPU);
        transformer.StreamBlocks(null, 1, torch.ScalarType.Float32, torch.CPU);

        LtxTimestepSelection selection = LtxTimestepSelection.FromTokens(parity["dit.timesteps"]);

        using torch.Tensor values = selection.Values;
        using torch.Tensor index = selection.Index;
        using torch.Tensor result = transformer.Forward(
            parity["dit.latent"],
            selection,
            parity["dit.sigma"],
            parity["dit.positions"],
            parity["dit.context"],
            parity["dit.keyframes"]);

        Assert.That(TestHelpers.MaxDifference(result, parity["dit.expected"]), Is.LessThan(1e-4f));
    }

    [Test]
    public void StateDictTest()
    {
        using LtxDit transformer = MetaDevice.Construct(() => new LtxDit(LtxDitOptions.Distilled));

        Assert.That(
            FormatShapes(transformer, name => !name.StartsWith("transformer_blocks.", StringComparison.Ordinal) ||
                                              name.StartsWith("transformer_blocks.0.", StringComparison.Ordinal)),
            Is.EqualTo([
                "adaln_single.emb.timestep_embedder.linear_1.bias 4096",
                "adaln_single.emb.timestep_embedder.linear_1.weight 4096x256",
                "adaln_single.emb.timestep_embedder.linear_2.bias 4096",
                "adaln_single.emb.timestep_embedder.linear_2.weight 4096x4096",
                "adaln_single.linear.bias 36864",
                "adaln_single.linear.weight 36864x4096",
                "keyframes_abs_pos_embedding 1x4096",
                "patchify_proj.bias 4096",
                "patchify_proj.weight 4096x128",
                "proj_out.bias 128",
                "proj_out.weight 128x4096",
                "prompt_adaln_single.emb.timestep_embedder.linear_1.bias 4096",
                "prompt_adaln_single.emb.timestep_embedder.linear_1.weight 4096x256",
                "prompt_adaln_single.emb.timestep_embedder.linear_2.bias 4096",
                "prompt_adaln_single.emb.timestep_embedder.linear_2.weight 4096x4096",
                "prompt_adaln_single.linear.bias 8192",
                "prompt_adaln_single.linear.weight 8192x4096",
                "scale_shift_table 2x4096",
                "transformer_blocks.0.attn1.k_norm.weight 4096",
                "transformer_blocks.0.attn1.q_norm.weight 4096",
                "transformer_blocks.0.attn1.to_gate_logits.bias 32",
                "transformer_blocks.0.attn1.to_gate_logits.weight 32x4096",
                "transformer_blocks.0.attn1.to_k.bias 4096",
                "transformer_blocks.0.attn1.to_k.weight 4096x4096",
                "transformer_blocks.0.attn1.to_out.0.bias 4096",
                "transformer_blocks.0.attn1.to_out.0.weight 4096x4096",
                "transformer_blocks.0.attn1.to_q.bias 4096",
                "transformer_blocks.0.attn1.to_q.weight 4096x4096",
                "transformer_blocks.0.attn1.to_v.bias 4096",
                "transformer_blocks.0.attn1.to_v.weight 4096x4096",
                "transformer_blocks.0.attn2.k_norm.weight 4096",
                "transformer_blocks.0.attn2.q_norm.weight 4096",
                "transformer_blocks.0.attn2.to_gate_logits.bias 32",
                "transformer_blocks.0.attn2.to_gate_logits.weight 32x4096",
                "transformer_blocks.0.attn2.to_k.bias 4096",
                "transformer_blocks.0.attn2.to_k.weight 4096x4096",
                "transformer_blocks.0.attn2.to_out.0.bias 4096",
                "transformer_blocks.0.attn2.to_out.0.weight 4096x4096",
                "transformer_blocks.0.attn2.to_q.bias 4096",
                "transformer_blocks.0.attn2.to_q.weight 4096x4096",
                "transformer_blocks.0.attn2.to_v.bias 4096",
                "transformer_blocks.0.attn2.to_v.weight 4096x4096",
                "transformer_blocks.0.ff.net.0.proj.weight 16384x4096",
                "transformer_blocks.0.ff.net.2.weight 4096x16384",
                "transformer_blocks.0.prompt_scale_shift_table 2x4096",
                "transformer_blocks.0.scale_shift_table 9x4096"
            ]));
    }
}
