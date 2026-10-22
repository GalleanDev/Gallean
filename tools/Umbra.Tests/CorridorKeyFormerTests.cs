using TorchSharp;

namespace Umbra.Tests;

public sealed class CorridorKeyFormerTests
{
    private static List<string> FormatShapes(torch.nn.Module module)
    {
        List<string> rows = [];

        foreach ((string name, torch.Tensor tensor) in module.state_dict())
        {
            rows.Add($"{name} {string.Join("x", tensor.shape)}");
        }

        rows.Sort(StringComparer.Ordinal);

        return rows;
    }

    [Test]
    public void RefinerBlockTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-net");

        using CorridorKeyRefinerBlock block = new(16, dilation: 2);

        ModuleParameters.Load(block, TestParity.LoadWeights("corridorkey-net", "refinerBlock.weights."));

        using torch.Tensor result = block.forward(dump["refinerBlock.input"]);

        Assert.That(result.shape, Is.EqualTo(dump["refinerBlock.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["refinerBlock.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void RefinerTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-net");

        using CorridorKeyCnnRefiner refiner = new(7, 16, 4);

        ModuleParameters.Load(refiner, TestParity.LoadWeights("corridorkey-net", "cnnRefiner.weights."));

        using torch.Tensor result = refiner.forward(dump["cnnRefiner.colors"], dump["cnnRefiner.coarse"]);

        Assert.That(result.shape, Is.EqualTo(dump["cnnRefiner.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["cnnRefiner.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void DecoderHeadTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-net");

        using CorridorKeyDecoderHead head = new([8, 16, 32, 64], 32, 3);

        ModuleParameters.Load(head, TestParity.LoadWeights("corridorkey-net", "decoderHead.weights."));

        using torch.Tensor result = head.Forward(
        [
            dump["decoderHead.input0"],
            dump["decoderHead.input1"],
            dump["decoderHead.input2"],
            dump["decoderHead.input3"]
        ]);

        Assert.That(result.shape, Is.EqualTo(dump["decoderHead.expected"].shape));
        Assert.That(TestHelpers.MaxDifference(result, dump["decoderHead.expected"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void ForwardTest()
    {
        Dictionary<string, torch.Tensor> dump = TestParity.Load("corridorkey-net");

        using CorridorKeyFormer former = new(new CorridorKeyOptions(
            ImageSize: 32,
            InputChannels: 4,
            EmbedDimension: 8,
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

        ModuleParameters.Load(former, TestParity.LoadWeights("corridorkey-net", "former.weights."));

        using CorridorKeyPrediction prediction = former.Forward(dump["former.input"]);

        Assert.That(prediction.Alpha.shape, Is.EqualTo(dump["former.expectedAlpha"].shape));
        Assert.That(TestHelpers.MaxDifference(prediction.Alpha, dump["former.expectedAlpha"]), Is.LessThan(1e-5f));
        Assert.That(TestHelpers.MaxDifference(prediction.Foreground, dump["former.expectedForeground"]), Is.LessThan(1e-5f));
    }

    [Test]
    public void StateDictTest()
    {
        using CorridorKeyFormer former = MetaDevice.Construct(() => new CorridorKeyFormer(CorridorKeyOptions.BasePlus(2048)));

        Assert.That(
            FormatShapes(former),
            Is.EqualTo([
                "alpha_decoder.bn.bias 256",
                "alpha_decoder.bn.running_mean 256",
                "alpha_decoder.bn.running_var 256",
                "alpha_decoder.bn.weight 256",
                "alpha_decoder.classifier.bias 1",
                "alpha_decoder.classifier.weight 1x256x1x1",
                "alpha_decoder.linear_c1.proj.bias 256",
                "alpha_decoder.linear_c1.proj.weight 256x112",
                "alpha_decoder.linear_c2.proj.bias 256",
                "alpha_decoder.linear_c2.proj.weight 256x224",
                "alpha_decoder.linear_c3.proj.bias 256",
                "alpha_decoder.linear_c3.proj.weight 256x448",
                "alpha_decoder.linear_c4.proj.bias 256",
                "alpha_decoder.linear_c4.proj.weight 256x896",
                "alpha_decoder.linear_fuse.weight 256x1024x1x1",
                "encoder.blocks.0.attn.proj.bias 112",
                "encoder.blocks.0.attn.proj.weight 112x112",
                "encoder.blocks.0.attn.qkv.bias 336",
                "encoder.blocks.0.attn.qkv.weight 336x112",
                "encoder.blocks.0.mlp.fc1.bias 448",
                "encoder.blocks.0.mlp.fc1.weight 448x112",
                "encoder.blocks.0.mlp.fc2.bias 112",
                "encoder.blocks.0.mlp.fc2.weight 112x448",
                "encoder.blocks.0.norm1.bias 112",
                "encoder.blocks.0.norm1.weight 112",
                "encoder.blocks.0.norm2.bias 112",
                "encoder.blocks.0.norm2.weight 112",
                "encoder.blocks.1.attn.proj.bias 112",
                "encoder.blocks.1.attn.proj.weight 112x112",
                "encoder.blocks.1.attn.qkv.bias 336",
                "encoder.blocks.1.attn.qkv.weight 336x112",
                "encoder.blocks.1.mlp.fc1.bias 448",
                "encoder.blocks.1.mlp.fc1.weight 448x112",
                "encoder.blocks.1.mlp.fc2.bias 112",
                "encoder.blocks.1.mlp.fc2.weight 112x448",
                "encoder.blocks.1.norm1.bias 112",
                "encoder.blocks.1.norm1.weight 112",
                "encoder.blocks.1.norm2.bias 112",
                "encoder.blocks.1.norm2.weight 112",
                "encoder.blocks.10.attn.proj.bias 448",
                "encoder.blocks.10.attn.proj.weight 448x448",
                "encoder.blocks.10.attn.qkv.bias 1344",
                "encoder.blocks.10.attn.qkv.weight 1344x448",
                "encoder.blocks.10.mlp.fc1.bias 1792",
                "encoder.blocks.10.mlp.fc1.weight 1792x448",
                "encoder.blocks.10.mlp.fc2.bias 448",
                "encoder.blocks.10.mlp.fc2.weight 448x1792",
                "encoder.blocks.10.norm1.bias 448",
                "encoder.blocks.10.norm1.weight 448",
                "encoder.blocks.10.norm2.bias 448",
                "encoder.blocks.10.norm2.weight 448",
                "encoder.blocks.11.attn.proj.bias 448",
                "encoder.blocks.11.attn.proj.weight 448x448",
                "encoder.blocks.11.attn.qkv.bias 1344",
                "encoder.blocks.11.attn.qkv.weight 1344x448",
                "encoder.blocks.11.mlp.fc1.bias 1792",
                "encoder.blocks.11.mlp.fc1.weight 1792x448",
                "encoder.blocks.11.mlp.fc2.bias 448",
                "encoder.blocks.11.mlp.fc2.weight 448x1792",
                "encoder.blocks.11.norm1.bias 448",
                "encoder.blocks.11.norm1.weight 448",
                "encoder.blocks.11.norm2.bias 448",
                "encoder.blocks.11.norm2.weight 448",
                "encoder.blocks.12.attn.proj.bias 448",
                "encoder.blocks.12.attn.proj.weight 448x448",
                "encoder.blocks.12.attn.qkv.bias 1344",
                "encoder.blocks.12.attn.qkv.weight 1344x448",
                "encoder.blocks.12.mlp.fc1.bias 1792",
                "encoder.blocks.12.mlp.fc1.weight 1792x448",
                "encoder.blocks.12.mlp.fc2.bias 448",
                "encoder.blocks.12.mlp.fc2.weight 448x1792",
                "encoder.blocks.12.norm1.bias 448",
                "encoder.blocks.12.norm1.weight 448",
                "encoder.blocks.12.norm2.bias 448",
                "encoder.blocks.12.norm2.weight 448",
                "encoder.blocks.13.attn.proj.bias 448",
                "encoder.blocks.13.attn.proj.weight 448x448",
                "encoder.blocks.13.attn.qkv.bias 1344",
                "encoder.blocks.13.attn.qkv.weight 1344x448",
                "encoder.blocks.13.mlp.fc1.bias 1792",
                "encoder.blocks.13.mlp.fc1.weight 1792x448",
                "encoder.blocks.13.mlp.fc2.bias 448",
                "encoder.blocks.13.mlp.fc2.weight 448x1792",
                "encoder.blocks.13.norm1.bias 448",
                "encoder.blocks.13.norm1.weight 448",
                "encoder.blocks.13.norm2.bias 448",
                "encoder.blocks.13.norm2.weight 448",
                "encoder.blocks.14.attn.proj.bias 448",
                "encoder.blocks.14.attn.proj.weight 448x448",
                "encoder.blocks.14.attn.qkv.bias 1344",
                "encoder.blocks.14.attn.qkv.weight 1344x448",
                "encoder.blocks.14.mlp.fc1.bias 1792",
                "encoder.blocks.14.mlp.fc1.weight 1792x448",
                "encoder.blocks.14.mlp.fc2.bias 448",
                "encoder.blocks.14.mlp.fc2.weight 448x1792",
                "encoder.blocks.14.norm1.bias 448",
                "encoder.blocks.14.norm1.weight 448",
                "encoder.blocks.14.norm2.bias 448",
                "encoder.blocks.14.norm2.weight 448",
                "encoder.blocks.15.attn.proj.bias 448",
                "encoder.blocks.15.attn.proj.weight 448x448",
                "encoder.blocks.15.attn.qkv.bias 1344",
                "encoder.blocks.15.attn.qkv.weight 1344x448",
                "encoder.blocks.15.mlp.fc1.bias 1792",
                "encoder.blocks.15.mlp.fc1.weight 1792x448",
                "encoder.blocks.15.mlp.fc2.bias 448",
                "encoder.blocks.15.mlp.fc2.weight 448x1792",
                "encoder.blocks.15.norm1.bias 448",
                "encoder.blocks.15.norm1.weight 448",
                "encoder.blocks.15.norm2.bias 448",
                "encoder.blocks.15.norm2.weight 448",
                "encoder.blocks.16.attn.proj.bias 448",
                "encoder.blocks.16.attn.proj.weight 448x448",
                "encoder.blocks.16.attn.qkv.bias 1344",
                "encoder.blocks.16.attn.qkv.weight 1344x448",
                "encoder.blocks.16.mlp.fc1.bias 1792",
                "encoder.blocks.16.mlp.fc1.weight 1792x448",
                "encoder.blocks.16.mlp.fc2.bias 448",
                "encoder.blocks.16.mlp.fc2.weight 448x1792",
                "encoder.blocks.16.norm1.bias 448",
                "encoder.blocks.16.norm1.weight 448",
                "encoder.blocks.16.norm2.bias 448",
                "encoder.blocks.16.norm2.weight 448",
                "encoder.blocks.17.attn.proj.bias 448",
                "encoder.blocks.17.attn.proj.weight 448x448",
                "encoder.blocks.17.attn.qkv.bias 1344",
                "encoder.blocks.17.attn.qkv.weight 1344x448",
                "encoder.blocks.17.mlp.fc1.bias 1792",
                "encoder.blocks.17.mlp.fc1.weight 1792x448",
                "encoder.blocks.17.mlp.fc2.bias 448",
                "encoder.blocks.17.mlp.fc2.weight 448x1792",
                "encoder.blocks.17.norm1.bias 448",
                "encoder.blocks.17.norm1.weight 448",
                "encoder.blocks.17.norm2.bias 448",
                "encoder.blocks.17.norm2.weight 448",
                "encoder.blocks.18.attn.proj.bias 448",
                "encoder.blocks.18.attn.proj.weight 448x448",
                "encoder.blocks.18.attn.qkv.bias 1344",
                "encoder.blocks.18.attn.qkv.weight 1344x448",
                "encoder.blocks.18.mlp.fc1.bias 1792",
                "encoder.blocks.18.mlp.fc1.weight 1792x448",
                "encoder.blocks.18.mlp.fc2.bias 448",
                "encoder.blocks.18.mlp.fc2.weight 448x1792",
                "encoder.blocks.18.norm1.bias 448",
                "encoder.blocks.18.norm1.weight 448",
                "encoder.blocks.18.norm2.bias 448",
                "encoder.blocks.18.norm2.weight 448",
                "encoder.blocks.19.attn.proj.bias 448",
                "encoder.blocks.19.attn.proj.weight 448x448",
                "encoder.blocks.19.attn.qkv.bias 1344",
                "encoder.blocks.19.attn.qkv.weight 1344x448",
                "encoder.blocks.19.mlp.fc1.bias 1792",
                "encoder.blocks.19.mlp.fc1.weight 1792x448",
                "encoder.blocks.19.mlp.fc2.bias 448",
                "encoder.blocks.19.mlp.fc2.weight 448x1792",
                "encoder.blocks.19.norm1.bias 448",
                "encoder.blocks.19.norm1.weight 448",
                "encoder.blocks.19.norm2.bias 448",
                "encoder.blocks.19.norm2.weight 448",
                "encoder.blocks.2.attn.proj.bias 224",
                "encoder.blocks.2.attn.proj.weight 224x224",
                "encoder.blocks.2.attn.qkv.bias 672",
                "encoder.blocks.2.attn.qkv.weight 672x112",
                "encoder.blocks.2.mlp.fc1.bias 896",
                "encoder.blocks.2.mlp.fc1.weight 896x224",
                "encoder.blocks.2.mlp.fc2.bias 224",
                "encoder.blocks.2.mlp.fc2.weight 224x896",
                "encoder.blocks.2.norm1.bias 112",
                "encoder.blocks.2.norm1.weight 112",
                "encoder.blocks.2.norm2.bias 224",
                "encoder.blocks.2.norm2.weight 224",
                "encoder.blocks.2.proj.bias 224",
                "encoder.blocks.2.proj.weight 224x112",
                "encoder.blocks.20.attn.proj.bias 448",
                "encoder.blocks.20.attn.proj.weight 448x448",
                "encoder.blocks.20.attn.qkv.bias 1344",
                "encoder.blocks.20.attn.qkv.weight 1344x448",
                "encoder.blocks.20.mlp.fc1.bias 1792",
                "encoder.blocks.20.mlp.fc1.weight 1792x448",
                "encoder.blocks.20.mlp.fc2.bias 448",
                "encoder.blocks.20.mlp.fc2.weight 448x1792",
                "encoder.blocks.20.norm1.bias 448",
                "encoder.blocks.20.norm1.weight 448",
                "encoder.blocks.20.norm2.bias 448",
                "encoder.blocks.20.norm2.weight 448",
                "encoder.blocks.21.attn.proj.bias 896",
                "encoder.blocks.21.attn.proj.weight 896x896",
                "encoder.blocks.21.attn.qkv.bias 2688",
                "encoder.blocks.21.attn.qkv.weight 2688x448",
                "encoder.blocks.21.mlp.fc1.bias 3584",
                "encoder.blocks.21.mlp.fc1.weight 3584x896",
                "encoder.blocks.21.mlp.fc2.bias 896",
                "encoder.blocks.21.mlp.fc2.weight 896x3584",
                "encoder.blocks.21.norm1.bias 448",
                "encoder.blocks.21.norm1.weight 448",
                "encoder.blocks.21.norm2.bias 896",
                "encoder.blocks.21.norm2.weight 896",
                "encoder.blocks.21.proj.bias 896",
                "encoder.blocks.21.proj.weight 896x448",
                "encoder.blocks.22.attn.proj.bias 896",
                "encoder.blocks.22.attn.proj.weight 896x896",
                "encoder.blocks.22.attn.qkv.bias 2688",
                "encoder.blocks.22.attn.qkv.weight 2688x896",
                "encoder.blocks.22.mlp.fc1.bias 3584",
                "encoder.blocks.22.mlp.fc1.weight 3584x896",
                "encoder.blocks.22.mlp.fc2.bias 896",
                "encoder.blocks.22.mlp.fc2.weight 896x3584",
                "encoder.blocks.22.norm1.bias 896",
                "encoder.blocks.22.norm1.weight 896",
                "encoder.blocks.22.norm2.bias 896",
                "encoder.blocks.22.norm2.weight 896",
                "encoder.blocks.23.attn.proj.bias 896",
                "encoder.blocks.23.attn.proj.weight 896x896",
                "encoder.blocks.23.attn.qkv.bias 2688",
                "encoder.blocks.23.attn.qkv.weight 2688x896",
                "encoder.blocks.23.mlp.fc1.bias 3584",
                "encoder.blocks.23.mlp.fc1.weight 3584x896",
                "encoder.blocks.23.mlp.fc2.bias 896",
                "encoder.blocks.23.mlp.fc2.weight 896x3584",
                "encoder.blocks.23.norm1.bias 896",
                "encoder.blocks.23.norm1.weight 896",
                "encoder.blocks.23.norm2.bias 896",
                "encoder.blocks.23.norm2.weight 896",
                "encoder.blocks.3.attn.proj.bias 224",
                "encoder.blocks.3.attn.proj.weight 224x224",
                "encoder.blocks.3.attn.qkv.bias 672",
                "encoder.blocks.3.attn.qkv.weight 672x224",
                "encoder.blocks.3.mlp.fc1.bias 896",
                "encoder.blocks.3.mlp.fc1.weight 896x224",
                "encoder.blocks.3.mlp.fc2.bias 224",
                "encoder.blocks.3.mlp.fc2.weight 224x896",
                "encoder.blocks.3.norm1.bias 224",
                "encoder.blocks.3.norm1.weight 224",
                "encoder.blocks.3.norm2.bias 224",
                "encoder.blocks.3.norm2.weight 224",
                "encoder.blocks.4.attn.proj.bias 224",
                "encoder.blocks.4.attn.proj.weight 224x224",
                "encoder.blocks.4.attn.qkv.bias 672",
                "encoder.blocks.4.attn.qkv.weight 672x224",
                "encoder.blocks.4.mlp.fc1.bias 896",
                "encoder.blocks.4.mlp.fc1.weight 896x224",
                "encoder.blocks.4.mlp.fc2.bias 224",
                "encoder.blocks.4.mlp.fc2.weight 224x896",
                "encoder.blocks.4.norm1.bias 224",
                "encoder.blocks.4.norm1.weight 224",
                "encoder.blocks.4.norm2.bias 224",
                "encoder.blocks.4.norm2.weight 224",
                "encoder.blocks.5.attn.proj.bias 448",
                "encoder.blocks.5.attn.proj.weight 448x448",
                "encoder.blocks.5.attn.qkv.bias 1344",
                "encoder.blocks.5.attn.qkv.weight 1344x224",
                "encoder.blocks.5.mlp.fc1.bias 1792",
                "encoder.blocks.5.mlp.fc1.weight 1792x448",
                "encoder.blocks.5.mlp.fc2.bias 448",
                "encoder.blocks.5.mlp.fc2.weight 448x1792",
                "encoder.blocks.5.norm1.bias 224",
                "encoder.blocks.5.norm1.weight 224",
                "encoder.blocks.5.norm2.bias 448",
                "encoder.blocks.5.norm2.weight 448",
                "encoder.blocks.5.proj.bias 448",
                "encoder.blocks.5.proj.weight 448x224",
                "encoder.blocks.6.attn.proj.bias 448",
                "encoder.blocks.6.attn.proj.weight 448x448",
                "encoder.blocks.6.attn.qkv.bias 1344",
                "encoder.blocks.6.attn.qkv.weight 1344x448",
                "encoder.blocks.6.mlp.fc1.bias 1792",
                "encoder.blocks.6.mlp.fc1.weight 1792x448",
                "encoder.blocks.6.mlp.fc2.bias 448",
                "encoder.blocks.6.mlp.fc2.weight 448x1792",
                "encoder.blocks.6.norm1.bias 448",
                "encoder.blocks.6.norm1.weight 448",
                "encoder.blocks.6.norm2.bias 448",
                "encoder.blocks.6.norm2.weight 448",
                "encoder.blocks.7.attn.proj.bias 448",
                "encoder.blocks.7.attn.proj.weight 448x448",
                "encoder.blocks.7.attn.qkv.bias 1344",
                "encoder.blocks.7.attn.qkv.weight 1344x448",
                "encoder.blocks.7.mlp.fc1.bias 1792",
                "encoder.blocks.7.mlp.fc1.weight 1792x448",
                "encoder.blocks.7.mlp.fc2.bias 448",
                "encoder.blocks.7.mlp.fc2.weight 448x1792",
                "encoder.blocks.7.norm1.bias 448",
                "encoder.blocks.7.norm1.weight 448",
                "encoder.blocks.7.norm2.bias 448",
                "encoder.blocks.7.norm2.weight 448",
                "encoder.blocks.8.attn.proj.bias 448",
                "encoder.blocks.8.attn.proj.weight 448x448",
                "encoder.blocks.8.attn.qkv.bias 1344",
                "encoder.blocks.8.attn.qkv.weight 1344x448",
                "encoder.blocks.8.mlp.fc1.bias 1792",
                "encoder.blocks.8.mlp.fc1.weight 1792x448",
                "encoder.blocks.8.mlp.fc2.bias 448",
                "encoder.blocks.8.mlp.fc2.weight 448x1792",
                "encoder.blocks.8.norm1.bias 448",
                "encoder.blocks.8.norm1.weight 448",
                "encoder.blocks.8.norm2.bias 448",
                "encoder.blocks.8.norm2.weight 448",
                "encoder.blocks.9.attn.proj.bias 448",
                "encoder.blocks.9.attn.proj.weight 448x448",
                "encoder.blocks.9.attn.qkv.bias 1344",
                "encoder.blocks.9.attn.qkv.weight 1344x448",
                "encoder.blocks.9.mlp.fc1.bias 1792",
                "encoder.blocks.9.mlp.fc1.weight 1792x448",
                "encoder.blocks.9.mlp.fc2.bias 448",
                "encoder.blocks.9.mlp.fc2.weight 448x1792",
                "encoder.blocks.9.norm1.bias 448",
                "encoder.blocks.9.norm1.weight 448",
                "encoder.blocks.9.norm2.bias 448",
                "encoder.blocks.9.norm2.weight 448",
                "encoder.patch_embed.proj.bias 112",
                "encoder.patch_embed.proj.weight 112x4x7x7",
                "encoder.pos_embed 1x262144x112",
                "fg_decoder.bn.bias 256",
                "fg_decoder.bn.running_mean 256",
                "fg_decoder.bn.running_var 256",
                "fg_decoder.bn.weight 256",
                "fg_decoder.classifier.bias 3",
                "fg_decoder.classifier.weight 3x256x1x1",
                "fg_decoder.linear_c1.proj.bias 256",
                "fg_decoder.linear_c1.proj.weight 256x112",
                "fg_decoder.linear_c2.proj.bias 256",
                "fg_decoder.linear_c2.proj.weight 256x224",
                "fg_decoder.linear_c3.proj.bias 256",
                "fg_decoder.linear_c3.proj.weight 256x448",
                "fg_decoder.linear_c4.proj.bias 256",
                "fg_decoder.linear_c4.proj.weight 256x896",
                "fg_decoder.linear_fuse.weight 256x1024x1x1",
                "refiner.final.bias 4",
                "refiner.final.weight 4x64x1x1",
                "refiner.res1.conv1.bias 64",
                "refiner.res1.conv1.weight 64x64x3x3",
                "refiner.res1.conv2.bias 64",
                "refiner.res1.conv2.weight 64x64x3x3",
                "refiner.res1.gn1.bias 64",
                "refiner.res1.gn1.weight 64",
                "refiner.res1.gn2.bias 64",
                "refiner.res1.gn2.weight 64",
                "refiner.res2.conv1.bias 64",
                "refiner.res2.conv1.weight 64x64x3x3",
                "refiner.res2.conv2.bias 64",
                "refiner.res2.conv2.weight 64x64x3x3",
                "refiner.res2.gn1.bias 64",
                "refiner.res2.gn1.weight 64",
                "refiner.res2.gn2.bias 64",
                "refiner.res2.gn2.weight 64",
                "refiner.res3.conv1.bias 64",
                "refiner.res3.conv1.weight 64x64x3x3",
                "refiner.res3.conv2.bias 64",
                "refiner.res3.conv2.weight 64x64x3x3",
                "refiner.res3.gn1.bias 64",
                "refiner.res3.gn1.weight 64",
                "refiner.res3.gn2.bias 64",
                "refiner.res3.gn2.weight 64",
                "refiner.res4.conv1.bias 64",
                "refiner.res4.conv1.weight 64x64x3x3",
                "refiner.res4.conv2.bias 64",
                "refiner.res4.conv2.weight 64x64x3x3",
                "refiner.res4.gn1.bias 64",
                "refiner.res4.gn1.weight 64",
                "refiner.res4.gn2.bias 64",
                "refiner.res4.gn2.weight 64",
                "refiner.stem.0.bias 64",
                "refiner.stem.0.weight 64x7x3x3",
                "refiner.stem.1.bias 64",
                "refiner.stem.1.weight 64"
            ]));
    }
}
