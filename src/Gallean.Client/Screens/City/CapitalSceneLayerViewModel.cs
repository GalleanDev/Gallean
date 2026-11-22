namespace Gallean.Client;

internal sealed record CapitalSceneLayerViewModel(
    AssetSpriteKey? SpriteKey,
    AssetAnimationKey? AnimationKey,
    CapitalBuildingViewModel? Building,
    bool IsConstructed);
