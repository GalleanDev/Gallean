using Avalonia;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderContentProvider
{
    private static class Rain
    {
        public static BattleRainShaderProgram BuildShaderProgram(
            AssetSpriteKey.BattleBackground background,
            BattleRainConfiguration? configuration,
            IAssetProvider assets)
        {
            using AssetCache<Sprite>.Lease lease = assets.GetRequiredSprite(background);

            Sprite sprite = lease;
            AssetSpriteMetadata metadata = sprite.Metadata;
            SKRect scene = SKRect.Create(metadata.OffsetX, metadata.OffsetY, metadata.Width, metadata.Height);
            SKImage normal = GetLayer(sprite.Normal, background, AssetSpriteLayerType.Normal);
            SKImage depth = GetLayer(sprite.Depth, background, AssetSpriteLayerType.Depth);
            Matrix sceneToGround = BattleRenderLayout.SceneToGroundTransform;

            BattleRainShaderProgram program = new();

            using BattleRainShaderProgram.Editor editor = program.Edit();

            editor.SetNormals(BuildSceneShader(normal, scene, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp));
            editor.SetHeightField(BuildSceneShader(depth, scene, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp));
            editor.SetGroundNormal(BattleRenderLayout.GroundNormal.ToSKPoint3());
            editor.SetSceneToGround(new SKColorF(
                (float)sceneToGround.M11,
                (float)sceneToGround.M21,
                (float)sceneToGround.M12,
                (float)sceneToGround.M22));
            editor.SetGroundOffset(new SKPoint((float)sceneToGround.M31, (float)sceneToGround.M32));
            editor.SetBattlefieldCenter(BattleRenderLayout.BattlefieldCenter.ToSKPoint());
            editor.SetPerspectiveScalePerPixel((float)BattleRenderLayout.PerspectiveScalePerPixel);

            if (configuration is not null)
            {
                editor.SetStreakSpacing(new SKPoint((float)configuration.StreakSpacing.X, (float)configuration.StreakSpacing.Y));
                editor.SetStreakFall((float)configuration.StreakFall);
                editor.SetStreakSlant((float)configuration.StreakSlant);
                editor.SetStreakLength((float)configuration.StreakLength);
                editor.SetStreakWidth((float)configuration.StreakWidth);
                editor.SetStreakChance((float)configuration.StreakChance);
                editor.SetStreakOpacity((float)configuration.StreakOpacity);
                editor.SetDepthSpread((float)configuration.DepthSpread);
                editor.SetDepthFade((float)configuration.DepthFade);
                editor.SetSplashSpacing((float)configuration.SplashSpacing);
                editor.SetSplashRate((float)configuration.SplashRate);
                editor.SetSplashRadius((float)configuration.SplashRadius);
                editor.SetSplashWidth((float)configuration.SplashWidth);
                editor.SetSplashChance((float)configuration.SplashChance);
                editor.SetGroundFacing((float)configuration.GroundFacing);
                editor.SetHaze((float)configuration.Haze);
            }

            return program;
        }
    }
}
