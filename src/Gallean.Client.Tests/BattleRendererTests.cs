using Avalonia;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class BattleRendererTests
{
    private static SKImage MakeImage(int width, int height, SKColor color)
    {
        SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        using SKSurface surface = SKSurface.Create(info);

        surface.Canvas.Clear(color);

        SKImage snapshot = surface.Snapshot();

        return snapshot.ToRasterImage(ensurePixelData: true);
    }

    [Test]
    public void RigLightsTest()
    {
        using SKImage image = MakeImage(288, 120, SKColors.Gray);
        using SKImage normals = MakeImage(288, 120, SKColors.Blue);
        using SKImage depth = MakeImage(288, 120, SKColors.Black);
        using SKImage albedo = MakeImage(288, 120, SKColors.Gray);

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
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Capital(WorldRace.Undead), 0),
            Units: [],
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday(
            [
                BattleLight.MakeGhostCrystal(new Point(691.0, 44.0), range: 54.0, power: 1.53),
                BattleLight.MakeGhostCrystal(new Point(863.0, 42.0), range: 54.0, power: 1.48),
                BattleLight.MakeGhostCrystal(new Point(812.0, 17.0), range: 126.0, power: 1.42)
            ]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleRenderContent content = new BattleRenderContentProvider().Get(state, assets);

        using SKSurface surface = SKSurface.Create(new SKImageInfo(950, 600, SKColorType.Bgra8888, SKAlphaType.Premul));

        Assert.DoesNotThrow(() => new BattleRenderer().Render(
            surface.Canvas,
            content,
            state,
            null,
            null,
            assets,
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.Zero,
            renderResolution: 1.0f,
            lightOverlay: false,
            hitboxOverlay: false));
    }
}
