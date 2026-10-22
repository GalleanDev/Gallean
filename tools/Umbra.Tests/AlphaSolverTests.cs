using TorchSharp;
using TorchSharp.Modules;

namespace Umbra.Tests;

public sealed class AlphaSolverTests
{
    private sealed class DataProvider(IReadOnlyList<UmbraSequence> sequences) : IUmbraDataProvider
    {
        public UmbraSprite GetUpscaledSprite(UmbraFrame frame)
        {
            foreach (UmbraSequence sequence in sequences)
            {
                foreach (UmbraFrame upscaled in sequence.Frames)
                {
                    if (upscaled.Hash == frame.Hash)
                    {
                        return upscaled.Sprite;
                    }
                }
            }

            throw new KeyNotFoundException($"No upscaled frame for '{TestHelpers.FormatSpriteHash(frame.Hash)}'.");
        }
    }

    private static AlphaSolver CreateAlphaSolver(AlphaOptions options)
    {
        Dictionary<ChromaKey, CorridorKeyFormer> keyFormers = new()
        {
            [ChromaKey.Blue] = CreateKeyFormer(options.ImageSize, 3),
            [ChromaKey.Green] = CreateKeyFormer(options.ImageSize, 4)
        };

        return new AlphaSolver(keyFormers, options);
    }

    private static CorridorKeyFormer CreateKeyFormer(int imageSize, int seed)
    {
        torch.manual_seed(seed);

        CorridorKeyFormer keyFormer = new(new CorridorKeyOptions(
            ImageSize: imageSize,
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

        using (torch.no_grad())
        {
            foreach ((string name, Parameter parameter) in keyFormer.named_parameters())
            {
                using torch.Tensor noise = torch.randn(parameter.shape, parameter.dtype).mul(0.2);

                if (name.EndsWith("running_var", StringComparison.Ordinal))
                {
                    using torch.Tensor variance = noise.abs().add(0.5);

                    parameter.copy_(variance);
                }
                else
                {
                    parameter.copy_(noise);
                }
            }
        }

        return keyFormer;
    }

    private static UmbraSprite MakeSprite(int width, int height, int border, bool additive)
    {
        byte[] pixels = new byte[width * height * 4];

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int index = ((row * width) + column) * 4;
                bool outside = row < border || column < border || row >= height - border || column >= width - border;

                pixels[index + 0] = outside ? (byte)0 : (byte)200;
                pixels[index + 1] = outside ? (byte)0 : (byte)90;
                pixels[index + 2] = outside ? (byte)255 : (byte)60;
                pixels[index + 3] = outside ? (byte)0 : (byte)255;
            }
        }

        return new UmbraSprite(
            new UmbraSpriteMetadata(width, height, width, height, 0, 0, additive), pixels);
    }

    private static UmbraSprite MakeKeySprite(int width, int height, int border)
    {
        byte[] pixels = new byte[width * height * 4];

        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int index = ((row * width) + column) * 4;
                bool outside = row < border || column < border || row >= height - border || column >= width - border;

                pixels[index + 0] = 0;
                pixels[index + 1] = 71;
                pixels[index + 2] = 187;
                pixels[index + 3] = outside ? (byte)0 : (byte)255;
            }
        }

        return new UmbraSprite(
            new UmbraSpriteMetadata(width, height, width, height, 0, 0, false), pixels);
    }

    private static List<string> FormatAlphaRows(UmbraUpscaledFrame frame)
    {
        ReadOnlySpan<byte> pixels = frame.Sprite.Pixels.Span;
        UmbraSpriteMetadata metadata = frame.Sprite.Metadata;
        List<string> rows = [];

        for (int row = 0; row < metadata.PixelHeight; row++)
        {
            char[] cells = new char[metadata.PixelWidth];

            for (int column = 0; column < metadata.PixelWidth; column++)
            {
                cells[column] = pixels[(((row * metadata.PixelWidth) + column) * 4) + 3] switch
                {
                    0 => '.',
                    255 => '#',
                    _ => '+'
                };
            }

            rows.Add(new string(cells));
        }

        return rows;
    }

    [Test]
    public void SolveTest()
    {
        using AlphaSolver solver = CreateAlphaSolver(
            new AlphaOptions(torch.CPU, torch.ScalarType.Float32) with { ImageSize = 32, HintBound = 0 });

        UmbraSequence sequence = new(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), MakeSprite(12, 12, 3, additive: false))]);

        List<UmbraUpscaledFrame> frames = [.. solver.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])], new DataProvider([sequence]))];

        Assert.That(FormatAlphaRows(frames[0]), Is.EqualTo([
            "............",
            "............",
            "............",
            "....+++++...",
            "....+++++...",
            "............",
            "....++..+...",
            "...++++++...",
            "...+.++++...",
            "............",
            "............",
            "............"
        ]));
    }

    [Test]
    public void KeyedUpscaleTest()
    {
        using AlphaSolver solver = CreateAlphaSolver(
            new AlphaOptions(torch.CPU, torch.ScalarType.Float32) with { ImageSize = 32, HintBound = 0 });

        UmbraSequence refined = new(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), MakeSprite(12, 12, 3, additive: false))]);
        UmbraSequence upscaled = new(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), MakeKeySprite(12, 12, 3))]);

        List<UmbraUpscaledFrame> frames = [.. solver.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [refined])], new DataProvider([upscaled]))];

        Assert.That(FormatAlphaRows(frames[0]), Is.EqualTo([
            "............",
            "............",
            "............",
            "...++++++...",
            "...++++++...",
            "...++++++...",
            "...++++++...",
            "...++++++...",
            "...++++++...",
            "............",
            "............",
            "............"
        ]));
    }

    [Test]
    public void OpaqueTest()
    {
        using AlphaSolver solver = CreateAlphaSolver(
            new AlphaOptions(torch.CPU, torch.ScalarType.Float32) { ImageSize = 32 });

        UmbraSprite sprite = MakeSprite(8, 8, border: 0, additive: false);
        UmbraSequence sequence = new(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), sprite)]);

        List<UmbraUpscaledFrame> frames = [.. solver.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])], new DataProvider([sequence]))];

        Assert.That(frames[0].Sprite.Pixels.ToArray(), Is.EqualTo(sprite.Pixels.ToArray()));
    }

    [Test]
    public void AdditiveTest()
    {
        using AlphaSolver solver = CreateAlphaSolver(new AlphaOptions(torch.CPU, torch.ScalarType.Float32) { ImageSize = 32 });

        UmbraSprite sprite = MakeSprite(4, 4, 1, additive: true);
        UmbraSequence sequence = new(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), sprite)]);

        List<UmbraUpscaledFrame> frames = [.. solver.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])], new DataProvider([sequence]))];

        Assert.That(frames[0].Sprite.Pixels.ToArray(), Is.EqualTo(sprite.Pixels.ToArray()));
    }

    [Test]
    public void SharedFrameTest()
    {
        using AlphaSolver solver = CreateAlphaSolver(new AlphaOptions(torch.CPU, torch.ScalarType.Float32) { ImageSize = 32 });

        UmbraFrame shared = new(TestHelpers.MakeSpriteHash("a-0"), MakeSprite(8, 8, 2, additive: false));
        UmbraSequence first = new(new UmbraSequenceId("a"), [shared]);
        UmbraSequence second = new(new UmbraSequenceId("b"), [shared, new UmbraFrame(TestHelpers.MakeSpriteHash("b-1"), MakeSprite(8, 8, 3, additive: false))]);

        List<string> hashes = [];

        foreach (UmbraUpscaledFrame frame in solver.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [first]), new UmbraSequenceGroup(new UmbraSequenceId("b"), [second])], new DataProvider([first, second])))
        {
            hashes.Add(TestHelpers.FormatSpriteHash(frame.Hash));
        }

        Assert.That(hashes, Is.EqualTo(["a-0", "b-1"]));
    }
}
