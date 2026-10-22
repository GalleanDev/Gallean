using Avalonia;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class SpriteAtlasTests
{
    private static Sprite CreateSprite(SKColor color, int width, int height)
    {
        SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(color);
        SKImage snapshot = surface.Snapshot();
        SKImage image = snapshot.ToRasterImage(ensurePixelData: true);

        return new Sprite(new AssetSpriteMetadata(width, height, width, height, 0, 0, Additive: false), image, Normal: null, Depth: null, Albedo: null, Lights: []);
    }

    private static IReadOnlyList<SKColor> ReadColors(SKSurface surface, IReadOnlyList<SKPointI> points)
    {
        using SKImage snapshot = surface.Snapshot();
        using SKPixmap pixels = snapshot.PeekPixels();
        List<SKColor> colors = [];

        foreach (SKPointI point in points)
        {
            colors.Add(pixels.GetPixelColor(point.X, point.Y));
        }

        return colors;
    }

    [Test]
    public void DrawTest()
    {
        Sprite redSprite = CreateSprite(SKColors.Red, 2, 2);
        Sprite blueFrame = CreateSprite(SKColors.Blue, 4, 4);
        Sprite limeFrame = CreateSprite(SKColors.Lime, 4, 4);

        AtlasTexture.RegionKey redRegion = new(redSprite.Image, redSprite.Metadata.Width, redSprite.Metadata.Height);
        AtlasTexture.RegionKey blueRegion = new(blueFrame.Image, blueFrame.Metadata.Width, blueFrame.Metadata.Height);
        AtlasTexture.RegionKey limeRegion = new(limeFrame.Image, limeFrame.Metadata.Width, limeFrame.Metadata.Height);
        SpriteAtlasSource.Plan plan = SpriteAtlasSource.CreatePlan(
        [
            new SpriteAtlasSource.StaticSprite(redRegion, 20, 20, 2),
            new SpriteAtlasSource.AnimatedSprite([blueRegion, limeRegion], [blueFrame, limeFrame], new AssetAnimationMetadata(2, 4, 4, 0, 0, TriggerFrame: null, SecondTriggerFrame: null), new Point(41, 44))
        ]);
        using AtlasTexture texture = AtlasTexture.Build(plan.Regions);
        SpriteAtlasSource source = SpriteAtlasSource.FromPlan(plan, texture);
        SpriteAtlas atlas = source.CreateAtlas();
        using SKPaint paint = new() { IsAntialias = false };
        SKImageInfo info = new(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        Rect visible = new(0, 0, 64, 64);
        IReadOnlyList<SKPointI> samplePoints =
        [
            new(20, 20),
            new(40, 41)
        ];

        surface.Canvas.Clear(SKColors.Transparent);
        atlas.UpdateAnimations(frameIndex: 0, visible);
        atlas.Draw(surface.Canvas, paint, visible);
        IReadOnlyList<SKColor> firstFrame = ReadColors(surface, samplePoints);

        surface.Canvas.Clear(SKColors.Transparent);
        atlas.UpdateAnimations(frameIndex: 1, visible);
        atlas.Draw(surface.Canvas, paint, visible);
        IReadOnlyList<SKColor> secondFrame = ReadColors(surface, samplePoints);

        Assert.That(atlas.SpriteCount, Is.EqualTo(2));
        Assert.That(firstFrame, Is.EqualTo([SKColors.Red, SKColors.Blue]));
        Assert.That(secondFrame, Is.EqualTo([SKColors.Red, SKColors.Lime]));
    }

    [Test]
    public void DrawTintedTest()
    {
        Sprite whiteSprite = CreateSprite(SKColors.White, 2, 2);

        AtlasTexture.RegionKey whiteRegion = new(whiteSprite.Image, whiteSprite.Metadata.Width, whiteSprite.Metadata.Height);
        SpriteAtlasSource.Plan plan = SpriteAtlasSource.CreatePlan([new SpriteAtlasSource.StaticSprite(whiteRegion, 20, 20, 2)]);
        using AtlasTexture texture = AtlasTexture.Build(plan.Regions);
        SpriteAtlasSource source = SpriteAtlasSource.FromPlan(plan, texture);
        SpriteAtlas atlas = source.CreateAtlas();
        using SKPaint paint = new() { IsAntialias = false };
        SKImageInfo info = new(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        Rect visible = new(0, 0, 64, 64);
        IReadOnlyList<SKPointI> samplePoints = [new(20, 20)];

        surface.Canvas.Clear(SKColors.Transparent);
        atlas.Draw(surface.Canvas, paint, visible, tints: [new SKColor(255, 0, 0)]);
        IReadOnlyList<SKColor> tinted = ReadColors(surface, samplePoints);

        Assert.That(tinted, Is.EqualTo([SKColors.Red]));
    }

    [Test]
    public void IndependentAtlasBuffersTest()
    {
        Sprite blueFrame = CreateSprite(SKColors.Blue, 4, 4);
        Sprite limeFrame = CreateSprite(SKColors.Lime, 4, 4);

        AtlasTexture.RegionKey blueRegion = new(blueFrame.Image, blueFrame.Metadata.Width, blueFrame.Metadata.Height);
        AtlasTexture.RegionKey limeRegion = new(limeFrame.Image, limeFrame.Metadata.Width, limeFrame.Metadata.Height);
        SpriteAtlasSource.Plan plan = SpriteAtlasSource.CreatePlan(
        [
            new SpriteAtlasSource.AnimatedSprite([blueRegion, limeRegion], [blueFrame, limeFrame], new AssetAnimationMetadata(2, 4, 4, 0, 0, TriggerFrame: null, SecondTriggerFrame: null), new Point(41, 44))
        ]);
        using AtlasTexture texture = AtlasTexture.Build(plan.Regions);
        SpriteAtlasSource source = SpriteAtlasSource.FromPlan(plan, texture);
        SpriteAtlas firstAtlas = source.CreateAtlas();
        SpriteAtlas secondAtlas = source.CreateAtlas();
        using SKPaint paint = new() { IsAntialias = false };
        SKImageInfo info = new(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface firstSurface = SKSurface.Create(info);
        using SKSurface secondSurface = SKSurface.Create(info);
        Rect visible = new(0, 0, 64, 64);
        IReadOnlyList<SKPointI> samplePoints = [new(40, 41)];

        firstAtlas.UpdateAnimations(frameIndex: 1, visible);
        secondAtlas.UpdateAnimations(frameIndex: 0, visible);

        firstSurface.Canvas.Clear(SKColors.Transparent);
        firstAtlas.Draw(firstSurface.Canvas, paint, visible);
        secondSurface.Canvas.Clear(SKColors.Transparent);
        secondAtlas.Draw(secondSurface.Canvas, paint, visible);

        Assert.That(firstAtlas, Is.Not.SameAs(secondAtlas));
        Assert.That(ReadColors(firstSurface, samplePoints), Is.EqualTo([SKColors.Lime]));
        Assert.That(ReadColors(secondSurface, samplePoints), Is.EqualTo([SKColors.Blue]));
    }

    [Test]
    public void RepeatedDrawTest()
    {
        Sprite blueFrame = CreateSprite(SKColors.Blue, 4, 4);
        Sprite limeFrame = CreateSprite(SKColors.Lime, 4, 4);

        AtlasTexture.RegionKey blueRegion = new(blueFrame.Image, blueFrame.Metadata.Width, blueFrame.Metadata.Height);
        AtlasTexture.RegionKey limeRegion = new(limeFrame.Image, limeFrame.Metadata.Width, limeFrame.Metadata.Height);
        SpriteAtlasSource.Plan plan = SpriteAtlasSource.CreatePlan(
        [
            new SpriteAtlasSource.AnimatedSprite([blueRegion, limeRegion], [blueFrame, limeFrame], new AssetAnimationMetadata(2, 4, 4, 0, 0, TriggerFrame: null, SecondTriggerFrame: null), new Point(41, 44))
        ]);
        using AtlasTexture texture = AtlasTexture.Build(plan.Regions);
        SpriteAtlasSource source = SpriteAtlasSource.FromPlan(plan, texture);
        SpriteAtlas atlas = source.CreateAtlas();
        using SKPaint paint = new() { IsAntialias = false };
        SKImageInfo info = new(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface firstSurface = SKSurface.Create(info);
        using SKSurface secondSurface = SKSurface.Create(info);
        Rect visible = new(0, 0, 64, 64);
        IReadOnlyList<SKPointI> samplePoints = [new(40, 41)];

        atlas.UpdateAnimations(frameIndex: 1, visible);

        firstSurface.Canvas.Clear(SKColors.Transparent);
        atlas.Draw(firstSurface.Canvas, paint, visible);
        secondSurface.Canvas.Clear(SKColors.Transparent);
        atlas.Draw(secondSurface.Canvas, paint, visible);

        Assert.That(ReadColors(firstSurface, samplePoints), Is.EqualTo([SKColors.Lime]));
        Assert.That(ReadColors(secondSurface, samplePoints), Is.EqualTo([SKColors.Lime]));
    }

    [Test]
    public void PlanRegionsTest()
    {
        Sprite redSprite = CreateSprite(SKColors.Red, 2, 2);
        Sprite blueSprite = CreateSprite(SKColors.Blue, 4, 4);

        AtlasTexture.RegionKey redRegion = new(redSprite.Image, redSprite.Metadata.Width, redSprite.Metadata.Height);
        AtlasTexture.RegionKey blueRegion = new(blueSprite.Image, blueSprite.Metadata.Width, blueSprite.Metadata.Height);

        SpriteAtlasSource.Plan initial = SpriteAtlasSource.CreatePlan([new SpriteAtlasSource.StaticSprite(redRegion, 20, 20, 2)]);
        SpriteAtlasSource.Plan moved = SpriteAtlasSource.CreatePlan([new SpriteAtlasSource.StaticSprite(redRegion, 30, 25, 2)]);
        SpriteAtlasSource.Plan added = SpriteAtlasSource.CreatePlan(
        [
            new SpriteAtlasSource.StaticSprite(redRegion, 20, 20, 2),
            new SpriteAtlasSource.StaticSprite(blueRegion, 30, 30, 4)
        ]);

        Assert.That(initial.Regions, Is.EquivalentTo([redRegion]));
        Assert.That(moved.Regions, Is.EquivalentTo([redRegion]));
        Assert.That(added.Regions, Is.EquivalentTo([redRegion, blueRegion]));
    }

    [Test]
    public void ChooseNarrowWidthTest()
    {
        Sprite firstSprite = CreateSprite(SKColors.Red, 4, 4);
        Sprite secondSprite = CreateSprite(SKColors.Blue, 4, 4);

        AtlasTexture.RegionKey firstRegion = new(firstSprite.Image, firstSprite.Metadata.Width, firstSprite.Metadata.Height);
        AtlasTexture.RegionKey secondRegion = new(secondSprite.Image, secondSprite.Metadata.Width, secondSprite.Metadata.Height);

        HashSet<AtlasTexture.RegionKey> regions = [firstRegion, secondRegion];

        using AtlasTexture texture = AtlasTexture.Build(regions);

        Assert.That(texture.Image.Width, Is.EqualTo(256));
    }

    [Test]
    public void NoRegionsTest()
    {
        HashSet<AtlasTexture.RegionKey> regions = [];

        using AtlasTexture texture = AtlasTexture.Build(regions);

        Assert.That(texture.Regions, Is.Empty);
        Assert.That(texture.Image.Width, Is.EqualTo(256));
        Assert.That(texture.Image.Height, Is.EqualTo(2));
    }
}
