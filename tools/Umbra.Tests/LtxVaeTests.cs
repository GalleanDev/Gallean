using TorchSharp;

namespace Umbra.Tests;

public sealed class LtxVaeTests
{
    private static Dictionary<string, torch.Tensor> LoadVaeWeights()
    {
        Dictionary<string, torch.Tensor> weights = [];

        foreach ((string key, torch.Tensor tensor) in TestParity.LoadWeights("ltx-vae", "encoder.weights."))
        {
            if (key.StartsWith("per_channel_statistics.", StringComparison.Ordinal))
            {
                weights[key] = tensor;
            }
            else
            {
                weights[$"encoder.{key}"] = tensor;
            }
        }

        foreach ((string key, torch.Tensor tensor) in TestParity.LoadWeights("ltx-vae", "decoder.weights."))
        {
            if (!key.StartsWith("per_channel_statistics.", StringComparison.Ordinal))
            {
                weights[$"decoder.{key}"] = tensor;
            }
        }

        return weights;
    }

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
    public void CausalConvolutionTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxCausalConv3d convolution = new(4, 6, causal: true, LtxCausalConv3d.DefaultChunkBytes);

        ModuleParameters.Load(convolution, TestParity.LoadWeights("ltx-vae", "conv.causal.weights."));

        using torch.Tensor result = convolution.forward(parity["conv.causal.input"]);

        Assert.That(TestHelpers.MaxDifference(result, parity["conv.causal.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void SymmetricConvolutionTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxCausalConv3d convolution = new(4, 6, causal: false, LtxCausalConv3d.DefaultChunkBytes);

        ModuleParameters.Load(convolution, TestParity.LoadWeights("ltx-vae", "conv.symmetric.weights."));

        using torch.Tensor result = convolution.forward(parity["conv.symmetric.input"]);

        Assert.That(TestHelpers.MaxDifference(result, parity["conv.symmetric.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void ChunkedConvolutionTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxCausalConv3d causal = new(4, 6, causal: true, chunkBytes: 1);
        using LtxCausalConv3d symmetric = new(4, 6, causal: false, chunkBytes: 1);

        ModuleParameters.Load(causal, TestParity.LoadWeights("ltx-vae", "conv.causal.weights."));
        ModuleParameters.Load(symmetric, TestParity.LoadWeights("ltx-vae", "conv.symmetric.weights."));

        using torch.Tensor causalResult = causal.forward(parity["conv.causal.input"]);
        using torch.Tensor symmetricResult = symmetric.forward(parity["conv.symmetric.input"]);

        Assert.That(TestHelpers.MaxDifference(causalResult, parity["conv.causal.expected"]), Is.LessThan(1e-5f));
        Assert.That(TestHelpers.MaxDifference(symmetricResult, parity["conv.symmetric.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void SpaceToDepthTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxVaeSpaceToDepth block = new(4, 8, new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2), LtxCausalConv3d.DefaultChunkBytes);

        ModuleParameters.Load(block, TestParity.LoadWeights("ltx-vae", "spaceToDepth.weights."));

        using torch.Tensor result = block.forward(parity["spaceToDepth.input"]);

        Assert.That(result.shape, Is.EqualTo(parity["spaceToDepth.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, parity["spaceToDepth.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void DepthToSpaceTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxVaeDepthToSpace block = new(8, new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2), causal: false, LtxCausalConv3d.DefaultChunkBytes);

        ModuleParameters.Load(block, TestParity.LoadWeights("ltx-vae", "depthToSpace.weights."));

        using torch.Tensor result = block.forward(parity["depthToSpace.input"]);

        Assert.That(result.shape, Is.EqualTo(parity["depthToSpace.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, parity["depthToSpace.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void PatchifyRoundTripTest()
    {
        using torch.Tensor input = torch.arange(2 * 3 * 2 * 8 * 12, torch.ScalarType.Float32).view(2, 3, 2, 8, 12);
        using torch.Tensor patched = LtxVaeDepth.Patchify(input, 4);
        using torch.Tensor restored = LtxVaeDepth.Unpatchify(patched, 4);

        Assert.That(patched.shape, Is.EqualTo(new long[] { 2, 48, 2, 2, 3 }));
        Assert.That(TestHelpers.MaxDifference(restored, input), Is.Zero);
    }

    [Test]
    public void EncodeTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxVae vae = MetaDevice.Construct(() => new LtxVae(new LtxVaeOptions(
            LatentChannels: 8,
            BaseChannels: 4,
            PatchSize: 4,
            Blocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            DecoderBlocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            ChunkBytes: LtxCausalConv3d.DefaultChunkBytes)));

        ModuleParameters.Load(vae, LoadVaeWeights());

        using torch.Tensor result = vae.Encode(parity["encoder.input"]);

        Assert.That(result.shape, Is.EqualTo(parity["encoder.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, parity["encoder.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void StreamedEncodeTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxVae vae = MetaDevice.Construct(() => new LtxVae(new LtxVaeOptions(
            LatentChannels: 8,
            BaseChannels: 4,
            PatchSize: 4,
            Blocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            DecoderBlocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            ChunkBytes: LtxCausalConv3d.DefaultChunkBytes)
        {
            StreamBytes = 1
        }));

        ModuleParameters.Load(vae, LoadVaeWeights());

        using torch.Tensor result = vae.Encode(parity["encoder.input"]);

        Assert.That(result.shape, Is.EqualTo(parity["encoder.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, parity["encoder.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void StreamedDecodeTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxVae vae = MetaDevice.Construct(() => new LtxVae(new LtxVaeOptions(
            LatentChannels: 8,
            BaseChannels: 4,
            PatchSize: 4,
            Blocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            DecoderBlocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            ChunkBytes: LtxCausalConv3d.DefaultChunkBytes)
        {
            StreamBytes = 1
        }));

        ModuleParameters.Load(vae, LoadVaeWeights());

        using torch.Tensor frames = torch.arange(9L);
        using torch.Tensor result = vae.Decode(parity["decoder.input"], frames);

        Assert.That(result.shape, Is.EqualTo(parity["decoder.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, parity["decoder.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void DecodeTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxVae vae = MetaDevice.Construct(() => new LtxVae(new LtxVaeOptions(
            LatentChannels: 8,
            BaseChannels: 4,
            PatchSize: 4,
            Blocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            DecoderBlocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            ChunkBytes: LtxCausalConv3d.DefaultChunkBytes)));

        ModuleParameters.Load(vae, LoadVaeWeights());

        using torch.Tensor frames = torch.arange(9L);
        using torch.Tensor result = vae.Decode(parity["decoder.input"], frames);

        Assert.That(result.shape, Is.EqualTo(parity["decoder.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, parity["decoder.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void FactorsTest()
    {
        LtxVaeOptions options = new(
            LatentChannels: 8,
            BaseChannels: 4,
            PatchSize: 4,
            Blocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            DecoderBlocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            ChunkBytes: LtxCausalConv3d.DefaultChunkBytes);

        Assert.That(options.SpatialFactor, Is.EqualTo(32));
        Assert.That(options.TemporalFactor, Is.EqualTo(8));
        Assert.That(options.BottleneckChannels, Is.EqualTo(32));
        Assert.That(LtxVaeOptions.Distilled.SpatialFactor, Is.EqualTo(32));
        Assert.That(LtxVaeOptions.Distilled.TemporalFactor, Is.EqualTo(8));
        Assert.That(LtxVaeOptions.Distilled.BottleneckChannels, Is.EqualTo(1024));
        Assert.That(LtxVaeOptions.Distilled.DecoderBottleneckChannels, Is.EqualTo(1024));
    }

    [Test]
    public void UpsamplerTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxLatentUpsampler upsampler = new(8, 32, 1);

        ModuleParameters.Load(upsampler, TestParity.LoadWeights("ltx-vae", "upsampler.weights."));

        using torch.Tensor result = upsampler.forward(parity["upsampler.input"]);

        Assert.That(result.shape, Is.EqualTo(parity["upsampler.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, parity["upsampler.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void WrappedUpsamplerTest()
    {
        Dictionary<string, torch.Tensor> parity = TestParity.Load("ltx-vae");

        using LtxVae vae = MetaDevice.Construct(() => new LtxVae(new LtxVaeOptions(
            LatentChannels: 8,
            BaseChannels: 4,
            PatchSize: 4,
            Blocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            DecoderBlocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1)
            ],
            ChunkBytes: LtxCausalConv3d.DefaultChunkBytes)));
        using LtxLatentUpsampler upsampler = new(8, 32, 1);

        ModuleParameters.Load(vae, LoadVaeWeights());
        ModuleParameters.Load(upsampler, TestParity.LoadWeights("ltx-vae", "upsampler.weights."));

        using torch.Tensor raw = vae.Unnormalize(parity["upsampler.input"]);
        using torch.Tensor upsampled = upsampler.forward(raw);
        using torch.Tensor result = vae.Normalize(upsampled);

        Assert.That(TestHelpers.MaxDifference(result, parity["upsampler.expectedWrapped"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void StateDictTest()
    {
        using LtxVae vae = MetaDevice.Construct(() => new LtxVae(LtxVaeOptions.Distilled));

        Assert.That(
            FormatShapes(vae, name => name.EndsWith(".weight", StringComparison.Ordinal) ||
                                      name.StartsWith("per_channel_statistics.", StringComparison.Ordinal)),
            Is.EqualTo([
                "decoder.conv_in.conv.weight 1024x128x3x3x3",
                "decoder.conv_out.conv.weight 48x128x3x3x3",
                "decoder.up_blocks.0.res_blocks.0.conv1.conv.weight 1024x1024x3x3x3",
                "decoder.up_blocks.0.res_blocks.0.conv2.conv.weight 1024x1024x3x3x3",
                "decoder.up_blocks.0.res_blocks.1.conv1.conv.weight 1024x1024x3x3x3",
                "decoder.up_blocks.0.res_blocks.1.conv2.conv.weight 1024x1024x3x3x3",
                "decoder.up_blocks.1.conv.conv.weight 4096x1024x3x3x3",
                "decoder.up_blocks.2.res_blocks.0.conv1.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.2.res_blocks.0.conv2.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.2.res_blocks.1.conv1.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.2.res_blocks.1.conv2.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.3.conv.conv.weight 4096x512x3x3x3",
                "decoder.up_blocks.4.res_blocks.0.conv1.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.4.res_blocks.0.conv2.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.4.res_blocks.1.conv1.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.4.res_blocks.1.conv2.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.4.res_blocks.2.conv1.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.4.res_blocks.2.conv2.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.4.res_blocks.3.conv1.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.4.res_blocks.3.conv2.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.5.conv.conv.weight 512x512x3x3x3",
                "decoder.up_blocks.6.res_blocks.0.conv1.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.0.conv2.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.1.conv1.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.1.conv2.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.2.conv1.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.2.conv2.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.3.conv1.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.3.conv2.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.4.conv1.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.4.conv2.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.5.conv1.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.6.res_blocks.5.conv2.conv.weight 256x256x3x3x3",
                "decoder.up_blocks.7.conv.conv.weight 512x256x3x3x3",
                "decoder.up_blocks.8.res_blocks.0.conv1.conv.weight 128x128x3x3x3",
                "decoder.up_blocks.8.res_blocks.0.conv2.conv.weight 128x128x3x3x3",
                "decoder.up_blocks.8.res_blocks.1.conv1.conv.weight 128x128x3x3x3",
                "decoder.up_blocks.8.res_blocks.1.conv2.conv.weight 128x128x3x3x3",
                "decoder.up_blocks.8.res_blocks.2.conv1.conv.weight 128x128x3x3x3",
                "decoder.up_blocks.8.res_blocks.2.conv2.conv.weight 128x128x3x3x3",
                "decoder.up_blocks.8.res_blocks.3.conv1.conv.weight 128x128x3x3x3",
                "decoder.up_blocks.8.res_blocks.3.conv2.conv.weight 128x128x3x3x3",
                "encoder.conv_in.conv.weight 128x48x3x3x3",
                "encoder.conv_out.conv.weight 129x1024x3x3x3",
                "encoder.down_blocks.0.res_blocks.0.conv1.conv.weight 128x128x3x3x3",
                "encoder.down_blocks.0.res_blocks.0.conv2.conv.weight 128x128x3x3x3",
                "encoder.down_blocks.0.res_blocks.1.conv1.conv.weight 128x128x3x3x3",
                "encoder.down_blocks.0.res_blocks.1.conv2.conv.weight 128x128x3x3x3",
                "encoder.down_blocks.0.res_blocks.2.conv1.conv.weight 128x128x3x3x3",
                "encoder.down_blocks.0.res_blocks.2.conv2.conv.weight 128x128x3x3x3",
                "encoder.down_blocks.0.res_blocks.3.conv1.conv.weight 128x128x3x3x3",
                "encoder.down_blocks.0.res_blocks.3.conv2.conv.weight 128x128x3x3x3",
                "encoder.down_blocks.1.conv.conv.weight 64x128x3x3x3",
                "encoder.down_blocks.2.res_blocks.0.conv1.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.0.conv2.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.1.conv1.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.1.conv2.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.2.conv1.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.2.conv2.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.3.conv1.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.3.conv2.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.4.conv1.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.4.conv2.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.5.conv1.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.2.res_blocks.5.conv2.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.3.conv.conv.weight 256x256x3x3x3",
                "encoder.down_blocks.4.res_blocks.0.conv1.conv.weight 512x512x3x3x3",
                "encoder.down_blocks.4.res_blocks.0.conv2.conv.weight 512x512x3x3x3",
                "encoder.down_blocks.4.res_blocks.1.conv1.conv.weight 512x512x3x3x3",
                "encoder.down_blocks.4.res_blocks.1.conv2.conv.weight 512x512x3x3x3",
                "encoder.down_blocks.4.res_blocks.2.conv1.conv.weight 512x512x3x3x3",
                "encoder.down_blocks.4.res_blocks.2.conv2.conv.weight 512x512x3x3x3",
                "encoder.down_blocks.4.res_blocks.3.conv1.conv.weight 512x512x3x3x3",
                "encoder.down_blocks.4.res_blocks.3.conv2.conv.weight 512x512x3x3x3",
                "encoder.down_blocks.5.conv.conv.weight 128x512x3x3x3",
                "encoder.down_blocks.6.res_blocks.0.conv1.conv.weight 1024x1024x3x3x3",
                "encoder.down_blocks.6.res_blocks.0.conv2.conv.weight 1024x1024x3x3x3",
                "encoder.down_blocks.6.res_blocks.1.conv1.conv.weight 1024x1024x3x3x3",
                "encoder.down_blocks.6.res_blocks.1.conv2.conv.weight 1024x1024x3x3x3",
                "encoder.down_blocks.7.conv.conv.weight 128x1024x3x3x3",
                "encoder.down_blocks.8.res_blocks.0.conv1.conv.weight 1024x1024x3x3x3",
                "encoder.down_blocks.8.res_blocks.0.conv2.conv.weight 1024x1024x3x3x3",
                "encoder.down_blocks.8.res_blocks.1.conv1.conv.weight 1024x1024x3x3x3",
                "encoder.down_blocks.8.res_blocks.1.conv2.conv.weight 1024x1024x3x3x3",
                "per_channel_statistics.mean-of-means 128",
                "per_channel_statistics.std-of-means 128"
            ]));
    }

    [Test]
    public void UpsamplerStateDictTest()
    {
        using LtxLatentUpsampler upsampler = MetaDevice.Construct(() => new LtxLatentUpsampler(128, 1024, 4));

        Assert.That(FormatShapes(upsampler, _ => true), Is.EqualTo([
            "final_conv.bias 128",
            "final_conv.weight 128x1024x3x3x3",
            "initial_conv.bias 1024",
            "initial_conv.weight 1024x128x3x3x3",
            "initial_norm.bias 1024",
            "initial_norm.weight 1024",
            "post_upsample_res_blocks.0.conv1.bias 1024",
            "post_upsample_res_blocks.0.conv1.weight 1024x1024x3x3x3",
            "post_upsample_res_blocks.0.conv2.bias 1024",
            "post_upsample_res_blocks.0.conv2.weight 1024x1024x3x3x3",
            "post_upsample_res_blocks.0.norm1.bias 1024",
            "post_upsample_res_blocks.0.norm1.weight 1024",
            "post_upsample_res_blocks.0.norm2.bias 1024",
            "post_upsample_res_blocks.0.norm2.weight 1024",
            "post_upsample_res_blocks.1.conv1.bias 1024",
            "post_upsample_res_blocks.1.conv1.weight 1024x1024x3x3x3",
            "post_upsample_res_blocks.1.conv2.bias 1024",
            "post_upsample_res_blocks.1.conv2.weight 1024x1024x3x3x3",
            "post_upsample_res_blocks.1.norm1.bias 1024",
            "post_upsample_res_blocks.1.norm1.weight 1024",
            "post_upsample_res_blocks.1.norm2.bias 1024",
            "post_upsample_res_blocks.1.norm2.weight 1024",
            "post_upsample_res_blocks.2.conv1.bias 1024",
            "post_upsample_res_blocks.2.conv1.weight 1024x1024x3x3x3",
            "post_upsample_res_blocks.2.conv2.bias 1024",
            "post_upsample_res_blocks.2.conv2.weight 1024x1024x3x3x3",
            "post_upsample_res_blocks.2.norm1.bias 1024",
            "post_upsample_res_blocks.2.norm1.weight 1024",
            "post_upsample_res_blocks.2.norm2.bias 1024",
            "post_upsample_res_blocks.2.norm2.weight 1024",
            "post_upsample_res_blocks.3.conv1.bias 1024",
            "post_upsample_res_blocks.3.conv1.weight 1024x1024x3x3x3",
            "post_upsample_res_blocks.3.conv2.bias 1024",
            "post_upsample_res_blocks.3.conv2.weight 1024x1024x3x3x3",
            "post_upsample_res_blocks.3.norm1.bias 1024",
            "post_upsample_res_blocks.3.norm1.weight 1024",
            "post_upsample_res_blocks.3.norm2.bias 1024",
            "post_upsample_res_blocks.3.norm2.weight 1024",
            "res_blocks.0.conv1.bias 1024",
            "res_blocks.0.conv1.weight 1024x1024x3x3x3",
            "res_blocks.0.conv2.bias 1024",
            "res_blocks.0.conv2.weight 1024x1024x3x3x3",
            "res_blocks.0.norm1.bias 1024",
            "res_blocks.0.norm1.weight 1024",
            "res_blocks.0.norm2.bias 1024",
            "res_blocks.0.norm2.weight 1024",
            "res_blocks.1.conv1.bias 1024",
            "res_blocks.1.conv1.weight 1024x1024x3x3x3",
            "res_blocks.1.conv2.bias 1024",
            "res_blocks.1.conv2.weight 1024x1024x3x3x3",
            "res_blocks.1.norm1.bias 1024",
            "res_blocks.1.norm1.weight 1024",
            "res_blocks.1.norm2.bias 1024",
            "res_blocks.1.norm2.weight 1024",
            "res_blocks.2.conv1.bias 1024",
            "res_blocks.2.conv1.weight 1024x1024x3x3x3",
            "res_blocks.2.conv2.bias 1024",
            "res_blocks.2.conv2.weight 1024x1024x3x3x3",
            "res_blocks.2.norm1.bias 1024",
            "res_blocks.2.norm1.weight 1024",
            "res_blocks.2.norm2.bias 1024",
            "res_blocks.2.norm2.weight 1024",
            "res_blocks.3.conv1.bias 1024",
            "res_blocks.3.conv1.weight 1024x1024x3x3x3",
            "res_blocks.3.conv2.bias 1024",
            "res_blocks.3.conv2.weight 1024x1024x3x3x3",
            "res_blocks.3.norm1.bias 1024",
            "res_blocks.3.norm1.weight 1024",
            "res_blocks.3.norm2.bias 1024",
            "res_blocks.3.norm2.weight 1024",
            "upsampler.0.bias 4096",
            "upsampler.0.weight 4096x1024x3x3"
        ]));
    }
}
