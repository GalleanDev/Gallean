using TorchSharp;
using TorchSharp.Modules;

namespace Umbra.Tests;

public sealed class MosrUpscalerTests
{
    private static UmbraSprite MakeEmptySprite(int width, int height, bool additive)
    {
        return new UmbraSprite(new UmbraSpriteMetadata(width, height, width, height, 0, 0, additive), new byte[width * height * 4]);
    }

    private static UmbraSprite MakePeriodicSprite(int period, int height, bool additive, byte alpha)
    {
        int width = period * 2;
        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = (((y * width) + x) * 4);

                pixels[offset] = (byte)((x % period) * 7);
                pixels[offset + 1] = (byte)(y * 5);
                pixels[offset + 2] = (byte)(((x % period) * 3) + y);
                pixels[offset + 3] = alpha;
            }
        }

        return new UmbraSprite(new UmbraSpriteMetadata(width, height, width, height, 0, 0, additive), pixels);
    }

    private static List<byte> GetColors(UmbraSprite sprite)
    {
        List<byte> colors = [];
        ReadOnlySpan<byte> pixels = sprite.Pixels.Span;

        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            colors.Add(pixels[offset]);
            colors.Add(pixels[offset + 1]);
            colors.Add(pixels[offset + 2]);
        }

        return colors;
    }

    private static List<byte> GetAlpha(UmbraSprite sprite)
    {
        List<byte> alpha = [];
        ReadOnlySpan<byte> pixels = sprite.Pixels.Span;

        for (int offset = 3; offset < pixels.Length; offset += 4)
        {
            alpha.Add(pixels[offset]);
        }

        return alpha;
    }

    private static UmbraSprite RunSingle(MosrUpscaler upscaler, string id, UmbraSprite sprite)
    {
        List<UmbraSequence> sequences = [new(new UmbraSequenceId(id), [new UmbraFrame(TestHelpers.MakeSpriteHash($"{id}-0"), sprite)])];

        using (torch.no_grad())
        {
            List<UmbraUpscaledFrame> frames = [.. upscaler.Run(sequences)];

            return frames[0].Sprite;
        }
    }

    [Test]
    public void OpaqueFrameTest()
    {
        using MosrUpscaler upscaler = new(new MosrNetwork(new MosrOptions(
            InChannels: 3,
            OutChannels: 3,
            Scale: 2,
            BlockCount: 2,
            Dimension: 8,
            KernelSize: 7,
            ExpansionRatio: 1.5,
            ConvRatio: 1.0,
            SampleGroups: 4)), 2, torch.CPU);

        UmbraSprite sprite = RunSingle(upscaler, "opaque", MakePeriodicSprite(16, 12, additive: false, alpha: 255));

        Assert.That(sprite.Metadata.PixelWidth, Is.EqualTo(64));
        Assert.That(sprite.Metadata.PixelHeight, Is.EqualTo(24));
        Assert.That(GetAlpha(sprite), Is.All.EqualTo(255));
    }

    [Test]
    public void AdditiveFrameTest()
    {
        using MosrUpscaler upscaler = new(new MosrNetwork(new MosrOptions(
            InChannels: 3,
            OutChannels: 3,
            Scale: 2,
            BlockCount: 2,
            Dimension: 8,
            KernelSize: 7,
            ExpansionRatio: 1.5,
            ConvRatio: 1.0,
            SampleGroups: 4)), 2, torch.CPU);

        UmbraSprite sprite = RunSingle(upscaler, "additive", MakePeriodicSprite(16, 12, additive: true, alpha: 255));

        Assert.That(GetAlpha(sprite), Is.All.EqualTo(0));
    }

    [Test]
    public void TranslucentFrameTest()
    {
        using MosrUpscaler upscaler = new(new MosrNetwork(new MosrOptions(
            InChannels: 3,
            OutChannels: 3,
            Scale: 2,
            BlockCount: 2,
            Dimension: 8,
            KernelSize: 7,
            ExpansionRatio: 1.5,
            ConvRatio: 1.0,
            SampleGroups: 4)), 2, torch.CPU);

        UmbraSprite sprite = RunSingle(upscaler, "translucent", MakePeriodicSprite(16, 12, additive: false, alpha: 128));

        Assert.That(GetAlpha(sprite), Has.Some.LessThan(255));
    }

    [Test]
    public void AdditiveEmptyTest()
    {
        using MosrUpscaler upscaler = new(new MosrNetwork(new MosrOptions(
            InChannels: 3,
            OutChannels: 3,
            Scale: 2,
            BlockCount: 2,
            Dimension: 8,
            KernelSize: 7,
            ExpansionRatio: 1.5,
            ConvRatio: 1.0,
            SampleGroups: 4)), 2, torch.CPU);

        UmbraSprite sprite = RunSingle(upscaler, "empty", MakeEmptySprite(24, 16, additive: true));

        Assert.That(GetColors(sprite), Is.All.EqualTo(0));
    }

    [Test]
    public void AdditiveSimpleTest()
    {
        using MosrUpscaler upscaler = new(new MosrNetwork(new MosrOptions(
            InChannels: 3,
            OutChannels: 3,
            Scale: 2,
            BlockCount: 2,
            Dimension: 8,
            KernelSize: 7,
            ExpansionRatio: 1.5,
            ConvRatio: 1.0,
            SampleGroups: 4)), 2, torch.CPU);

        UmbraSprite sprite = RunSingle(upscaler, "lit", MakePeriodicSprite(16, 12, additive: true, alpha: 0));

        Assert.That(GetColors(sprite), Has.Some.GreaterThan(0));
    }

    [Test]
    public void TransparentFrameTest()
    {
        MosrNetwork network = new(new MosrOptions(
            InChannels: 3,
            OutChannels: 3,
            Scale: 2,
            BlockCount: 2,
            Dimension: 8,
            KernelSize: 7,
            ExpansionRatio: 1.5,
            ConvRatio: 1.0,
            SampleGroups: 4));

        using (torch.no_grad())
        {
            torch.manual_seed(3);

            foreach ((string _, Parameter parameter) in network.named_parameters())
            {
                using torch.Tensor noise = torch.randn(parameter.shape, parameter.dtype).mul(0.2);

                parameter.copy_(noise);
            }
        }

        using MosrUpscaler upscaler = new(network, 2, torch.CPU);

        UmbraSprite sprite = RunSingle(upscaler, "transparent", MakeEmptySprite(24, 16, additive: false));

        Assert.That(GetAlpha(sprite), Is.All.EqualTo(0));
    }
}
