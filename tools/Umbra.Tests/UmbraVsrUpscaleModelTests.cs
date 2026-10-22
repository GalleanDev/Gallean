using Gallean.Tests.Common;
using TorchSharp;

namespace Umbra.Tests;

public sealed class UmbraVsrUpscaleModelTests
{
    private static string WriteTestUpscaleModel(Sandbox sb)
    {
        string directory = sb.CreateDirectory("models");
        string path = Path.Combine(directory, UmbraVsrUpscaleModel.FileName);

        using BasicVsrNetwork network = new(8, 1, sb.RootPath, 128);
        Dictionary<string, torch.Tensor> state = network.state_dict();
        Dictionary<string, torch.Tensor> payload = [];

        foreach (KeyValuePair<string, torch.Tensor> parameter in state)
        {
            payload[$"generator.{parameter.Key}"] = parameter.Value;
        }

        SafeTensors.SaveStateDict(path, payload);

        return directory;
    }

    private static UmbraSprite MakeSprite(int pixelWidth, int pixelHeight, byte[] color, bool additive)
    {
        UmbraSpriteMetadata metadata = new(pixelWidth, pixelHeight, pixelWidth, pixelHeight, 0, 0, additive);

        byte[] pixels = new byte[pixelWidth * pixelHeight * 4];

        for (int index = 0; index < pixelWidth * pixelHeight; index++)
        {
            color.CopyTo(pixels, index * 4);
        }

        return new UmbraSprite(metadata, pixels);
    }

    private static List<string> FormatUpscales(IReadOnlyList<UmbraUpscaledFrame> upscales)
    {
        List<string> rows = [];

        foreach (UmbraUpscaledFrame upscale in upscales)
        {
            UmbraSpriteMetadata metadata = upscale.Sprite.Metadata;

            rows.Add(
                $"{TestHelpers.FormatSpriteHash(upscale.Hash)} {metadata.Width}x{metadata.Height} px {metadata.PixelWidth}x{metadata.PixelHeight} " +
                $"bytes {upscale.Sprite.Pixels.Length}");
        }

        return rows;
    }

    private static List<byte> ReadAlphaValues(UmbraSprite sprite)
    {
        SortedSet<byte> values = [];

        for (int offset = 3; offset < sprite.Pixels.Length; offset += 4)
        {
            values.Add(sprite.Pixels.Span[offset]);
        }

        return [.. values];
    }

    [Test]
    public void UpscaleTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = WriteTestUpscaleModel(sb);

        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), MakeSprite(2, 2, [10, 20, 30, 255], additive: false)),
            new UmbraFrame(TestHelpers.MakeSpriteHash("bbbb"), MakeSprite(3, 3, [40, 50, 60, 255], additive: false))
        ]);

        using UmbraVsrUpscaleModel model = new(modelDirectory, sb.RootPath, UmbraDevice.Cpu);

        Assert.That(FormatUpscales([.. model.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])]), Is.EqualTo([
            "aaaa 2x2 px 4x4 bytes 64",
            "bbbb 3x3 px 6x6 bytes 144"
        ]));
    }

    [Test]
    public void WindowedClipTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = WriteTestUpscaleModel(sb);

        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/attack/attacker"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), MakeSprite(2, 2, [10, 20, 30, 255], additive: false)),
            new UmbraFrame(TestHelpers.MakeSpriteHash("bbbb"), MakeSprite(3, 3, [40, 50, 60, 255], additive: false)),
            new UmbraFrame(TestHelpers.MakeSpriteHash("cccc"), MakeSprite(4, 4, [70, 80, 90, 255], additive: false)),
            new UmbraFrame(TestHelpers.MakeSpriteHash("dddd"), MakeSprite(5, 5, [100, 110, 120, 255], additive: false)),
            new UmbraFrame(TestHelpers.MakeSpriteHash("eeee"), MakeSprite(6, 6, [130, 140, 150, 255], additive: false))
        ]);

        using UmbraVsrUpscaleModel model = new(modelDirectory, sb.RootPath, UmbraDevice.Cpu);

        Assert.That(FormatUpscales([.. model.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])]), Is.EqualTo([
            "aaaa 2x2 px 4x4 bytes 64",
            "bbbb 3x3 px 6x6 bytes 144",
            "cccc 4x4 px 8x8 bytes 256",
            "dddd 5x5 px 10x10 bytes 400",
            "eeee 6x6 px 12x12 bytes 576"
        ]));
    }

    [Test]
    public void AdditiveTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = WriteTestUpscaleModel(sb);

        UmbraSequence sequence = new(new UmbraSequenceId("battle-effect-attack/x"), [
            new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), MakeSprite(2, 2, [10, 20, 30, 0], additive: true))
        ]);

        using UmbraVsrUpscaleModel model = new(modelDirectory, sb.RootPath, UmbraDevice.Cpu);

        IReadOnlyList<UmbraUpscaledFrame> upscales = [.. model.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [sequence])])];

        Assert.That(FormatUpscales(upscales), Is.EqualTo([
            "aaaa 2x2 px 4x4 bytes 64"
        ]));

        Assert.That(ReadAlphaValues(upscales[0].Sprite), Is.EqualTo(new byte[] { 0 }));
    }

    [Test]
    public void SharedFrameTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = WriteTestUpscaleModel(sb);

        UmbraSprite shared = MakeSprite(2, 2, [10, 20, 30, 255], additive: false);

        UmbraSequence first = new(new UmbraSequenceId("stack-unit/x"), [new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), shared)]);
        UmbraSequence second = new(new UmbraSequenceId("stack-unit/y"), [new UmbraFrame(TestHelpers.MakeSpriteHash("aaaa"), shared)]);

        using UmbraVsrUpscaleModel model = new(modelDirectory, sb.RootPath, UmbraDevice.Cpu);

        Assert.That(FormatUpscales([.. model.Run([new UmbraSequenceGroup(new UmbraSequenceId("a"), [first]), new UmbraSequenceGroup(new UmbraSequenceId("b"), [second])])]), Is.EqualTo([
            "aaaa 2x2 px 4x4 bytes 64"
        ]));
    }

    [Test]
    public void MissingModelTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = sb.CreateDirectory("models");

        Assert.Throws<FileNotFoundException>(() =>
        {
            new UmbraVsrUpscaleModel(modelDirectory, sb.RootPath, UmbraDevice.Cpu);
        });
    }

    [Test]
    public void MissingModelDirectoryTest()
    {
        using Sandbox sb = Sandbox.Create();

        Assert.Throws<DirectoryNotFoundException>(() =>
        {
            new UmbraVsrUpscaleModel(sb.GetPath("absent"), sb.RootPath, UmbraDevice.Cpu);
        });
    }
}
