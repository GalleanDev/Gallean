using Gallean.Common;
using Gallean.Tests.Common;
using TorchSharp;
using TorchSharp.Modules;

namespace Umbra.Tests;

public sealed class UmbraSurfaceModelTests
{
    private static string WriteTestSurfaceModel(Sandbox sb)
    {
        string root = sb.GetPath("surface-models");
        string directory = Path.Combine(root, "vrgbx-inverse");

        Directory.CreateDirectory(directory);

        using (WanDit dit = new(128, 32, 16, 256, 16, 32, 1, 1, (1, 2, 2)))
        {
            SafeTensors.SaveStateDict(Path.Combine(directory, "dit.safetensors"), CollectParameters(dit));
        }

        using (WanVae vae = new(8, 16, [1, 2, 4, 4], 1))
        {
            SafeTensors.SaveStateDict(Path.Combine(directory, "vae.safetensors"), CollectParameters(vae));
        }

        Dictionary<string, torch.Tensor> context = [];

        foreach (string name in new[] { "albedo", "normal", "material", "negative" })
        {
            context[name] = torch.zeros([1, 4, 16]);
        }

        SafeTensors.SaveStateDict(Path.Combine(directory, "context.safetensors"), context);

        return root;
    }

    private static Dictionary<string, torch.Tensor> CollectParameters(torch.nn.Module module)
    {
        Dictionary<string, torch.Tensor> parameters = [];

        foreach ((string name, Parameter parameter) in module.named_parameters())
        {
            parameters[name] = parameter;
        }

        return parameters;
    }

    private static List<string> FormatLayerSizes(IReadOnlyList<UmbraSurfaceLayer> layers)
    {
        List<string> rows = [];

        foreach (UmbraSurfaceLayer layer in layers)
        {
            rows.Add($"{TestHelpers.FormatSpriteHash(layer.Hash)} {layer.Type} px {layer.PixelWidth}x{layer.PixelHeight}");
        }

        return rows;
    }

    private static List<string> FormatLayerAlphas(IReadOnlyList<UmbraSurfaceLayer> layers)
    {
        List<string> rows = [];

        foreach (UmbraSurfaceLayer layer in layers)
        {
            JoinStringBuilder alphas = new(",");

            for (int offset = 3; offset < layer.Pixels.Length; offset += 4)
            {
                alphas.Add(layer.Pixels.Span[offset].ToString());
            }

            rows.Add($"{TestHelpers.FormatSpriteHash(layer.Hash)} {layer.Type} alpha {alphas}");
        }

        return rows;
    }

    private static UmbraSprite MakeSprite(int pixelWidth, int pixelHeight, byte[] color)
    {
        UmbraSpriteMetadata metadata = new(pixelWidth, pixelHeight, pixelWidth, pixelHeight, 0, 0, Additive: false);

        byte[] pixels = new byte[pixelWidth * pixelHeight * 4];

        for (int index = 0; index < pixelWidth * pixelHeight; index++)
        {
            color.CopyTo(pixels, index * 4);
        }

        return new UmbraSprite(metadata, pixels);
    }

    [Test]
    public void LayerGroupsTest()
    {
        Assert.That(UmbraSurfaceModel.LayerGroups, Is.EqualTo([
            new[]
            {
                UmbraSurfaceLayerType.Normal
            }
        ]));
    }

    [Test]
    public void SmokeTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelPath = WriteTestSurfaceModel(sb);
        UmbraSprite shared = MakeSprite(4, 4, [200, 100, 50, 255]);

        UmbraSequence first = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), shared),
            new UmbraFrame(TestHelpers.MakeSpriteHash("bbbb"), MakeSprite(2, 2, [40, 80, 160, 255]))
        ]);

        UmbraSequence second = new(new UmbraSequenceId("battle-unit/x/hit/attacker"), [new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), shared)]);

        using UmbraSurfaceModel model = new([UmbraSurfaceLayerType.Normal], modelPath, 2, UmbraDevice.Cpu);

        IReadOnlyList<UmbraSurfaceLayer> layers = [.. model.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [first]), new UmbraSequenceGroup(new UmbraSequenceId("b"), [second])])];

        Assert.That(FormatLayerSizes(layers), Is.EquivalentTo([
            "aaaa Normal px 4x4",
            "bbbb Normal px 2x2",
            "aaaa Normal px 4x4"
        ]));
    }

    [Test]
    public void SourceAlphaTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelPath = WriteTestSurfaceModel(sb);
        UmbraSpriteMetadata metadata = new(2, 2, 2, 2, 0, 0, Additive: false);

        byte[] pixels =
        [
            200, 100, 50, 255,
            40, 80, 160, 128,
            10, 20, 30, 64,
            70, 60, 90, 0
        ];

        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), new UmbraSprite(metadata, pixels))
        ]);

        using UmbraSurfaceModel model = new([UmbraSurfaceLayerType.Normal], modelPath, 2, UmbraDevice.Cpu);

        IReadOnlyList<UmbraSurfaceLayer> layers = [.. model.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(FormatLayerAlphas(layers), Is.EquivalentTo(["aaaa Normal alpha 255,128,64,0"]));
        Assert.That(FormatLayerSizes(layers), Is.EquivalentTo(["aaaa Normal px 2x2"]));
    }

    [Test]
    public void PinnedSmokeTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelPath = WriteTestSurfaceModel(sb);
        UmbraSprite sprite = MakeSprite(4, 4, [200, 100, 50, 255]);

        UmbraSequence hit = new(new UmbraSequenceId("battle-unit/x/hit/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), sprite),
            new UmbraFrame(TestHelpers.MakeSpriteHash("cccc"), MakeSprite(4, 4, [40, 80, 160, 255]))
        ]);

        using UmbraSurfaceModel model = new([UmbraSurfaceLayerType.Normal], modelPath, 2, UmbraDevice.Cpu);

        IReadOnlyList<UmbraSurfaceLayer> layers = [.. model.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [hit])])];

        Assert.That(FormatLayerSizes(layers), Is.EquivalentTo([
            "aaaa Normal px 4x4",
            "cccc Normal px 4x4"
        ]));
    }

    [Test]
    public void PartiallyStoredFrameTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelPath = WriteTestSurfaceModel(sb);
        UmbraSprite sprite = MakeSprite(4, 4, [200, 100, 50, 255]);

        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), sprite),
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), sprite)
        ]);

        using UmbraSurfaceModel model = new([UmbraSurfaceLayerType.Normal], modelPath, 2, UmbraDevice.Cpu);

        IReadOnlyList<UmbraSurfaceLayer> layers =
            [.. model.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(FormatLayerSizes(layers), Is.EquivalentTo([
            "aaaa Normal px 4x4"
        ]));
    }
}
