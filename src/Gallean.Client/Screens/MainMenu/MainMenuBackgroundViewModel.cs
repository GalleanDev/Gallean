namespace Gallean.Client;

internal sealed record MainMenuBackgroundViewModel(AssetSpriteKey SpriteKey, AssetAnimationKey RiverOverlayKey)
{
    public static MainMenuBackgroundViewModel Create()
    {
        return new MainMenuBackgroundViewModel(
            new AssetSpriteKey.MainMenuBackgroundV2(),
            new AssetAnimationKey.MainMenuBackgroundOverlay(1));
    }
}
