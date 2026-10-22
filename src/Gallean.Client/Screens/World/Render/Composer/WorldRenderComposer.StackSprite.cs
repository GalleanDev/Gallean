namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private readonly record struct StackSprite(
        AssetAnimationKey SpriteKey,
        AssetAnimationKey? ShadowKey,
        AssetAnimationKey? AuraKey);
}
