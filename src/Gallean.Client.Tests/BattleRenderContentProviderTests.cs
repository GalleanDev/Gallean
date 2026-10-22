using Avalonia;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class BattleRenderContentProviderTests
{
    private static SKImage MakeGradientImage(int width, int height, SKColor top, SKColor bottom)
    {
        SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using SKSurface surface = SKSurface.Create(info);
        using SKShader gradient = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, height), [top, bottom], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = gradient };

        surface.Canvas.DrawRect(SKRect.Create(width, height), paint);

        SKImage snapshot = surface.Snapshot();

        return snapshot.ToRasterImage(ensurePixelData: true);
    }

    [Test]
    public void GroundTest()
    {
        using SKImage image = MakeGradientImage(288, 120, SKColors.Gray, SKColors.Gray);
        using SKImage normals = MakeGradientImage(288, 120, SKColors.Blue, SKColors.Blue);
        using SKImage depth = MakeGradientImage(288, 120, SKColors.White, SKColors.Black);
        using SKImage albedo = MakeGradientImage(288, 120, SKColors.Gray, SKColors.Gray);

        Sprite background = new(
            new AssetSpriteMetadata(1440, 600, 288, 120, -245, 0, Additive: false),
            image,
            normals,
            depth,
            albedo,
            Lights: []);

        using TestAssetProvider assets = new TestAssetProviderBuilder()
            .ResolveSprite(_ => background)
            .ResolveAnimation(_ => null)
            .ResolveSound(_ => null)
            .Build();

        BattleRenderState state = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Undead), 1),
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleGround ground = new BattleRenderContentProvider().Get(state, assets).Ground;

        Assert.That(ground.Bounds, Is.EqualTo(new Rect(-245.0, 0.0, 1440.0, 600.0)));
        Assert.That(ground.Columns, Is.EqualTo(360));
        Assert.That(ground.Rows, Is.EqualTo(150));
        Assert.That(ground.DepthScale, Is.EqualTo(829.89).Within(0.01));
        Assert.That(ground.GetDistance(new Point(475.0, 0.0)), Is.EqualTo(826.63).Within(0.01));
        Assert.That(ground.GetDistance(new Point(475.0, 599.0)), Is.EqualTo(3.25).Within(0.01));
    }
}
