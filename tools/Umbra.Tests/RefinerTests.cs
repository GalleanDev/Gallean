using TorchSharp;
using TorchSharp.Modules;

namespace Umbra.Tests;

public sealed class RefinerTests
{
    private static Refiner CreateRefiner(
        RefineOptions options,
        RefinePromptConfiguration prompts,
        RefineRepaintConfiguration repaint,
        IReadOnlyDictionary<UmbraSequenceId, RefineConfiguration> configurations)
    {
        torch.manual_seed(3);

        LtxDit transformer = new(new LtxDitOptions(
            HiddenSize: 16,
            Heads: 2,
            Depth: 1,
            InputChannels: 8,
            OutputChannels: 8,
            ContextChannels: 16,
            RopeTheta: 10000.0,
            RopeMaxPositions: [20, 2048, 2048],
            TimestepScale: 1000.0));
        LtxVae vae = new(new LtxVaeOptions(
            LatentChannels: 8,
            BaseChannels: 4,
            PatchSize: 4,
            Blocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1)
            ],
            DecoderBlocks:
            [
                new LtxVaeBlock(LtxVaeBlockType.Residual, 1),
                new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
                new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
                new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1)
            ],
            ChunkBytes: LtxCausalConv3d.DefaultChunkBytes));
        LtxLatentUpsampler upsampler = new(8, 32, 1);

        Randomize(transformer, 0.2);
        Randomize(vae, 0.2);
        Randomize(upsampler, 0.2);

        RefineContext contexts = new(
            new Dictionary<string, torch.Tensor> { ["empty"] = torch.randn(1, 3, 16), ["painted"] = torch.randn(1, 3, 16) },
            prompts);

        return new Refiner(transformer, vae, upsampler, contexts, repaint, configurations, options);
    }

    private static void Randomize(torch.nn.Module module, double scale)
    {
        using (torch.no_grad())
        {
            foreach ((string _, Parameter parameter) in module.named_parameters())
            {
                using torch.Tensor drawn = torch.randn(parameter.shape, parameter.dtype).mul(scale);

                parameter.copy_(drawn);
            }
        }
    }

    private static UmbraSprite MakeSprite(int width, int height, byte level, bool additive, int offsetX, int offsetY)
    {
        byte[] pixels = new byte[width * height * 4];

        for (int index = 0; index < width * height; index++)
        {
            pixels[(index * 4) + 0] = level;
            pixels[(index * 4) + 1] = (byte)(255 - level);
            pixels[(index * 4) + 2] = 128;
            pixels[(index * 4) + 3] = 255;
        }

        return new UmbraSprite(
            new UmbraSpriteMetadata(width, height, width, height, offsetX, offsetY, additive), pixels);
    }

    private static UmbraSequence MakeSequence(string id, int frames, int width, int height)
    {
        List<UmbraFrame> sequenceFrames = [];

        for (int index = 0; index < frames; index++)
        {
            sequenceFrames.Add(new UmbraFrame(TestHelpers.MakeSpriteHash($"{id}-{index}"), MakeSprite(width, height, (byte)(40 + (index * 30)), false, 0, 0)));
        }

        return new UmbraSequence(new UmbraSequenceId(id), sequenceFrames);
    }

    private static List<string> FormatUpscaledFrames(IEnumerable<UmbraUpscaledFrame> frames)
    {
        List<string> rows = [];

        foreach (UmbraUpscaledFrame frame in frames)
        {
            UmbraSpriteMetadata metadata = frame.Sprite.Metadata;

            rows.Add($"{TestHelpers.FormatSpriteHash(frame.Hash)} {metadata.Width}x{metadata.Height} px {metadata.PixelWidth}x{metadata.PixelHeight} bytes {frame.Sprite.Pixels.Length}");
        }

        return rows;
    }

    private static byte[] ReadColorValues(UmbraUpscaledFrame frame)
    {
        HashSet<byte> values = [];
        ReadOnlySpan<byte> pixels = frame.Sprite.Pixels.Span;

        for (int index = 0; index < pixels.Length; index++)
        {
            if (index % 4 != 3)
            {
                values.Add(pixels[index]);
            }
        }

        return [.. values.Order()];
    }

    private static byte[] ReadAlphaValues(UmbraUpscaledFrame frame)
    {
        HashSet<byte> values = [];
        ReadOnlySpan<byte> pixels = frame.Sprite.Pixels.Span;

        for (int index = 3; index < pixels.Length; index += 4)
        {
            values.Add(pixels[index]);
        }

        return [.. values.Order()];
    }

    [Test]
    public void UpscaleTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 3, 6, 5)])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo([
            "a-0 6x5 px 12x10 bytes 480",
            "a-1 6x5 px 12x10 bytes 480",
            "a-2 6x5 px 12x10 bytes 480"
        ]));
    }

    [Test]
    public void DeterministicTest()
    {
        using Refiner first = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });
        using Refiner second = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        List<UmbraUpscaledFrame> firstFrames = [.. first.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 2, 6, 5)])])];
        List<UmbraUpscaledFrame> secondFrames = [.. second.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 2, 6, 5)])])];

        Assert.That(secondFrames[1].Sprite.Pixels.ToArray(), Is.EqualTo(firstFrames[1].Sprite.Pixels.ToArray()));
    }

    [Test]
    public void AlphaTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1, ChromaKeyScreenThreshold = 2.0, ChromaKeyObjectThreshold = 0.0 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        UmbraSprite sprite = MakeSprite(4, 4, 40, false, 0, 0);

        byte[] pixels = sprite.Pixels.ToArray();
        pixels[3] = 0;
        UmbraSprite alpha = sprite with { Pixels = pixels };

        UmbraSequence sequence = new(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), alpha)]);

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(ReadAlphaValues(frames[0]), Is.EqualTo(new byte[] { 0, 31, 165 }));
    }

    [Test]
    public void OpaqueAlphaTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1, ChromaKeyScreenThreshold = 0.0 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 1, 4, 4)])])];

        Assert.That(ReadAlphaValues(frames[0]), Is.EqualTo(new byte[] { 255 }));
    }

    [Test]
    public void OpaqueFrameTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1, ChromaKeyScreenThreshold = 2.0, ChromaKeyObjectThreshold = 0.0 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 1, 4, 4)])])];

        Assert.That(ReadAlphaValues(frames[0]), Is.EqualTo(new byte[] { 255 }));
    }

    [Test]
    public void AdditiveTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        UmbraSequence sequence = new(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), MakeSprite(4, 4, 90, true, 0, 0))]);

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo(["a-0 4x4 px 8x8 bytes 256"]));
        Assert.That(ReadAlphaValues(frames[0]), Is.EqualTo(new byte[] { 0 }));
    }

    [Test]
    public void SharedFrameTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128), [new UmbraSequenceId("b")] = new(128) });

        UmbraSequence first = MakeSequence("a", 2, 6, 5);
        UmbraSequence second = new(new UmbraSequenceId("b"), [first.Frames[1], new UmbraFrame(TestHelpers.MakeSpriteHash("b-1"), MakeSprite(6, 5, 200, false, 0, 0))]);

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [first]), new UmbraSequenceGroup(new UmbraSequenceId("b"), [second])])];
        List<string> hashes = [];

        foreach (UmbraUpscaledFrame frame in frames)
        {
            hashes.Add(TestHelpers.FormatSpriteHash(frame.Hash));
        }

        Assert.That(hashes, Is.EqualTo(["a-0", "a-1", "b-1"]));
    }

    [Test]
    public void LoopedClipTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 5 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 2, 6, 5)])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo([
            "a-0 6x5 px 12x10 bytes 480",
            "a-1 6x5 px 12x10 bytes 480"
        ]));
    }

    [Test]
    public void GroupedAnimationsTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        UmbraSequence idle = MakeSequence("a", 3, 6, 5);
        UmbraSequence attack = MakeSequence("b", 4, 6, 5);

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [idle, attack])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo([
            "a-0 6x5 px 12x10 bytes 480",
            "a-1 6x5 px 12x10 bytes 480",
            "a-2 6x5 px 12x10 bytes 480",
            "b-0 6x5 px 12x10 bytes 480",
            "b-1 6x5 px 12x10 bytes 480",
            "b-2 6x5 px 12x10 bytes 480",
            "b-3 6x5 px 12x10 bytes 480"
        ]));
    }

    [Test]
    public void RepeatedFrameTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        UmbraSequence source = MakeSequence("a", 2, 6, 5);
        UmbraSequence sequence = new(new UmbraSequenceId("a"), [source.Frames[0], source.Frames[1], source.Frames[0]]);

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo([
            "a-0 6x5 px 12x10 bytes 480",
            "a-1 6x5 px 12x10 bytes 480"
        ]));
    }

    [Test]
    public void StillClipTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing), new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 1, 4, 4)])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo(["a-0 4x4 px 8x8 bytes 256"]));
    }

    [Test]
    public void FrameRepeatsTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 2, 4, 4)])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo([
            "a-0 4x4 px 8x8 bytes 256",
            "a-1 4x4 px 8x8 bytes 256"
        ]));
    }

    [Test]
    public void BlackAdditiveTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        UmbraSequence sequence = new(new UmbraSequenceId("a"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), new UmbraSprite(new UmbraSpriteMetadata(4, 4, 4, 4, 0, 0, true), new byte[64]))
        ]);

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo(["a-0 4x4 px 8x8 bytes 256"]));
        Assert.That(ReadColorValues(frames[0]), Is.EqualTo(new byte[] { 0 }));
    }

    [Test]
    public void StillAnimationTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128) });

        UmbraSequence still = new(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), MakeSprite(6, 5, 40, false, 0, 0)), new UmbraFrame(TestHelpers.MakeSpriteHash("a-1"), MakeSprite(6, 5, 40, false, 0, 0))]);

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [still, MakeSequence("b", 2, 6, 5)])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo([
            "a-0 6x5 px 12x10 bytes 480",
            "a-1 6x5 px 12x10 bytes 480",
            "b-0 6x5 px 12x10 bytes 480",
            "b-1 6x5 px 12x10 bytes 480"
        ]));
    }

    [Test]
    public void MinClipDimensionTest()
    {
        using Refiner refiner = CreateRefiner(new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 }, new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()), new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>()), new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(16) });

        List<UmbraUpscaledFrame> frames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [MakeSequence("a", 2, 6, 5)])])];

        Assert.That(FormatUpscaledFrames(frames), Is.EqualTo([
            "a-0 6x5 px 12x10 bytes 480",
            "a-1 6x5 px 12x10 bytes 480"
        ]));
    }

    [Test]
    public void RepaintTest()
    {
        using Refiner refiner = CreateRefiner(
            new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 },
            new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>()),
            new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>> { [new UmbraSequenceId("b")] = [new RefineRegion(0, 0, 4, 6)] }),
            new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128), [new UmbraSequenceId("b")] = new(128) });

        UmbraSprite sprite = MakeSprite(8, 6, 40, false, 0, 0);

        List<UmbraUpscaledFrame> plainFrames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [new UmbraSequence(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), sprite)])])])];
        List<UmbraUpscaledFrame> repaintedFrames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("b"), [new UmbraSequence(new UmbraSequenceId("b"), [new UmbraFrame(TestHelpers.MakeSpriteHash("b-0"), sprite)])])])];

        Assert.That(FormatUpscaledFrames(repaintedFrames), Is.EqualTo(["b-0 8x6 px 16x12 bytes 768"]));
        Assert.That(repaintedFrames[0].Sprite.Pixels.ToArray(), Is.Not.EqualTo(plainFrames[0].Sprite.Pixels.ToArray()));
    }

    [Test]
    public void PromptRegionTest()
    {
        using Refiner refiner = CreateRefiner(
            new RefineOptions(torch.CPU, torch.ScalarType.Float32, torch.ScalarType.Float32, LtxSchedule.Detailing) with { MinClipFrames = 1 },
            new RefinePromptConfiguration(new Dictionary<UmbraSequenceId, RefinePrompt>
            {
                [new UmbraSequenceId("b")] = new(
                    new RefinePromptId("empty"),
                    [new RefinePromptRegion(new RefineRegion(0, 0, 4, 6), new RefinePromptId("painted"))])
            }),
            new RefineRepaintConfiguration(new Dictionary<UmbraSequenceId, IReadOnlyList<RefineRegion>>
            {
                [new UmbraSequenceId("a")] = [new RefineRegion(0, 0, 4, 6)],
                [new UmbraSequenceId("b")] = [new RefineRegion(0, 0, 4, 6)]
            }),
            new Dictionary<UmbraSequenceId, RefineConfiguration> { [new UmbraSequenceId("a")] = new(128), [new UmbraSequenceId("b")] = new(128) });

        UmbraSprite sprite = MakeSprite(8, 6, 40, false, 0, 0);

        List<UmbraUpscaledFrame> plainFrames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [new UmbraSequence(new UmbraSequenceId("a"), [new UmbraFrame(TestHelpers.MakeSpriteHash("a-0"), sprite)])])])];
        List<UmbraUpscaledFrame> promptedFrames = [.. refiner.Run([new UmbraSequenceGroup(new UmbraSequenceId("b"), [new UmbraSequence(new UmbraSequenceId("b"), [new UmbraFrame(TestHelpers.MakeSpriteHash("b-0"), sprite)])])])];

        Assert.That(FormatUpscaledFrames(promptedFrames), Is.EqualTo(["b-0 8x6 px 16x12 bytes 768"]));
        Assert.That(promptedFrames[0].Sprite.Pixels.ToArray(), Is.Not.EqualTo(plainFrames[0].Sprite.Pixels.ToArray()));
    }
}
