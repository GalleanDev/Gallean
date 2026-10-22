namespace Umbra.Tests;

public sealed class UpscaleClipBuilderTests
{
    private static UmbraFrame MakeFrame(string hash, int x, int y, int width, int height, int scale)
    {
        UmbraSpriteMetadata metadata = new(width, height, width * scale, height * scale, x, y, Additive: false);

        return new UmbraFrame(TestHelpers.MakeSpriteHash(hash), new UmbraSprite(metadata, new byte[width * scale * height * scale * 4]));
    }

    private static List<string> FormatClips(IReadOnlyList<UpscaleClip> clips)
    {
        List<string> rows = [];

        foreach (UpscaleClip clip in clips)
        {
            rows.Add($"canvas {clip.CanvasWidth}x{clip.CanvasHeight}");

            foreach (UpscaleClipFrame clipFrame in clip.Frames)
            {
                rows.Add($"  {TestHelpers.FormatSpriteHash(clipFrame.Frame.Hash)} at {clipFrame.Left},{clipFrame.Top}");
            }
        }

        return rows;
    }

    [Test]
    public void CanvasTest()
    {
        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            MakeFrame("a", 0, 0, 40, 30, 1),
            MakeFrame("b", 5, 2, 36, 28, 1),
            MakeFrame("c", 2, 1, 30, 26, 1)
        ]);

        Assert.That(FormatClips(UpscaleClipBuilder.Build([sequence])), Is.EqualTo([
            "canvas 41x30",
            "  a at 0,0",
            "  b at 5,2",
            "  c at 2,1"
        ]));
    }

    [Test]
    public void DistinctFramesTest()
    {
        UmbraFrame first = MakeFrame("a", 0, 0, 8, 8, 1);
        UmbraFrame second = MakeFrame("b", 4, 0, 8, 8, 1);
        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [first, second, first, second, first]);

        Assert.That(FormatClips(UpscaleClipBuilder.Build([sequence])), Is.EqualTo([
            "canvas 12x8",
            "  a at 0,0",
            "  b at 4,0"
        ]));
    }

    [Test]
    public void ScaledOffsetsTest()
    {
        UmbraSequence sequence = new(new UmbraSequenceId("battle-unit/x/idle/attacker"), [
            MakeFrame("a", 0, 0, 10, 10, 2),
            MakeFrame("b", 3, 4, 10, 10, 2)
        ]);

        Assert.That(FormatClips(UpscaleClipBuilder.Build([sequence])), Is.EqualTo([
            "canvas 26x28",
            "  a at 0,0",
            "  b at 6,8"
        ]));
    }
}
