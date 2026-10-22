using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class BattleRenderContentProvider
{
    private static class Background
    {
        public static BattleBackgroundShaderProgram BuildShaderProgram(AssetSpriteKey.BattleBackground background, IAssetProvider assets)
        {
            using AssetCache<Sprite>.Lease lease = assets.GetRequiredSprite(background);

            Sprite sprite = lease;
            AssetSpriteMetadata metadata = sprite.Metadata;
            SKRect scene = SKRect.Create(metadata.OffsetX, metadata.OffsetY, metadata.Width, metadata.Height);
            SKImage normal = GetLayer(sprite.Normal, background, AssetSpriteLayerType.Normal);
            SKImage depth = GetLayer(sprite.Depth, background, AssetSpriteLayerType.Depth);
            SKImage albedo = GetLayer(sprite.Albedo, background, AssetSpriteLayerType.Albedo);

            BattleBackgroundShaderProgram program = new();

            using BattleBackgroundShaderProgram.Editor editor = program.Edit();

            editor.SetSource(BuildSceneShader(sprite.Image, scene, SKShaderTileMode.Mirror, SKShaderTileMode.Clamp));
            editor.SetNormals(BuildSceneShader(normal, scene, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp));
            editor.SetHeightField(BuildSceneShader(depth, scene, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp));
            editor.SetAlbedo(BuildSceneShader(albedo, scene, SKShaderTileMode.Mirror, SKShaderTileMode.Clamp));
            editor.SetGroundNormal(BattleRenderLayout.GroundNormal.ToSKPoint3());

            return program;
        }
    }
}
