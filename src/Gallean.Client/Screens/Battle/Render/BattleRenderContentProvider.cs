using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderContentProvider
{
    private readonly Part<BattleRenderState, AssetSpriteKey.BattleBackground, BattleGround> groundPart = new(static state => state.Background);
    private readonly Part<BattleRenderState, AssetSpriteKey.BattleBackground, BattleUnitShaderProgram> unitShaderPart = new(static state => state.Background);
    private readonly Part<BattleRenderState, AssetSpriteKey.BattleBackground, BattleBackgroundShaderProgram> backgroundShaderPart = new(static state => state.Background);
    private readonly Part<BattleRenderState, AssetSpriteKey.BattleBackground, IReadOnlyList<BattleFluidShaderProgram>> fluidShaderPart = new(static state => state.Background);
    private readonly Part<BattleRenderState, AssetSpriteKey.BattleBackground, BattleRainShaderProgram> rainShaderPart = new(static state => state.Background);
    private readonly Part<BattleRenderState, AssetSpriteKey.BattleBackground, SKImage> fluidStillPart = new(static state => state.Background);
    private readonly Part<BattleRenderState, (AssetSpriteKey.BattleBackground, BattleLightConfiguration), BattleShadowShaderProgram> shadowShaderPart = new(static state => (state.Background, state.LightConfiguration));
    private readonly BattleWorldTransitionShaderProgram transitionShaderProgram = new();

    public BattleRenderContent Get(BattleRenderState state, IAssetProvider assets)
    {
        BattleGround ground = groundPart.Resolve(state, current => Ground.Build(current.Background, assets));
        BattleBackgroundShaderProgram backgroundShaderProgram = backgroundShaderPart.Resolve(state, current => Background.BuildShaderProgram(current.Background, assets));
        IReadOnlyList<BattleFluidShaderProgram> fluidShaderPrograms = fluidShaderPart.Resolve(state, current => Fluid.BuildShaderPrograms(current.Background, current.FluidConfigurations, assets));
        SKImage fluidStill = fluidStillPart.Resolve(state, current => Fluid.BuildStill(current.Background, current.FluidConfigurations, fluidShaderPrograms, assets));
        BattleUnitShaderProgram unitShaderProgram = unitShaderPart.Resolve(state, current => Unit.BuildShaderProgram(current.Background, fluidStill, assets));
        BattleRainShaderProgram rainShaderProgram = rainShaderPart.Resolve(state, current => Rain.BuildShaderProgram(current.Background, current.RainConfiguration, assets));

        BattleShadowShaderProgram shadowShaderProgram = shadowShaderPart.Resolve(state, current => Shadow.BuildShaderProgram(current.LightConfiguration, ground));
        double shadowStrength = Shadow.GetStrength(state.LightConfiguration, ground);

        return new BattleRenderContent(ground, unitShaderProgram, backgroundShaderProgram, fluidShaderPrograms, rainShaderProgram, shadowShaderProgram, transitionShaderProgram, shadowStrength);
    }

    private static SKShader BuildSceneShader(SKImage image, SKRect scene, SKShaderTileMode tileX, SKShaderTileMode tileY)
    {
        SKMatrix imageToScene = SKMatrix.CreateScaleTranslation(
            scene.Width / image.Width,
            scene.Height / image.Height,
            scene.Left,
            scene.Top);

        return image.ToShader(tileX, tileY, RenderSamplingOptions.Linear, imageToScene);
    }

    private static SKImage GetLayer(SKImage? layer, AssetSpriteKey.BattleBackground background, AssetSpriteLayerType type)
    {
        if (layer is null)
        {
            throw new InvalidOperationException($"The background '{background.CanonicalId}' has no layer '{type}'.");
        }

        return layer;
    }
}
