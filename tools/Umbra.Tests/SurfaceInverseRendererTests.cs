using System.Security.Cryptography;
using TorchSharp;

namespace Umbra.Tests;

public sealed class SurfaceInverseRendererTests
{
    private static UmbraFrame Frame(string hash, int width, int height)
    {
        UmbraSpriteMetadata metadata = new(width, height, width, height, 0, 0, false);

        return new UmbraFrame(TestHelpers.MakeSpriteHash(hash), new UmbraSprite(metadata, new byte[width * height * 4]));
    }

    private static SurfaceInverseRenderer Build(IReadOnlyList<SurfacePass> passes)
    {
        Dictionary<string, torch.Tensor> embeddings = new()
        {
            ["normal"] = torch.zeros([1, 4, 128]),
            ["negative"] = torch.zeros([1, 4, 128])
        };

        WanDit dit = new(
            dim: 64,
            inChannels: 32,
            outChannels: 16,
            feedForwardDim: 128,
            textDim: 128,
            frequencyDim: 64,
            heads: 2,
            layers: 1,
            patch: (1, 2, 2));

        WanVae vae = new(dim: 8, latentChannels: 16, [1, 2, 4, 4], residualBlocks: 1);

        SurfaceDiffuser diffuser = new(
            new SurfaceWeights(dit.state_dict(), vae.state_dict()),
            dit,
            vae,
            new SurfaceContext(embeddings),
            denoisingSteps: 2);

        return new SurfaceInverseRenderer(diffuser, passes, new SurfaceOptions());
    }

    private static string FormatLayer(UmbraSurfaceLayer layer)
    {
        string digest = Convert.ToHexString(SHA256.HashData(layer.Pixels.Span));

        return $"{TestHelpers.FormatSpriteHash(layer.Hash)} {layer.Type} {layer.PixelWidth}x{layer.PixelHeight} {digest[..16]}";
    }

    private static List<string> FormatLayers(IReadOnlyList<UmbraSurfaceLayer> layers)
    {
        List<string> rows = [];

        foreach (UmbraSurfaceLayer layer in layers)
        {
            rows.Add(FormatLayer(layer));
        }

        return rows;
    }

    [Test]
    public void LayersTest()
    {
        using SurfaceInverseRenderer renderer = Build(
        [
            new SurfacePass(UmbraSurfaceLayerType.Normal, SurfaceChannelType.Normal)
        ]);

        UmbraSequence sequence = new(new UmbraSequenceId("g000uu0001"), [Frame("a", 24, 24), Frame("b", 24, 24), Frame("c", 24, 24)]);

        List<UmbraSurfaceLayer> layers = [.. renderer.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(
            layers.Select(layer => (TestHelpers.FormatSpriteHash(layer.Hash), layer.Type, layer.PixelWidth, layer.PixelHeight)),
            Is.EqualTo([
                ("a", UmbraSurfaceLayerType.Normal, 24, 24),
                ("b", UmbraSurfaceLayerType.Normal, 24, 24),
                ("c", UmbraSurfaceLayerType.Normal, 24, 24)
            ]));
    }

    [Test]
    public void RepeatedFrameTest()
    {
        using SurfaceInverseRenderer renderer = Build([new SurfacePass(UmbraSurfaceLayerType.Normal, SurfaceChannelType.Normal)]);

        UmbraSequence sequence = new(new UmbraSequenceId("g000uu0001"), [Frame("a", 24, 24), Frame("a", 24, 24), Frame("b", 24, 24)]);

        List<UmbraSurfaceLayer> layers = [.. renderer.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(layers.Select(layer => TestHelpers.FormatSpriteHash(layer.Hash)), Is.EqualTo(["a", "b"]));
    }

    [Test]
    public void SharedFrameTest()
    {
        using SurfaceInverseRenderer renderer = Build([new SurfacePass(UmbraSurfaceLayerType.Normal, SurfaceChannelType.Normal)]);

        UmbraSequence narrow = new(new UmbraSequenceId("g000uu0001/idle"), [Frame("s", 24, 24), Frame("n1", 24, 24), Frame("n2", 24, 24)]);
        UmbraSequence wide = new(new UmbraSequenceId("g000uu0001/attack"), [Frame("s", 24, 24), Frame("w1", 64, 24), Frame("w2", 24, 24)]);

        List<UmbraSurfaceLayer> narrowAlone = [.. renderer.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [narrow])])];
        List<UmbraSurfaceLayer> wideAlone = [.. renderer.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [wide])])];
        List<UmbraSurfaceLayer> together = [.. renderer.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [wide]), new UmbraSequenceGroup(new UmbraSequenceId("b"), [narrow])])];
        List<UmbraSurfaceLayer> reversed = [.. renderer.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [narrow]), new UmbraSequenceGroup(new UmbraSequenceId("b"), [wide])])];

        List<string> narrowRows = FormatLayers(narrowAlone);
        List<string> wideRows = FormatLayers(wideAlone);
        List<string> wideThenNarrow = [.. wideRows, .. narrowRows];
        List<string> narrowThenWide = [.. narrowRows, .. wideRows];

        Assert.That(FormatLayers(together), Is.EqualTo(wideThenNarrow));
        Assert.That(FormatLayers(reversed), Is.EqualTo(narrowThenWide));
    }
}
