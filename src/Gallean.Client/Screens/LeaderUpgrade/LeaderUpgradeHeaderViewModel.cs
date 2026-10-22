namespace Gallean.Client;

internal sealed record LeaderUpgradeHeaderViewModel(
    AssetSpriteKey.UnitFace FaceKey,
    string LevelUpText,
    TextSpans PendingUpgradesText,
    UnitDetailsViewModel Details) : IDetailsSource
{
    object? IDetailsSource.DetailsContent => Details;
}
